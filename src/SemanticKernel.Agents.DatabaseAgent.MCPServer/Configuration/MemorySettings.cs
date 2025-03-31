namespace SemanticKernel.Agents.DatabaseAgent.MCPServer.Configuration;

internal class MemorySettings
{
    public enum StorageType
    {
        Volatile,
        SQLite,
        Qdrant,
    }

    public required StorageType Kind { get; set; }
}
