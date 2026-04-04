using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Microsoft.ML.Tokenizers;
using phanxuanquang.SemanticKernel.Connectors.Onnx.Gemma.Enums;
using phanxuanquang.SemanticKernel.Connectors.Onnx.Gemma.Extensions;
using phanxuanquang.SemanticKernel.Connectors.Onnx.Gemma.Services.Options;
using System.Buffers;

namespace phanxuanquang.SemanticKernel.Connectors.Onnx.Gemma
{
    /// <summary>
    /// Text embedding generation service that uses the EmbeddingGemma-300m model via ONNX Runtime.
    /// </summary>
    public sealed class EmbeddingGemmaOnnxTextEmbeddingGenerationService : IEmbeddingGenerator<string, Embedding<float>>
    {
        private readonly InferenceSession _session;
        private readonly LlamaTokenizer _tokenizer;
        private readonly EmbeddingGeneratorMetadata _metadata;

        private readonly bool _hasTokenTypeIds;
        private readonly string _embeddingOutputName;

        /// <summary>
        /// The list of required files for the model to function properly. The service will validate the presence of these files in the specified model directory during initialization.
        /// </summary>
        public static readonly string[] RequiredFiles =
        [
            "tokenizer.json",
            "tokenizer.model",
            "tokenizer_config.json",
            "model.onnx",
            "model.onnx_data"
        ];

        /// <summary>
        /// Initializes the service using an options instance resolved from the DI container.
        /// </summary>
        /// <param name="options"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public EmbeddingGemmaOnnxTextEmbeddingGenerationService(IOptions<EmbeddingGemmaOnnxOptions> options) : this(options?.Value ?? throw new ArgumentNullException(nameof(options)))
        {
        }

        /// <summary>
        /// Initializes a new instance of the GemmaTextEmbeddingGenerationService class using the specified model options.
        /// </summary>
        /// <param name="options">The configuration options specifying the model directory and related settings. Cannot be null, and the model directory must exist and contain all required files.</param>
        /// <exception cref="DirectoryNotFoundException">Thrown if the directory specified by the model options does not exist.</exception>
        /// <exception cref="FileNotFoundException">Thrown if any required model file is missing from the specified model directory.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the model does not provide at least two output tensors as expected.</exception>
        public EmbeddingGemmaOnnxTextEmbeddingGenerationService(EmbeddingGemmaOnnxOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);
            var modelDirectory = options.ModelDirectory;

            ArgumentException.ThrowIfNullOrWhiteSpace(modelDirectory);

            if (!Directory.Exists(modelDirectory))
                throw new DirectoryNotFoundException($"Model directory not found: {modelDirectory}");

            foreach (var file in RequiredFiles)
            {
                var filePath = Path.Combine(modelDirectory, file);
                if (!File.Exists(filePath))
                    throw new FileNotFoundException($"\"{file}\" file not found at the directory: {modelDirectory}", filePath);
            }

            var inferenceSessionOptions = new SessionOptions
            {
                GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL,
                IntraOpNumThreads = 0,
                EnableCpuMemArena = true,
                EnableMemoryPattern = true,
                LogSeverityLevel = OrtLoggingLevel.ORT_LOGGING_LEVEL_ERROR
            };

            inferenceSessionOptions.AddSessionConfigEntry("session.set_denormal_as_zero", "1");

            _session = new InferenceSession(
                modelPath: Path.Combine(modelDirectory, "model.onnx"),
                options: inferenceSessionOptions);

            _hasTokenTypeIds = _session.InputMetadata.ContainsKey("token_type_ids");

            var outputKeys = _session.OutputMetadata.Keys.ToArray();
            if (outputKeys.Length < 2)
                throw new InvalidOperationException($"Expected at least 2 model outputs (last_hidden_state + pooled embedding), but found: {string.Join(", ", outputKeys)}");

            _embeddingOutputName = outputKeys[1];

            using var stream = File.OpenRead(Path.Combine(modelDirectory, "tokenizer.model"));

