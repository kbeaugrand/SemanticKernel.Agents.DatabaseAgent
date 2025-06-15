using System.Text.Json.Serialization;

namespace SemanticKernel.Agents.DatabaseAgent
{
    internal class RewriteQueryResponse
    {
        [JsonPropertyName("thinking")]
        public string Thinking { get; set; } = string.Empty;

        [JsonPropertyName("query")]
        public string? Query { get; set; }
    }
}
