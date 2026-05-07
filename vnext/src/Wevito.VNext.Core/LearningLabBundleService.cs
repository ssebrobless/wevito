namespace Wevito.VNext.Core;

public sealed record LearningLabBundleRequest(
    LearningLabArtifactIndex Index,
    IReadOnlyDictionary<string, LearningLabLabelRecord> LatestLabels,
    string IntendedUse,
    bool RollbackPathKnown);

public sealed record LearningLabBundleGateResult(
    bool IsReady,
    int AcceptedCount,
    int RejectedCount,
    int WaitingCount,
    IReadOnlyList<string> Reasons,
    IReadOnlyList<string> EvalBenchmarks);

public sealed class LearningLabBundleService
{
    public static readonly IReadOnlyList<string> EvalBenchmarks =
    [
        "sprite-validator-vs-human",
        "palette-identity",
        "optional-overlay-policy",
        "contact-sheet-clarity",
        "habitat-scale",
        "ui-safety-language"
    ];

    public LearningLabBundleGateResult Evaluate(LearningLabBundleRequest request)
    {
        var latest = request.LatestLabels;
        var accepted = request.Index.Artifacts
            .Where(artifact => latest.TryGetValue(Path.GetFullPath(artifact.AbsolutePath), out var label) &&
                               string.Equals(label.Label, "accept", StringComparison.OrdinalIgnoreCase))
            .ToList();
        var rejectedCount = latest.Values.Count(label => string.Equals(label.Label, "reject", StringComparison.OrdinalIgnoreCase));
        var waitingCount = request.Index.Artifacts.Count - accepted.Count;
        var reasons = new List<string>();

        if (request.Index.Artifacts.Count == 0)
        {
            reasons.Add("No indexed artifacts are available.");
        }

        if (accepted.Count == 0)
        {
            reasons.Add("No accepted examples are available for a bundle.");
        }

        if (request.Index.Artifacts.Any(artifact => string.IsNullOrWhiteSpace(artifact.AbsolutePath) || string.IsNullOrWhiteSpace(artifact.RelativePath)))
        {
            reasons.Add("Every artifact must have source paths recorded.");
        }

        if (accepted.Any(artifact => artifact.ParseStatus is "invalid-json" or "unreadable"))
        {
            reasons.Add("Accepted examples cannot include unreadable or invalid manifests.");
        }

        if (request.Index.Artifacts.Any(artifact => !latest.ContainsKey(Path.GetFullPath(artifact.AbsolutePath))))
        {
            reasons.Add("Every bundle candidate needs a reviewer label first.");
        }

        if (string.IsNullOrWhiteSpace(request.IntendedUse))
        {
            reasons.Add("Bundle intended use must be stated.");
        }

        if (!request.RollbackPathKnown)
        {
            reasons.Add("Rollback or deprecation path must be known.");
        }

        return new LearningLabBundleGateResult(
            reasons.Count == 0,
            accepted.Count,
            rejectedCount,
            Math.Max(0, waitingCount),
            reasons.Count == 0 ? ["Bundle gate is ready."] : reasons,
            EvalBenchmarks);
    }
}
