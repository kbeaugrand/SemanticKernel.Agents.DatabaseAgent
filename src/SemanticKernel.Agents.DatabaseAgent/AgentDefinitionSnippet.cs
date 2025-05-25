using Microsoft.Extensions.VectorData;

namespace SemanticKernel.Agents.DatabaseAgent
{
    public sealed class AgentDefinitionSnippet
    {
        public required string AgentName { get; set; }

        public required Guid Key { get; set; }

        public string Description { get; set; }

        public string Instructions { get; set; }

        public ReadOnlyMemory<float> TextEmbedding { get; set; }
    }
}
