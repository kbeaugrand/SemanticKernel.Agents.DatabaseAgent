namespace SemanticKernel.Agents.DatabaseAgent.MCPServer.Configuration
{
    internal class SQLiteMemorySettings
    {
        public string ConnectionString { get; set; }

        public int Dimensions { get; set; } = 1536;
    }
}
