using PhanXuanQuang.AI.LocalEmbeddings.EmbeddingGemma.Attributes;

namespace PhanXuanQuang.AI.LocalEmbeddings.EmbeddingGemma.Enums;

public enum EmbeddingGemmaTaskType : byte
{
    [TaskPrefix("task: search result | query: ")]
    Query,

    [TaskPrefix("title: none | text: ")]
    Document,

    [TaskPrefix("task: search result | query: ")]
    BitextMining,

    [TaskPrefix("task: clustering | query: ")]
    Clustering,

    [TaskPrefix("task: classification | query: ")]
    Classification,

    [TaskPrefix("task: code retrieval | query: ")]
    InstructionRetrieval,

    [TaskPrefix("task: classification | query: ")]
    MultilabelClassification,

    [TaskPrefix("task: sentence similarity | query: ")]
    PairClassification,

    [TaskPrefix("task: search result | query: ")]
    Reranking,

    [TaskPrefix("task: search result | query: ")]
    Retrieval,

    [TaskPrefix("task: search result | query: ")]
    RetrievalQuery,

    [TaskPrefix("title: none | text: ")]
    RetrievalDocument,

    [TaskPrefix("task: sentence similarity | query: ")]
    SentenceSimilarity,

    [TaskPrefix("task: summarization | query: ")]
    Summarization,

    [TaskPrefix("task: question answering | query: ")]
    QuestionAnswering,

    [TaskPrefix("task: fact checking | query: ")]
    FactVerification,
}
