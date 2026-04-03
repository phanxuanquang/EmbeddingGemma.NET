using EmbeddingGemma.DemoApp.Helpers;
using EmbeddingGemma.DemoApp.Models;
using EmbeddingGemma.SemanticKernel.Enums;
using EmbeddingGemma.SemanticKernel.Options;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using System.Diagnostics;

namespace EmbeddingGemma.DemoApp
{
    public partial class Form1 : Form
    {
        private readonly VectorStoreCollection<int, SemanticSearchDataModel> _vectorStoreCollection;
        private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;

        private readonly List<WindowsEventLog> _windowsLogs;

        public Form1(VectorStore vectorStore, IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator)
        {
            _vectorStoreCollection = vectorStore.GetCollection<int, SemanticSearchDataModel>("Example");
            _embeddingGenerator = embeddingGenerator;
            _windowsLogs = [.. WindowsEventLogHelper.ReadApplicationLogs(maxRecords: 3000, from: DateTime.Now - TimeSpan.FromDays(7))];

            InitializeComponent();

            this.ExecutionTimeLabel.Text = $"Total items: {_windowsLogs.Count} / Initializing local vector store...";
        }

        private async void SearchButton_Click(object sender, EventArgs e)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            var stopwatch = Stopwatch.StartNew();
            long before = GC.GetAllocatedBytesForCurrentThread();

            var results = new List<(SemanticSearchDataModel, double)>();

            var queryAsEmbedding = await _embeddingGenerator.GenerateAsync(
                value: SearchBox.Text,
                options: new EmbeddingGemmaGenerationOptions
                {
                    TaskType = EmbeddingGemmaTaskType.RetrievalQuery
                });

            await foreach (var result in _vectorStoreCollection.SearchAsync(
               searchValue: queryAsEmbedding,
               top: 30,
               options: new VectorSearchOptions<SemanticSearchDataModel>
               {
                   IncludeVectors = false, // Exclude the embedding vectors from the search results for better performance.
               }))
            {
                var score = result.Score!.Value;
                results.Add((result.Record, score));
            }

            long after = GC.GetAllocatedBytesForCurrentThread();
            stopwatch.Stop();

            this.ExecutionTimeLabel.Text = $"Execution Time of Semantic Search: {stopwatch.ElapsedMilliseconds / 1000.000D} seconds / Total items: {_windowsLogs.Count} / Consumed Memory: {Math.Round((after - before) / 1024.000000D / 1024.000000D, 3)} MB";

            this.ResultGridView.DataSource = results
                .OrderByDescending(r => r.Item2)
                .ThenByDescending(r => r.Item1.Data.Timestamp)
                .Select(r => new
                {
                    Similarity = Math.Round(r.Item2, 3),
                    Message = r.Item1.Data.LogMessage,
                    LogLevel = r.Item1.Data.LogLevel,
                    Timestamp = r.Item1.Data.Timestamp,
                })
                .ToList();
        }

        private async void Form_Load(object sender, EventArgs e)
        {
            this.ResultGridView.DataSource = _windowsLogs;

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            var stopwatch = Stopwatch.StartNew();
            long before = GC.GetAllocatedBytesForCurrentThread();

            await _vectorStoreCollection.EnsureCollectionExistsAsync();

            var embeddings = await _embeddingGenerator.GenerateAsync(
                _windowsLogs.Select(l => $"From [{l.Source}] source with log level [{l.LogLevel}]: {l.LogMessage}"), // Use only the log message for semantic search 
                new EmbeddingGemmaGenerationOptions
                {
                    TaskType = EmbeddingGemmaTaskType.RetrievalDocument
                });

            var semanticRecords = _windowsLogs
                .Zip(embeddings, (data, embedding) => new SemanticSearchDataModel
                {
                    Data = data,
                    Embedding = embedding.Vector,
                })
                .ToList();

            await _vectorStoreCollection.UpsertAsync(semanticRecords);

            long after = GC.GetAllocatedBytesForCurrentThread();
            stopwatch.Stop();

            this.ExecutionTimeLabel.Text = $"Execution Time for Embedding Generation and Upsert: {stopwatch.ElapsedMilliseconds / 1000.000D} seconds / Total items: {_windowsLogs.Count} / Consumed Memory: {Math.Round((after - before) / 1024.000000D / 1024.000000D, 3)} MB";
        }
    }
}
