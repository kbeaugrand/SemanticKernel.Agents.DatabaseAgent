namespace SemanticKernel.Agents.DatabaseAgent.MCPServer.Configuration;

internal class MemorySettings
{
    public enum StorageType
    {
        Volatile,
        SQLite
    }

    public required StorageType Kind { get; set; }
}
