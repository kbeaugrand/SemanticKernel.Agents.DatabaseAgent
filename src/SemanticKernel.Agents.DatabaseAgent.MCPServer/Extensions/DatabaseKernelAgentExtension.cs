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
            options.ServerInfo = new() {
                Name = agent.Name!,
                Version = "1.0.0"
            };
            options.ServerInstructions = agent.Description;
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
                                    Name= "Ask",
                                    Description = "Asks a question to the database. The question should be written in natural language.",
                                    InputSchema = JsonSerializer.Deserialize<JsonElement>("""
                                        { "type": "object", "properties": { "query": { "type": "string", "description": "The question to ask the database." } }, "required": ["query"] }
                                    """),
                                },
                                new Tool(){
                                    Name = "ListTables",
                                    Description = "Returns a list of all table names in the database.",
                                    InputSchema = JsonSerializer.Deserialize<JsonElement>("""
                                        { "type": "object", "properties": {}, "required": [] }
                                    """),
                                },
                                new Tool(){
                                    Name = "GetTableDefinition",
                                    Description = "Returns the semantic definition of a specified table.",
                                    InputSchema = JsonSerializer.Deserialize<JsonElement>("""
                                        { "type": "object", "properties": { "tableName": { "type": "string", "description": "The name of the table." } }, "required": ["tableName"] }
                                    """),
                                }
                            ]
                        };
                    },
                    CallToolHandler = async (context, cancellationToken) =>
                    {
                        var vectorStore = agent.Kernel.GetRequiredService<Microsoft.Extensions.VectorData.VectorStoreCollection<System.Guid, SemanticKernel.Agents.DatabaseAgent.TableDefinitionSnippet>>();
                        var callToolResponse = new CallToolResponse();

                        switch (context.Params?.Name)
                        {
                            case "Ask":
                                if (context.Params?.Arguments?.TryGetValue("query", out var queryObj) != true)
                                    throw new InvalidOperationException("Missing required argument 'query'");
                                var query = queryObj.ToString();

                                var responses = agent.InvokeAsync(new ChatMessageContent(AuthorRole.User, query), thread: null)
                                                .ConfigureAwait(false);

                                await foreach (var item in responses)
                                {
                                    callToolResponse.Content.Add(new()
                                    {
                                        Type = "text",
                                        Text = item.Message.Content!
                                    });
                                }
                                break;
                            case "ListTables":
                                var tables = new List<string>();
                                await foreach (var record in vectorStore.GetAsync(x => true, top: int.MaxValue, cancellationToken: cancellationToken))
                                {
                                    tables.Add(record.TableName);
                                }
                                callToolResponse.Content.Add(new() { Type = "json", Text = JsonSerializer.Serialize(tables) });
                                break;
                            case "GetTableDefinition":
                                if (context.Params?.Arguments?.TryGetValue("tableName", out var tableNameObj) != true)
                                    throw new InvalidOperationException("Missing required argument 'tableName'");
                                var tableName = tableNameObj.ToString();
                                var found = false;
                                await foreach (var record in vectorStore.GetAsync(x => x.TableName == tableName, top: int.MaxValue, cancellationToken: cancellationToken))
                                {
                                    callToolResponse.Content.Add(new() { Type = "json", Text = JsonSerializer.Serialize(record) });
                                    found = true;
                                    break;
                                }
                                if (!found)
                                    throw new InvalidOperationException($"Table '{tableName}' not found.");
                                break;

                            default:
                                throw new InvalidOperationException($"Unknown tool: '{context.Params?.Name}'");
                        }
                        return callToolResponse;
                    }
                }
            };
        }
    }
}
