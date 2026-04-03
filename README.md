# EmbeddingGemma.NET

![EmbeddingGemma](https://ollama.com/assets/library/embeddinggemma/9a20d963-4bf1-4177-9568-ca5d53a2d14e)

**Run Google DeepMind's EmbeddingGemma-300m embedding model fully locally in your .NET app — zero API cost, zero data leakage, zero cloud dependency.**

EmbeddingGemma.NET provides two NuGet packages that plug local semantic search / vector search into any .NET 10 application, with first-class support for Microsoft Semantic Kernel and `Microsoft.Extensions.VectorData`.

---

## Why EmbeddingGemma?

| | |
|---|---|
| **Zero runtime cost** | Runs on CPU via ONNX Runtime — no OpenAI/Azure credits consumed |
| **Privacy-first** | Your data never leaves the machine |
| **State-of-the-art accuracy** | [#1 open multilingual embedding model under 500 M parameters on MTEB](https://deepmind.google/models/gemma/embeddinggemma/#performance) |
| **100+ languages compatibility** | Multilingual out-of-the-box |
| **Instructed embeddings** | Pre-defined prompt prefixing for 15 task types (e.g., retrieval, QA, classification, clustering) |

---

## Packages

| Package | Use when… |
|---|---|
| `EmbeddingGemma.Core` | Building a plain .NET / ASP.NET Core app |
| `EmbeddingGemma.SemanticKernel` | Using Microsoft Semantic Kernel |

> `EmbeddingGemma.SemanticKernel` already wraps Core. If you use Semantic Kernel, you only need that one package.

---

## Installation

### Prepare the ONNX model and tokenizer files.

The pre-exported ONNX model is hosted at **[onnx-community/embeddinggemma-300m-ONNX](https://huggingface.co/onnx-community/embeddinggemma-300m-ONNX/tree/main)** on Hugging Face. To run the model locally, you need to download the ONNX weights and tokenizer files as listed below:

| File to download | Source path in the repo | Size |
|-----------------|------------------------|------|
| `model.onnx` | [`onnx/model.onnx`](https://huggingface.co/onnx-community/embeddinggemma-300m-ONNX/resolve/main/onnx/model.onnx?download=true) | ~480 kB |
| `model.onnx_data` | [`onnx/model.onnx_data`](https://huggingface.co/onnx-community/embeddinggemma-300m-ONNX/resolve/main/onnx/model.onnx_data?download=true) | ~1.23 GB |
| `tokenizer.json` | [`tokenizer.json`](https://huggingface.co/onnx-community/embeddinggemma-300m-ONNX/resolve/main/tokenizer.json?download=true) | ~20 MB |
| `tokenizer.model` | [`tokenizer.model`](https://huggingface.co/onnx-community/embeddinggemma-300m-ONNX/resolve/main/tokenizer.model?download=true) | ~4.7 MB |
| `tokenizer_config.json` | [`tokenizer_config.json`](https://huggingface.co/onnx-community/embeddinggemma-300m-ONNX/resolve/main/tokenizer_config.json?download=true) | ~1.2 MB |


Your folder should look like this:

```
embeddinggemma-onnx/
├── model.onnx
├── model.onnx_data
├── tokenizer.json
├── tokenizer.model
└── tokenizer_config.json
```

Alternatively, you can run the included PowerShell script **once** to download them from Hugging Face as well. By default, it will create a folder named `.embedding_resources` in the same directory as the script, but you can specify any path you like.

```powershell
.\Initialize-Embedding-Resources.ps1
```

---

## Installation

### If your project uses Semantic Kernel:

```bash
dotnet add package EmbeddingGemma.SemanticKernel
```

### If your project does NOT use Semantic Kernel:

```bash
dotnet add package EmbeddingGemma.Core
```

---

## Usage Guide

#### With `IServiceCollection` (Core)

```csharp
using EmbeddingGemma.Core;

builder.Services.AddGemmaTextEmbeddingGenerator(options => options.ModelDirectory = @"C:\path\to\.embedding_resources");
```

#### With Semantic Kernel (`IKernelBuilder`)

```csharp
using EmbeddingGemma.SemanticKernel;
using Microsoft.SemanticKernel;

var builder = Kernel.CreateBuilder();

builder.AddGemmaTextEmbeddingGenerator(options => options.ModelDirectory = @"C:\path\to\.embedding_resources");

var kernel = builder.Build();

var embeddingGenerator = kernel.Services.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();
```

---

## Quick Start Example

The following uses the built-in **in-memory vector store** (great for prototyping or desktop apps). You can swap it for any other [VectorData-compatible store](https://learn.microsoft.com/en-us/semantic-kernel/concepts/vector-store-connectors/) (Redis, Qdrant, Azure AI Search, etc.) with zero changes to embedding code.

The following example is referred to[the Python example in the **onnx-community/embeddinggemma-300m-ONNX**](https://huggingface.co/onnx-community/embeddinggemma-300m-ONNX#using-the-onnx-runtime-in-python) and demonstrates how to generate embeddings for a set of documents, upsert them into a vector store collection, and then search for similar documents based on a query embedding.

```csharp
using EmbeddingGemma.Core;
using EmbeddingGemma.Core.Enums;
using EmbeddingGemma.Core.Options;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Connectors.InMemory;

// 1. Setup DI
var services = new ServiceCollection();
services.AddInMemoryVectorStore();
services.AddGemmaTextEmbeddingGenerator(options => options.ModelDirectory = @"C:\path\to\.embedding_resources");
var provider = services.BuildServiceProvider();

public class SemanticDataModel
{
    [VectorStoreKey]
    public long Key { get; set; }

    [VectorStoreData]
    public required string Data { get; set; }

    [VectorStoreVector(768)] // EmbeddingGemma-300m produces 768-dimensional embeddings
    public ReadOnlyMemory<float> Embedding { get; set; }
}

var vectorStore = provider.GetRequiredService<VectorStore>();
var embeddingGenerator = provider.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();

// 2. Create a vector store collection
var collection = vectorStore.GetCollection<Guid, string>("planets");

var query = "What planet is known as the Red Planet?";
var documents = new List<string>
{
   "Venus is often called Earth's twin because of its similar size and proximity.",
   "Mars, known for its reddish appearance, is often referred to as the Red Planet.",
   "Jupiter, the largest planet in our solar system, has a prominent red spot.",
   "Saturn, famous for its rings, is sometimes mistaken for the Red Planet.",
};

// 3. Generate embeddings and upsert documents into the vector store
var embededDocuments = await embeddingGenerator.GenerateAsync(
    values: documents,
    options: new EmbeddingGemmaGenerationOptions
    {
        TaskType = EmbeddingGemmaTaskType.RetrievalDocument
    });

var semanticRecords = documents
    .Zip(embededDocuments, (doc, embedding) => new SemanticDataModel
    {
        Data = doc,
        Embedding = embedding.Vector,
    })
    .ToList();

await collection.UpsertAsync(semanticRecords);

// 4. Generate embedding for the query and search for similar documents
var embededQuery = await embeddingGenerator.GenerateAsync(
    value: query,
    options: new EmbeddingGemmaGenerationOptions
    {
        TaskType = EmbeddingGemmaTaskType.RetrievalQuery
    });

await foreach (var result in collection.SearchAsync(
   searchValue: embededQuery,
   top: 10, // Retrieve the top 10 most similar documents.
   options: new VectorSearchOptions<SemanticSearchDataModel>
   {
       IncludeVectors = false, // Exclude the embedding vectors from the search results for better performance.
   }))
{
    var similarityScore = result.Score!.Value;
    Console.WriteLine($"Document: {result.Record.Data} | Similarity Score: {similarityScore}");
}
```

---

## Prompting with Task Types

Pass `EmbeddingGemmaGenerationOptions.TaskType` to get embeddings optimized for your specific use case. When omitted, no prefix is added.

| `EmbeddingGemmaTaskType` | Best for |
|---|---|
| `RetrievalQuery` | User search queries |
| `RetrievalDocument` | Documents / pages being indexed |
| `QuestionAnswering` | Questions in a QA system |
| `FactVerification` | Claims that need evidence lookup |
| `Classification` | Text sentiment, spam detection, labelling |
| `Clustering` | Grouping similar documents by topic |
| `SentenceSimilarity` / `PairClassification` | Direct text-to-text similarity |
| `Summarization` | Texts being summarized |
| `InstructionRetrieval` | Natural-language → code-block search |
| `BitextMining` | Parallel sentence detection across languages |

For effective usage of task types, refer to the following resources:
* [EmbeddingGemma: Prompt Instructions](https://ai.google.dev/gemma/docs/embeddinggemma/model_card#prompt-instructions)
* [Using Prompts with EmbeddingGemma](https://ai.google.dev/gemma/docs/embeddinggemma/inference-embeddinggemma-with-sentence-transformers#using_prompts_with_embeddinggemma)