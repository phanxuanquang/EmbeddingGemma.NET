namespace EmbeddingGemma.SemanticKernel.Attributes
{
    internal class TaskPrefixAttribute(string prefix) : Attribute
    {
        internal string Prefix { get; } = prefix;
    }
}
