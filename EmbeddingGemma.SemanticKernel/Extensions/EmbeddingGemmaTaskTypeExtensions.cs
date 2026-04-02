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
    }
}
