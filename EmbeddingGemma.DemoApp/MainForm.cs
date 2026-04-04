using EmbeddingGemma.DemoApp.Models;
using EmbeddingGemma.DemoApp.Services;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using phanxuanquang.SemanticKernel.Connectors.Onnx.Gemma.Enums;
using phanxuanquang.SemanticKernel.Connectors.Onnx.Gemma.Services.Options;
using System.Diagnostics;

namespace EmbeddingGemma.DemoApp
{
    public partial class MainForm : Form
    {
        private readonly VectorStoreCollection<Guid, SemanticSearchDataModel> _vectorStoreCollection;
        private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;
        private readonly IBrowserHistoryService _browserHistoryService;

        private readonly List<BrowserHistoryEntry> _browserHistoryEntries;

        public MainForm(VectorStore vectorStore, IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator, IBrowserHistoryService browserHistoryService)
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
            if (string.IsNullOrWhiteSpace(SearchBox.Text)) { return; }

            SearchBox.Enabled = SearchButton.Enabled = false;

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            var stopwatch = Stopwatch.StartNew();
            long before = GC.GetAllocatedBytesForCurrentThread();

            var results = new List<(BrowserHistoryEntry, double)>();

            var queryAsEmbedding = await _embeddingGenerator.GenerateAsync(
                value: SearchBox.Text,
                options: new EmbeddingGemmaEmbeddingGenerationOptions
                {
                    TaskType = EmbeddingGemmaTaskType.RetrievalQuery
                });

            await foreach (var result in _vectorStoreCollection.SearchAsync(
               searchValue: queryAsEmbedding,
               top: (int)(_browserHistoryEntries.Count * 0.2),
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
                    Similarity = $"{Math.Round(r.Item2 * 100, 2)}%",
                    Tittle = r.Item1.Title,
                    LastVisitDateTime = $"{r.Item1.LastVisitTime.ToLocalTime():t}, {r.Item1.LastVisitTime.ToLocalTime():d}",
                })
                .ToList();

            SearchBox.Enabled = SearchButton.Enabled = true;
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
            await RefreshAsync(availableBrowserTypes[0], 3000);

        }

        private async void BrowserCombobox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (BrowserCombobox.SelectedIndex < 0) { return; }

            var selectedBrowserType = (BrowserType)BrowserCombobox.SelectedItem!;
            //await RefreshAsync(selectedBrowserType);

        }

        private async Task RefreshAsync(BrowserType browserType, int top = 100)
        {
            SearchBox.Enabled = SearchButton.Enabled = false;

            var browserHistoryEntries = (await _browserHistoryService.GetBrowserHistoriesAsync(browserType, DateTime.UtcNow.AddDays(-7), null))
                .Where(e => !string.IsNullOrWhiteSpace(e.Title))
                .OrderByDescending(e => e.LastVisitTime)
                .Take(top)
                .ToList();

            //var browserHistoryEntries = new List<string>
            //{
            //    "Venus is often called Earth's twin because of its similar size and proximity.",
            //    "Mars, known for its reddish appearance, is often referred to as the Red Planet.",
            //    "Jupiter, the largest planet in our solar system, has a prominent red spot.",
            //    "Saturn, famous for its rings, is sometimes mistaken for the Red Planet.",
            //    "Mercury, the closest planet to the Sun, has a cratered surface.",
            //    "Neptune, the farthest planet from the Sun, has a deep blue color.",
            //    "Uranus, an ice giant, rotates on its side and has a pale blue color.",
            //    "Pluto, once considered the ninth planet, is now classified as a dwarf planet.",
            //    "The Sun is the star at the center of our solar system and provides light and heat to the planets.",
            //    "Earth is the only planet known to support life, with a diverse range of ecosystems and climates.",
            //    "Venus has a thick atmosphere that traps heat, making it the hottest planet in our solar system.",
            //    "Mars has the largest volcano in the solar system, Olympus Mons, which is about three times the height of Mount Everest.",
            //}
            //.Select((title, index) => new BrowserHistoryEntry
            //{
            //    Title = title,
            //    Url = $"https://example.com/{title.Replace(" ", "").ToLower()}",
            //    LastVisitTime = DateTime.UtcNow.AddMinutes(-index * 10)
            //})
            //.ToList();

            _browserHistoryEntries.Clear();
            _browserHistoryEntries.AddRange(browserHistoryEntries);

            this.ResultGridView.DataSource = _browserHistoryEntries;

            this.ExecutionTimeLabel.Text = $"Total items: {_browserHistoryEntries.Count} / Initializing local vector store...";

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            var stopwatch = Stopwatch.StartNew();
            long before = GC.GetAllocatedBytesForCurrentThread();

            await _vectorStoreCollection.EnsureCollectionDeletedAsync();
            await _vectorStoreCollection.EnsureCollectionExistsAsync();

            var semanticRecords = new List<SemanticSearchDataModel>();

            for (var i = 0; i < browserHistoryEntries.Count; i++)
            {
                var entry = browserHistoryEntries[i];
                this.ExecutionTimeLabel.Text = $"Generating embedding for {i + 1}/{browserHistoryEntries.Count} entries...";

                var embedding = await _embeddingGenerator.GenerateAsync(
                    value: $"Website title: {entry.Title}", // Use only the web title for semantic search 
                    options: new EmbeddingGemmaEmbeddingGenerationOptions
                    {
                        TaskType = EmbeddingGemmaTaskType.RetrievalDocument
                    });

                semanticRecords.Add(new SemanticSearchDataModel
                {
                    Data = entry,
                    Embedding = embedding.Vector,
                });
            }

            await _vectorStoreCollection.UpsertAsync(semanticRecords);

            long after = GC.GetAllocatedBytesForCurrentThread();
            stopwatch.Stop();

            this.ExecutionTimeLabel.Text = $"Execution Time for Embedding Generation and Upsert: {stopwatch.ElapsedMilliseconds / 1000.000D} seconds / Total items: {_browserHistoryEntries.Count} / Consumed Memory: {Math.Round((after - before) / 1024.000000D / 1024.000000D, 3)} MB";

            SearchBox.Enabled = SearchButton.Enabled = true;
        }

        private void SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                SearchButton_Click(sender, e);
                e.SuppressKeyPress = true;
            }
        }
    }
}
