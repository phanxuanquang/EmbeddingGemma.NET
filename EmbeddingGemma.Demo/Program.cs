using EmbeddingGemma.SemanticKernel;
using EmbeddingGemma.SemanticKernel.Enums;
using EmbeddingGemma.SemanticKernel.Models;
using EmbeddingGemma.SemanticKernel.Options;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
                     services.AddLogging(services =>
                     {
                         services.AddConsole(); // Log to console for demo purposes.
                         services.SetMinimumLevel(LogLevel.Trace);
                     });

                     // Use the default in-memory vector store for demo purposes. 
                     services.AddInMemoryVectorStore();

                     services.AddGemmaTextEmbeddingGenerator(options =>
                     {
                         options.ModelDirectory = modelDir;
                     });
                 })
                 .Build();

            var logger = host.Services.GetRequiredService<ILogger<Program>>();

            logger.LogInformation("Init services:");

            logger.LogTrace("    Vector Store: {VectorStoreType}", host.Services.GetRequiredService<VectorStore>().GetType().Name);
            var vectorStore = host.Services.GetRequiredService<VectorStore>();

            logger.LogInformation("Initializing GemmaTextEmbeddingGenerationService with model directory: {ModelDir}", modelDir);
            var embeddingGenerator = host.Services.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();

            logger.LogInformation("Ensuring collection '{CollectionName}' exists in the vector store.", collectionName);
            var collection = vectorStore.GetCollection<Guid, EmbeddingGemmaSemanticRecord>(collectionName);
            await collection.EnsureCollectionExistsAsync();

            var rawQuery = "Which planet is known as the Red Planet?";
            var rawDocuments = new[]
            {
                "Venus is often called Earth's twin because of its similar size and proximity.",
                "Mars, known for its reddish appearance, is often referred to as the Red Planet.",
                "Jupiter, the largest planet in our solar system, has a prominent red spot.",
                "Saturn, famous for its rings, is sometimes mistaken for the Red Planet."
            };
            logger.LogInformation("Raw Query: {RawQuery}", rawQuery);

            logger.LogInformation("Generating embeddings for query and documents...");
            var embeddings = await embeddingGenerator.GenerateAsync(rawDocuments, new EmbeddingGemmaGenerationOptions
            {
                TaskType = EmbeddingGemmaTaskType.RetrievalDocument
            });
            var semanticRecords = rawDocuments
                .Zip(embeddings, (text, emb) => new EmbeddingGemmaSemanticRecord
                {
                    Text = text,
                    Embedding = emb.Vector,
                })
                .ToList();

            logger.LogInformation("Upserting {Count} records into the collection '{CollectionName}'...", semanticRecords.Count, collectionName);
            await collection.UpsertAsync(semanticRecords);

            logger.LogInformation("Performing vector search for the query...");
            var queryAsEmbedding = await embeddingGenerator.GenerateAsync(rawQuery, new EmbeddingGemmaGenerationOptions
            {
                TaskType = EmbeddingGemmaTaskType.RetrievalQuery
            });
            var results = new List<(EmbeddingGemmaSemanticRecord, double)>();

            await foreach (var result in collection.SearchAsync(
                searchValue: queryAsEmbedding,
                top: 10,
                options: new VectorSearchOptions<EmbeddingGemmaSemanticRecord>
                {
                    IncludeVectors = false,
                }))
            {
                var score = result.Score!.Value;
                logger.LogInformation($"    {score * 100:F2}%: {result.Record.Text}");
            }

            await host.StopAsync();
        }
    }
}
