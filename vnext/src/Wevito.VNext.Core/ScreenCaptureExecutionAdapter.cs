using System.Text.Json;
using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed class ScreenCaptureExecutionAdapter
{
    private const string ToolFamily = "screenCapture";

    private readonly IScreenCaptureBackend _backend;

    public ScreenCaptureExecutionAdapter(IScreenCaptureBackend backend)
    {
        _backend = backend;
    }

    public async Task<TaskAdapterResult> ExecuteAsync(
        TaskAdapterRequest request,
        DateTimeOffset? nowUtc = null,
        CaptureRegion? region = null,
        CancellationToken cancellationToken = default)
    {
        var timestamp = nowUtc ?? DateTimeOffset.UtcNow;
        if (request.RunMode != TaskAdapterRunMode.Execute)
        {
            return Block(request, "Screen capture execution requires explicit execute mode.", timestamp);
        }

        if (!string.Equals(request.PolicySnapshot.ToolFamily, ToolFamily, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(request.Intent.RequestedToolFamily, ToolFamily, StringComparison.OrdinalIgnoreCase))
        {
            return Block(request, "Task intent and policy must both target screenCapture.", timestamp);
        }

        if (request.PolicySnapshot.AccessMode != ToolAccessMode.ReadOnly ||
            request.PolicySnapshot.ApprovalRequirement == ApprovalRequirement.None)
        {
            return Block(request, "Screen capture execution requires an approval-gated read-only policy.", timestamp);
        }

        var captureRequest = BuildCaptureRequest(request, timestamp, region);
        if (captureRequest.TargetKind is CaptureTargetKind.SelectedRegion or CaptureTargetKind.LastRegion &&
            captureRequest.Region is null)
        {
            return Block(request, $"Screen capture target {captureRequest.TargetKind} requires a selected region.", timestamp);
        }

        var policyDecision = new CapturePolicyEvaluator().Evaluate(captureRequest);
        if (policyDecision.Status != ToolPolicyDecisionStatus.ApprovalRequired ||
            policyDecision.RiskLevel != ToolRiskLevel.Medium ||
            policyDecision.ApprovalRequirement is not (ApprovalRequirement.ActionTime or ApprovalRequirement.BeforeExecution))
        {
            return Block(request, $"Screen capture execution is blocked by capture policy: {policyDecision.Reason}", timestamp);
        }

        var artifactRoot = ResolveArtifactRoot(request.ArtifactRoot, timestamp);
        if (!IsSafePetTaskArtifactRoot(artifactRoot))
        {
            return Block(request, "Screen capture execution artifacts must be written under a pet-tasks artifact folder.", timestamp);
        }

        Directory.CreateDirectory(artifactRoot);
        var screenshotPath = Path.Combine(artifactRoot, "screenshot.png");
        var manifestPath = Path.Combine(artifactRoot, "manifest.json");
        var summaryPath = Path.Combine(artifactRoot, "run-summary.md");

        ScreenCaptureBackendResult backendResult;
        try
        {
            backendResult = captureRequest.TargetKind switch
            {
                CaptureTargetKind.SelectedRegion or CaptureTargetKind.LastRegion when captureRequest.Region is { } selectedRegion =>
                    await _backend.CaptureRegionAsync(selectedRegion, screenshotPath, cancellationToken).ConfigureAwait(false),
                CaptureTargetKind.WevitoWindow =>
                    await _backend.CaptureWevitoWindowAsync(screenshotPath, cancellationToken).ConfigureAwait(false),
                _ => throw new NotSupportedException($"Screen capture target {captureRequest.TargetKind} is not executable in this phase.")
            };
        }
        catch (Exception exception) when (exception is InvalidOperationException or NotSupportedException or IOException)
        {
            return Block(request, $"Screen capture unavailable: {exception.Message}", timestamp);
        }

        if (!backendResult.DidCapture || !File.Exists(screenshotPath))
        {
            return Block(request, "Screen capture backend did not produce screenshot.png.", timestamp);
        }

        var manifest = new ScreenCaptureExecutionManifest(
            "1",
            request.TaskCardId,
            captureRequest.Id,
            captureRequest.Preset,
            captureRequest.TargetKind,
            captureRequest.OutputKind,
            captureRequest.PrivacyLevel,
            artifactRoot,
            screenshotPath,
            manifestPath,
            summaryPath,
            backendResult.TargetWindowTitle,
            backendResult.TargetWindowRect,
            backendResult.IndicatorVisible,
            backendResult.RedactionState,
            DidUploadOrShare: false,
            backendResult.Warnings ?? [],
            timestamp);

        File.WriteAllText(manifestPath, JsonSerializer.Serialize(manifest, JsonDefaults.Options));
        File.WriteAllText(summaryPath, BuildMarkdown(manifest));

        return new TaskAdapterResult(
            request.TaskCardId,
            ToolFamily,
            TaskAdapterResultStatus.Completed,
            DidMutate: false,
            ReadPaths: [],
            WrittenPaths: [screenshotPath, manifestPath, summaryPath],
            ResultSummary: $"screenCapture execution complete: {summaryPath}",
            AuditLogPath: summaryPath,
            CompletedAtUtc: timestamp);
    }

    private static CaptureRequest BuildCaptureRequest(TaskAdapterRequest request, DateTimeOffset timestamp, CaptureRegion? region)
    {
        return ScreenCaptureTargetResolver.ResolveRequest(
            request.Intent.RawText,
            request.TaskCardId,
            timestamp,
            region);
    }

    private static string ResolveArtifactRoot(string artifactRoot, DateTimeOffset timestamp)
    {
        if (!string.IsNullOrWhiteSpace(artifactRoot))
        {
            return Path.GetFullPath(artifactRoot);
        }

        var slug = timestamp.ToString("yyyyMMdd-HHmmss") + "-screencapture-execute";
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

    private static string BuildMarkdown(ScreenCaptureExecutionManifest manifest)
    {
        var lines = new List<string>
        {
            "# PET TASKS Screen Capture Execution",
            "",
            $"Captured: {manifest.CapturedAtUtc:O}",
            $"TaskCard: `{manifest.TaskCardId}`",
            "",
            "## Summary",
            "",
            $"- Target: {manifest.TargetKind}",
            $"- Window title: {manifest.TargetWindowTitle}",
            $"- Output: {manifest.OutputPath}",
            $"- OS capture indicator expected/visible: {manifest.IndicatorVisible}",
            $"- Redaction state: {manifest.RedactionState}",
            $"- Did upload/share: {manifest.DidUploadOrShare}",
            "",
            "## Window Rect",
            "",
            $"- X: {manifest.TargetWindowRect.X}",
            $"- Y: {manifest.TargetWindowRect.Y}",
            $"- Width: {manifest.TargetWindowRect.Width}",
            $"- Height: {manifest.TargetWindowRect.Height}",
            "",
            "## Warnings",
            ""
        };

        lines.AddRange(manifest.Warnings.Count == 0
            ? ["- none"]
            : manifest.Warnings.Select(warning => $"- {warning}"));

        return string.Join(Environment.NewLine, lines) + Environment.NewLine;
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

    private sealed record ScreenCaptureExecutionManifest(
        string SchemaVersion,
        Guid TaskCardId,
        Guid RequestId,
        CapturePreset Preset,
        CaptureTargetKind TargetKind,
        CaptureOutputKind OutputKind,
        CapturePrivacyLevel PrivacyLevel,
        string ArtifactRoot,
        string OutputPath,
        string ManifestPath,
        string SummaryPath,
        string TargetWindowTitle,
        CaptureRegion TargetWindowRect,
        bool IndicatorVisible,
        string RedactionState,
        bool DidUploadOrShare,
        IReadOnlyList<string> Warnings,
        DateTimeOffset CapturedAtUtc);
}
