namespace EmbeddingGemma.Core.Options;

/// <summary>
/// The options used to configure <see cref="GemmaTextEmbeddingGenerationService"/>. Currently
/// </summary>
public class EmbeddingGemmaOptions
{
    /// <summary>
    /// The directory where the model files are stored. This is required and must be set by the caller.
    /// </summary>
    public required string ModelDirectory { get; set; }
}
