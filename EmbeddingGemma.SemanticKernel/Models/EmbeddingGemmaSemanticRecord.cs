using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Data;

namespace EmbeddingGemma.SemanticKernel.Models
{
    /// <summary>
    /// Default vector store data model for Gemma embeddings.
    /// Maps directly to a <see cref="Microsoft.SemanticKernel.Data.TextSearchResult"/>
    /// using the built-in Semantic Kernel text search attributes.
    ///
    /// Embedding dimension: 768 (matches the gemma-embedding model in this repo).
    ///
    /// If you need a different key type, a different embedding dimension, or extra
    /// filterable fields, define your own record class and use
    /// <c>AddGemmaVectorStoreTextSearch&lt;TKey, TRecord&gt;()</c> instead.
    /// </summary>
    public sealed class EmbeddingGemmaSemanticRecord
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
