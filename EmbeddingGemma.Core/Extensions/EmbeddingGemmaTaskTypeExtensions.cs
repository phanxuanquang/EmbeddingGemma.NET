using PhanXuanQuang.AI.LocalEmbeddings.EmbeddingGemma.Attributes;
using PhanXuanQuang.AI.LocalEmbeddings.EmbeddingGemma.Enums;
using System.Collections.Frozen;
using System.Reflection;

namespace PhanXuanQuang.AI.LocalEmbeddings.EmbeddingGemma.Extensions;

public static class EmbeddingGemmaTaskTypeExtensions
{
    private static readonly FrozenDictionary<EmbeddingGemmaTaskType, string> _prefixCache =
        Enum.GetValues<EmbeddingGemmaTaskType>().ToFrozenDictionary(
            t => t,
            t => (typeof(EmbeddingGemmaTaskType).GetField(t.ToString())!
                    .GetCustomAttribute<TaskPrefixAttribute>()
                    ?? throw new InvalidOperationException($"No {nameof(TaskPrefixAttribute)} on {t}.")).Prefix);

    public static string GetPrefix(this EmbeddingGemmaTaskType value)
        => _prefixCache.TryGetValue(value, out var prefix)
            ? prefix
            : throw new InvalidOperationException($"No {nameof(TaskPrefixAttribute)} on {value}.");

    private static readonly string _defaultDocumentPrefix = "title: none | text: ";

    public static string GetDocumentPrefix(string? title = null)
        => string.IsNullOrWhiteSpace(title)
            ? _defaultDocumentPrefix
            : $"title: {title} | text: ";
}