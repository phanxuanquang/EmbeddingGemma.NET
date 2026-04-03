using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Data;

namespace EmbeddingGemma.DemoApp.Models
{
    public sealed record SemanticSearchDataModel
    {
        [VectorStoreKey]
        [TextSearchResultName]
        public int Key { get; init; }

        [VectorStoreData]
        [TextSearchResultValue]
        public required WindowsEventLog Data { get; init; }

        [VectorStoreVector(768)]
        public ReadOnlyMemory<float> Embedding { get; init; } // The embedding vector for this record, which will be generated and stored in the vector store.
    }
}
