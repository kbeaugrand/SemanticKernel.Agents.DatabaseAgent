namespace SemanticKernel.Agents.DatabaseAgent.MCPServer.Configuration;

internal class MCPServerSettings
{
    public required DatabaseSettings Database { get; set; }

    public required MemorySettings Memory { get; set; }
}