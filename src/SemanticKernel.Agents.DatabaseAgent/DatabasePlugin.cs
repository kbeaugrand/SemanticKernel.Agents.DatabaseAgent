using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Embeddings;
using SemanticKernel.Agents.DatabaseAgent.Extensions;
using SemanticKernel.Agents.DatabaseAgent.Filters;
using SemanticKernel.Agents.DatabaseAgent.Internals;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Text;

namespace SemanticKernel.Agents.DatabaseAgent;

public class DatabasePlugin
{
    private readonly ILogger<DatabasePlugin> _log;
    private readonly KernelFunction _writeSQLFunction;

    private readonly ILoggerFactory? _loggerFactory;

    private readonly IVectorStoreRecordCollection<Guid, TableDefinitionSnippet> _vectorStore;

    public DatabasePlugin(IVectorStoreRecordCollection<Guid, TableDefinitionSnippet> vectorStore, ILoggerFactory? loggerFactory = null)
    {
        this._loggerFactory = loggerFactory;
        this._log = loggerFactory?.CreateLogger<DatabasePlugin>() ?? new NullLogger<DatabasePlugin>();
        this._vectorStore = vectorStore;

        this._writeSQLFunction = KernelFunctionFactory.CreateFromPrompt(EmbeddedPromptProvider.ReadPrompt("WriteSQLQuery"), new OpenAIPromptExecutionSettings
        {
            MaxTokens = 4096,
            Temperature = 0.1,
            TopP = 0.1
        });
    }

    [Description("Execute a query into the database. " +
                 "The query should be formulate in natural language. " +
                 "No worry about the schema, I'll look for you in the database.")]
    [KernelFunction]
    [return: Description("A Markdown representation of the query result.")]
    public async Task<string> ExecuteQueryAsync(Kernel kernel,
                                                [Description("The user query in natural language.")]
                                                string prompt,
                                                CancellationToken cancellationToken)
    {
        try
        {
            var connection = kernel.GetRequiredService<DbConnection>();
            var textEmbeddingService = kernel.GetRequiredService<ITextEmbeddingGenerationService>();

            var embeddings = await textEmbeddingService.GenerateEmbeddingAsync(prompt, cancellationToken: cancellationToken)
                                                                        .ConfigureAwait(false);

            var relatedTables = await this._vectorStore.VectorizedSearchAsync(embeddings, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            var tableDefinitionsSb = new StringBuilder();

            await foreach(var relatedTable in relatedTables.Results)
            {
                tableDefinitionsSb.AppendLine(relatedTable.Record.Definition);
            }

            var tableDefinitions = tableDefinitionsSb.ToString();


            var sqlQuery = await GetSQLQueryStringAsync(kernel, prompt, tableDefinitions, cancellationToken)
                            .ConfigureAwait(false);

            var queryExecutionContext = new QueryExecutionContext(kernel, prompt, tableDefinitions, sqlQuery, cancellationToken);

            bool isQueryExecutionFiltered = true;

            await InvokeFiltersOrQueryAsync(kernel.GetAllServices<IQueryExecutionFilter>().ToList(),
                                     _ =>
                                    {
                                        isQueryExecutionFiltered = false;
                                        return Task.CompletedTask;
                                    },
                                    queryExecutionContext)
                                .ConfigureAwait(false);

            if (isQueryExecutionFiltered)
            {
                return "Query execution was filtered.";
            }

            using var dataTable = await QueryExecutor.ExecuteSQLAsync(connection, sqlQuery, this._loggerFactory, cancellationToken)
                                                     .ConfigureAwait(false);

            return MarkdownRenderer.Render(dataTable);
        }
        catch (Exception e)
        {
            this._log.LogError(e, "Error executing query: {prompt}", prompt);
            throw;
        }
    }

    private async Task<string> GetSQLQueryStringAsync(Kernel kernel,
                                                              string prompt,
                                                              string tablesDefinitions,
                                            CancellationToken cancellationToken)
    {
        var arguments = new KernelArguments()
        {
            { "prompt", prompt },
            { "tablesDefinition", tablesDefinitions },
            { "providerName", kernel.GetRequiredService<DbConnection>().GetProviderName() }
        };

        this._log.LogInformation("Write SQL query for: {prompt}", prompt);

        var functionResult = await this._writeSQLFunction.InvokeAsync(kernel, arguments, cancellationToken)
                                                         .ConfigureAwait(false);

        return functionResult.GetValue<string>()!;
    }

    private static async Task InvokeFiltersOrQueryAsync(
        List<IQueryExecutionFilter>? functionFilters,
        Func<QueryExecutionContext, Task> functionCallback,
        QueryExecutionContext context,
        int index = 0)
    {
        if (functionFilters is { Count: > 0 } && index < functionFilters.Count)
        {
            await functionFilters[index].OnQueryExecutionAsync(context,
                (context) => InvokeFiltersOrQueryAsync(functionFilters, functionCallback, context, index + 1)).ConfigureAwait(false);
        }
        else
        {
            await functionCallback(context).ConfigureAwait(false);
        }
    }
}
