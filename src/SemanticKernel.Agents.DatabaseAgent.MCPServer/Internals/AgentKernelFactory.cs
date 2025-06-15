using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.InMemory;
using Microsoft.SemanticKernel.Connectors.Qdrant;
using Microsoft.SemanticKernel.Connectors.SqliteVec;
using Qdrant.Client;
using SemanticKernel.Agents.DatabaseAgent.Internals;
using SemanticKernel.Agents.DatabaseAgent.MCPServer.Configuration;
using SemanticKernel.Agents.DatabaseAgent.MCPServer.Extensions;

namespace SemanticKernel.Agents.DatabaseAgent.MCPServer.Internals;

internal static class AgentKernelFactory
{
    private static VectorStoreCollectionDefinition GetVectorStoreRecordDefinition(int vectorDimensions = 1536)
    {
        return new()
        {
            Properties = new List<VectorStoreProperty>
            {
                new VectorStoreDataProperty(nameof(TableDefinitionSnippet.TableName), typeof(string)){ IsIndexed = true },
                new VectorStoreKeyProperty(nameof(TableDefinitionSnippet.Key), typeof(Guid)),
                new VectorStoreDataProperty(nameof(TableDefinitionSnippet.Definition), typeof(string)),
                new VectorStoreDataProperty(nameof(TableDefinitionSnippet.Description), typeof(string)){ IsFullTextIndexed  = true },
                new VectorStoreVectorProperty(nameof(TableDefinitionSnippet.TextEmbedding), typeof(ReadOnlyMemory<float>), dimensions: vectorDimensions)
            }
        };
    }

    private static VectorStoreCollectionDefinition GetAgentStoreRecordDefinition(int vectorDimensions = 1536)
    {
        return new()
        {
            Properties = new List<VectorStoreProperty>
            {
                new VectorStoreDataProperty(nameof(AgentDefinitionSnippet.AgentName), typeof(string)){ IsIndexed = true },
                new VectorStoreKeyProperty(nameof(AgentDefinitionSnippet.Key), typeof(Guid)),
                new VectorStoreDataProperty(nameof(AgentDefinitionSnippet.Description), typeof(string)){ IsFullTextIndexed  = true },
                new VectorStoreDataProperty(nameof(AgentDefinitionSnippet.Instructions), typeof(string)),
                new VectorStoreVectorProperty(nameof(AgentDefinitionSnippet.TextEmbedding), typeof(ReadOnlyMemory<float>), dimensions: vectorDimensions)
            }
        };
    }

    internal static Kernel ConfigureKernel(IConfiguration configuration, ILoggerFactory loggerFactory)
    {
        var kernelSettings = configuration.GetSection("kernel").Get<KernelSettings>()!;
        var databaseSettings = configuration.GetSection("database").Get<DatabaseSettings>()!;
        var memorySection = configuration.GetSection("memory");

        var kernelBuilder = Kernel.CreateBuilder();

        kernelBuilder.Services
                    .UseDatabaseAgentQualityAssurance(opts =>
                    {
                        configuration.GetSection("agent:qualityAssurance").Bind(opts);
                    });

        kernelBuilder.Services.AddScoped(sp => DbConnectionFactory.CreateDbConnection(databaseSettings.ConnectionString, databaseSettings.Provider));

        kernelBuilder.Services.Configure<DatabasePluginOptions>(options =>
        {
            configuration.GetSection("memory")
                            .Bind(options);
        });

        var memorySettings = memorySection.Get<MemorySettings>();

        loggerFactory.CreateLogger(nameof(AgentKernelFactory))
                .LogInformation("Using memory kind {kind}", memorySettings!.Kind);

        switch (memorySettings.Kind)
        {
            case MemorySettings.StorageType.Volatile:
                kernelBuilder.Services.AddInMemoryVectorStoreRecordCollection<Guid, AgentDefinitionSnippet>("agent", options: new()
                {
                    Definition = GetAgentStoreRecordDefinition()
                });
                kernelBuilder.Services.AddInMemoryVectorStoreRecordCollection<Guid, TableDefinitionSnippet>("tables", options: new()
                {
                    Definition = GetVectorStoreRecordDefinition()
                });
                break;
            case MemorySettings.StorageType.SQLite:
                var sqliteSettings = memorySection.Get<SQLiteMemorySettings>()!;
                kernelBuilder.Services.AddSqliteCollection<Guid, AgentDefinitionSnippet>("agent",
                    sqliteSettings.ConnectionString,
                    options: new SqliteCollectionOptions()
                    {
                        Definition = GetAgentStoreRecordDefinition(sqliteSettings.Dimensions)
                    });
                kernelBuilder.Services.AddSqliteCollection<Guid, TableDefinitionSnippet>("tables",
                    sqliteSettings.ConnectionString,
                    options: new SqliteCollectionOptions()
                    {
                        Definition = GetVectorStoreRecordDefinition(sqliteSettings.Dimensions)
                    });
                break;
            case MemorySettings.StorageType.Qdrant:
                var qdrantSettings = memorySection.Get<QdrantMemorySettings>()!;

                kernelBuilder.Services.AddQdrantCollection<Guid, AgentDefinitionSnippet>("agent",
                            qdrantSettings.Host,
                            qdrantSettings.Port,
                            qdrantSettings.Https,
                            qdrantSettings.APIKey,
                            new QdrantCollectionOptions
                            {
                                Definition = GetAgentStoreRecordDefinition(qdrantSettings.Dimensions)
                            });
                kernelBuilder.Services.AddQdrantCollection<Guid, TableDefinitionSnippet>("tables",
                            qdrantSettings.Host,
                            qdrantSettings.Port,
                            qdrantSettings.Https,
                            qdrantSettings.APIKey,
                            new QdrantCollectionOptions
                            {
                                Definition = GetVectorStoreRecordDefinition(qdrantSettings.Dimensions)
                            });
                break;
            default: throw new ArgumentException($"Unknown storage type '{memorySection.Get<MemorySettings>()!.Kind}'");
        }

        _ = kernelBuilder.Services.AddSingleton<IPromptProvider, EmbeddedPromptProvider>();
        _ = kernelBuilder.Services.AddSingleton(loggerFactory);

        return kernelBuilder
                     .AddTextEmbeddingFromConfiguration(configuration, kernelSettings.Embedding, loggerFactory)
                     .AddCompletionServiceFromConfiguration(configuration, kernelSettings.Completion, loggerFactory)
                     .Build();
    }
}
