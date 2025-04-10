using Microsoft.Extensions.VectorData;

namespace SemanticKernel.Agents.DatabaseAgent
{
    public sealed class TableDefinitionSnippet
    {
        [VectorStoreRecordData]
        public required string TableName { get; set; }

        [VectorStoreRecordKey]
        public required Guid Key { get; set; }

        [VectorStoreRecordData]
        public string? Definition { get; set; }

        [VectorStoreRecordData]
        public string? Description { get; set; }

        [VectorStoreRecordData]
        public string? SampleData { get; set; }

        [VectorStoreRecordVector]
        public ReadOnlyMemory<float> TextEmbedding { get; set; }
    }
}
