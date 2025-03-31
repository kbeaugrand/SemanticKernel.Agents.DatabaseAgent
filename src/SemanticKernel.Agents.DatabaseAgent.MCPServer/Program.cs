using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Org.BouncyCastle.Tls.Crypto.Impl.BC;
using SemanticKernel.Agents.DatabaseAgent.MCPServer.Configuration;
using SemanticKernel.Agents.DatabaseAgent.MCPServer.Extensions;

namespace SemanticKernel.Agents.DatabaseAgent.MCPServer;

internal class Program
{
    static async Task Main(string[] args)
    {
        IConfiguration configuration = new ConfigurationBuilder()
                                                .AddEnvironmentVariables()
                                                .AddCommandLine(args)
                                                .Build();

        var kernel = ConfigureKernel(configuration);

        var agent = await DatabaseAgentFactory.CreateAgentAsync(kernel);

        await using var mcpServer = agent.ToMcpServer();

        await mcpServer.StartAsync();

        await Task.Delay(Timeout.Infinite);
    }

    static Kernel ConfigureKernel(IConfiguration configuration)
    {
        var kernelSettings = configuration.GetSection("kernel").Get<KernelSettings>()!;
        var databaseSettings = configuration.GetSection("database").Get<DatabaseSettings>()!;
        var memorySettings = configuration.GetSection("memory").Get<MemorySettings>()!;        

        var kernelBuilder = Kernel.CreateBuilder();

        kernelBuilder.Services
                    .UseDatabaseAgentQualityAssurance(opts =>
                    {
                        configuration.GetSection("agent:qualityAssurance").Bind(opts);
                    });

        kernelBuilder.Services.AddScoped(sp => DbConnectionFactory.CreateDbConnection(databaseSettings.ConnectionString, databaseSettings.Provider));

        switch (memorySettings.Kind)
        {
            case MemorySettings.StorageType.Volatile:
                kernelBuilder.AddInMemoryVectorStoreRecordCollection<string, TableDefinitionSnippet>("tables");
                break;
            default: throw new ArgumentException("Unknown storage type");

        }

        return kernelBuilder
                     .AddTextEmbeddingFromConfiguration(configuration, kernelSettings.Embedding)
                     .AddCompletionServiceFromConfiguration(configuration, kernelSettings.Completion)
                     .Build();
    }
}
