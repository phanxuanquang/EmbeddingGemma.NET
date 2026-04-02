using EmbeddingGemma.SemanticKernel.Attributes;
using EmbeddingGemma.SemanticKernel.Enums;
using System.Reflection;

namespace EmbeddingGemma.SemanticKernel.Extensions
{
    public static class EmbeddingGemmaTaskTypeExtensions
    {
        public static string GetPrefix(this EmbeddingGemmaTaskType value)
        {
            var member = typeof(EmbeddingGemmaTaskType).GetField(value.ToString())!;
            var attribute = member.GetCustomAttribute<TaskPrefixAttribute>()
                ?? throw new InvalidOperationException($"No {nameof(TaskPrefixAttribute)} on {value}.");

            return attribute.Prefix;
        }

        /// <summary>
        /// Builds the document prompt prefix <c>title: {title} | text: </c>.
        /// Use this when you have a real document title to include, which improves
        /// retrieval quality over the default <c>title: none | text: </c>.
        /// </summary>
        /// <param name="title">
        /// The document title. Pass <see langword="null"/> or empty to fall back to "none".
        /// </param>
        public static string GetDocumentPrefix(string? title = null)
            => $"title: {(string.IsNullOrWhiteSpace(title) ? "none" : title)} | text: ";
    }
}
