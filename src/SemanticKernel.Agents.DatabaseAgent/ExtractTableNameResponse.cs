using System.Text.Json.Serialization;

namespace SemanticKernel.Agents.DatabaseAgent
{
    internal class ExtractTableNameResponse
    {
        [JsonPropertyName("thinking")]
        public string Thinking { get; set; } = string.Empty;

        [JsonPropertyName("tableName")]
        public string? TableName { get; set; }
    }
}
