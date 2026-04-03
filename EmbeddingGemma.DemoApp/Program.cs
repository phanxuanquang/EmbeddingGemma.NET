using EmbeddingGemma.DemoApp.Services;
using EmbeddingGemma.SemanticKernel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.SemanticKernel;

namespace EmbeddingGemma.DemoApp
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            // For demo purposes, let's assume the model files are located in a folder named ".embedding_resources" at the solution level.
            var modelDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".embedding_resources"));

            var host = Host.CreateDefaultBuilder()
                .ConfigureServices(services =>
                {
                    // Use the in-memory vector store for quick demo purposes.
                    services.AddInMemoryVectorStore();

                    services.AddGemmaTextEmbeddingGenerator(options =>
                    {
                        options.ModelDirectory = modelDir;
                    });

                    services.AddScoped<IBrowserHistoryService, BrowserHistoryService>();

                    services.AddTransient<Form1>();
                })
                .Build();

            ApplicationConfiguration.Initialize();
            Application.Run(host.Services.GetRequiredService<Form1>());
        }
    }
}