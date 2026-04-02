using Microsoft.Extensions.AI;
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

        public GemmaTextEmbeddingGenerationService(string modelDir)
        {
            var onnxPath = Path.Combine(modelDir, "onnx", "model.onnx");
            var tokenizerModelPath = Path.Combine(modelDir, "tokenizer.model");

            _session = new InferenceSession(onnxPath);

            using var stream = File.OpenRead(tokenizerModelPath);
            // add_bos_token: true, add_eos_token: true per tokenizer_config.json
            _tokenizer = LlamaTokenizer.Create(stream, addBeginOfSentence: true, addEndOfSentence: true);

            _metadata = new EmbeddingGeneratorMetadata(
                providerName: "GemmaOnnx",
                providerUri: null,
                defaultModelId: "gemma-embedding");
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
