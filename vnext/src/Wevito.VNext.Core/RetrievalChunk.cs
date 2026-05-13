namespace Wevito.VNext.Core;

public sealed record RetrievalChunk(
    string ChunkId,
    string DocId,
    string Path,
    string Sha256,
    string Text,
    int ByteStart,
    int ByteEnd,
    float[] Embedding,
    DateTimeOffset ChunkedAtUtc);

public sealed record RetrievalScore(
    string ChunkId,
    double DenseScore,
    double KeywordScore,
    double FusionScore,
    double RerankScore);
