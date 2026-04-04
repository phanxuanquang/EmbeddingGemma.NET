namespace PhanXuanQuang.AI.LocalEmbeddings.EmbeddingGemma.Attributes;

internal class TaskPrefixAttribute(string prefix) : Attribute
{
    internal string Prefix { get; } = prefix;
}