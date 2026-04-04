namespace phanxuanquang.SemanticKernel.Connectors.Onnx.Gemma.Attributes;

internal class TaskPrefixAttribute(string prefix) : Attribute
{
    internal string Prefix { get; } = prefix;
}