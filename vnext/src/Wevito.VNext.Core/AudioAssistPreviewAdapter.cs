using System.Text.Json;
using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed class AudioAssistPreviewAdapter
{
    private const string ToolFamily = "audioAssist";
    private readonly IAudioEndpointStatusReader _statusReader;

    public AudioAssistPreviewAdapter()
        : this(new WindowsAudioEndpointStatusReader())
    {
    }

    public AudioAssistPreviewAdapter(IAudioEndpointStatusReader statusReader)
    {
        _statusReader = statusReader ?? throw new ArgumentNullException(nameof(statusReader));
    }

    public TaskAdapterResult BuildStatusReport(TaskAdapterRequest request, DateTimeOffset? nowUtc = null)
    {
        var timestamp = nowUtc ?? DateTimeOffset.UtcNow;
        if (request.RunMode != TaskAdapterRunMode.DryRunPreview)
        {
            return Block(request, "Audio assist preview only supports dry-run report mode right now.", timestamp);
        }

        if (!string.Equals(request.PolicySnapshot.ToolFamily, ToolFamily, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(request.Intent.RequestedToolFamily, ToolFamily, StringComparison.OrdinalIgnoreCase))
        {
            return Block(request, "Task intent and policy must both target audioAssist.", timestamp);
        }

        if (request.PolicySnapshot.AccessMode != ToolAccessMode.ReadOnly)
        {
            return Block(request, "Audio assist preview requires a read-only policy.", timestamp);
        }

        var artifactRoot = ResolveArtifactRoot(request.ArtifactRoot, timestamp);
        if (!IsSafePetTaskArtifactRoot(artifactRoot))
        {
            return Block(request, "Audio assist artifacts must be written under a pet-tasks artifact folder.", timestamp);
        }

        var endpointStatus = _statusReader.ReadDefaultRenderEndpoint(timestamp);
        var report = new AudioAssistStatusReport(
            "1",
            request.TaskCardId,
            ToolFamily,
            BuildCapabilities(endpointStatus),
            BuildCurrentStatusSummary(endpointStatus),
            BuildSafetyNotes(),
            DidInspectSystemAudio: endpointStatus.IsAvailable,
            DidChangeAudio: false,
            DidMutate: false,
            timestamp,
            endpointStatus);

        Directory.CreateDirectory(artifactRoot);
        var jsonPath = Path.Combine(artifactRoot, "audio-assist-status-report.json");
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
            PreviewSummary: "Wrote audioAssist status report. No audio settings were changed.",
            ResultSummary: $"audioAssist report ready: {markdownPath}",
            AuditLogPath: markdownPath,
            CompletedAtUtc: timestamp);
    }

    private static IReadOnlyList<AudioAssistCapability> BuildCapabilities(AudioEndpointStatus endpointStatus)
    {
        return
        [
            new AudioAssistCapability(
                AudioAssistActionKind.InspectVolume,
                endpointStatus.IsAvailable ? AudioAssistCapabilityStatus.Available : AudioAssistCapabilityStatus.NotImplemented,
                ToolRiskLevel.Low,
                ApprovalRequirement.None,
                endpointStatus.IsAvailable
                    ? "Reads the Windows default output endpoint volume and mute state without changing audio settings."
                    : endpointStatus.Detail),
            new AudioAssistCapability(
                AudioAssistActionKind.SetVolume,
                AudioAssistCapabilityStatus.ApprovalRequired,
                ToolRiskLevel.Medium,
                ApprovalRequirement.BeforeExecution,
                "Future normal Windows volume control only, capped at 100%."),
            new AudioAssistCapability(
                AudioAssistActionKind.Mute,
                AudioAssistCapabilityStatus.ApprovalRequired,
                ToolRiskLevel.Medium,
                ApprovalRequirement.BeforeExecution,
                "Future mute action requires explicit approval."),
            new AudioAssistCapability(
                AudioAssistActionKind.Unmute,
                AudioAssistCapabilityStatus.ApprovalRequired,
                ToolRiskLevel.Medium,
                ApprovalRequirement.BeforeExecution,
                "Future unmute action requires explicit approval."),
            new AudioAssistCapability(
                AudioAssistActionKind.BoostGuide,
                AudioAssistCapabilityStatus.Available,
                ToolRiskLevel.Low,
                ApprovalRequirement.None,
                "Can provide safe guidance for external boost/equalizer tools without changing the system."),
            new AudioAssistCapability(
                AudioAssistActionKind.BoostHandoff,
                AudioAssistCapabilityStatus.Available,
                ToolRiskLevel.Low,
                ApprovalRequirement.None,
                "Can detect FxSound / Equalizer APO evidence and write a handoff guide without installing or editing configs."),
            new AudioAssistCapability(
                AudioAssistActionKind.ExternalEnhancerHandoff,
                AudioAssistCapabilityStatus.Blocked,
                ToolRiskLevel.High,
                ApprovalRequirement.HandOffRequired,
                "Installing/configuring APOs, drivers, FxSound, or Equalizer APO remains a handoff, not an automatic action.")
        ];
    }

    private static string BuildCurrentStatusSummary(AudioEndpointStatus endpointStatus)
    {
        if (!endpointStatus.IsAvailable)
        {
            return $"Read-only audio policy is ready, but live device volume inspection is unavailable: {endpointStatus.Detail}";
        }

        var volume = endpointStatus.MasterVolumePercent.HasValue
            ? endpointStatus.MasterVolumePercent.Value.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) + "%"
            : "unknown";
        var mute = endpointStatus.IsMuted.HasValue ? endpointStatus.IsMuted.Value.ToString() : "unknown";
        return $"Read-only audio policy is ready. Current default output volume is {volume}; muted: {mute}.";
    }

    private static IReadOnlyList<string> BuildSafetyNotes()
    {
        return
        [
            "No audio settings were changed.",
            "No driver/APO/equalizer install or configuration was attempted.",
            "Volume boost beyond normal Windows endpoint volume is blocked.",
            "Future volume changes must require explicit approval and remain capped at 100%."
        ];
    }

    private static string BuildMarkdown(AudioAssistStatusReport report)
    {
        var lines = new List<string>
        {
            "# PET TASKS Audio Assist Status",
            "",
            $"Generated: {report.GeneratedAtUtc:O}",
            $"TaskCard: `{report.TaskCardId}`",
            "",
            "## Summary",
            "",
            $"- {report.CurrentStatusSummary}",
            $"- Inspected system audio: {report.DidInspectSystemAudio}",
            $"- Changed audio: {report.DidChangeAudio}",
            $"- Did mutate files: {report.DidMutate}",
            "",
            "## Capabilities",
            ""
        };

        lines.AddRange(report.Capabilities.Select(capability => $"- {capability.ActionKind}: {capability.Status}, risk {capability.RiskLevel}, approval {capability.ApprovalRequirement}. {capability.Detail}"));
        if (report.EndpointStatus is not null)
        {
            lines.Add("");
            lines.Add("## Endpoint Status");
            lines.Add("");
            lines.Add($"- Source: {report.EndpointStatus.Source}");
            lines.Add($"- Available: {report.EndpointStatus.IsAvailable}");
            lines.Add($"- Master volume: {FormatNullablePercent(report.EndpointStatus.MasterVolumePercent)}");
            lines.Add($"- Muted: {FormatNullableBool(report.EndpointStatus.IsMuted)}");
            lines.Add($"- Endpoint id: `{report.EndpointStatus.EndpointId}`");
            lines.Add($"- Detail: {report.EndpointStatus.Detail}");
        }

        lines.Add("");
        lines.Add("## Safety Notes");
        lines.Add("");
        lines.AddRange(report.SafetyNotes.Select(note => $"- {note}"));
        return string.Join(Environment.NewLine, lines) + Environment.NewLine;
    }

    private static string FormatNullablePercent(double? value)
    {
        return value.HasValue ? value.Value.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) + "%" : "unknown";
    }

    private static string FormatNullableBool(bool? value)
    {
        return value.HasValue ? value.Value.ToString() : "unknown";
    }

    private static string ResolveArtifactRoot(string artifactRoot, DateTimeOffset timestamp)
    {
        if (!string.IsNullOrWhiteSpace(artifactRoot))
        {
            return Path.GetFullPath(artifactRoot);
        }

        var slug = timestamp.ToString("yyyyMMdd-HHmmss") + "-audio-assist";
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
