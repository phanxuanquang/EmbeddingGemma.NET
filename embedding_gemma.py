import onnxruntime as ort
from transformers import AutoTokenizer
import os

model_dir = os.path.join(os.path.dirname(__file__), "model")
onnx_path = os.path.join(model_dir, "onnx", "model.onnx")

session = ort.InferenceSession(onnx_path)
tokenizer = AutoTokenizer.from_pretrained(model_dir)

prefixes = {
  "query": "task: search result | query: ",
  "document": "title: none | text: ",
}
# Example user query 
query = prefixes["query"] + "Which planet is known as the Red Planet?"

# Example documents
documents = [
    "Venus is often called Earth's twin because of its similar size and proximity.",
    "Mars, known for its reddish appearance, is often referred to as the Red Planet.",
    "Jupiter, the largest planet in our solar system, has a prominent red spot.",
    "Saturn, famous for its rings, is sometimes mistaken for the Red Planet."
]

# Prepend the document prefix to each document
documents = [prefixes["document"] + x for x in documents]

# Tokenize the query and documents together, ensuring they are processed in a single batch for the model to compute embeddings correctly. The query is the first item, followed by the documents.
inputs = tokenizer([query] + documents, padding=True, return_tensors="np")

_, sentence_embedding = session.run(None, inputs.data)

# Compute similarities to determine a ranking
query_embeddings = sentence_embedding[0]
document_embeddings = sentence_embedding[1:]
similarities = query_embeddings @ document_embeddings.T
print(similarities)  # Expected: [0.30109745 0.635883 0.49304956 0.48887485]

# Rank the documents based on similarity scores
ranking = similarities.argsort()[::-1]
for i, idx in enumerate(ranking):
    print(f"{similarities[idx] * 100 :.1f}% similarity: '{documents[idx]}'")