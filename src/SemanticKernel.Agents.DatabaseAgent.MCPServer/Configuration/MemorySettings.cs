namespace SemanticKernel.Agents.DatabaseAgent.MCPServer.Configuration;

internal class MemorySettings
{
    public enum StorageType
    {
        Volatile,    }

    public required StorageType Kind { get; set; }
}
