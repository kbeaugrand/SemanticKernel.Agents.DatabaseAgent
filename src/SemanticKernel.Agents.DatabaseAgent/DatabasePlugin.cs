using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Embeddings;
using SemanticKernel.Agents.DatabaseAgent.Extensions;
using SemanticKernel.Agents.DatabaseAgent.Filters;
using SemanticKernel.Agents.DatabaseAgent.Internals;
using System.ComponentModel;
using System.Data.Common;
using System.Text;
using System.Text.Json;

namespace SemanticKernel.Agents.DatabaseAgent;

internal sealed class DatabasePlugin
{
    private readonly ILogger<DatabasePlugin> _log;
    private readonly KernelFunction _writeSQLFunction;

    private readonly ILoggerFactory? _loggerFactory;

    private readonly IVectorStoreRecordCollection<Guid, TableDefinitionSnippet> _vectorStore;

    private readonly DatabasePluginOptions _options;

    public DatabasePlugin(
        IOptions<DatabasePluginOptions> options,
        IVectorStoreRecordCollection<Guid, TableDefinitionSnippet> vectorStore,
        ILoggerFactory? loggerFactory = null)
    {
        this._options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        this._loggerFactory = loggerFactory;
        this._log = loggerFactory?.CreateLogger<DatabasePlugin>() ?? new NullLogger<DatabasePlugin>();
        this._vectorStore = vectorStore;

        this._writeSQLFunction = KernelFunctionFactory.CreateFromPrompt(EmbeddedPromptProvider.ReadPrompt("WriteSQLQuery"), new OpenAIPromptExecutionSettings
        {
            MaxTokens = this._options.MaxTokens,
            Temperature = this._options.Temperature,
            TopP = this._options.TopP,
            Seed = 0,
#pragma warning disable SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            ResponseFormat = "json_object"
#pragma warning restore SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
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

            var relatedTables = await this._vectorStore.VectorizedSearchAsync(embeddings, options: new()
            {
                Top = this._options.TopK
            }, cancellationToken: cancellationToken)
                   .ConfigureAwait(false);

            var tableDefinitionsSb = new StringBuilder();

            await foreach (var relatedTable in relatedTables.Results)
            {
                tableDefinitionsSb.AppendLine($"## {relatedTable.Record.TableName}");
                tableDefinitionsSb.AppendLine(relatedTable.Record.Definition);
            }

            var tableDefinitions = tableDefinitionsSb.ToString();

            var sqlQuery = await GetSQLQueryStringAsync(kernel, prompt, tableDefinitions, cancellationToken)
                                            .ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(sqlQuery))
            {
                this._log.LogWarning("SQL query is empty for prompt: {prompt}", prompt);
                throw new InvalidOperationException("The kernel was unable to generate the expected query.");
            }

            var queryExecutionContext = new QueryExecutionContext(kernel, prompt, tableDefinitions, sqlQuery, cancellationToken);

            (bool isQueryExecutionFiltered, string filterMessage) = await InvokeFiltersOrQueryAsync(kernel.GetAllServices<IQueryExecutionFilter>().ToList(),
                                     _ =>
                                    {
                                        return Task.FromResult((false, string.Empty));
                                    },
                                    queryExecutionContext)
                                .ConfigureAwait(false);

            if (isQueryExecutionFiltered)
            {
                return filterMessage;
            }

            using var dataTable = await QueryExecutor.ExecuteSQLAsync(connection, sqlQuery, this._loggerFactory, cancellationToken)
                                                     .ConfigureAwait(false);

            var result = MarkdownRenderer.Render(dataTable);

            this._log.LogInformation("Query result: {result}", result);

            return result;
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

        return JsonSerializer.Deserialize<WriteSQLQueryResponse>(functionResult.GetValue<string>()!)!.Query!;
    }

    private static async Task<(bool, string)> InvokeFiltersOrQueryAsync(
        List<IQueryExecutionFilter>? functionFilters,
        Func<QueryExecutionContext, Task<(bool, string)>> functionCallback,
        QueryExecutionContext context,
        int index = 0)
    {
        if (functionFilters is { Count: > 0 } && index < functionFilters.Count)
        {
            return await functionFilters[index].OnQueryExecutionAsync(context,
                (context) => InvokeFiltersOrQueryAsync(functionFilters, functionCallback, context, index + 1)).ConfigureAwait(false);
        }
        else
        {
            return await functionCallback(context).ConfigureAwait(false);
        }
    }
}
