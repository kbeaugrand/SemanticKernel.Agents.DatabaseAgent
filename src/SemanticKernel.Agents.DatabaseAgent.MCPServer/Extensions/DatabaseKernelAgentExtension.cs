using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Agents;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using SemanticKernel.Agents.DatabaseAgent.MCPServer.Configuration;
using System.Text.Json;

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
                case TransportSettings.TransportType.HttpStreamable:
                    // By default the SDK handles HttpStreamable and fall back to SSE in case the client doesn't support it.
                    var webAppOptions = new WebApplicationOptions();
                    configuration.Bind(webAppOptions);
                    var webAppBuilder = WebApplication.CreateBuilder(webAppOptions);
                    webAppBuilder.Logging.AddConsole();
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
            options.ServerInfo = new Implementation
            {
                Name = agent.Name!,
                Version = "1.0.0"
            };

            // Keep if you want model instructions exposed; safe to omit if unused by your client.
            options.ServerInstructions = agent.Description;

            options.Capabilities = new ServerCapabilities
            {
                Tools = new ToolsCapability
                {
                    ListToolsHandler = (request, cancellationToken) =>
                        ValueTask.FromResult(new ListToolsResult
                        {
                            Tools =
                            [
                                new Tool
                                {
                                    Name = "Ask",
                                    Description = "Ask your question to the database using natural language in English only; do not attempt to write or generate an SQL query.",
                                    InputSchema = JsonSerializer.Deserialize<JsonElement>("""
                                        {
                                          "type": "object",
                                          "properties": {
                                            "query": {
                                              "type": "string",
                                              "description": "The question to ask the database."
                                            }
                                          },
                                          "required": ["query"]
                                        }
                                        """),
                                },
                                new Tool
                                {
                                    Name = "ListTables",
                                    Description = "Returns a list of all table names in the database.",
                                    InputSchema = JsonSerializer.Deserialize<JsonElement>("""
                                        {
                                          "type": "object",
                                          "properties": {},
                                          "required": []
                                        }
                                        """),
                                },
                                new Tool
                                {
                                    Name = "GetTableDefinition",
                                    Description = "Returns the semantic definition of a specified table.",
                                    InputSchema = JsonSerializer.Deserialize<JsonElement>("""
                                        {
                                          "type": "object",
                                          "properties": {
                                            "tableName": {
                                              "type": "string",
                                              "description": "The name of the table."
                                            }
                                          },
                                          "required": ["tableName"]
                                        }
                                        """),
                                }
                            ]
                        }),

                    CallToolHandler = async (request, cancellationToken) =>
                    {
                        // Resolve your vector store (same as before)
                        var vectorStore =
                            agent.Kernel.GetRequiredService<VectorStoreCollection<Guid, TableDefinitionSnippet>>();

                        var name = request.Params?.Name;
                        if (string.IsNullOrWhiteSpace(name))
                        {
                            throw new McpException("Missing tool name.");
                        }

                        switch (name)
                        {
                            case "Ask":
                                {
                                    if (request.Params?.Arguments?.TryGetValue("query", out var queryObj) is not true)
                                    {
                                        throw new McpException("Missing required argument 'query'");
                                    }

                                    var query = queryObj.GetString() ?? string.Empty;
                                    var contentBlocks = new List<ContentBlock>();

                                    // Stream responses from the agent and forward them as text blocks
                                    var responses = agent.InvokeAsync(query)
                                                            .ConfigureAwait(false);

                                    await foreach (var item in responses)
                                    {
                                        if (string.IsNullOrEmpty(item.Message.Content))
                                        {
                                            continue;
                                        }
                                        contentBlocks.Add(new TextContentBlock
                                        {
                                            Type = "text",
                                            Text = item.Message.Content
                                        });
                                    }

                                    return new CallToolResult { Content = contentBlocks.ToArray() };
                                }

                            case "ListTables":
                                {
                                    var contentBlocks = new List<ContentBlock>();

                                    await foreach (var item in vectorStore.GetAsync(x => true, top: int.MaxValue, cancellationToken: cancellationToken))
                                    {
                                        if (string.IsNullOrEmpty(item.TableName))
                                        {
                                            continue;
                                        }

                                        contentBlocks.Add(new TextContentBlock
                                        {
                                            Type = "text",
                                            Text = item.TableName
                                        });
                                    }

                                    return new CallToolResult { Content = contentBlocks.ToArray() };
                                }

                            case "GetTableDefinition":
                                {
                                    if (request.Params?.Arguments?.TryGetValue("tableName", out var tableNameObj) is not true)
                                    {
                                        throw new McpException("Missing required argument 'tableName'");
                                    }

                                    var tableName = tableNameObj.GetString() ?? string.Empty;
                                    if (string.IsNullOrWhiteSpace(tableName))
                                    {
                                        throw new McpException("Argument 'tableName' cannot be empty");
                                    }

                                    var contentBlocks = new List<ContentBlock>();

                                    await foreach (var item in vectorStore.GetAsync(x => x.TableName.Contains(tableName), top: int.MaxValue, cancellationToken: cancellationToken))
                                    {
                                        if (string.IsNullOrEmpty(item.TableName))
                                        {
                                            continue;
                                        }

                                        contentBlocks.Add(new TextContentBlock
                                        {
                                            Type = "text",
                                            Text = item.Definition!
                                        });
                                    }

                                    return new CallToolResult { Content = contentBlocks.ToArray() };
                                }

                            default:
                                throw new McpException($"Unknown tool: '{name}'");
                        }
                    },
                }
            };
        }
    }
}
