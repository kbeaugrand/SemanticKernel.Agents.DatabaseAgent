using System.Text.Json.Serialization;

namespace SemanticKernel.Agents.DatabaseAgent;

internal sealed class ExplainTableResponse
{
    [JsonPropertyName("tableName")]
    public string TableName { get; set; } = string.Empty;

    [JsonPropertyName("attributes")]
    public string Attributes { get; set; } = string.Empty;

    [JsonPropertyName("recordSample")]
    public string RecordSample { get; set; } = string.Empty;

    [JsonPropertyName("definition")]
    public string Definition { get; set; } = string.Empty;

    [JsonPropertyName("relations")]
    public string Relations { get; set; } = string.Empty;
}
