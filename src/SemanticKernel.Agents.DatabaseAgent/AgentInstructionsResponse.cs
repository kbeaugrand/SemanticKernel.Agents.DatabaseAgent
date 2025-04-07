using System.Text.Json.Serialization;

namespace SemanticKernel.Agents.DatabaseAgent;

internal sealed class AgentInstructionsResponse
{
    [JsonPropertyName("instructions")]
    public string Instructions { get; set; }
}
