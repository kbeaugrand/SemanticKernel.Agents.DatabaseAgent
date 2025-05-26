using Microsoft.Extensions.VectorData;

namespace SemanticKernel.Agents.DatabaseAgent
{
    public sealed class TableDefinitionSnippet
    {
        public required string TableName { get; set; }

        public required Guid Key { get; set; }

        public string? Definition { get; set; }

        public string? Description { get; set; }

        public string? SampleData { get; set; }

        public ReadOnlyMemory<float> TextEmbedding { get; set; }
    }
}
