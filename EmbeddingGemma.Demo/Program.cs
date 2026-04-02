using EmbeddingGemma.SemanticKernel.Enums;
using EmbeddingGemma.SemanticKernel.Extensions;
using EmbeddingGemma.SemanticKernel.Models;
using EmbeddingGemma.SemanticKernel.Services;
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
            const string modelDir = @"D:\Work\New folder\tokenizer-to-onnx-model\GemmaEmbedding\model";

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
                 })
                 .Build();

            var logger = host.Services.GetRequiredService<ILogger<Program>>();

            logger.LogInformation("Init services:");

            logger.LogTrace("    Vector Store: {VectorStoreType}", host.Services.GetRequiredService<VectorStore>().GetType().Name);
            var vectorStore = host.Services.GetRequiredService<VectorStore>();

            logger.LogInformation("Initializing GemmaTextEmbeddingGenerationService with model directory: {ModelDir}", modelDir);
            using var embeddingGenerator = new GemmaTextEmbeddingGenerationService(modelDir);

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
            var query = EmbeddingGemmaTaskType.Query.GetPrefix() + rawQuery;
            var documents = rawDocuments.Select(d => EmbeddingGemmaTaskType.RetrievalDocument.GetPrefix() + d);
            var embeddings = await embeddingGenerator.GenerateAsync(documents);
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
            var queryAsEmbedding = await embeddingGenerator.GenerateAsync(query);
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