            _tokenizer = LlamaTokenizer.Create(stream, addBeginOfSentence: true, addEndOfSentence: true);
            _metadata = new EmbeddingGeneratorMetadata(
                providerName: "Google DeepMind",
                providerUri: new Uri("https://deepmind.google/models/gemma/embeddinggemma"),
                defaultModelId: "embeddinggemma-300m",
                defaultModelDimensions: 768);
        }

        /// <inheritdoc />
        public EmbeddingGeneratorMetadata Metadata => _metadata;

        /// <inheritdoc />
        public object? GetService(Type serviceType, object? key = null) => serviceType.IsInstanceOfType(this) ? this : null;

        /// <inheritdoc />
        public async Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(IEnumerable<string> values, EmbeddingGenerationOptions? options = null, CancellationToken cancellationToken = default)
        {
            IEnumerable<string> inputs = values;

            if (options is EmbeddingGemmaEmbeddingGenerationOptions gemmaOptions && gemmaOptions.TaskType.HasValue)
            {
                var taskType = gemmaOptions.TaskType.Value;
                var prefix = taskType is EmbeddingGemmaTaskType.Document or EmbeddingGemmaTaskType.RetrievalDocument
                    ? EmbeddingGemmaTaskTypeExtensions.GetDocumentPrefix(gemmaOptions.DocumentTitle)
                    : taskType.GetPrefix();

                inputs = values.Select(v => prefix + v);
            }

            return await Task.Run(() => RunInference(inputs, cancellationToken), cancellationToken);
        }

        private GeneratedEmbeddings<Embedding<float>> RunInference(IEnumerable<string> values, CancellationToken cancellationToken = default)
        {
            var texts = values.ToArray();
            int batchSize = texts.Length;

            var encodings = new IReadOnlyList<int>[batchSize];
            Parallel.For(0, batchSize, i =>
            {
                encodings[i] = _tokenizer.EncodeToIds(texts[i]);
            });

            cancellationToken.ThrowIfCancellationRequested();

            int maxLen = encodings.Max(e => e.Count);
            int totalSize = batchSize * maxLen;

            var inputIds = ArrayPool<long>.Shared.Rent(totalSize);
            var attentionMask = ArrayPool<long>.Shared.Rent(totalSize);

            try
            {
                Array.Clear(inputIds, 0, totalSize);
                Array.Clear(attentionMask, 0, totalSize);

                for (int i = 0; i < batchSize; i++)
                {
                    IReadOnlyList<int> ids = encodings[i];
                    int seqLen = ids.Count;
                    int rowOffset = i * maxLen;
                    for (int j = 0; j < seqLen; j++)
                    {
                        inputIds[rowOffset + j] = ids[j];
                        attentionMask[rowOffset + j] = 1L;
                    }
                }

                cancellationToken.ThrowIfCancellationRequested();

                int[] dims = [batchSize, maxLen];
                var onnxInputs = new List<NamedOnnxValue>(_hasTokenTypeIds ? 3 : 2)
                {
                    NamedOnnxValue.CreateFromTensor("input_ids", new DenseTensor<long>(new Memory<long>(inputIds, 0, totalSize), dims)),
                    NamedOnnxValue.CreateFromTensor("attention_mask", new DenseTensor<long>(new Memory<long>(attentionMask, 0, totalSize), dims)),
                };

                if (_hasTokenTypeIds)
                {
                    onnxInputs.Add(NamedOnnxValue.CreateFromTensor("token_type_ids", new DenseTensor<long>(new long[totalSize], dims)));
                }

                using var results = _session.Run(onnxInputs, [_embeddingOutputName]);
                var embeddingTensor = (DenseTensor<float>)results[0].AsTensor<float>();
                int embeddingDim = embeddingTensor.Dimensions[1];

                var buffer = embeddingTensor.Buffer;
                var embeddings = new GeneratedEmbeddings<Embedding<float>>(batchSize);
                for (int i = 0; i < batchSize; i++)
                {
                    embeddings.Add(new Embedding<float>(buffer.Slice(i * embeddingDim, embeddingDim).ToArray()));
                }

                return embeddings;
            }
            finally
            {
                ArrayPool<long>.Shared.Return(inputIds);
                ArrayPool<long>.Shared.Return(attentionMask);
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _session.Dispose();
        }
    }
}
