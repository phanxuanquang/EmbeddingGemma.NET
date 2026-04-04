using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;

namespace phanxuanquang.SemanticKernel.Connectors.Onnx.Gemma;

public static class DependencyInjection
{
    /// <summary>
    /// Registers <see cref="EmbeddingGemmaOnnxTextEmbeddingGenerationService"/> as a keyed singleton.
    /// <see cref="IEmbeddingGenerator{TInput,TEmbedding}"/> using the provided configuration delegate.
    /// </summary>
    /// <param name="services">The service collection to add to.</param>
    /// <param name="configure">Delegate that sets <see cref="EmbeddingGemmaOnnxOptions.ModelDirectory"/>.</param>
    public static IServiceCollection AddGemmaOnnxEmbeddingGenerator(this IServiceCollection services, Action<EmbeddingGemmaOnnxOptions> configure, string? serviceId = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.Configure(configure);
        services.AddKeyedSingleton<IEmbeddingGenerator<string, Embedding<float>>, EmbeddingGemmaOnnxTextEmbeddingGenerationService>(serviceId, (serviceProvider, _) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<EmbeddingGemmaOnnxOptions>>();
            return new EmbeddingGemmaOnnxTextEmbeddingGenerationService(options);
        });

        return services;
    }

    /// <summary>
    /// Registers <see cref="EmbeddingGemmaOnnxTextEmbeddingGenerationService"/> as a keyed singleton.
    /// </summary>
    /// <param name="builder">The kernel builder to add the service to.</param>
    /// <param name="configure">Delegate that sets <see cref="EmbeddingGemmaOnnxOptions.ModelDirectory"/>.</param>
    /// <param name="serviceId">Optional service ID for keyed registration.</param>
    public static IKernelBuilder AddGemmaOnnxEmbeddingGenerator(this IKernelBuilder builder, Action<EmbeddingGemmaOnnxOptions> configure, string? serviceId = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        builder.Services.AddGemmaOnnxEmbeddingGenerator(configure, serviceId);
        return builder;
    }
}
