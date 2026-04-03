using EmbeddingGemma.SemanticKernel.Enums;
using EmbeddingGemma.SemanticKernel.Extensions;
using EmbeddingGemma.SemanticKernel.Options;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Microsoft.ML.Tokenizers;

namespace EmbeddingGemma.SemanticKernel.Services
{
    /// <summary>
    /// Text embedding generation service that uses the EmbeddingGemma-300m model via ONNX Runtime.
    /// </summary>
    public sealed class GemmaTextEmbeddingGenerationService : IEmbeddingGenerator<string, Embedding<float>>
    {
        private readonly InferenceSession _session;
        private readonly LlamaTokenizer _tokenizer;
        private readonly EmbeddingGeneratorMetadata _metadata;

        private readonly bool _hasTokenTypeIds;
        private readonly string _embeddingOutputName;
        private const long PadTokenId = 0L; // <pad> id in Gemma vocab

        /// <summary>
        /// Initializes the service using an options instance resolved from the DI container.
        /// </summary>
        /// <param name="options"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public GemmaTextEmbeddingGenerationService(IOptions<EmbeddingGemmaOptions> options) : this(options?.Value ?? throw new ArgumentNullException(nameof(options)))
        {
        }

        public GemmaTextEmbeddingGenerationService(EmbeddingGemmaOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);
            var modelDirectory = options.ModelDirectory;

            ArgumentException.ThrowIfNullOrWhiteSpace(modelDirectory);

            if (!Directory.Exists(modelDirectory))
                throw new DirectoryNotFoundException($"Model directory not found: {modelDirectory}");

            var tokenizerJsonPath = Path.Combine(modelDirectory, "tokenizer.json");
            if (!File.Exists(tokenizerJsonPath))
                throw new FileNotFoundException($"\"tokenizer.json\" file not found at the directory: {modelDirectory}");

            var tokenizerModelPath = Path.Combine(modelDirectory, "tokenizer.model");
            if (!File.Exists(tokenizerModelPath))
                throw new FileNotFoundException($"\"tokenizer.model\" file not found at the directory: {modelDirectory}");

            var tokenizerConfigJsonPath = Path.Combine(modelDirectory, "tokenizer_config.json");
            if (!File.Exists(tokenizerConfigJsonPath))
                throw new FileNotFoundException($"\"tokenizer_config.json\" file not found at the directory: {modelDirectory}");

            var modelOnnxPath = Path.Combine(modelDirectory, "model.onnx");
            if (!File.Exists(modelOnnxPath))
                throw new FileNotFoundException($"\"model.onnx\" file not found at the directory: {modelDirectory}");

            var modelOnnxDataPath = Path.Combine(modelDirectory, "model.onnx_data");
            if (!File.Exists(modelOnnxDataPath))
                throw new FileNotFoundException($"\"model.onnx_data\" file not found at the directory: {modelDirectory}");

            _session = new InferenceSession(modelOnnxPath);
            _hasTokenTypeIds = _session.InputMetadata.ContainsKey("token_type_ids");
            _embeddingOutputName = _session.OutputMetadata.Keys.ElementAt(1);

            using var stream = File.OpenRead(tokenizerModelPath);

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

            if (options is EmbeddingGemmaGenerationOptions gemmaOptions && gemmaOptions.TaskType.HasValue)
            {
                var taskType = gemmaOptions.TaskType.Value;
                var prefix = taskType is EmbeddingGemmaTaskType.Document or EmbeddingGemmaTaskType.RetrievalDocument
                    ? EmbeddingGemmaTaskTypeExtensions.GetDocumentPrefix(gemmaOptions.DocumentTitle)
                    : taskType.GetPrefix();

                inputs = values.Select(v => prefix + v);
            }

            return await Task.Run(() => RunInference(inputs), cancellationToken);
        }

        private GeneratedEmbeddings<Embedding<float>> RunInference(IEnumerable<string> values)
        {
            var texts = values.ToArray();
            var encodings = texts.Select(t => _tokenizer.EncodeToIds(t)).ToArray();

            int batchSize = texts.Length;
            int maxLen = encodings.Max(e => e.Count);

            var inputIds = new long[batchSize * maxLen];
            var attentionMask = new long[batchSize * maxLen];

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

            int[] dims = [batchSize, maxLen];
            var onnxInputs = new List<NamedOnnxValue>(_hasTokenTypeIds ? 3 : 2)
            {
                NamedOnnxValue.CreateFromTensor("input_ids", new DenseTensor<long>(inputIds, dims)),
                NamedOnnxValue.CreateFromTensor("attention_mask", new DenseTensor<long>(attentionMask, dims)),
            };

            if (_hasTokenTypeIds)
            {
                onnxInputs.Add(NamedOnnxValue.CreateFromTensor("token_type_ids", new DenseTensor<long>(new long[batchSize * maxLen], dims)));
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

        /// <inheritdoc />
        public void Dispose()
        {
            _session.Dispose();
        }
    }
}
