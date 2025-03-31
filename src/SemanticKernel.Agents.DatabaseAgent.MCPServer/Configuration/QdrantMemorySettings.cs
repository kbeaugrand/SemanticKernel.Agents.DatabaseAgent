namespace SemanticKernel.Agents.DatabaseAgent.MCPServer.Configuration
{
    internal class QdrantMemorySettings
    {
        public string Host { get; set; }

        public int Port { get; set; } = 6333;

        public bool Https { get; set; } = false;

        public string? APIKey { get; set; } = null;
    }
}
