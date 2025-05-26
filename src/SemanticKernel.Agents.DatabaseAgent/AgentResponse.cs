

using System.Text.Json.Serialization;

namespace SemanticKernel.Agents.DatabaseAgent
{
    internal class AgentResponse
    {
        [JsonPropertyName("thinking")]
        public string Tinking { get; set; } = string.Empty;

        [JsonPropertyName("answer")]
        public string Answer { get; set; } = string.Empty;
    }
}
