namespace SemanticKernel.Agents.DatabaseAgent.MCPServer.Configuration;

internal class MemorySettings
{
    public enum StorageType
    {
        Volatile,
        Disk
    }

    public required StorageType Kind { get; set; }

    public required string Path { get; set; }

    public required string Completion { get; set; }

    public required string Embedding { get; set; }
}
