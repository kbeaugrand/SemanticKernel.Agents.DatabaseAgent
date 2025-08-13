using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel.PromptTemplates.Handlebars;
using SemanticKernel.Agents.DatabaseAgent.Extensions;
using SemanticKernel.Agents.DatabaseAgent.Internals;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace SemanticKernel.Agents.DatabaseAgent;

public static class DatabaseAgentFactory
{
    public static async Task<DatabaseKernelAgent> CreateAgentAsync(
        Kernel kernel,
        ILoggerFactory? loggingFactory = null,
        CancellationToken? cancellationToken = null)
    {
        return await CreateAgentAsync(kernel, name: null!, update: false, loggingFactory, cancellationToken)
                        .ConfigureAwait(false);
    }

    public static async Task<DatabaseKernelAgent> CreateAgentAsync(
            Kernel kernel,
            string? name = null,
            ILoggerFactory? loggingFactory = null,
            CancellationToken? cancellationToken = null)
    {
        return await CreateAgentAsync(kernel, update: false, loggingFactory, cancellationToken)
                        .ConfigureAwait(false);
    }

    public static async Task<DatabaseKernelAgent> CreateAgentAsync(
        Kernel kernel,
        bool? update = false,
        ILoggerFactory? loggingFactory = null,
        CancellationToken? cancellationToken = null)
    {
        return await CreateAgentAsync(kernel, name: null!, update: update, loggingFactory, cancellationToken)
                        .ConfigureAwait(false);
    }

    public static async Task<DatabaseKernelAgent> CreateAgentAsync(
            Kernel kernel,
            string name = null!,
            bool? update = false,
            ILoggerFactory? loggingFactory = null,
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

        return await BuildAgentAsync(kernel,
            name,
            update,
            loggingFactory,
            cancellationToken ?? CancellationToken.None)
                            .ConfigureAwait(false);
    }

    private static async Task<DatabaseKernelAgent> BuildAgentAsync(
        Kernel kernel,
        string? name = null!,
        bool? update = false,
        ILoggerFactory? loggingFactory = null,
        CancellationToken? cancellationToken = null)
    {
        var agentKernel = kernel.Clone();

        AgentDefinitionSnippet? existingDefinition = null!;

        if (!string.IsNullOrWhiteSpace(name))
        {

            existingDefinition = kernel.GetRequiredService<VectorStoreCollection<Guid, AgentDefinitionSnippet>>()
                                            .GetAsync(a => a.AgentName == name, top: 1)
                                            .ToBlockingEnumerable()
                                            .FirstOrDefault();
        }
        else
        {
            existingDefinition = await kernel.GetRequiredService<VectorStoreCollection<Guid, AgentDefinitionSnippet>>()
                                            .GetAsync(Guid.Empty)
                                            .ConfigureAwait(false);
        }

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

        var tableDescriptions = await MemorizeAgentSchema(kernel, update ?? false, loggingFactory, cancellationToken ?? CancellationToken.None);

        var promptProvider = kernel.GetRequiredService<IPromptProvider>() ?? new EmbeddedPromptProvider();

        var agentDescription = await KernelFunctionFactory.CreateFromPrompt(promptProvider.ReadPrompt(AgentPromptConstants.AgentDescriptionGenerator), PromptExecutionSettingsHelper.GetPromptExecutionSettings<AgentDescriptionResponse>(), functionName: AgentPromptConstants.AgentDescriptionGenerator)
                                        .InvokeAsync(kernel, new KernelArguments
                                        {
                                            { "tableDefinitions", tableDescriptions },
                                            { "expectedName", name }
                                        })
                                        .ConfigureAwait(false);

        loggingFactory.CreateLogger<DatabaseKernelAgent>()
            .LogInformation("Agent description: {Description}", agentDescription.GetValue<string>()!);

        FunctionResult agentName = null!;
        if (string.IsNullOrWhiteSpace(name))
        {
            agentName = await KernelFunctionFactory.CreateFromPrompt(promptProvider.ReadPrompt(AgentPromptConstants.AgentNameGenerator), PromptExecutionSettingsHelper.GetPromptExecutionSettings<AgentNameRespone>(), functionName: AgentPromptConstants.AgentNameGenerator)
                                        .InvokeAsync(kernel, new KernelArguments
                                        {
                                            { "agentDescription", agentDescription.GetValue<string>()! }
                                        })
                                        .ConfigureAwait(false);

            loggingFactory.CreateLogger<DatabaseKernelAgent>()
                .LogInformation("Agent name: {Name}", agentName.GetValue<string>()!);
        }

        var agentInstructions = await KernelFunctionFactory.CreateFromPrompt(promptProvider.ReadPrompt(AgentPromptConstants.AgentInstructionsGenerator), PromptExecutionSettingsHelper.GetPromptExecutionSettings<AgentInstructionsResponse>(), functionName: AgentPromptConstants.AgentInstructionsGenerator)
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
            AgentName = string.IsNullOrEmpty(name) ? JsonSerializer.Deserialize<AgentNameRespone>(agentName.GetValue<string>()!)!.Name : name,
            Description = JsonSerializer.Deserialize<AgentDescriptionResponse>(agentDescription.GetValue<string>()!)!.Description,
            Instructions = JsonSerializer.Deserialize<AgentInstructionsResponse>(agentInstructions.GetValue<string>()!)!.Instructions
        };

