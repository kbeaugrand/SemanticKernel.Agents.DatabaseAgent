using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using ModelContextProtocol.Protocol.Transport;
using ModelContextProtocol.Protocol.Types;
using ModelContextProtocol.Server;
using SemanticKernel.Agents.DatabaseAgent.MCPServer.Configuration;
using System.Text.Json;

namespace SemanticKernel.Agents.DatabaseAgent.MCPServer.Extensions
{
    internal static class DatabaseKernelAgentExtension
    {
        public static IMcpServer ToMcpServer(this DatabaseKernelAgent agent, IConfiguration configuration, ILoggerFactory? loggerFactory = null)
        {
            loggerFactory ??= NullLoggerFactory.Instance;

            IServerTransport transport = null!;

            var options = GetMcpServerOptions(agent);

            TransportSettings configuredTransport = new();
            var transportConfiguration = configuration.GetSection("Agent:Transport");

            if (transportConfiguration.Exists())
                transportConfiguration.Bind(configuredTransport);

            switch (configuredTransport.Kind)
            {
                case TransportSettings.TransportType.Stdio:
                    transport = new StdioServerTransport(agent.Name!);
                    break;
                case TransportSettings.TransportType.Http:
                    var port = int.TryParse(transportConfiguration.GetValue<string>("Port"), out var parsedPort) ? parsedPort : 8080;
                    transport = new HttpListenerSseServerTransport(options, port, loggerFactory!);
                    break;
                default:
                    throw new NotSupportedException($"Transport '{configuredTransport.Kind}' is not supported.");
            }

            return McpServerFactory.Create(transport, options);
        }

        static McpServerOptions GetMcpServerOptions(DatabaseKernelAgent agent)
        {
            return new McpServerOptions
            {
                ServerInfo = new() { Name = agent.Name!, Version = "1.0.0" },
                Capabilities = new()
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
                                throw new McpServerException($"Unknown tool: '{context.Params?.Name}'");
                            }

                            if (context.Params?.Arguments?.TryGetValue("message", out var message) is not true)
                            {
                                throw new McpServerException("Missing required argument 'message'");
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
                }
            };
        }
    }
}
