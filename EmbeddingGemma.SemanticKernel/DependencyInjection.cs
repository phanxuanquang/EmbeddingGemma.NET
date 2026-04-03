
using EmbeddingGemma.Core;
using EmbeddingGemma.Core.Options;
using Microsoft.SemanticKernel;

namespace EmbeddingGemma.SemanticKernel;

public static class DependencyInjection
{
    public static IKernelBuilder AddGemmaTextEmbeddingGenerator(this IKernelBuilder kernelBuilder, Action<EmbeddingGemmaOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(kernelBuilder);

        kernelBuilder.Services.AddGemmaTextEmbeddingGenerator(configure);
        return kernelBuilder;
    }
}
