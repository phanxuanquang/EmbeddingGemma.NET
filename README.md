# EmbeddingGemma.NET

![EmbeddingGemma](https://ollama.com/assets/library/embeddinggemma/9a20d963-4bf1-4177-9568-ca5d53a2d14e) 

A .NET library that runs Google's [EmbeddingGemma-300m](https://huggingface.co/google/embeddinggemma-300m) text embedding model **fully offline / locally** via Semantic Kernel.

**What it gives you:**
- Local, private text embeddings (768-dimensional `float32` vectors)
- Drop-in integration with any [Semantic Kernel Vector Store](https://learn.microsoft.com/en-us/semantic-kernel/concepts/vector-store-connectors/) (in-memory, Qdrant, Azure AI Search, etc.)
- Semantic/vector search over your own documents
- Very high-effiency even in low-end devices.

---

## Requirements

| Item | Version |
|------|---------|
| .NET | 10.0+ |
| ONNX model files | see below |

---

## Preparing the ONNX Model

The pre-exported ONNX model is hosted at **[onnx-community/embeddinggemma-300m-ONNX](https://huggingface.co/onnx-community/embeddinggemma-300m-ONNX/tree/main)** on Hugging Face. Just download the files below directly from your browser.

**1. Create a local folder** (e.g. `D:\models\embeddinggemma-onnx\`).

**2. Download these 5 files into that folder:**

| File to download | Source path in the repo | Size |
|-----------------|------------------------|------|
| `model.onnx` | [`onnx/model.onnx`](https://huggingface.co/onnx-community/embeddinggemma-300m-ONNX/resolve/main/onnx/model.onnx?download=true) | ~480 kB |
| `model.onnx_data` | [`onnx/model.onnx_data`](https://huggingface.co/onnx-community/embeddinggemma-300m-ONNX/resolve/main/onnx/model.onnx_data?download=true) | ~1.23 GB |
| `tokenizer.json` | [`tokenizer.json`](https://huggingface.co/onnx-community/embeddinggemma-300m-ONNX/resolve/main/tokenizer.json?download=true) | ~20 MB |
| `tokenizer.model` | [`tokenizer.model`](https://huggingface.co/onnx-community/embeddinggemma-300m-ONNX/resolve/main/tokenizer.model?download=true) | ~4.7 MB |
| `tokenizer_config.json` | [`tokenizer_config.json`](https://huggingface.co/onnx-community/embeddinggemma-300m-ONNX/resolve/main/tokenizer_config.json?download=true) | ~1.2 MB |

> **Tip:** Click the download link in each row, or open the repo page and click the download icon next to the file name.

**3. Your folder should look like this:**

```
embeddinggemma-onnx/
├── model.onnx
├── model.onnx_data
├── tokenizer.json
├── tokenizer.model
└── tokenizer_config.json
```

Both `model.onnx` and `model.onnx_data` **must be in the same folder**, the ONNX runtime loads them together.

---

## Quick Start

```csharp
using EmbeddingGemma.SemanticKernel;
using EmbeddingGemma.SemanticKernel.Enums;
using EmbeddingGemma.SemanticKernel.Extensions;
using EmbeddingGemma.SemanticKernel.Models;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.VectorData;

var host = Host.CreateDefaultBuilder()
    .ConfigureServices(services =>
    {
        // Register an in-memory vector store for quick prototyping.
        // Swap with Qdrant, Azure AI Search, etc. for production.
        services.AddInMemoryVectorStore();

        // Register the embedding generator pointing to your ONNX model folder.
        services.AddGemmaTextEmbeddingGenerator(options =>
        {
            options.ModelDirectory = @"D:\path\to\embeddinggemma-onnx";
        });
    })
    .Build();

var embeddingGenerator = host.Services.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();
var vectorStore = host.Services.GetRequiredService<VectorStore>();

// 1. Create / ensure a collection exists.
var collection = vectorStore.GetCollection<Guid, EmbeddingGemmaSemanticRecord>("my-docs");
await collection.EnsureCollectionExistsAsync();

// 2. Embed and upsert documents.
//    Always prepend the task prefix so the model produces optimised vectors.
var rawDocs = new[]
{
    "Mars is often called the Red Planet.",
    "Venus is Earth's closest planetary neighbour.",
};
var docTexts = rawDocs.Select(d => EmbeddingGemmaTaskType.RetrievalDocument.GetPrefix() + d);
var embeddings = await embeddingGenerator.GenerateAsync(docTexts);

var records = rawDocs
    .Zip(embeddings, (text, emb) => new EmbeddingGemmaSemanticRecord { Text = text, Embedding = emb.Vector })
    .ToList();
await collection.UpsertAsync(records);

// 3. Search.
var queryText = EmbeddingGemmaTaskType.Query.GetPrefix() + "Which planet is the Red Planet?";
var queryEmbedding = await embeddingGenerator.GenerateAsync(queryText);

await foreach (var result in collection.SearchAsync(queryEmbedding, top: 3))
{
    Console.WriteLine($"{result.Score * 100:F1}%  {result.Record.Text}");
}
```

---

## Task Types and Prefixes

EmbeddingGemma is a **task-aware** model — prepending the right prefix to your input **meaningfully improves embedding quality**. Use `EmbeddingGemmaTaskType.GetPrefix()` to get the correct string automatically.

| `EmbeddingGemmaTaskType` | When to use |
|--------------------------|-------------|
| `Query` | A search query entered by a user |
| `RetrievalDocument` | A document to be indexed and searched over |
| `RetrievalQuery` | Alias of `Query` for explicit retrieval scenarios |
| `SentenceSimilarity` / `PairClassification` | Comparing two pieces of text for similarity |
| `Classification` / `MultilabelClassification` | Classifying text into categories |
| `Clustering` | Grouping similar documents |
| `InstructionRetrieval` | Code retrieval using natural language queries |
| `Summarization` | Summarisation-oriented embeddings |
| `BitextMining` | Cross-lingual parallel sentence detection |
| `Document` | Generic document (no specific retrieval task) |

**Rule of thumb:** use `RetrievalDocument` when indexing, `Query` when searching.

---

## Custom Data Model

`EmbeddingGemmaSemanticRecord` is the built-in record class (768-dimension, `Guid` key). If you need extra fields, a different key type, or a different vector dimension, define your own:

```csharp
using Microsoft.Extensions.VectorData;

public class ArticleRecord
{
    [VectorStoreKey]
    public int Id { get; set; }

    [VectorStoreData(IsIndexed = true)]
    public string Title { get; set; } = "";

    [VectorStoreData]
    public string Body { get; set; } = "";

    [VectorStoreVector(768)]          // must match model output (768)
    public ReadOnlyMemory<float> Embedding { get; set; }
}
```

Then use `vectorStore.GetCollection<int, ArticleRecord>("articles")` as normal.

---

## Project Structure

```
EmbeddingGemma.NET/
├── EmbeddingGemma.SemanticKernel/      # The library (add this to your project)
│   ├── DependencyInjection.cs          # AddGemmaTextEmbeddingGenerator() extension
│   ├── Options/
│   │   └── EmbeddingGemmaOptions.cs    # Config: only ModelDirectory is required
│   ├── Services/
│   │   └── GemmaTextEmbeddingGenerationService.cs  # Core ONNX inference logic
│   ├── Models/
│   │   └── EmbeddingGemmaSemanticRecord.cs          # Default vector store record
│   ├── Enums/
│   │   └── EmbeddingGemmaTaskType.cs                # Task type enum
│   ├── Extensions/
│   │   └── EmbeddingGemmaTaskTypeExtensions.cs      # .GetPrefix() helper
│   └── Attributes/
│       └── TaskPrefixAttribute.cs                   # Internal attribute for prefix mapping
│
└── EmbeddingGemma.Demo/                # Runnable demo — planets semantic search
    └── Program.cs
```

---

## References

- [EmbeddingGemma — Google DeepMind](https://deepmind.google/models/gemma/embeddinggemma/)
- [google/embeddinggemma-300m](https://huggingface.co/google/embeddinggemma-300m)
- [onnx-community/embeddinggemma-300m-ONNX](https://huggingface.co/onnx-community/embeddinggemma-300m-ONNX)
- [Microsoft's Semantic Kernel](https://learn.microsoft.com/en-us/semantic-kernel/overview/)
- [Semantic Kernel: Text Embedding Generation](https://learn.microsoft.com/en-us/semantic-kernel/concepts/ai-services/embedding-generation/)
- [Semantic Kernel: Vector Store Connectors](https://learn.microsoft.com/en-us/semantic-kernel/concepts/vector-store-connectors/)
- [Semantic Kernel: Vector Store Text Search (RAG)](https://learn.microsoft.com/en-us/semantic-kernel/concepts/text-search/text-search-vector-stores)
- [Microsoft.Extensions.VectorData — NuGet](https://www.nuget.org/packages/Microsoft.Extensions.VectorData.Abstractions/)
