using Microsoft.Extensions.Configuration;
using SemanticKernel.Agents.DatabaseAgent.MCPServer.Extensions;
using SemanticKernel.Agents.DatabaseAgent.MCPServer.Internals;

namespace SemanticKernel.Agents.DatabaseAgent.MCPServer;

internal class Program
{
    static async Task Main(string[] args)
    {
        IConfiguration configuration = new ConfigurationBuilder()
                                                .AddEnvironmentVariables()
                                                .AddCommandLine(args)
                                                .Build();

        var kernel = AgentKernelFactory.ConfigureKernel(configuration);

        var agent = await DatabaseAgentFactory.CreateAgentAsync(kernel);

        await using var mcpServer = agent.ToMcpServer();

        await mcpServer.StartAsync();

        await Task.Delay(Timeout.Infinite);
    }    
}
