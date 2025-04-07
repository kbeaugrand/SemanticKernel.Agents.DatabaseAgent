using System.Text.Json.Serialization;

namespace SemanticKernel.Agents.DatabaseAgent;

internal sealed class ExplainTableResponse
{
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
}
