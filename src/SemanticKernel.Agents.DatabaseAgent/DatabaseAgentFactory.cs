using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Embeddings;
using SemanticKernel.Agents.DatabaseAgent.Extensions;
using SemanticKernel.Agents.DatabaseAgent.Internals;
using System.Data;
using System.Data.Common;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace SemanticKernel.Agents.DatabaseAgent;

public static class DatabaseAgentFactory
{
    private static PromptExecutionSettings promptExecutionSettings = new OpenAIPromptExecutionSettings
    {
        MaxTokens = 4096,
        Temperature = .1E-9,
        TopP = .1E-9,
#pragma warning disable SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        ResponseFormat = "json_object"
#pragma warning restore SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    };

    public static async Task<DatabaseKernelAgent> CreateAgentAsync(
            Kernel kernel,
            ILoggerFactory loggingFactory,
            CancellationToken? cancellationToken = null)
    {
        var vectorStore = kernel.Services.GetService<IVectorStoreRecordCollection<Guid, TableDefinitionSnippet>>();

        if (vectorStore is null)
        {
            throw new InvalidOperationException("The kernel does not have a vector store for table definitions.");
        }

        var agentStore = kernel.Services.GetService<IVectorStoreRecordCollection<Guid, AgentDefinitionSnippet>>();

        if (agentStore is null)
        {
            throw new InvalidOperationException("The kernel does not have a vector store for agent.");
        }

        await vectorStore.CreateCollectionIfNotExistsAsync()
                         .ConfigureAwait(false);

        await agentStore.CreateCollectionIfNotExistsAsync()
                         .ConfigureAwait(false);

        return await BuildAgentAsync(kernel, loggingFactory, cancellationToken ?? CancellationToken.None)
                            .ConfigureAwait(false);
    }

    private static async Task<DatabaseKernelAgent> BuildAgentAsync(Kernel kernel, ILoggerFactory loggingFactory, CancellationToken cancellationToken)
    {
        var agentKernel = kernel.Clone();

        var existingDefinition = await kernel.GetRequiredService<IVectorStoreRecordCollection<Guid, AgentDefinitionSnippet>>()
                                            .GetAsync(Guid.Empty)
                                            .ConfigureAwait(false);

        loggingFactory.CreateLogger<DatabaseKernelAgent>()
            .LogInformation("Agent definition: {Definition}", existingDefinition);

        if (existingDefinition is not null)
        {
            return new DatabaseKernelAgent
            {
                Kernel = agentKernel,
                Name = existingDefinition.AgentName,
                Description = existingDefinition.Description,
                Instructions = existingDefinition.Instructions
            };
        }

        loggingFactory.CreateLogger<DatabaseKernelAgent>()
            .LogInformation("Creating a new agent definition.");

        var tableDescriptions = await MemorizeAgentSchema(kernel, cancellationToken);

        var promptProvider = kernel.GetRequiredService<IPromptProvider>() ?? new EmbeddedPromptProvider();

        var agentDescription = await KernelFunctionFactory.CreateFromPrompt(promptProvider.ReadPrompt(AgentPromptConstants.AgentDescriptionGenerator), promptExecutionSettings, functionName: AgentPromptConstants.AgentDescriptionGenerator)
                                        .InvokeAsync(kernel, new KernelArguments
                                        {
                                            { "tableDefinitions", tableDescriptions }
                                        })
                                        .ConfigureAwait(false);

        loggingFactory.CreateLogger<DatabaseKernelAgent>()
            .LogInformation("Agent description: {Description}", agentDescription.GetValue<string>()!);

        var agentName = await KernelFunctionFactory.CreateFromPrompt(promptProvider.ReadPrompt(AgentPromptConstants.AgentNameGenerator), promptExecutionSettings, functionName: AgentPromptConstants.AgentNameGenerator)
                                        .InvokeAsync(kernel, new KernelArguments
                                        {
                                            { "agentDescription", agentDescription.GetValue<string>()! }
                                        })
                                        .ConfigureAwait(false);

        loggingFactory.CreateLogger<DatabaseKernelAgent>()
            .LogInformation("Agent name: {Name}", agentName.GetValue<string>()!);

        var agentInstructions = await KernelFunctionFactory.CreateFromPrompt(promptProvider.ReadPrompt(AgentPromptConstants.AgentInstructionsGenerator), promptExecutionSettings, functionName: AgentPromptConstants.AgentInstructionsGenerator)
                                        .InvokeAsync(kernel, new KernelArguments
                                        {
                                            { "agentDescription", agentDescription.GetValue<string>()! }
                                        })
                                        .ConfigureAwait(false);

        loggingFactory.CreateLogger<DatabaseKernelAgent>()
            .LogInformation("Agent instructions: {Instructions}", agentInstructions.GetValue<string>()!);

        var agentDefinition = new AgentDefinitionSnippet
        {
            Key = Guid.Empty,
            AgentName = JsonSerializer.Deserialize<AgentNameRespone>(agentName.GetValue<string>()!)!.Name,
            Description = JsonSerializer.Deserialize<AgentDescriptionResponse>(agentDescription.GetValue<string>()!)!.Description,
            Instructions = JsonSerializer.Deserialize<AgentInstructionsResponse>(agentInstructions.GetValue<string>()!)!.Instructions
        };

        agentDefinition.TextEmbedding = await kernel.GetRequiredService<ITextEmbeddingGenerationService>()
                                                        .GenerateEmbeddingAsync(agentDefinition.Description)
                                                        .ConfigureAwait(false);

        _ = await kernel.GetRequiredService<IVectorStoreRecordCollection<Guid, AgentDefinitionSnippet>>()
                        .UpsertAsync(agentDefinition, cancellationToken)
                        .ConfigureAwait(false);

        return new DatabaseKernelAgent()
        {
            Kernel = agentKernel,
            Name = agentDefinition.AgentName,
            Description = agentDefinition.Description,
            Instructions = agentDefinition.Instructions
        };
    }

    private static async Task<string> MemorizeAgentSchema(Kernel kernel, CancellationToken cancellationToken)
    {
        var stringBuilder = new StringBuilder();

        var descriptions = GetTablesDescription(kernel, GetTablesAsync(kernel, cancellationToken), cancellationToken)
                                                                .ConfigureAwait(false);

        var embeddingTextGenerator = kernel.GetRequiredService<ITextEmbeddingGenerationService>();

        await foreach (var (tableName, definition, description, dataSample) in descriptions)
        {
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(description));

            await kernel.GetRequiredService<IVectorStoreRecordCollection<Guid, TableDefinitionSnippet>>()
                            .UpsertAsync(new TableDefinitionSnippet
                            {
                                Key = Guid.NewGuid(),
                                TableName = tableName,
                                Definition = definition,
                                Description = description,
                                SampleData = dataSample,
                                TextEmbedding = await embeddingTextGenerator.GenerateEmbeddingAsync(description, cancellationToken: cancellationToken)
                                                                                                    .ConfigureAwait(false)
                            })
                            .ConfigureAwait(false);

            stringBuilder.AppendLine(description);
        }

        return stringBuilder.ToString();
    }

