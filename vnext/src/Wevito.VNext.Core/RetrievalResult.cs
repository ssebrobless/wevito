namespace Wevito.VNext.Core;

public sealed record RetrievalResult(
    IReadOnlyList<RetrievalChunk> Chunks,
    IReadOnlyDictionary<string, RetrievalScore> Scores,
    IReadOnlyList<string> MethodTrace,
    DateTimeOffset RetrievedAtUtc);
