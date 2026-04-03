using EmbeddingGemma.DemoApp.Models;
using EmbeddingGemma.DemoApp.Services;
using EmbeddingGemma.SemanticKernel.Enums;
using EmbeddingGemma.SemanticKernel.Options;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using System.Diagnostics;

namespace EmbeddingGemma.DemoApp
{
    public partial class Form1 : Form
    {
        private readonly VectorStoreCollection<Guid, SemanticSearchDataModel> _vectorStoreCollection;
        private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;
        private readonly IBrowserHistoryService _browserHistoryService;

        private readonly List<BrowserHistoryEntry> _browserHistoryEntries;

        public Form1(VectorStore vectorStore, IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator, IBrowserHistoryService browserHistoryService)
        {
            _vectorStoreCollection = vectorStore.GetCollection<Guid, SemanticSearchDataModel>(nameof(_browserHistoryEntries));
            _embeddingGenerator = embeddingGenerator;
            _browserHistoryService = browserHistoryService;
            _browserHistoryEntries = [];

            InitializeComponent();

            this.ExecutionTimeLabel.Text = $"Total items: {_browserHistoryEntries.Count} / Initializing local vector store...";
            this.BrowserCombobox.Enabled = false;
        }

        private async void SearchButton_Click(object sender, EventArgs e)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            var stopwatch = Stopwatch.StartNew();
            long before = GC.GetAllocatedBytesForCurrentThread();

            var results = new List<(BrowserHistoryEntry, double)>();

            var queryAsEmbedding = await _embeddingGenerator.GenerateAsync(
                value: SearchBox.Text,
                options: new EmbeddingGemmaGenerationOptions
                {
                    TaskType = EmbeddingGemmaTaskType.RetrievalQuery
                });

            await foreach (var result in _vectorStoreCollection.SearchAsync(
               searchValue: queryAsEmbedding,
               top: _browserHistoryEntries.Count,
               options: new VectorSearchOptions<SemanticSearchDataModel>
               {
                   IncludeVectors = false, // Exclude the embedding vectors from the search results for better performance.
               }))
            {
                var score = result.Score!.Value;
                results.Add((result.Record.Data, score));
            }

            long after = GC.GetAllocatedBytesForCurrentThread();
            stopwatch.Stop();

            this.ExecutionTimeLabel.Text = $"Execution Time of Semantic Search: {stopwatch.ElapsedMilliseconds / 1000.000D} seconds / Total items: {_browserHistoryEntries.Count} / Consumed Memory: {Math.Round((after - before) / 1024.000000D / 1024.000000D, 3)} MB";

            this.ResultGridView.DataSource = results
                .OrderByDescending(r => r.Item2)
                .Select(r => new
                {
                    Similarity = Math.Round(r.Item2, 3),
                    Tittle = r.Item1.Title,
                    LastVisitDateTime = $"{r.Item1.LastVisitTime.ToLocalTime():t}, {r.Item1.LastVisitTime.ToLocalTime():d}",
                })
                .ToList();
        }

        private async void Form_Load(object sender, EventArgs e)
        {
            var availableBrowserTypes = _browserHistoryService.GetAvailableBrowserTypes();

            BrowserCombobox.Items.Clear();
            foreach (var browserType in availableBrowserTypes)
            {
                BrowserCombobox.Items.Add(browserType);
            }

            BrowserCombobox.SelectedItem = availableBrowserTypes[0];
            await RefreshAsync(availableBrowserTypes[0]);

        }

        private async void BrowserCombobox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (BrowserCombobox.SelectedIndex < 0) { return; }

            var selectedBrowserType = (BrowserType)BrowserCombobox.SelectedItem!;
            //await RefreshAsync(selectedBrowserType);

        }

        private async Task RefreshAsync(BrowserType browserType, int top = 100)
        {
            var browserHistoryEntries = await _browserHistoryService.GetBrowserHistoriesAsync(browserType, DateTime.UtcNow.AddDays(-3), null);

            _browserHistoryEntries.Clear();
            _browserHistoryEntries.AddRange(browserHistoryEntries.Take(top));

            this.ResultGridView.DataSource = _browserHistoryEntries;

            this.ExecutionTimeLabel.Text = $"Total items: {_browserHistoryEntries.Count} / Initializing local vector store...";

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            var stopwatch = Stopwatch.StartNew();
            long before = GC.GetAllocatedBytesForCurrentThread();

            await _vectorStoreCollection.EnsureCollectionDeletedAsync();
            await _vectorStoreCollection.EnsureCollectionExistsAsync();

            var embeddings = await _embeddingGenerator.GenerateAsync(
                _browserHistoryEntries.Select(l => $"Website tittle: {l.Title}"), // Use only the web title for semantic search 
                new EmbeddingGemmaGenerationOptions
                {
                    TaskType = EmbeddingGemmaTaskType.RetrievalDocument
                });

            var semanticRecords = _browserHistoryEntries
                .Zip(embeddings, (data, embedding) => new SemanticSearchDataModel
                {
                    Data = data,
                    Embedding = embedding.Vector,
                })
                .ToList();

            await _vectorStoreCollection.UpsertAsync(semanticRecords);

            long after = GC.GetAllocatedBytesForCurrentThread();
            stopwatch.Stop();

            this.ExecutionTimeLabel.Text = $"Execution Time for Embedding Generation and Upsert: {stopwatch.ElapsedMilliseconds / 1000.000D} seconds / Total items: {_browserHistoryEntries.Count} / Consumed Memory: {Math.Round((after - before) / 1024.000000D / 1024.000000D, 3)} MB";
        }
    }
}
