# EmbeddingGemma.NET
![NuGet Version](https://img.shields.io/nuget/v/EmbeddingGemma.Core) ![NuGet Downloads](https://img.shields.io/nuget/dt/EmbeddingGemma.Core)

![EmbeddingGemma](https://ollama.com/assets/library/embeddinggemma/9a20d963-4bf1-4177-9568-ca5d53a2d14e)

Run Google DeepMind's EmbeddingGemma-300m embedding model fully locally and natively in your .NET app with zero API cost, zero data leakage, zero cloud dependency.

EmbeddingGemma.NET provides two NuGet packages that plug local semantic search / vector search into any .NET 10 application, with first-class support for Microsoft Semantic Kernel and `Microsoft.Extensions.VectorData`.

---

## Why EmbeddingGemma?

| | |
|---|---|
| **Zero runtime cost** | Runs locally on your device without further setups |
| **Privacy-first** | Your data never leaves the machine |
| **State-of-the-art accuracy** | [#1 open multilingual embedding model under 500 M parameters on MTEB](https://deepmind.google/models/gemma/embeddinggemma/#performance) |
| **100+ languages compatibility** | Multilingual out-of-the-box |
| **Instructed embeddings** | Pre-defined prompt prefixing for 15 task types (e.g., retrieval, QA, classification, clustering) |

---

## Installation

| Package | Use when |
|---|---|
| `EmbeddingGemma.Core` | Building a plain .NET / ASP.NET Core app |
| `EmbeddingGemma.SemanticKernel` | Using Microsoft Semantic Kernel |

Further details can be found at [EmbeddingGemma.Core](https://www.nuget.org/packages/EmbeddingGemma.Core) and [EmbeddingGemma.SemanticKernel](https://www.nuget.org/packages/EmbeddingGemma.SemanticKernel) on Microsoft's NuGet Gallery.

---

## Local Model Initialization

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
