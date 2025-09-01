using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.PromptTemplates.Handlebars;
using Mosaik.Core;
using SemanticKernel.Agents.DatabaseAgent.Extensions;
using SemanticKernel.Agents.DatabaseAgent.Filters;
using SemanticKernel.Agents.DatabaseAgent.Internals;
using SemanticKernel.Reranker.BM25;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Text;
using System.Text.Json;

namespace SemanticKernel.Agents.DatabaseAgent;

internal sealed class DatabasePlugin
{
    private readonly ILogger<DatabasePlugin> _log;
    private readonly KernelFunction _writeSQLFunction;

    private readonly ILoggerFactory? _loggerFactory;

    private readonly IVectorSearchable<TableDefinitionSnippet> _vectorStore;

    private readonly DatabasePluginOptions _options;

    public DatabasePlugin(
        IPromptProvider promptProvider,
        IOptions<DatabasePluginOptions> options,
        IVectorSearchable<TableDefinitionSnippet> vectorStore,
        ILoggerFactory? loggerFactory = null)
    {
        this._options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        this._loggerFactory = loggerFactory;
        this._log = loggerFactory?.CreateLogger<DatabasePlugin>() ?? new NullLogger<DatabasePlugin>();
        this._vectorStore = vectorStore;

        this._writeSQLFunction = KernelFunctionFactory.CreateFromPrompt(executionSettings: new OpenAIPromptExecutionSettings
        {
            MaxTokens = this._options.MaxTokens,
            Temperature = this._options.Temperature,
            TopP = this._options.TopP,
            Seed = 0,
            ResponseFormat = AIJsonUtilities.CreateJsonSchema(typeof(WriteSQLQueryResponse))
        },
        templateFormat: "handlebars",
        promptTemplate: promptProvider.ReadPrompt(AgentPromptConstants.WriteSQLQuery),
        promptTemplateFactory: new HandlebarsPromptTemplateFactory());
    }

    [Description("Execute a query into the database. " +
                 "The query should be formulate in natural language. " +
                 "No worry about the schema, I'll look for you in the database.")]
    [KernelFunction]
    [return: Description("A Markdown representation of the query result.")]
    public async Task<string> ExecuteQueryAsync(Kernel kernel,
                                                [Description("The user query in natural language.")]
                                                string prompt,
                                                [Description("The original query, used for debugging purposes.")]
                                                string originalQuery,
                                                CancellationToken cancellationToken)
    {
        try
        {
            var connection = kernel.GetRequiredService<DbConnection>();
            var textEmbeddingService = kernel.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();

            var embeddings = await textEmbeddingService.GenerateVectorAsync(prompt, cancellationToken: cancellationToken)
                                                                        .ConfigureAwait(false);

            var ranker = new BM25Reranker(supportedLanguages: [Language.English]);

            var relatedTables = this._vectorStore.SearchAsync(embeddings, top: int.Min(this._options.TopK * 5, 100), cancellationToken: cancellationToken);

            var ranked = ranker.RankAsync(prompt, relatedTables, r => r.Description, topN: this._options.TopK);

            var tableDefinitionsSb = new StringBuilder();

            await foreach (var relatedTable in ranked)
            {

                tableDefinitionsSb.AppendLine(relatedTable.Result.Record.Description);
                tableDefinitionsSb.AppendLine();
                tableDefinitionsSb.AppendLine("---");
                tableDefinitionsSb.AppendLine();
            }

            var tableDefinitions = tableDefinitionsSb.ToString();

            var sqlQuery = string.Empty;

            using var dataTable = await RetryHelper.Try<DataTable>(async (e) =>
            {
                sqlQuery = await GetSQLQueryStringAsync(kernel,
                                                        prompt, tableDefinitions,
                                                        cancellationToken: cancellationToken,
                                                        previousSQLException: e,
                                                        previousSQLQuery: sqlQuery)
                                            .ConfigureAwait(false);

                if (string.IsNullOrWhiteSpace(sqlQuery))
                {
                    this._log.LogWarning("SQL query is empty for prompt: {prompt}", prompt);
                    throw new InvalidOperationException("The kernel was unable to generate the expected query.");
                }

                this._log.LogInformation("SQL query generated: {sqlQuery}", sqlQuery);

                var queryExecutionContext = new QueryExecutionContext(kernel, originalQuery, tableDefinitions, sqlQuery, cancellationToken);

                (bool isQueryExecutionFiltered, string filterMessage) = await InvokeFiltersOrQueryAsync(kernel.GetAllServices<IQueryExecutionFilter>().ToList(),
                                         _ =>
                                         {
                                             return Task.FromResult((false, string.Empty));
                                         },
                                        queryExecutionContext)
                                    .ConfigureAwait(false);

                if (isQueryExecutionFiltered)
                {
                    throw new Exception($"Query execution was filtered: {filterMessage}");
                }

                return await QueryExecutor.ExecuteSQLAsync(connection, sqlQuery, this._loggerFactory, cancellationToken)
                                                         .ConfigureAwait(false);
            }, count: 3, _loggerFactory, cancellationToken)
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
                                                      string? previousSQLQuery = null,
                                                      Exception? previousSQLException = null,
                                                      CancellationToken? cancellationToken = null)
    {
        var arguments = new KernelArguments()
        {
            { "prompt", prompt },
            { "tablesDefinition", tablesDefinitions },
            { "previousAttempt", previousSQLQuery },
            { "previousException", previousSQLException?.Message },
            { "providerName", kernel.GetRequiredService<DbConnection>().GetProviderName() }
        };

        this._log.LogInformation("Write SQL query for: {prompt}", prompt);

        var functionResult = await this._writeSQLFunction.InvokeAsync(kernel, arguments, cancellationToken ?? CancellationToken.None)
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
