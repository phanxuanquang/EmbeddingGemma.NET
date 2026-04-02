using EmbeddingGemma.Demo.Models;
using EmbeddingGemma.SemanticKernel;
using EmbeddingGemma.SemanticKernel.Enums;
using EmbeddingGemma.SemanticKernel.Options;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;

namespace EmbeddingGemma.Demo
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            const string collectionName = "planets";

            // For demo purposes, we assume the model files are located in a folder named ".embedding_resources" at the solution level.
            var modelDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".embedding_resources"));

            var host = Host.CreateDefaultBuilder(args)
                 .ConfigureServices(services =>
                 {
                     // Use the in-memory vector store for quick demo purposes. 
                     services.AddInMemoryVectorStore();

                     services.AddGemmaTextEmbeddingGenerator(options =>
                     {
                         options.ModelDirectory = modelDir;
                     });
                 })
                 .Build();

            Console.WriteLine("Init services.");

            var vectorStore = host.Services.GetRequiredService<VectorStore>();

            var embeddingGenerator = host.Services.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();

            var collection = vectorStore.GetCollection<Guid, SemanticSearchDataModel>(collectionName);
            await collection.EnsureCollectionExistsAsync();

            var rawQuery = "Which planet is known as the Red Planet?";
            var rawDocuments = new[]
            {
                "Venus is often called Earth's twin because of its similar size and proximity.",
                "Mars, known for its reddish appearance, is often referred to as the Red Planet.",
                "Jupiter, the largest planet in our solar system, has a prominent red spot.",
                "Saturn, famous for its rings, is sometimes mistaken for the Red Planet."
            };
            Console.WriteLine($"Query: {rawQuery}");

            var embeddings = await embeddingGenerator.GenerateAsync(rawDocuments, new EmbeddingGemmaGenerationOptions
            {
                TaskType = EmbeddingGemmaTaskType.RetrievalDocument
            });

            var semanticRecords = rawDocuments
                .Zip(embeddings, (text, emb) => new SemanticSearchDataModel
                {
                    Text = text,
                    Embedding = emb.Vector,
                })
                .ToList();

            await collection.UpsertAsync(semanticRecords);

            var queryAsEmbedding = await embeddingGenerator.GenerateAsync(
                value: rawQuery,
                options: new EmbeddingGemmaGenerationOptions
                {
                    TaskType = EmbeddingGemmaTaskType.RetrievalQuery
                });

            await foreach (var result in collection.SearchAsync(
                searchValue: queryAsEmbedding,
                top: 10,
                options: new VectorSearchOptions<SemanticSearchDataModel>
                {
                    IncludeVectors = false, // Exclude the embedding vectors from the search results for better performance.
                }))
            {
                var score = result.Score!.Value;
                Console.WriteLine($"    {score * 100:F2}%: {result.Record.Text}");
            }

            await host.StopAsync();
        }
    }
}
