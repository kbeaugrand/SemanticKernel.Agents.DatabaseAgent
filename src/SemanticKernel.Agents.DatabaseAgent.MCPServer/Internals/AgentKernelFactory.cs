using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Qdrant;
using Microsoft.SemanticKernel.Connectors.Sqlite;
using SemanticKernel.Agents.DatabaseAgent.MCPServer.Configuration;
using SemanticKernel.Agents.DatabaseAgent.MCPServer.Extensions;

namespace SemanticKernel.Agents.DatabaseAgent.MCPServer.Internals;

internal static class AgentKernelFactory
{
    private static VectorStoreRecordDefinition GetVectorStoreRecordDefinition(int vectorDimensions = 1536)
    {
        return new()
        {
            Properties = new List<VectorStoreRecordProperty>
            {
                new VectorStoreRecordDataProperty(nameof(TableDefinitionSnippet.TableName), typeof(string)),
                new VectorStoreRecordKeyProperty(nameof(TableDefinitionSnippet.Key), typeof(Guid)),
                new VectorStoreRecordDataProperty(nameof(TableDefinitionSnippet.Definition), typeof(string)),
                new VectorStoreRecordDataProperty(nameof(TableDefinitionSnippet.Description), typeof(string)),
                new VectorStoreRecordVectorProperty(nameof(TableDefinitionSnippet.TextEmbedding), typeof(ReadOnlyMemory<float>)) 
                {
                    Dimensions = vectorDimensions
                }
            }
        };
    }

    internal static Kernel ConfigureKernel(IConfiguration configuration)
    {
        var kernelSettings = configuration.GetSection("kernel").Get<KernelSettings>()!;
        var databaseSettings = configuration.GetSection("database").Get<DatabaseSettings>()!;
        var memorySettings = configuration.GetSection("memory");

        var kernelBuilder = Kernel.CreateBuilder();

        kernelBuilder.Services.AddLogging(logging =>
        {
            logging.AddConsole();
            logging.AddConfiguration(configuration.GetSection("logging"));
        });

        kernelBuilder.Services
                    .UseDatabaseAgentQualityAssurance(opts =>
                    {
                        configuration.GetSection("agent:qualityAssurance").Bind(opts);
                    });

        kernelBuilder.Services.AddScoped(sp => DbConnectionFactory.CreateDbConnection(databaseSettings.ConnectionString, databaseSettings.Provider));

        switch (memorySettings.Get<MemorySettings>()!.Kind)
        {
            case MemorySettings.StorageType.Volatile:
                kernelBuilder.AddInMemoryVectorStoreRecordCollection<Guid, TableDefinitionSnippet>("tables");
                break;
            case MemorySettings.StorageType.SQLite:
                var sqliteSettings = memorySettings.Get<SQLiteMemorySettings>()!;
                kernelBuilder.Services.AddSqliteVectorStoreRecordCollection<Guid, TableDefinitionSnippet>("tables",
                    sqliteSettings.ConnectionString,
                    options: new SqliteVectorStoreRecordCollectionOptions<TableDefinitionSnippet>()
                    {
                        VectorStoreRecordDefinition = GetVectorStoreRecordDefinition(sqliteSettings.Dimensions)
                    });
                break;
            case MemorySettings.StorageType.Qdrant:
                var qdrantSettings = memorySettings.Get<QdrantMemorySettings>()!;
                kernelBuilder.AddQdrantVectorStoreRecordCollection<Guid, TableDefinitionSnippet>("tables",
                            qdrantSettings.Host,
                            qdrantSettings.Port,
                            qdrantSettings.Https,
                            qdrantSettings.APIKey,
                            new QdrantVectorStoreRecordCollectionOptions<TableDefinitionSnippet>
                            {
                                VectorStoreRecordDefinition = GetVectorStoreRecordDefinition(qdrantSettings.Dimensions)
                            });
                break;
            default: throw new ArgumentException($"Unknown storage type '{memorySettings.Get<MemorySettings>()!.Kind}'");
        }

        return kernelBuilder
                     .AddTextEmbeddingFromConfiguration(configuration, kernelSettings.Embedding)
                     .AddCompletionServiceFromConfiguration(configuration, kernelSettings.Completion)
                     .Build();
    }
}
