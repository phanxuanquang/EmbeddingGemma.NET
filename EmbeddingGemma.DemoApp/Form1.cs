using EmbeddingGemma.DemoApp.Models;
using EmbeddingGemma.SemanticKernel.Enums;
using EmbeddingGemma.SemanticKernel.Options;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;

namespace EmbeddingGemma.DemoApp
{
    public partial class Form1 : Form
    {
        private readonly VectorStoreCollection<Guid, SemanticSearchDataModel> _vectorStoreCollection;
        private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;

        private readonly List<string> _sampleValues;
        private const string VectorCollectionName = "Example";

        public Form1(VectorStore vectorStore, IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator)
        {
            _vectorStoreCollection = vectorStore.GetCollection<Guid, SemanticSearchDataModel>(VectorCollectionName);
            _embeddingGenerator = embeddingGenerator;
            _sampleValues =
            [
                "Venus is often called Earth's twin because of its similar size and proximity.",
                "Mars, known for its reddish appearance, is often referred to as the Red Planet.",
                "Jupiter, the largest planet in our solar system, has a prominent red spot.",
                "Saturn, famous for its rings, is sometimes mistaken for the Red Planet."
            ];

            InitializeComponent();
        }

        private async void SearchButton_Click(object sender, EventArgs e)
        {
            var results = new Dictionary<double, SemanticSearchDataModel>();

            var queryAsEmbedding = await _embeddingGenerator.GenerateAsync(
                value: SearchBox.Text,
                options: new EmbeddingGemmaGenerationOptions
                {
                    TaskType = EmbeddingGemmaTaskType.RetrievalQuery
                });

            await foreach (var result in _vectorStoreCollection.SearchAsync(
               searchValue: queryAsEmbedding,
               top: 10,
               options: new VectorSearchOptions<SemanticSearchDataModel>
               {
                   IncludeVectors = false, // Exclude the embedding vectors from the search results for better performance.
               }))
            {
                var score = result.Score!.Value;
                results.Add(score, result.Record);
            }

            this.ResultGridView.DataSource = results
                .Select(r => new
                {
                    Similarity = Math.Round(r.Key, 3),
                    Text = r.Value.Text,
                })
                .ToList();
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            this.ResultGridView.DataSource = _sampleValues.Select(v => new { Text = v }).ToList();

            await _vectorStoreCollection.EnsureCollectionExistsAsync();

            var embeddings = await _embeddingGenerator.GenerateAsync(_sampleValues, new EmbeddingGemmaGenerationOptions
            {
                TaskType = EmbeddingGemmaTaskType.RetrievalDocument
            });

            var semanticRecords = _sampleValues
                .Zip(embeddings, (text, emb) => new SemanticSearchDataModel
                {
                    Text = text,
                    Embedding = emb.Vector,
                })
                .ToList();

            await _vectorStoreCollection.UpsertAsync(semanticRecords);
        }
    }
}
