using Microsoft.Extensions.VectorData;

namespace SemanticKernel.Agents.DatabaseAgent
{
    public sealed class AgentDefinitionSnippet
    {
        [VectorStoreRecordData]
        public required string AgentName { get; set; }

        [VectorStoreRecordKey]
        public required Guid Key { get; set; }

        [VectorStoreRecordData]
        public string Description { get; set; }

        [VectorStoreRecordData]
        public string Instructions { get; set; }

        [VectorStoreRecordVector]
        public ReadOnlyMemory<float> TextEmbedding { get; set; }
    }
}
