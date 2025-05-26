using System.Text.Json.Serialization;

namespace SemanticKernel.Agents.DatabaseAgent.QualityAssurance.Evaluators
{
    internal class QueryRelevancyEvaluation
    {
        [JsonPropertyName("reasoning")]
        public string Reasoning { get; set; } = string.Empty;

        [JsonPropertyName("question")]
        public string Question { get; set; } = string.Empty;
    }
}
