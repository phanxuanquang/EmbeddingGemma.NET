using Microsoft.Extensions.AI;
using PhanXuanQuang.AI.LocalEmbeddings.EmbeddingGemma.Enums;

namespace PhanXuanQuang.AI.LocalEmbeddings.EmbeddingGemma.Services.Options;

/// <summary>
/// EmbeddingGemma-specific generation options that extend
/// <see cref="EmbeddingGenerationOptions"/> with task-type prompting support.
/// When <see cref="TaskType"/> is set, the service automatically prepends the
/// appropriate prompt prefix to every input value before running inference,
/// so callers pass raw text rather than manually formatting prompts.
/// </summary>
public sealed class EmbeddingGemmaEmbeddingGenerationOptions : EmbeddingGenerationOptions
{
    /// <summary>
    /// The task type whose prompt prefix is automatically prepended to every
    /// input value before inference. When <see langword="null"/>, no prefix is
    /// added and the caller is responsible for any prompt formatting.
    /// </summary>
    public EmbeddingGemmaTaskType? TaskType { get; init; }

    /// <summary>
    /// Optional document title used when <see cref="TaskType"/> is
    /// <see cref="EmbeddingGemmaTaskType.Document"/> or
    /// <see cref="EmbeddingGemmaTaskType.RetrievalDocument"/>.
    /// When set, produces <c>title: {DocumentTitle} | text: </c> instead of
    /// the default <c>title: none | text: </c>, which improves retrieval quality.
    /// </summary>
    public string? DocumentTitle { get; init; }
}
