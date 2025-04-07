using System.Text.Json.Serialization;

namespace SemanticKernel.Agents.DatabaseAgent;

internal sealed class AgentDescriptionResponse
{
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
}
