using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.KernelMemory;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using SemanticKernel.Agents.DatabaseAgent.Extensions;
using SemanticKernel.Agents.DatabaseAgent.Internals;
using System.ComponentModel;
using System.Data.Common;

namespace SemanticKernel.Agents.DatabaseAgent;

public class DBMSPlugin
{
    private readonly ILogger<DBMSPlugin> _log;
    private readonly KernelFunction _writeSQLFunction;

    private readonly IKernelMemory _kernelMemory;
    private readonly ILoggerFactory? _loggerFactory;

    public DBMSPlugin(IKernelMemory kernelMemory, ILoggerFactory? loggerFactory = null)
    {
        this._loggerFactory = loggerFactory;
        this._log = loggerFactory?.CreateLogger<DBMSPlugin>() ?? new NullLogger<DBMSPlugin>();
        this._kernelMemory = kernelMemory;

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
                                                [Description("The prompt in natural language.")]
                                                string prompt,
                                                CancellationToken cancellationToken)
    {
        var connection = kernel.GetRequiredService<DbConnection>();

        var relatedTables = await _kernelMemory.SearchAsync(prompt, minRelevance: 1, cancellationToken: cancellationToken)
                                            .ConfigureAwait(false);

        var tableDefinitions = string.Join(Environment.NewLine, relatedTables.Results.Select(c => c.Partitions.First().Tags["definition"].First()));

        var sqlQuery = await GetSQLQueryStringAsync(kernel, prompt, tableDefinitions, cancellationToken)
                                .ConfigureAwait(false);

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
}
