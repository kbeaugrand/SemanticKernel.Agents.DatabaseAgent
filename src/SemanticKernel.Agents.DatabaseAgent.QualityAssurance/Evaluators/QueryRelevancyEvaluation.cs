using System.Text.Json.Serialization;

namespace SemanticKernel.Agents.DatabaseAgent.QualityAssurance.Evaluators
{
    internal class QueryRelevancyEvaluation
    {
        [JsonPropertyName("reasoning")]
        public string[] Reasoning { get; set; } = [];

        [JsonPropertyName("questions")]
        public string[] Questions { get; set; } = [];
    }
}
