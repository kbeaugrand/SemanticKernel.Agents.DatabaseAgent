using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
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
        var vectorStore = kernel.Services.GetService<VectorStoreCollection<Guid, TableDefinitionSnippet>>();

        if (vectorStore is null)
        {
            throw new InvalidOperationException("The kernel does not have a vector store for table definitions.");
        }

        var agentStore = kernel.Services.GetService<VectorStoreCollection<Guid, AgentDefinitionSnippet>>();

        if (agentStore is null)
        {
            throw new InvalidOperationException("The kernel does not have a vector store for agent.");
        }

        await vectorStore.EnsureCollectionExistsAsync()
                         .ConfigureAwait(false);

        await agentStore.EnsureCollectionExistsAsync()
                         .ConfigureAwait(false);

        return await BuildAgentAsync(kernel, loggingFactory, cancellationToken ?? CancellationToken.None)
                            .ConfigureAwait(false);
    }

    private static async Task<DatabaseKernelAgent> BuildAgentAsync(Kernel kernel, ILoggerFactory loggingFactory, CancellationToken cancellationToken)
    {
        var agentKernel = kernel.Clone();

        var existingDefinition = await kernel.GetRequiredService<VectorStoreCollection<Guid, AgentDefinitionSnippet>>()
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

        var tableDescriptions = await MemorizeAgentSchema(kernel, loggingFactory, cancellationToken);

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

        await kernel.GetRequiredService<VectorStoreCollection<Guid, AgentDefinitionSnippet>>()
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

    private static async Task<string> MemorizeAgentSchema(Kernel kernel, ILoggerFactory loggerFactory, CancellationToken cancellationToken)
    {
        var stringBuilder = new StringBuilder();

        var descriptions = GetTablesDescription(kernel, GetTablesAsync(kernel, cancellationToken), loggerFactory, cancellationToken)
                                                                .ConfigureAwait(false);

        var embeddingTextGenerator = kernel.GetRequiredService<ITextEmbeddingGenerationService>();

        await foreach (var (tableName, definition, description, dataSample) in descriptions)
        {
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(description));

            await kernel.GetRequiredService<VectorStoreCollection<Guid, TableDefinitionSnippet>>()
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

        using var reader = await Try(async (e) =>
        {
            var tablesGenerator = await sqlWriter.InvokeAsync(kernel, new KernelArguments(defaultKernelArguments)
            {
                { "prompt", "List all available tables" }
            })
            .ConfigureAwait(false);

            var response = JsonSerializer.Deserialize<WriteSQLQueryResponse>(tablesGenerator.GetValue<string>()!)!;

            return await QueryExecutor.ExecuteSQLAsync(connection, response.Query, null, cancellationToken)
                                         .ConfigureAwait(false);
        }, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        foreach (DataRow row in reader!.Rows)
        {
            yield return MarkdownRenderer.Render(row);
        }
    }

    private static async IAsyncEnumerable<(string tableName, string tableDefinition, string tableDescription, string dataSample)> GetTablesDescription(Kernel kernel, IAsyncEnumerable<string> tables, ILoggerFactory loggerFactory, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var connection = kernel.GetRequiredService<DbConnection>();
        var promptProvider = kernel.GetRequiredService<IPromptProvider>() ?? new EmbeddedPromptProvider();
        var sqlWriter = KernelFunctionFactory.CreateFromPrompt(promptProvider.ReadPrompt(AgentPromptConstants.WriteSQLQuery), promptExecutionSettings, functionName: AgentPromptConstants.WriteSQLQuery);
        var extractTableName = KernelFunctionFactory.CreateFromPrompt(promptProvider.ReadPrompt(AgentPromptConstants.ExtractTableName), promptExecutionSettings, functionName: AgentPromptConstants.ExtractTableName);
        var tableDescriptionGenerator = KernelFunctionFactory.CreateFromPrompt(promptProvider.ReadPrompt(AgentPromptConstants.ExplainTable), promptExecutionSettings, functionName: AgentPromptConstants.ExplainTable);
        var defaultKernelArguments = new KernelArguments
            {
                { "providerName", connection.GetProviderName() }
            };

        StringBuilder sb = new StringBuilder();

        var logger = loggerFactory?.CreateLogger(nameof(DatabaseAgentFactory)) ?? NullLoggerFactory.Instance.CreateLogger(nameof(DatabaseAgentFactory));

        await foreach (var item in tables)
        {
            logger.LogDebug("Processing table: {Table}", item);

            var tableName = await Try(async (e) =>
             {
                 var tableNameResponse = await extractTableName.InvokeAsync(kernel, new KernelArguments(defaultKernelArguments)
                                    {
                                        { "item", item }
                                    }, cancellationToken: cancellationToken)
                                    .ConfigureAwait(false);

                 return JsonSerializer.Deserialize<ExtractTableNameResponse>(tableNameResponse.GetValue<string>()!)!.TableName;
             }, loggerFactory: loggerFactory!, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            logger.LogDebug("Extracted table name: {TableName}", tableName);

            var existingRecordSearch = kernel.GetRequiredService<VectorStoreCollection<Guid, TableDefinitionSnippet>>()
                                                    .SearchAsync(await kernel.GetRequiredService<ITextEmbeddingGenerationService>()
                                                        .GenerateEmbeddingAsync(item)
                                                        .ConfigureAwait(false), top: 1)
                                                    .ConfigureAwait(false);

            VectorSearchResult<TableDefinitionSnippet> existingRecord = null!;

            await foreach (var searchResult in existingRecordSearch)
            {
                if (searchResult.Record.TableName == tableName)
                {
                    existingRecord = searchResult;
                    break;
                }
            }

            if (existingRecord is not null)
            {
                yield return (tableName!, existingRecord.Record.Definition!, existingRecord.Record.Description!, existingRecord.Record.SampleData!);
                continue;
            }

            logger.LogDebug("No existing record found for table: {TableName}, generating structure and data sample.", tableName);

            var tableStructureResponse = await Try(async (e) =>
            {
                var definition = await sqlWriter.InvokeAsync(kernel, new KernelArguments(defaultKernelArguments)
                {
                    { "prompt", $"Show the current structure of '{tableName}'" }
                }, cancellationToken)
                .ConfigureAwait(false);

                return JsonSerializer.Deserialize<WriteSQLQueryResponse>(definition.GetValue<string>()!)!;
            }, loggerFactory: loggerFactory!, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            logger.LogDebug("Generated table structure for {TableName}: {Query}", tableName, tableStructureResponse.Query);

            var tableDefinition = MarkdownRenderer.Render(await QueryExecutor.ExecuteSQLAsync(connection, tableStructureResponse.Query, null, cancellationToken)
                                            .ConfigureAwait(false));

            logger.LogDebug("Table definition for {TableName}: {Definition}", tableName, tableDefinition);

            var tableDataStructure = await Try(async (e) =>
            {
                var extract = await sqlWriter.InvokeAsync(kernel, new KernelArguments(defaultKernelArguments)
                {
                    { "prompt", $"Get the first 5 rows for '{tableName}'" }
                }, cancellationToken)
                    .ConfigureAwait(false);

                return JsonSerializer.Deserialize<WriteSQLQueryResponse>(extract.GetValue<string>()!)!;
            }, loggerFactory: loggerFactory!, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            logger.LogDebug("Generated table data extract for {TableName}: {Query}", tableName, tableDataStructure.Query);

            var tableExtract = MarkdownRenderer.Render(await QueryExecutor.ExecuteSQLAsync(connection, tableDataStructure.Query, null, cancellationToken)
                                            .ConfigureAwait(false));

            logger.LogDebug("Table data extract for {TableName}: {Extract}", tableName, tableExtract);

            var tableExplain = await Try(async (e) =>
            {
                var description = await tableDescriptionGenerator.InvokeAsync(kernel, new KernelArguments
                                    {
                                        { "tableDefinition", tableDefinition },
                                        { "tableDataExtract", tableExtract }
                                    })
                                  .ConfigureAwait(false);

                return JsonSerializer.Deserialize<ExplainTableResponse>(description.GetValue<string>()!)!;
            }, loggerFactory: loggerFactory!, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            var description = $"""
                ### {tableName}
                {tableExplain.Definition}

                #### Attributes
                {tableExplain.Attributes}

                #### Relations
                {tableExplain.Relations}
                """;

            logger.LogDebug("Generated table description for {TableName}: {Description}", tableName, description);

            yield return (tableName, tableDefinition, description, tableExtract)!;
        }
    }

    private static async Task<T> Try<T>(Func<Exception?, Task<T>> func, int count = 3, ILoggerFactory loggerFactory = null!, CancellationToken? cancellationToken = null)
    {
        var token = cancellationToken ?? CancellationToken.None;

        Exception? lastException = null;

        for (int i = 0; i < count; i++)
        {
            try
            {
                return await func(lastException)
                            .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (i == count - 1)
                {
                    (loggerFactory ?? NullLoggerFactory.Instance)
                                .CreateLogger(nameof(DatabaseAgentFactory))
                                    .LogWarning(ex, "Failed to execute the function after {Count} attempts.", count);
                    throw;
                }

                lastException = ex;

                await Task.Delay(200, token)
                            .ConfigureAwait(false);
            }
        }
        throw new InvalidOperationException("Failed to execute the function.");

    }
}
