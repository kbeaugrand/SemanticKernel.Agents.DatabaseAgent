using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Data;
using Microsoft.SemanticKernel.Embeddings;
using SemanticKernel.Agents.DatabaseAgent.Extensions;
using SemanticKernel.Agents.DatabaseAgent.Filters;
using SemanticKernel.Agents.DatabaseAgent.Internals;
using System.ComponentModel;
using System.Data;
using System.Data.Common;

namespace SemanticKernel.Agents.DatabaseAgent;

public class DatabasePlugin
{
    private readonly ILogger<DatabasePlugin> _log;
    private readonly KernelFunction _writeSQLFunction;

    private readonly ILoggerFactory? _loggerFactory;

    private readonly IVectorStoreRecordCollection<string, TableDefinitionSnippet> _vectorStore;

    public DatabasePlugin(IVectorStoreRecordCollection<string, TableDefinitionSnippet> vectorStore, ILoggerFactory? loggerFactory = null)
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
        var connection = kernel.GetRequiredService<DbConnection>();
        var textEmbeddingService = kernel.GetRequiredService<ITextEmbeddingGenerationService>();

        var embeddings = textEmbeddingService.GenerateEmbeddingAsync(prompt, cancellationToken: cancellationToken)
                                            .ConfigureAwait(false);

        var relatedTables = await this._vectorStore.VectorizedSearchAsync(embeddings, new VectorSearchOptions<TableDefinitionSnippet>
                                                            {
                                                                // TODO: Add a threshold for the search
                                                            }, cancellationToken: cancellationToken)
                                                    .ConfigureAwait(false);

        var tableDefinitions = string.Join(Environment.NewLine, relatedTables.Results.Select(c => c.Record.Definition));

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
