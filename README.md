# EmbeddingGemma.NET

![NuGet Version](https://img.shields.io/nuget/v/EmbeddingGemma.Core) ![NuGet Downloads](https://img.shields.io/nuget/dt/EmbeddingGemma.Core)

![EmbeddingGemma](https://ollama.com/assets/library/embeddinggemma/9a20d963-4bf1-4177-9568-ca5d53a2d14e)

**EmbeddingGemma.NET** provides .NET bindings for Google DeepMind's [EmbeddingGemma-300m](https://deepmind.google/models/gemma/embeddinggemma) model, enabling fully local, offline text embedding with no API key, no cloud dependency, and no data egress.

Two NuGet packages are available: one for general .NET applications and one for Microsoft Semantic Kernel.

---

## Why EmbeddingGemma?

| | |
|---|---|
| **No runtime cost** | Runs on-device, no API calls or external services required |
| **Privacy first** | All inference is performed locally; no data leaves the machine |
| **Top-class accuracy** | GemmaEmbedding is the [top #1 ranked among open multilingual embedding models under 500M parameters on MTEB](https://deepmind.google/models/gemma/embeddinggemma/#performance) |
| **High efficiency** | Be able to run on low-end devices without GPU and with as little as 4 GB of RAM, making it ideal for a wide range of applications and users |
| **Multilingual** | Supports 100+ languages out of the box |
| **Task-aware embeddings** | 15 built-in task types automatically apply the correct prompt prefix |

---

---

## Installation

| Package | Target scenario |
|---|---|
| [`EmbeddingGemma.Core`](https://www.nuget.org/packages/EmbeddingGemma.Core) | General .NET / ASP.NET Core applications |
| [`EmbeddingGemma.SemanticKernel`](https://www.nuget.org/packages/EmbeddingGemma.SemanticKernel) | Microsoft Semantic Kernel integration |


---

## Model Setup

The ONNX model and tokenizer files must be present on disk before the service can be used. They are hosted at **[onnx-community/embeddinggemma-300m-ONNX](https://huggingface.co/onnx-community/embeddinggemma-300m-ONNX/tree/main)** on Hugging Face.

### Option A — PowerShell script (recommended)

Run the included script once from the repository root. It downloads all required files into a `.embedding_resources` folder by default; pass `-OutputPath` to use a different location.

```powershell
.\Initialize-Embedding-Resources.ps1
```

### Option B — Manual download

Manually download the following files and place them in the same directory:

| File | Download link | Size |
|---|---|---|
| `model.onnx` | [`onnx/model.onnx`](https://huggingface.co/onnx-community/embeddinggemma-300m-ONNX/resolve/main/onnx/model.onnx?download=true) | ~480 KB |
| `model.onnx_data` | [`onnx/model.onnx_data`](https://huggingface.co/onnx-community/embeddinggemma-300m-ONNX/resolve/main/onnx/model.onnx_data?download=true) | ~1.23 GB |
| `tokenizer.json` | [`tokenizer.json`](https://huggingface.co/onnx-community/embeddinggemma-300m-ONNX/resolve/main/tokenizer.json?download=true) | ~20 MB |
| `tokenizer.model` | [`tokenizer.model`](https://huggingface.co/onnx-community/embeddinggemma-300m-ONNX/resolve/main/tokenizer.model?download=true) | ~4.7 MB |
| `tokenizer_config.json` | [`tokenizer_config.json`](https://huggingface.co/onnx-community/embeddinggemma-300m-ONNX/resolve/main/tokenizer_config.json?download=true) | ~1.2 MB |

The resulting directory must have the following structure:

```
<model-directory>/
├── model.onnx
├── model.onnx_data
├── tokenizer.json
├── tokenizer.model
└── tokenizer_config.json
```

---

## Usage

### Dependency Registration

**`EmbeddingGemma.Core` via `IServiceCollection`**

```csharp
using EmbeddingGemma.Core;

// Registers IEmbeddingGenerator<string, Embedding<float>> as a singleton.
builder.Services.AddGemmaTextEmbeddingGenerator(options => options.ModelDirectory = @"C:\path\to\model-directory");
```

**`EmbeddingGemma.SemanticKernel` via `IKernelBuilder`**

```csharp
using EmbeddingGemma.SemanticKernel;
using Microsoft.SemanticKernel;

var builder = Kernel.CreateBuilder();

builder.AddGemmaTextEmbeddingGenerator(options => options.ModelDirectory = @"C:\path\to\model-directory");

var kernel = builder.Build();
```

---

### Generating Embeddings

Resolve `IEmbeddingGenerator<string, Embedding<float>>` from the DI container and call `GenerateAsync`.

```csharp
using Microsoft.Extensions.AI;

var generator = serviceProvider.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();

// Without a task type — no prompt prefix is added.
var embeddings = await generator.GenerateAsync(["Hello, world!"]);

// With a task type — the appropriate prompt prefix is applied automatically.
var options = new EmbeddingGemmaGenerationOptions 
{ 
    TaskType = EmbeddingGemmaTaskType.RetrievalQuery 
};
var queryEmbeddings = await generator.GenerateAsync(["What is semantic search?"], options);
```

For document embeddings, supply an optional `DocumentTitle` to improve retrieval quality:

```csharp
var docOptions = new EmbeddingGemmaGenerationOptions
{
    TaskType = EmbeddingGemmaTaskType.RetrievalDocument,
    DocumentTitle = "Introduction to Semantic Search"
};

var docEmbeddings = await generator.GenerateAsync(["Semantic search ranks results by meaning..."], docOptions);
```

---

## Task Types

Set `EmbeddingGemmaGenerationOptions.TaskType` to have the service automatically prepend the correct prompt prefix for your scenario. When `TaskType` is `null`, no prefix is added.

| `EmbeddingGemmaTaskType` | Intended use |
|---|---|
| `RetrievalQuery` | User-supplied search queries |
| `RetrievalDocument` | Documents or passages being indexed |
| `Query` / `Retrieval` | General-purpose retrieval |
| `QuestionAnswering` | Questions in a QA pipeline |
| `FactVerification` | Claims requiring evidence lookup |
| `Classification` / `MultilabelClassification` | Sentiment, spam detection, labelling |
| `Clustering` | Grouping documents by topic |
| `SentenceSimilarity` / `PairClassification` | Direct text-to-text similarity comparison |
| `Summarization` | Texts intended for summarization |
| `InstructionRetrieval` | Natural-language-to-code retrieval |
| `Reranking` | Re-scoring a candidate result set |
| `BitextMining` | Parallel sentence alignment across languages |

For detailed guidance on prompt formatting, refer to:
- [EmbeddingGemma Model Card — Prompt Instructions](https://ai.google.dev/gemma/docs/embeddinggemma/model_card#prompt-instructions)
- [Using Prompts with EmbeddingGemma](https://ai.google.dev/gemma/docs/embeddinggemma/inference-embeddinggemma-with-sentence-transformers#using_prompts_with_embeddinggemma)
