﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SemanticKernel.Agents.DatabaseAgent.MCPServer.Extensions;
using SemanticKernel.Agents.DatabaseAgent.MCPServer.Internals;
using ZstdSharp.Unsafe;

namespace SemanticKernel.Agents.DatabaseAgent.MCPServer;

internal class Program
{
    static async Task Main(string[] args)
    {
        IConfiguration configuration = new ConfigurationBuilder()
                                                .AddEnvironmentVariables()
                                                .AddCommandLine(args)
                                                .Build();

        var loggerFactory = LoggerFactory.Create(options =>
        {
            options.AddDebug();
            options.SetMinimumLevel(LogLevel.Debug);
        });

        var kernel = AgentKernelFactory.ConfigureKernel(configuration, loggerFactory);

        var agent = await DatabaseAgentFactory.CreateAgentAsync(kernel, loggerFactory);

        await agent.ToMcpServer(configuration)
                    .RunAsync();

    }
}
