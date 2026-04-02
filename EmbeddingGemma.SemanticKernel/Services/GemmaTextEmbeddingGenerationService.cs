using EmbeddingGemma.SemanticKernel.Options;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Microsoft.ML.Tokenizers;

namespace EmbeddingGemma.SemanticKernel.Services
{
    public sealed class GemmaTextEmbeddingGenerationService : IEmbeddingGenerator<string, Embedding<float>>
    {
        private readonly InferenceSession _session;
        private readonly LlamaTokenizer _tokenizer;
        private readonly EmbeddingGeneratorMetadata _metadata;

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

            using var stream = File.OpenRead(tokenizerModelPath);
            // add_bos_token: true, add_eos_token: true per tokenizer_config.json
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
        public async Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
            IEnumerable<string> values,
            EmbeddingGenerationOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            // Run synchronous ONNX inference on a thread-pool thread to keep
            // the async signature without blocking the calling thread.
            return await Task.Run(() => RunInference(values), cancellationToken);
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
                for (int j = 0; j < maxLen; j++)
                {
                    if (j < ids.Count)
                    {
                        inputIds[i * maxLen + j] = ids[j];
                        attentionMask[i * maxLen + j] = 1L;
                    }
                    else
                    {
                        inputIds[i * maxLen + j] = PadTokenId;
                        attentionMask[i * maxLen + j] = 0L;
                    }
                }
            }

            int[] dims = [batchSize, maxLen];
            var onnxInputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor("input_ids", new DenseTensor<long>(inputIds, dims)),
                NamedOnnxValue.CreateFromTensor("attention_mask", new DenseTensor<long>(attentionMask, dims)),
            };

            if (_session.InputMetadata.ContainsKey("token_type_ids"))
            {
                onnxInputs.Add(NamedOnnxValue.CreateFromTensor("token_type_ids", new DenseTensor<long>(new long[batchSize * maxLen], dims)));
            }

            // output[1] is the sentence embedding tensor (batch_size × embedding_dim)
            using var results = _session.Run(onnxInputs);
            var embeddingTensor = results.ToList()[1].AsTensor<float>();
            int embeddingDim = embeddingTensor.Dimensions[1];

            var embeddings = new GeneratedEmbeddings<Embedding<float>>();
            for (int i = 0; i < batchSize; i++)
            {
                var vector = new float[embeddingDim];
                for (int j = 0; j < embeddingDim; j++)
                {
                    vector[j] = embeddingTensor[i, j];
                }

                embeddings.Add(new Embedding<float>(vector));
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
