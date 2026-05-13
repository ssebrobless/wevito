namespace Wevito.VNext.Core;

public sealed record RouterConfig(
    string SchemaVersion,
    string ConfigId,
    string DatasetVersion,
    double MemoryRouteThreshold,
    double DeterministicFallbackThreshold,
    IReadOnlyDictionary<string, string> ToolFamilyRoleHints,
    DateTimeOffset CreatedAtUtc)
{
    public static RouterConfig CreateDefault(string datasetVersion, DateTimeOffset createdAtUtc)
    {
        return new RouterConfig(
            "1",
            $"router-{createdAtUtc:yyyyMMdd-HHmmss}",
            datasetVersion,
            MemoryRouteThreshold: 0.15,
            DeterministicFallbackThreshold: 0.01,
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["spriteAudit"] = "SpriteReviewHelper",
                ["localDocs"] = "ResearchHelper",
                ["localResearch"] = "ResearchHelper",
                ["codePatchPlan"] = "ChecklistHelper",
                ["buildProof"] = "ChecklistHelper"
            },
            createdAtUtc);
    }
}
