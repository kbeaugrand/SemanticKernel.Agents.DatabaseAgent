

namespace SemanticKernel.Agents.DatabaseAgent.MCPServer.Configuration;

internal class OllamaConfig
{
    public string ModelId { get; set; }

    public string Endpoint { get; set; }

    public int TimeoutInSeconds { get; set; } = 60;
}