        agentDefinition.TextEmbedding = await kernel.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>()
                                                        .GenerateVectorAsync(agentDefinition.Description)
                                                        .ConfigureAwait(false);

        await kernel.GetRequiredService<VectorStoreCollection<Guid, AgentDefinitionSnippet>>()
                        .UpsertAsync(agentDefinition, cancellationToken ?? CancellationToken.None)
                        .ConfigureAwait(false);

        return new DatabaseKernelAgent()
        {
            Kernel = agentKernel,
            Name = agentDefinition.AgentName,
            Description = agentDefinition.Description,
            Instructions = agentDefinition.Instructions
        };
    }

    private static async Task<string> MemorizeAgentSchema(Kernel kernel, bool update, ILoggerFactory loggerFactory, CancellationToken cancellationToken)
    {
        var stringBuilder = new StringBuilder();

        try
        {
            var descriptions = GetTablesDescription(kernel, update, GetTablesAsync(kernel, cancellationToken), loggerFactory, cancellationToken)
                                                                    .ConfigureAwait(false);

            await foreach (var item in descriptions)
            {
                stringBuilder.AppendLine(item.Description);
            }

            return stringBuilder.ToString();
        }
        catch (Exception e)
        {
            throw;
        }
    }

    private static async IAsyncEnumerable<string> GetTablesAsync(Kernel kernel, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var connection = kernel.GetRequiredService<DbConnection>();
        var promptProvider = kernel.GetRequiredService<IPromptProvider>() ?? new EmbeddedPromptProvider();
        var sqlWriter = KernelFunctionFactory.CreateFromPrompt(
                executionSettings: PromptExecutionSettingsHelper.GetPromptExecutionSettings<WriteSQLQueryResponse>(),
                templateFormat: "handlebars",
                promptTemplate: promptProvider.ReadPrompt(AgentPromptConstants.WriteSQLQuery),
                promptTemplateFactory: new HandlebarsPromptTemplateFactory());

        var defaultKernelArguments = new KernelArguments
            {
                { "providerName", connection.GetProviderName() },
                { "tablesDefinitions", "" }
            };

        string previousSQLQuery = null!;
        using var reader = await RetryHelper.Try(async (e) =>
        {
            var tablesGenerator = await sqlWriter.InvokeAsync(kernel, new KernelArguments(defaultKernelArguments)
            {
                { "prompt", "List all tables" },
                { "previousAttempt", previousSQLQuery },
                { "previousException", e },
            })
            .ConfigureAwait(false);

            var response = JsonSerializer.Deserialize<WriteSQLQueryResponse>(tablesGenerator.GetValue<string>()!)!;

            previousSQLQuery = response.Query;

            return await QueryExecutor.ExecuteSQLAsync(connection, previousSQLQuery, null, cancellationToken)
                                         .ConfigureAwait(false);
        }, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        foreach (DataRow row in reader!.Rows)
        {
            yield return MarkdownRenderer.Render(row);
        }
    }

    private static async IAsyncEnumerable<TableDefinitionSnippet> GetTablesDescription(Kernel kernel, bool update, IAsyncEnumerable<string> tables, ILoggerFactory loggerFactory, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var connection = kernel.GetRequiredService<DbConnection>();
        var promptProvider = kernel.GetRequiredService<IPromptProvider>() ?? new EmbeddedPromptProvider();
        var sqlWriter = KernelFunctionFactory.CreateFromPrompt(
                executionSettings: PromptExecutionSettingsHelper.GetPromptExecutionSettings<WriteSQLQueryResponse>(),
                promptTemplate: promptProvider.ReadPrompt(AgentPromptConstants.WriteSQLQuery),
                templateFormat: "handlebars",
                promptTemplateFactory: new HandlebarsPromptTemplateFactory());
        var extractTableName = KernelFunctionFactory.CreateFromPrompt(
            promptTemplate: promptProvider.ReadPrompt(AgentPromptConstants.ExtractTableName),
            templateFormat: "handlebars",
            promptTemplateFactory: new HandlebarsPromptTemplateFactory(),
            executionSettings: PromptExecutionSettingsHelper.GetPromptExecutionSettings<ExtractTableNameResponse>(), 
            functionName: AgentPromptConstants.ExtractTableName);
        var tableDescriptionGenerator = KernelFunctionFactory.CreateFromPrompt(
            promptTemplate: promptProvider.ReadPrompt(AgentPromptConstants.ExplainTable),
            templateFormat: "handlebars",
            promptTemplateFactory: new HandlebarsPromptTemplateFactory(),
            executionSettings: PromptExecutionSettingsHelper.GetPromptExecutionSettings<ExplainTableResponse>(),
            functionName: AgentPromptConstants.ExplainTable);
        var defaultKernelArguments = new KernelArguments
            {
                { "providerName", connection.GetProviderName() }
            };

        var tableVectorCollection = kernel.GetRequiredService<VectorStoreCollection<Guid, TableDefinitionSnippet>>();
        var embeddingTextGenerator = kernel.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();

        StringBuilder sb = new StringBuilder();

        var logger = loggerFactory?.CreateLogger(nameof(DatabaseAgentFactory)) ?? NullLoggerFactory.Instance.CreateLogger(nameof(DatabaseAgentFactory));

        await foreach (var item in tables)
        {
            logger.LogDebug("Processing table: {Table}", item);

            var tableName = await RetryHelper.Try(async (e) =>
             {
                 var tableNameResponse = await extractTableName.InvokeAsync(kernel, new KernelArguments(defaultKernelArguments)
                                    {
                                        { "item", item }
                                    }, cancellationToken: cancellationToken)
                                    .ConfigureAwait(false);


                 var response = JsonSerializer.Deserialize<ExtractTableNameResponse>(tableNameResponse.GetValue<string>()!);
                 
                 if(response is null || string.IsNullOrWhiteSpace(response.TableName))
                 {
                     throw new InvalidOperationException("Failed to extract table name from the item.");
                 }

                 return response.TableName;

             }, loggerFactory: loggerFactory!, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            logger.LogDebug("Extracted table name: {TableName}", tableName);

            var existingRecordSearch = kernel.GetRequiredService<VectorStoreCollection<Guid, TableDefinitionSnippet>>()
                                                    .SearchAsync(await embeddingTextGenerator
                                                        .GenerateVectorAsync(item)
                                                        .ConfigureAwait(false), top: 10)
                                                    .ConfigureAwait(false);

            TableDefinitionSnippet tableDefinitionSnippet = null!;

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
                tableDefinitionSnippet = existingRecord.Record;
            }

            if (tableDefinitionSnippet is not null && !update)
            {
                logger.LogDebug("Found existing record for table: {Table}, returning existing record.", item);
                yield return tableDefinitionSnippet;
                continue;
            }

            logger.LogDebug("No existing record found for table: {Table}, extracting table definition.", item);

            if (tableDefinitionSnippet is null)
            {
                tableDefinitionSnippet = new TableDefinitionSnippet
                {
                    Key = Guid.NewGuid(),
                    TableName = tableName
                };
            }

            string previousSQLQuery = null!;
            var tableDefinition = await RetryHelper.Try(async (e) =>
            {
                var definition = await sqlWriter.InvokeAsync(kernel, new KernelArguments(defaultKernelArguments)
                {
                    { "prompt", $"Extract the structure of table {tableName} by listing the column attributes, including the column name, data type, maximum length, and default value." },
                    { "previousAttempt", previousSQLQuery },
                    { "previousException", e },
                }, cancellationToken)
                .ConfigureAwait(false);

                previousSQLQuery = JsonSerializer.Deserialize<WriteSQLQueryResponse>(definition.GetValue<string>()!).Query;

                logger.LogDebug("Generated table structure for {TableName}: {Query}", tableName, previousSQLQuery);

                return MarkdownRenderer.Render(await QueryExecutor.ExecuteSQLAsync(connection, previousSQLQuery, null, cancellationToken)
                                                .ConfigureAwait(false));
            }, loggerFactory: loggerFactory!, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            logger.LogDebug("Table definition for {TableName}: {Definition}", tableName, tableDefinition);

            var tableExtract = await RetryHelper.Try(async (e) =>
            {
                var extract = await sqlWriter.InvokeAsync(kernel, new KernelArguments(defaultKernelArguments)
                {
                    { "prompt", $"Get the first 5 rows for '{tableName}'" },
                    { "tablesDefinition", tableDefinition },
                    { "previousAttempt", previousSQLQuery },
                    { "previousException", e },
                }, cancellationToken)
                    .ConfigureAwait(false);

                previousSQLQuery = JsonSerializer.Deserialize<WriteSQLQueryResponse>(extract.GetValue<string>()!).Query;

                logger.LogDebug("Generated table data extract for {TableName}: {Query}", tableName, previousSQLQuery);

                return MarkdownRenderer.Render(await QueryExecutor.ExecuteSQLAsync(connection, previousSQLQuery, null, cancellationToken)
                                                .ConfigureAwait(false));
            }, loggerFactory: loggerFactory!, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            logger.LogDebug("Table data extract for {TableName}: {Extract}", tableName, tableExtract);

            var tableExplain = await RetryHelper.Try(async (e) =>
            {
                var description = await tableDescriptionGenerator.InvokeAsync(kernel, new KernelArguments(defaultKernelArguments)
                                    {
                                        { "tableName", tableName },
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

            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(description));

            tableDefinitionSnippet.Definition = tableDefinition;
            tableDefinitionSnippet.Description = description;
            tableDefinitionSnippet.SampleData = tableExtract;
            tableDefinitionSnippet.TextEmbedding = await embeddingTextGenerator.GenerateVectorAsync(description, cancellationToken: cancellationToken)
                                                                                                .ConfigureAwait(false);

            await tableVectorCollection
                            .UpsertAsync(tableDefinitionSnippet)
                            .ConfigureAwait(false);

            yield return tableDefinitionSnippet;
        }
    }
}
