using EmbeddingGemma.Core.Options;
using EmbeddingGemma.Core.Services;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace EmbeddingGemma.Core;

public static class DependencyInjection
{
    /// <summary>
    /// Registers <see cref="GemmaTextEmbeddingGenerationService"/> as a singleton
    /// <see cref="IEmbeddingGenerator{TInput,TEmbedding}"/> using the provided configuration delegate.
    /// </summary>
    /// <param name="services">The service collection to add to.</param>
    /// <param name="configure">Delegate that sets <see cref="EmbeddingGemmaOptions.ModelDirectory"/>.</param>
    public static IServiceCollection AddGemmaTextEmbeddingGenerator(this IServiceCollection services, Action<EmbeddingGemmaOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.Configure(configure);
        services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>, GemmaTextEmbeddingGenerationService>();

        return services;
    }
}
