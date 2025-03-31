using Microsoft.Extensions.VectorData;

namespace SemanticKernel.Agents.DatabaseAgent
{
    public sealed class TableDefinitionSnippet
    {
        [VectorStoreRecordKey]
        public required string TableName { get; set; }

        [VectorStoreRecordData]
        public string? Definition { get; set; }

        [VectorStoreRecordData]
        public string? Description { get; set; }

        [VectorStoreRecordVector(1536)]
        public ReadOnlyMemory<float> TextEmbedding { get; set; }
    }
}
