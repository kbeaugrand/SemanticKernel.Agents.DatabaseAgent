using System.Text.Json.Serialization;

namespace SemanticKernel.Agents.DatabaseAgent.Tests
{
    internal class Evaluation
    {
        [JsonPropertyName("score")]
        public float Score { get; set; }
    }
}