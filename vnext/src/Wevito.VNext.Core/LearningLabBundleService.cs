using System.Text.Json;
using Wevito.VNext.Contracts;

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
    int BlockedCount,
    int WaitingCount,
    IReadOnlyList<string> Reasons,
    IReadOnlyList<string> EvalBenchmarks);

public sealed record LearningLabReviewedBundleExportRequest(
    LearningLabBundleRequest BundleRequest,
    string OutputRoot,
    DateTimeOffset ExportedAtUtc);

public sealed record LearningLabReviewedBundleExportResult(
    bool Succeeded,
    LearningLabBundleGateResult Gate,
    string BundleFolder,
    string LabelsPath,
    string SourcesPath,
    string SummaryPath,
    string Message);

public sealed class LearningLabBundleService
{
    public const bool AutomaticTrainingEnabled = false;
    public const bool AutomaticMemoryPromotionEnabled = false;
    public const bool CopiesBinaryAssets = false;

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
        var indexedLabels = request.Index.Artifacts
            .Select(artifact => latest.TryGetValue(Path.GetFullPath(artifact.AbsolutePath), out var label) ? label : null)
            .Where(label => label is not null)
            .Cast<LearningLabLabelRecord>()
            .ToList();
        var rejectedCount = indexedLabels.Count(label => string.Equals(label.Label, "reject", StringComparison.OrdinalIgnoreCase));
        var blockedCount = indexedLabels.Count(label => string.Equals(label.Label, "blocked", StringComparison.OrdinalIgnoreCase));
        var waitingCount = request.Index.Artifacts.Count - indexedLabels.Count;
        var reasons = new List<string>();

        if (request.Index.Artifacts.Count == 0)
        {
            reasons.Add("No indexed artifacts are available.");
        }

        if (accepted.Count == 0)
        {
            reasons.Add("No accepted examples are available for a bundle.");
        }

        if (blockedCount > 0)
        {
            reasons.Add("Blocked labels prevent reviewed bundle export.");
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

        if (indexedLabels.Any(label => string.IsNullOrWhiteSpace(label.Reviewer)))
        {
            reasons.Add("Every exported label must include a reviewer.");
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
            blockedCount,
            Math.Max(0, waitingCount),
            reasons.Count == 0 ? ["Bundle gate is ready."] : reasons,
            EvalBenchmarks);
    }

    public LearningLabReviewedBundleExportResult ExportReviewedBundle(LearningLabReviewedBundleExportRequest request)
    {
        var gate = Evaluate(request.BundleRequest);
        if (!gate.IsReady)
        {
            return new LearningLabReviewedBundleExportResult(
                false,
                gate,
                "",
                "",
                "",
                "",
                "Reviewed bundle gate is not ready.");
        }

        var repoRoot = Path.GetFullPath(request.BundleRequest.Index.RepoRoot);
        var defaultRoot = Path.Combine(repoRoot, "vnext", "artifacts", "creative-learning-lab");
        var outputRoot = Path.GetFullPath(request.OutputRoot);
        if (!IsPathUnderRoot(outputRoot, defaultRoot) && !string.Equals(outputRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), defaultRoot, StringComparison.OrdinalIgnoreCase))
        {
            return new LearningLabReviewedBundleExportResult(
                false,
                gate,
                "",
                "",
                "",
                "",
                $"Reviewed bundle export must stay under {defaultRoot}.");
        }

        var bundleFolder = Path.Combine(outputRoot, $"{request.ExportedAtUtc:yyyyMMdd-HHmmss}-reviewed-bundle");
        Directory.CreateDirectory(bundleFolder);

        var latest = request.BundleRequest.LatestLabels;
        var rows = request.BundleRequest.Index.Artifacts
            .Select(artifact => new
            {
                artifact.Id,
                artifact.ArtifactKind,
                artifact.RelativePath,
                artifact.AbsolutePath,
                artifact.FileName,
                artifact.Status,
                artifact.Target,
                Label = latest[Path.GetFullPath(artifact.AbsolutePath)].Label,
                Reviewer = latest[Path.GetFullPath(artifact.AbsolutePath)].Reviewer,
                Notes = latest[Path.GetFullPath(artifact.AbsolutePath)].Notes,
                CreatedAtUtc = latest[Path.GetFullPath(artifact.AbsolutePath)].CreatedAtUtc,
                SchemaVersion = latest[Path.GetFullPath(artifact.AbsolutePath)].SchemaVersion
            })
            .OrderBy(row => row.RelativePath, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var labelsPath = Path.Combine(bundleFolder, "labels.json");
        var sourcesPath = Path.Combine(bundleFolder, "sources.json");
        var summaryPath = Path.Combine(bundleFolder, "summary.md");

        File.WriteAllText(labelsPath, JsonSerializer.Serialize(new
        {
            schemaVersion = LearningLabLabelStore.CurrentSchemaVersion,
            exportedAtUtc = request.ExportedAtUtc,
            intendedUse = request.BundleRequest.IntendedUse,
            automaticTrainingEnabled = AutomaticTrainingEnabled,
            automaticMemoryPromotionEnabled = AutomaticMemoryPromotionEnabled,
            labels = rows.Select(row => new
            {
                row.AbsolutePath,
                row.Label,
                row.Reviewer,
                row.Notes,
                row.CreatedAtUtc,
                row.SchemaVersion
            })
        }, JsonDefaults.Options));

        File.WriteAllText(sourcesPath, JsonSerializer.Serialize(new
        {
            schemaVersion = LearningLabLabelStore.CurrentSchemaVersion,
            copiedBinaryAssets = CopiesBinaryAssets,
            sources = rows.Select(row => new
            {
                row.Id,
                row.ArtifactKind,
                row.RelativePath,
                row.AbsolutePath,
                row.FileName,
                row.Status,
                row.Target
            })
        }, JsonDefaults.Options));

        File.WriteAllText(summaryPath, BuildSummary(request, gate, rows.Count));

        return new LearningLabReviewedBundleExportResult(
            true,
            gate,
            bundleFolder,
            labelsPath,
            sourcesPath,
            summaryPath,
            $"Exported reviewed bundle with {rows.Count} labeled source(s). No binary assets were copied.");
    }

    private static string BuildSummary(LearningLabReviewedBundleExportRequest request, LearningLabBundleGateResult gate, int rowCount)
    {
        return string.Join(Environment.NewLine, [
            "# Creative Learning Lab Reviewed Bundle",
            "",
            $"Exported: {request.ExportedAtUtc:O}",
            $"Intended use: {request.BundleRequest.IntendedUse}",
            $"Accepted: {gate.AcceptedCount}",
            $"Rejected: {gate.RejectedCount}",
            $"Blocked: {gate.BlockedCount}",
            $"Reviewed sources: {rowCount}",
            "",
            "Reviewed examples are saved locally. Nothing trains automatically.",
            "No private binary assets are copied into this bundle.",
            "Labels are not promoted into pet memory or model memory by this export."
        ]);
    }

    private static bool IsPathUnderRoot(string path, string root)
    {
        var fullPath = Path.GetFullPath(path);
        var fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        return fullPath.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase);
    }
}
