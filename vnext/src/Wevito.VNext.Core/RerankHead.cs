using System.Text.Json.Serialization;

namespace Wevito.VNext.Core;

public sealed record RerankHead(
    string SchemaVersion,
    string HeadId,
    string DatasetVersion,
    IReadOnlyDictionary<string, double> ToolFamilyWeights,
    double MinimumScoreBoost,
    DateTimeOffset CreatedAtUtc)
{
    public static RerankHead CreateDefault(string datasetVersion, DateTimeOffset createdAtUtc)
    {
        return new RerankHead(
            "1",
            $"rerank-{createdAtUtc:yyyyMMdd-HHmmss}",
            datasetVersion,
            new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
            {
                ["spriteAudit"] = 0.08,
                ["localDocs"] = 0.05,
                ["localResearch"] = 0.04,
                ["petState"] = 0.03
            },
            0.01,
            createdAtUtc);
    }

    public double Score(PetMemorySearchResult result, string requestedToolFamily)
    {
        var boost = !string.IsNullOrWhiteSpace(requestedToolFamily) &&
                    ToolFamilyWeights.TryGetValue(requestedToolFamily, out var weight) &&
                    string.Equals(result.Example.Kind, requestedToolFamily, StringComparison.OrdinalIgnoreCase)
            ? Math.Max(MinimumScoreBoost, weight)
            : 0;
        return result.Score + boost;
    }

    public IReadOnlyList<RetrievalChunk> Apply(IReadOnlyList<RetrievalChunk> chunks, string query)
    {
        var queryTokens = Tokenize(query);
        return chunks
            .Select((chunk, index) => new
            {
                Chunk = chunk,
                OriginalIndex = index,
                Score = ScoreChunk(chunk, queryTokens)
            })
            .OrderByDescending(item => item.Score)
            .ThenBy(item => item.OriginalIndex)
            .Select(item => item.Chunk)
            .ToList();
    }

    public double Score(RetrievalChunk chunk, string query)
    {
        return ScoreChunk(chunk, Tokenize(query));
    }

    private double ScoreChunk(RetrievalChunk chunk, HashSet<string> queryTokens)
    {
        if (queryTokens.Count == 0)
        {
            return 0;
        }

        var chunkTokens = Tokenize(chunk.Text);
        var overlap = queryTokens.Count(token => chunkTokens.Contains(token));
        var titleBoost = queryTokens.Any(token => Path.GetFileName(chunk.Path).Contains(token, StringComparison.OrdinalIgnoreCase))
            ? MinimumScoreBoost
            : 0;
        return overlap / (double)queryTokens.Count + titleBoost;
    }

    private static HashSet<string> Tokenize(string value)
    {
        return (value ?? "")
            .ToLowerInvariant()
            .Split([' ', '\t', '\r', '\n', '.', ',', ';', ':', '/', '\\', '-', '_', '(', ')', '[', ']', '{', '}'], StringSplitOptions.RemoveEmptyEntries)
            .Where(token => token.Length > 1)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }
}

public sealed record RerankHeadApplication(
    string SchemaVersion,
    string HeadId,
    string DatasetVersion,
    string AppliedBy,
    DateTimeOffset AppliedAtUtc);