    private static async IAsyncEnumerable<string> GetTablesAsync(Kernel kernel, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var connection = kernel.GetRequiredService<DbConnection>();
        var promptProvider = kernel.GetRequiredService<IPromptProvider>() ?? new EmbeddedPromptProvider();
        var sqlWriter = KernelFunctionFactory.CreateFromPrompt(promptProvider.ReadPrompt(AgentPromptConstants.WriteSQLQuery), promptExecutionSettings, functionName: AgentPromptConstants.WriteSQLQuery);

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

        var response = JsonSerializer.Deserialize<WriteSQLQueryResponse>(tablesGenerator.GetValue<string>()!)!;

        using var reader = await QueryExecutor.ExecuteSQLAsync(connection, response.Query, null, cancellationToken)
                                        .ConfigureAwait(false);

        foreach (DataRow row in reader!.Rows)
        {
            yield return MarkdownRenderer.Render(row);
        }
    }

    private static async IAsyncEnumerable<(string tableName, string tableDefinition, string tableDescription, string dataSample)> GetTablesDescription(Kernel kernel, IAsyncEnumerable<string> tables, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var connection = kernel.GetRequiredService<DbConnection>();
        var promptProvider = kernel.GetRequiredService<IPromptProvider>() ?? new EmbeddedPromptProvider();
        var sqlWriter = KernelFunctionFactory.CreateFromPrompt(promptProvider.ReadPrompt(AgentPromptConstants.WriteSQLQuery), promptExecutionSettings, functionName: AgentPromptConstants.WriteSQLQuery);
        var tableDescriptionGenerator = KernelFunctionFactory.CreateFromPrompt(promptProvider.ReadPrompt(AgentPromptConstants.ExplainTable), promptExecutionSettings, functionName: AgentPromptConstants.ExplainTable);
        var defaultKernelArguments = new KernelArguments
            {
                { "providerName", connection.GetProviderName() }
            };

        StringBuilder sb = new StringBuilder();

        await foreach (var item in tables)
        {
            var tableNameResponse = await kernel.InvokePromptAsync(promptProvider.ReadPrompt(AgentPromptConstants.ExtractTableName),
                                    new KernelArguments(defaultKernelArguments)
                                    {
                                        { "item", item }
                                    }, cancellationToken: cancellationToken)
                                    .ConfigureAwait(false);

            var tableName = tableNameResponse.GetValue<string>();

            var existingRecordSearch = await kernel.GetRequiredService<IVectorStoreRecordCollection<Guid, TableDefinitionSnippet>>()
                                                    .VectorizedSearchAsync(await kernel.GetRequiredService<ITextEmbeddingGenerationService>()
                                                        .GenerateEmbeddingAsync(item)
                                                        .ConfigureAwait(false))
                                                    .ConfigureAwait(false);

            var existingRecord = await existingRecordSearch.Results.FirstOrDefaultAsync(c => c.Record.TableName == tableName)
                                                .ConfigureAwait(false);

            if (existingRecord is not null)
            {
                yield return (tableName!, existingRecord.Record.Definition!, existingRecord.Record.Description!, existingRecord.Record.SampleData!);
                continue;
            }

            var definition = await sqlWriter.InvokeAsync(kernel, new KernelArguments(defaultKernelArguments)
                {
                    { "prompt", $"Show the current structure of '{tableName}'" }
                }, cancellationToken)
                .ConfigureAwait(false);

            var tableStructureResponse = JsonSerializer.Deserialize<WriteSQLQueryResponse>(definition.GetValue<string>()!)!;

            var tableDefinition = MarkdownRenderer.Render(await QueryExecutor.ExecuteSQLAsync(connection, tableStructureResponse.Query, null, cancellationToken)
                                            .ConfigureAwait(false));

            var extract = await sqlWriter.InvokeAsync(kernel, new KernelArguments(defaultKernelArguments)
                {
                    { "prompt", $"Get the first 5 rows for '{tableName}'" }
                }, cancellationToken)
                    .ConfigureAwait(false);

            var tableDataStructure = JsonSerializer.Deserialize<WriteSQLQueryResponse>(extract.GetValue<string>()!)!;

            var tableExtract = MarkdownRenderer.Render(await QueryExecutor.ExecuteSQLAsync(connection, tableDataStructure.Query, null, cancellationToken)
                                            .ConfigureAwait(false));

            var description = await tableDescriptionGenerator.InvokeAsync(kernel, new KernelArguments
                                    {
                                        { "tableDefinition", definition },
                                        { "tableDataExtract", tableExtract }
                                    })
                                    .ConfigureAwait(false);

            var tableExplain = JsonSerializer.Deserialize<ExplainTableResponse>(description.GetValue<string>()!)!;

            yield return (tableName, tableDefinition, tableExplain.Description, tableExtract)!;
        }
    }
}
