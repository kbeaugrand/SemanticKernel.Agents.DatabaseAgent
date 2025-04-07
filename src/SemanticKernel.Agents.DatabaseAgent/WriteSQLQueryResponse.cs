using System.Text.Json.Serialization;

namespace SemanticKernel.Agents.DatabaseAgent;

internal sealed class WriteSQLQueryResponse
{
    [JsonPropertyName("query")]
    public string Query { get; set; } = string.Empty;

    [JsonPropertyName("comments")]
    public IEnumerable<string> Comments { get; set; } = Enumerable.Empty<string>();
}
