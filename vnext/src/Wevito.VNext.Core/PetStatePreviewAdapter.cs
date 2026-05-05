using System.Text.Json;
using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed class PetStatePreviewAdapter
{
    private const string ToolFamily = "petState";

    private readonly PetDebugTruthReportBuilder _reportBuilder;

    public PetStatePreviewAdapter(PetDebugTruthReportBuilder? reportBuilder = null)
    {
        _reportBuilder = reportBuilder ?? new PetDebugTruthReportBuilder();
    }

    public TaskAdapterResult BuildReport(
        TaskAdapterRequest request,
        GameContent? content,
        IReadOnlyList<PetActor>? pets,
        CompanionMode mode,
        DateTimeOffset? nowUtc = null)
    {
        var timestamp = nowUtc ?? DateTimeOffset.UtcNow;
        if (request.RunMode != TaskAdapterRunMode.DryRunPreview)
        {
            return Block(request, "Pet state adapter only supports dry-run report mode right now.", timestamp);
        }

        if (!string.Equals(request.PolicySnapshot.ToolFamily, ToolFamily, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(request.Intent.RequestedToolFamily, ToolFamily, StringComparison.OrdinalIgnoreCase))
        {
            return Block(request, "Task intent and policy must both target petState.", timestamp);
        }

        if (request.PolicySnapshot.AccessMode != ToolAccessMode.ReadOnly)
        {
            return Block(request, "Pet state report requires a read-only policy.", timestamp);
        }

        if (content is null)
        {
            return Block(request, "Pet state report needs loaded game content.", timestamp);
        }

        var activePets = pets ?? [];
        var artifactRoot = ResolveArtifactRoot(request.ArtifactRoot, timestamp);
        if (!IsSafePetTaskArtifactRoot(artifactRoot))
        {
            return Block(request, "Pet state artifacts must be written under a pet-tasks artifact folder.", timestamp);
        }

        Directory.CreateDirectory(artifactRoot);
        var report = _reportBuilder.Build(content, activePets, mode, timestamp);
        var jsonPath = Path.Combine(artifactRoot, "pet-state-report.json");
        var markdownPath = Path.Combine(artifactRoot, "run-summary.md");
        File.WriteAllText(jsonPath, JsonSerializer.Serialize(report, JsonDefaults.Options));
        File.WriteAllText(markdownPath, BuildMarkdown(request, report));

        return new TaskAdapterResult(
            request.TaskCardId,
            ToolFamily,
            TaskAdapterResultStatus.PreviewReady,
            DidMutate: false,
            ReadPaths: [],
            WrittenPaths: [jsonPath, markdownPath],
            PreviewSummary: $"Wrote petState markdown and JSON reports for {activePets.Count} active pet(s). No pet state or assets were changed.",
            ResultSummary: $"petState report ready: {markdownPath}",
            AuditLogPath: markdownPath,
            CompletedAtUtc: timestamp);
    }

    private string BuildMarkdown(TaskAdapterRequest request, PetDebugTruthReport report)
    {
        return "# PET TASKS Pet State Report" +
               Environment.NewLine +
               Environment.NewLine +
               $"TaskCard: `{request.TaskCardId}`" +
               Environment.NewLine +
               $"Intent: {request.Intent.RawText}" +
               Environment.NewLine +
               Environment.NewLine +
               _reportBuilder.ToMarkdown(report) +
               Environment.NewLine +
               "## Safety" +
               Environment.NewLine +
               Environment.NewLine +
               "This adapter only wrote markdown/JSON artifacts in a new pet-task folder. It did not mutate pet state, runtime PNGs, source boards, prop anchors, shared assets, or visual-side candidate/proof folders." +
               Environment.NewLine;
    }

    private static string ResolveArtifactRoot(string artifactRoot, DateTimeOffset timestamp)
    {
        if (!string.IsNullOrWhiteSpace(artifactRoot))
        {
            return Path.GetFullPath(artifactRoot);
        }

        var slug = timestamp.ToString("yyyyMMdd-HHmmss") + "-pet-state";
        return Path.GetFullPath(Path.Combine("vnext", "artifacts", "pet-tasks", slug));
    }

    private static bool IsSafePetTaskArtifactRoot(string artifactRoot)
    {
        var fullPath = Path.GetFullPath(artifactRoot);
        var parts = fullPath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return parts.Length >= 2 &&
               parts.Any(part => string.Equals(part, "pet-tasks", StringComparison.OrdinalIgnoreCase)) &&
               !parts.Any(part => part.StartsWith("candidate-frames", StringComparison.OrdinalIgnoreCase) ||
                                  part.StartsWith("backup-before-", StringComparison.OrdinalIgnoreCase) ||
                                  part.StartsWith("godot-packaged-proof-", StringComparison.OrdinalIgnoreCase));
    }

    private static TaskAdapterResult Block(TaskAdapterRequest request, string reason, DateTimeOffset timestamp)
    {
        return new TaskAdapterResult(
            request.TaskCardId,
            ToolFamily,
            TaskAdapterResultStatus.Blocked,
            DidMutate: false,
            ReadPaths: [],
            WrittenPaths: [],
            BlockReason: reason,
            CompletedAtUtc: timestamp);
    }
}
