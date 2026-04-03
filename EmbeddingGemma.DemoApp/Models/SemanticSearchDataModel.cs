using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Data;

namespace EmbeddingGemma.DemoApp.Models
{
    public sealed record SemanticSearchDataModel
    {
        [VectorStoreKey]
        [TextSearchResultName]
        public Guid Key { get; init; } = Guid.NewGuid();

        [VectorStoreData]
        [TextSearchResultValue]
        public required string Text { get; init; }

        [VectorStoreVector(768)]
        public ReadOnlyMemory<float> Embedding { get; init; }
    }
}
