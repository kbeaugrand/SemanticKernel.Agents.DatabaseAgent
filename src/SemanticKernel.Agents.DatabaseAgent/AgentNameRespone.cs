using System.Text.Json.Serialization;

namespace SemanticKernel.Agents.DatabaseAgent;

internal sealed class AgentNameRespone
{
    [JsonPropertyName("name")]
    public string Name { get; set; }
}
