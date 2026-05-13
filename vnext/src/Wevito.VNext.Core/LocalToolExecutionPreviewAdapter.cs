using System.Text.Json;
using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed class LocalToolExecutionPreviewAdapter
{
    public const string ToolFamily = "localToolExec";
    private readonly UnifiedPolicyService _policyService;
    private readonly Func<IReadOnlyDictionary<string, string>> _settingsProvider;

    public LocalToolExecutionPreviewAdapter(
        UnifiedPolicyService? policyService = null,
        Func<IReadOnlyDictionary<string, string>>? settingsProvider = null)
    {
        _policyService = policyService ?? new UnifiedPolicyService();
        _settingsProvider = settingsProvider ?? (() => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
    }

    public TaskAdapterResult BuildPreview(TaskAdapterRequest request, DateTimeOffset? nowUtc = null)
    {
        var timestamp = nowUtc ?? DateTimeOffset.UtcNow;
        if (request.RunMode != TaskAdapterRunMode.DryRunPreview)
        {
            return Block(request, "localToolExec only supports dry-run preview in C-PHASE 71.", timestamp);
        }

        if (!string.Equals(request.PolicySnapshot.ToolFamily, ToolFamily, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(request.Intent.RequestedToolFamily, ToolFamily, StringComparison.OrdinalIgnoreCase))
        {
            return Block(request, "Task intent and policy must both target localToolExec.", timestamp);
        }

        var scriptPath = request.Intent.TargetPathsOrAssets?.FirstOrDefault(path => !string.IsNullOrWhiteSpace(path)) ?? "";
        if (string.IsNullOrWhiteSpace(scriptPath))
        {
            return Block(request, "localToolExec requires a target script path.", timestamp);
        }

        var decision = _policyService.EvaluateLocalToolExecution(scriptPath, _settingsProvider(), taskCardId: request.TaskCardId, nowUtc: timestamp);
        if (decision.IsBlocked)
        {
            return Block(request, decision.Reason, timestamp);
        }

        var artifactRoot = ResolveArtifactRoot(request.ArtifactRoot, timestamp);
        if (!IsSafePetTaskArtifactRoot(artifactRoot))
        {
            return Block(request, "localToolExec artifacts must be written under a pet-tasks artifact folder.", timestamp);
        }

        Directory.CreateDirectory(artifactRoot);
        var report = new LocalToolExecutionPreviewReport(
            "1",
            request.TaskCardId,
            decision.NormalizedPath ?? scriptPath,
            decision.Status.ToString(),
            decision.Reason,
            DidExecute: false,
            DidMutate: false,
            timestamp);
        var jsonPath = Path.Combine(artifactRoot, "local-tool-exec-preview-report.json");
        var markdownPath = Path.Combine(artifactRoot, "run-summary.md");
        File.WriteAllText(jsonPath, JsonSerializer.Serialize(report, JsonDefaults.Options));
        File.WriteAllText(markdownPath, BuildMarkdown(report, request));

        return new TaskAdapterResult(
            request.TaskCardId,
            ToolFamily,
            TaskAdapterResultStatus.PreviewReady,
            DidMutate: false,
            ReadPaths: [report.ScriptPath],
            WrittenPaths: [jsonPath, markdownPath],
            PreviewSummary: "Wrote localToolExec dry-run preview. No script was executed.",
            ResultSummary: $"localToolExec preview ready: {markdownPath}",
            AuditLogPath: markdownPath,
            CompletedAtUtc: timestamp);
    }

    private static string BuildMarkdown(LocalToolExecutionPreviewReport report, TaskAdapterRequest request)
    {
        var lines = new List<string>
        {
            "# PET TASKS Local Tool Execution Preview",
            "",
            $"Generated: {report.GeneratedAtUtc:O}",
            $"TaskCard: `{report.TaskCardId}`",
            $"Intent: {request.Intent.RawText}",
            "",
            "## Decision",
            "",
            $"- Script: `{report.ScriptPath}`",
            $"- Policy status: {report.PolicyStatus}",
            $"- Policy reason: {report.PolicyReason}",
            $"- Executed: {report.DidExecute}",
            $"- Mutated files: {report.DidMutate}",
            "",
            "## Safety",
            "",
            "C-PHASE 71 permits only dry-run previews for local tool execution. This adapter does not start processes, run scripts, mutate files, or bypass the unified policy service."
        };
        return string.Join(Environment.NewLine, lines) + Environment.NewLine;
    }

    private static string ResolveArtifactRoot(string artifactRoot, DateTimeOffset timestamp)
    {
        if (!string.IsNullOrWhiteSpace(artifactRoot))
        {
            return Path.GetFullPath(artifactRoot);
        }

        var slug = timestamp.ToString("yyyyMMdd-HHmmss") + "-local-tool-exec";
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

public sealed record LocalToolExecutionPreviewReport(
    string SchemaVersion,
    Guid TaskCardId,
    string ScriptPath,
    string PolicyStatus,
    string PolicyReason,
    bool DidExecute,
    bool DidMutate,
    DateTimeOffset GeneratedAtUtc);
