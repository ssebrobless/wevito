using System.Text.Json;
using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed class ScreenCapturePreviewAdapter
{
    private const string ToolFamily = "screenCapture";

    public TaskAdapterResult BuildPreview(TaskAdapterRequest request, DateTimeOffset? nowUtc = null)
    {
        var timestamp = nowUtc ?? DateTimeOffset.UtcNow;
        if (request.RunMode != TaskAdapterRunMode.DryRunPreview)
        {
            return Block(request, "Screen capture preview only supports dry-run report mode right now.", timestamp);
        }

        if (!string.Equals(request.PolicySnapshot.ToolFamily, ToolFamily, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(request.Intent.RequestedToolFamily, ToolFamily, StringComparison.OrdinalIgnoreCase))
        {
            return Block(request, "Task intent and policy must both target screenCapture.", timestamp);
        }

        if (request.PolicySnapshot.AccessMode != ToolAccessMode.ReadOnly)
        {
            return Block(request, "Screen capture preview requires a read-only policy.", timestamp);
        }

        var artifactRoot = ResolveArtifactRoot(request.ArtifactRoot, timestamp);
        if (!IsSafePetTaskArtifactRoot(artifactRoot))
        {
            return Block(request, "Screen capture preview artifacts must be written under a pet-tasks artifact folder.", timestamp);
        }

        var report = new ScreenCapturePreviewReport(
            "1",
            request.TaskCardId,
            ToolFamily,
            BuildCapabilities(),
            SummarizeCaptureRequest(request.Intent.RawText),
            BuildSafetyNotes(),
            DidCaptureScreen: false,
            DidRecordScreen: false,
            DidMutate: false,
            timestamp);

        Directory.CreateDirectory(artifactRoot);
        var jsonPath = Path.Combine(artifactRoot, "screen-capture-preview-report.json");
        var markdownPath = Path.Combine(artifactRoot, "run-summary.md");
        File.WriteAllText(jsonPath, JsonSerializer.Serialize(report, JsonDefaults.Options));
        File.WriteAllText(markdownPath, BuildMarkdown(report));

        return new TaskAdapterResult(
            request.TaskCardId,
            ToolFamily,
            TaskAdapterResultStatus.PreviewReady,
            DidMutate: false,
            ReadPaths: [],
            WrittenPaths: [jsonPath, markdownPath],
            PreviewSummary: "Wrote screenCapture preview report. No screenshot or recording was captured.",
            ResultSummary: $"screenCapture preview ready: {markdownPath}",
            AuditLogPath: markdownPath,
            CompletedAtUtc: timestamp);
    }

    private static IReadOnlyList<ScreenCaptureCapability> BuildCapabilities()
    {
        return
        [
            new ScreenCaptureCapability(
                ScreenCaptureActionKind.WindowScreenshot,
                ScreenCaptureCapabilityStatus.ApprovalRequired,
                ToolRiskLevel.Medium,
                ApprovalRequirement.BeforeExecution,
                "Execution target: capture only the Wevito window."),
            new ScreenCaptureCapability(
                ScreenCaptureActionKind.RegionScreenshot,
                ScreenCaptureCapabilityStatus.ApprovalRequired,
                ToolRiskLevel.Medium,
                ApprovalRequirement.BeforeExecution,
                "Execution target: capture an explicitly selected region or the saved last region."),
            new ScreenCaptureCapability(
                ScreenCaptureActionKind.Screenshot,
                ScreenCaptureCapabilityStatus.ApprovalRequired,
                ToolRiskLevel.High,
                ApprovalRequirement.BeforeExecution,
                "Full-desktop screenshots can expose private information and must remain explicitly approval-gated."),
            new ScreenCaptureCapability(
                ScreenCaptureActionKind.ScreenRecording,
                ScreenCaptureCapabilityStatus.ApprovalRequired,
                ToolRiskLevel.Medium,
                ApprovalRequirement.BeforeExecution,
                "Execution target: 5-10 second Wevito-window-only MP4 proof clip, no audio."),
            new ScreenCaptureCapability(
                ScreenCaptureActionKind.GifRecording,
                ScreenCaptureCapabilityStatus.Blocked,
                ToolRiskLevel.High,
                ApprovalRequirement.HandOffRequired,
                "GIF/video capture waits for the recording gate; preview mode never records.")
        ];
    }

    private static string SummarizeCaptureRequest(string rawText)
    {
        if (rawText.Contains("record", StringComparison.OrdinalIgnoreCase) ||
            rawText.Contains("video", StringComparison.OrdinalIgnoreCase) ||
            rawText.Contains("gif", StringComparison.OrdinalIgnoreCase))
        {
            return ScreenCaptureTargetResolver.ResolveTarget(rawText).TargetKind == CaptureTargetKind.WevitoWindow
                ? "The request appears to ask for a short Wevito-window proof clip. Execution requires approval, shows a visible Wevito recording indicator, records no audio, and is capped at 10 seconds."
                : "The request appears to ask for screen recording outside the Wevito window. Region, foreground, and desktop recording remain blocked in this phase.";
        }

        if (rawText.Contains("desktop", StringComparison.OrdinalIgnoreCase) ||
            rawText.Contains("full screen", StringComparison.OrdinalIgnoreCase) ||
            rawText.Contains("fullscreen", StringComparison.OrdinalIgnoreCase))
        {
            return "The request appears to ask for a full-desktop screenshot. This will require explicit approval before capture.";
        }

        if (ScreenCaptureTargetResolver.ResolveTarget(rawText).TargetKind == CaptureTargetKind.LastRegion)
        {
            return "The request appears to ask for the last saved screenshot region. Execution will require approval and a previously saved region.";
        }

        if (ScreenCaptureTargetResolver.ResolveTarget(rawText).TargetKind == CaptureTargetKind.SelectedRegion)
        {
            return "The request appears to ask for a selected-region screenshot. Execution will require approval, then a drag-to-select region picker.";
        }

        if (rawText.Contains("window", StringComparison.OrdinalIgnoreCase) ||
            rawText.Contains("wevito", StringComparison.OrdinalIgnoreCase))
        {
            return "The request appears to ask for a window screenshot. Future execution should prefer the Wevito window when possible.";
        }

        return "The request appears to ask for a screenshot. Future execution should ask for window/region/full-desktop scope before capture.";
    }

    private static IReadOnlyList<string> BuildSafetyNotes()
    {
        return
        [
            "No screenshot was captured.",
            "No screen recording was started.",
            "No clipboard, desktop, project file, or sprite asset was changed.",
            "Future screenshot execution should prefer Wevito-window or selected-region scope over full desktop.",
            "Short proof clips are Wevito-window-only, no-audio, approval-gated, and capped at 10 seconds."
        ];
    }

    private static string BuildMarkdown(ScreenCapturePreviewReport report)
    {
        var lines = new List<string>
        {
            "# PET TASKS Screen Capture Preview",
            "",
            $"Generated: {report.GeneratedAtUtc:O}",
            $"TaskCard: `{report.TaskCardId}`",
            "",
            "## Summary",
            "",
            $"- {report.RequestedCaptureSummary}",
            $"- Captured screen: {report.DidCaptureScreen}",
            $"- Recorded screen: {report.DidRecordScreen}",
            $"- Did mutate files: {report.DidMutate}",
            "",
            "## Capabilities",
            ""
        };

        lines.AddRange(report.Capabilities.Select(capability => $"- {capability.ActionKind}: {capability.Status}, risk {capability.RiskLevel}, approval {capability.ApprovalRequirement}. {capability.Detail}"));
        lines.Add("");
        lines.Add("## Safety Notes");
        lines.Add("");
        lines.AddRange(report.SafetyNotes.Select(note => $"- {note}"));
        return string.Join(Environment.NewLine, lines) + Environment.NewLine;
    }

    private static string ResolveArtifactRoot(string artifactRoot, DateTimeOffset timestamp)
    {
        if (!string.IsNullOrWhiteSpace(artifactRoot))
        {
            return Path.GetFullPath(artifactRoot);
        }

        var slug = timestamp.ToString("yyyyMMdd-HHmmss") + "-screen-capture";
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
