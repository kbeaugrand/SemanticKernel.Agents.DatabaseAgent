using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using ModelContextProtocol.Server;
using SemanticKernel.Agents.DatabaseAgent.MCPServer.Configuration;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using ModelContextProtocol.Protocol;

namespace SemanticKernel.Agents.DatabaseAgent.MCPServer.Extensions
{
    internal static class DatabaseKernelAgentExtension
    {
        public static IHost ToMcpServer(this DatabaseKernelAgent agent, IConfiguration configuration)
        {
            TransportSettings configuredTransport = new();
            var transportConfiguration = configuration.GetSection("Agent:Transport");

            if (transportConfiguration.Exists())
                transportConfiguration.Bind(configuredTransport);

            switch (configuredTransport.Kind)
            {
                case TransportSettings.TransportType.Stdio:
                    var builder = Host.CreateEmptyApplicationBuilder(settings: null);

                    var mcpServerBuilder = builder.Services
                        .AddMcpServer(options => BindMcpServerOptions(agent, options));
                    mcpServerBuilder.WithStdioServerTransport();

                    return builder.Build();

                case TransportSettings.TransportType.Sse:
                    var webAppOptions = new WebApplicationOptions();
                    configuration.Bind(webAppOptions);

                    var webAppBuilder = WebApplication.CreateBuilder(webAppOptions);

                    webAppBuilder.Logging
                        .AddConsole();

                    webAppBuilder.Services
                            .AddMcpServer((options) => BindMcpServerOptions(agent, options))
                            .WithHttpTransport(c => { });

                    var app = webAppBuilder.Build();

                    app.MapMcp();

                    return app;
                default:
                    throw new NotSupportedException($"Transport '{configuredTransport.Kind}' is not supported.");
            }
        }

        static void BindMcpServerOptions(DatabaseKernelAgent agent, McpServerOptions options)
        {
            options.ServerInfo = new() { Name = agent.Name!, Version = "1.0.0" };
            options.Capabilities = new()
            {
                Tools = new()
                {
                    ListToolsHandler = async (context, cancellationToken) =>
                    {
                        return new ListToolsResult()
                        {
                            Tools = [
                                new Tool(){
                                        Name = agent.Name!,
                                        Description = agent.Description,
                                        InputSchema = JsonSerializer.Deserialize<JsonElement>("""
                                            {
                                                "type": "object",
                                                "properties": {
                                                  "message": {
                                                    "type": "string",
                                                    "description": "The user query in natural language."
                                                  }
                                                },
                                                "required": ["message"]
                                            }
                                            """),
                                    }
                            ]

                        };
                    },
                    CallToolHandler = async (context, cancellationToken) =>
                    {
                        if (!string.Equals(agent.Name, context.Params?.Name))
                        {
                            throw new InvalidOperationException($"Unknown tool: '{context.Params?.Name}'");
                        }

                        if (context.Params?.Arguments?.TryGetValue("message", out var message) is not true)
                        {
                            throw new InvalidOperationException("Missing required argument 'message'");
                        }

                        var responses = agent.InvokeAsync(new ChatMessageContent(AuthorRole.User, message.ToString()), thread: null)
                                                .ConfigureAwait(false);

                        var callToolResponse = new CallToolResponse();

                        await foreach (var item in responses)
                        {
                            callToolResponse.Content.Add(new()
                            {
                                Type = "text",
                                Text = item.Message.Content!
                            });
                        }

                        return callToolResponse;
                    }
                }
            };
        }
    }
}
