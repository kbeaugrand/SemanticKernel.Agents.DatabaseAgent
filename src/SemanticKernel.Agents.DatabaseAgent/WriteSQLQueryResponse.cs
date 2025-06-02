using System.ComponentModel;
using System.Text.Json.Serialization;

namespace SemanticKernel.Agents.DatabaseAgent;

internal sealed class WriteSQLQueryResponse
{
    [JsonPropertyName("comments")]
    [JsonPropertyOrder(0)]
    [Description("A list of comments explaining: \n- Any assumptions or constraints applied while translating the natural language query.\n Considerations specific to the DBMS type (e.g., syntax adjustments or optimizations).")]
    public IEnumerable<string> Comments { get; set; } = Enumerable.Empty<string>();

    [JsonPropertyName("query")]
    [JsonRequired]
    [Description("The SQL query string, ensuring adherence to DBMS-specific rules")]
    [JsonPropertyOrder(1)]
    public string Query { get; set; } = string.Empty;
}
