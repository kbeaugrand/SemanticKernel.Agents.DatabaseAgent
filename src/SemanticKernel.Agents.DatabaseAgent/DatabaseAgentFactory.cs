using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.KernelMemory;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using SemanticKernel.Agents.DatabaseAgent.Extensions;
using SemanticKernel.Agents.DatabaseAgent.Internals;
using System.Data;
using System.Data.Common;
using System.Runtime.CompilerServices;
using System.Text;

namespace SemanticKernel.Agents.DatabaseAgent;

public static class DatabaseAgentFactory
{
    private static PromptExecutionSettings promptExecutionSettings = new OpenAIPromptExecutionSettings
    {
        MaxTokens = 4096,
        Temperature = 0.1,
        TopP = 0.1
    };

    private static string _agentDescriptionPrompt = EmbeddedPromptProvider.ReadPrompt("AgentDescriptionGenerator");
    private static string _agentInstructionsPrompt = EmbeddedPromptProvider.ReadPrompt("AgentInstructionsGenerator");
    private static string _agentNamePrompt = EmbeddedPromptProvider.ReadPrompt("AgentNameGenerator");
    private static string _tableDescriptionPrompt = EmbeddedPromptProvider.ReadPrompt("ExplainTable");
    private static string _writeSQLQueryPrompt = EmbeddedPromptProvider.ReadPrompt("WriteSQLQuery");

    public static async Task<DatabaseKernelAgent> CreateAgentAsync(
            Kernel kernel,
            IKernelMemory memory,
            CancellationToken? cancellationToken = null)
    {
        var tableDescriptions = await MemorizeAgentSchema(kernel, memory, cancellationToken ?? CancellationToken.None);

        return await BuildAgentAsync(kernel, tableDescriptions, memory, cancellationToken ?? CancellationToken.None)
                            .ConfigureAwait(false);
    }

    private static async Task<DatabaseKernelAgent> BuildAgentAsync(Kernel kernel, string tableDescriptions, IKernelMemory memory, CancellationToken cancellationToken)
    {
        var agentDescription = await KernelFunctionFactory.CreateFromPrompt(_agentDescriptionPrompt, promptExecutionSettings)
                                        .InvokeAsync(kernel, new KernelArguments
                                        {
                                            { "tableDefinitions", tableDescriptions }
                                        })
                                        .ConfigureAwait(false);

        var agentName = await KernelFunctionFactory.CreateFromPrompt(_agentNamePrompt, promptExecutionSettings)
                                        .InvokeAsync(kernel, new KernelArguments
                                        {
                                            { "agentDescription", agentDescription.GetValue<string>()! }
                                        })
                                        .ConfigureAwait(false);

        var agentInstructions = await KernelFunctionFactory.CreateFromPrompt(_agentInstructionsPrompt, promptExecutionSettings)
                                        .InvokeAsync(kernel, new KernelArguments
                                        {
                                            { "agentDescription", agentDescription.GetValue<string>()! }
                                        })
                                        .ConfigureAwait(false);

        var agentKernel = kernel.Clone();

        agentKernel.ImportPluginFromObject(new DatabasePlugin(memory, NullLoggerFactory.Instance));

        return new DatabaseKernelAgent
        {
            Kernel = agentKernel,
            Name = agentName.GetValue<string>(),
            Description = agentDescription.GetValue<string>(),
            Instructions = agentInstructions.GetValue<string>()
        };
    }

    private static async Task<string> MemorizeAgentSchema(Kernel kernel, IKernelMemory memory, CancellationToken cancellationToken)
    {
        var stringBuilder = new StringBuilder();

        var descriptions = GetTablesDescription(kernel, GetTablesAsync(kernel, memory, cancellationToken), cancellationToken)
                                                                .ConfigureAwait(false);

        await foreach (var (tableName, definition, description) in descriptions)
        {
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(description));

            await memory.ImportDocumentAsync(new Document(tableName)
                            .AddStream("table.txt", stream)
                            .AddTag("definition", definition),
                            cancellationToken: cancellationToken)
                        .ConfigureAwait(false);

            stringBuilder.AppendLine(description);
        }

        return stringBuilder.ToString();
    }

    private static async IAsyncEnumerable<(string tableName, string tableDefinition)> GetTablesAsync(Kernel kernel, IKernelMemory memory, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var connection = kernel.GetRequiredService<DbConnection>();
        var sqlWriter = KernelFunctionFactory.CreateFromPrompt(_writeSQLQueryPrompt, promptExecutionSettings);

        var defaultKernelArguments = new KernelArguments
            {
                { "providerName", connection.GetProviderName() },
                { "tablesDefinitions", "" }
            };

        var tablesGenerator = await sqlWriter.InvokeAsync(kernel, new KernelArguments(defaultKernelArguments)
            {
                { "prompt", "List all available tables" }
            })
            .ConfigureAwait(false);

        using var reader = await QueryExecutor.ExecuteSQLAsync(connection, tablesGenerator.GetValue<string>()!, null, cancellationToken)
                            .ConfigureAwait(false);

        foreach (DataRow row in reader!.Rows)
        {
            if (!memory.IsDocumentReadyAsync(row[0].ToString()!,  cancellationToken: cancellationToken).Result)
            {
                continue;
            }

            var tableDefinitionScript = await sqlWriter.InvokeAsync(kernel, new KernelArguments(defaultKernelArguments)
                {
                    { "prompt", $"What is the current CREATE statements for table '{row[0].ToString()}'" }
                }, cancellationToken)
                .ConfigureAwait(false);

            var tableDefinition = MarkdownRenderer.Render(await QueryExecutor.ExecuteSQLAsync(connection, tableDefinitionScript.GetValue<string>()!, null, cancellationToken)
                                            .ConfigureAwait(false));

            yield return (row[0].ToString()!, tableDefinition);
        }
    }

    private static async IAsyncEnumerable<(string tableName, string tableDefinition, string tableDescription)> GetTablesDescription(Kernel kernel, IAsyncEnumerable<(string tableName, string tableDefinition)> values, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var tableDescriptionGenerator = KernelFunctionFactory.CreateFromPrompt(_tableDescriptionPrompt, promptExecutionSettings);

        StringBuilder sb = new StringBuilder();

        await foreach (var item in values)
        {
            var definition = $"{item.tableName}:\n{item.tableDefinition}";

            var description = await tableDescriptionGenerator.InvokeAsync(kernel, new KernelArguments
                                    {
                                        { "tableDefinition", definition }
                                    })
                                    .ConfigureAwait(false);

            yield return (item.tableName, definition, $"{item.tableName}:\n{description.GetValue<string>()}")!;
        }
    }
}
