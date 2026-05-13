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
}

public sealed record RerankHeadApplication(
    string SchemaVersion,
    string HeadId,
    string DatasetVersion,
    string AppliedBy,
    DateTimeOffset AppliedAtUtc);
