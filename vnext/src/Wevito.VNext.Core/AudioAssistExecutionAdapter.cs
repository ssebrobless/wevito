using System.Text.Json;
using System.Text.RegularExpressions;
using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed class AudioAssistExecutionAdapter
{
    private const string ToolFamily = "audioAssist";
    private readonly IAudioEndpointController _controller;

    public AudioAssistExecutionAdapter()
        : this(new WindowsAudioEndpointStatusReader())
    {
    }

    public AudioAssistExecutionAdapter(IAudioEndpointController controller)
    {
        _controller = controller ?? throw new ArgumentNullException(nameof(controller));
    }

    public TaskAdapterResult Execute(TaskAdapterRequest request, DateTimeOffset? nowUtc = null)
    {
        var timestamp = nowUtc ?? DateTimeOffset.UtcNow;
        if (request.RunMode != TaskAdapterRunMode.Execute)
        {
            return Block(request, "Audio assist execution requires explicit execute mode.", timestamp);
        }

        if (!string.Equals(request.PolicySnapshot.ToolFamily, ToolFamily, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(request.Intent.RequestedToolFamily, ToolFamily, StringComparison.OrdinalIgnoreCase))
        {
            return Block(request, "Task intent and policy must both target audioAssist.", timestamp);
        }

        if (request.PolicySnapshot.AccessMode != ToolAccessMode.Write ||
            request.PolicySnapshot.ApprovalRequirement == ApprovalRequirement.None)
        {
            return Block(request, "Audio assist execution requires an approval-gated write policy.", timestamp);
        }

        var artifactRoot = ResolveArtifactRoot(request.ArtifactRoot, timestamp);
        if (!IsSafePetTaskArtifactRoot(artifactRoot))
        {
            return Block(request, "Audio assist execution artifacts must be written under a pet-tasks artifact folder.", timestamp);
        }

        var command = ParseCommand(request.Intent.RawText);
        if (command.ActionKind is AudioAssistActionKind.InspectVolume)
        {
            return Block(request, "Volume inspection is handled by preview mode. Execution requires a set volume, mute, or unmute request.", timestamp);
        }

        if (command.ActionKind is AudioAssistActionKind.BoostGuide or AudioAssistActionKind.ExternalEnhancerHandoff)
        {
            return Block(request, "Volume boost beyond normal Windows endpoint volume is blocked. Use the guidance/handoff path instead.", timestamp);
        }

        if (command.ActionKind == AudioAssistActionKind.SetVolume &&
            (command.VolumePercent is null or < 0d or > 100d))
        {
            return Block(request, "Set volume execution requires a target between 0% and 100%.", timestamp);
        }

        var before = _controller.ReadDefaultRenderEndpoint(timestamp);
        if (!before.IsAvailable)
        {
            return Block(request, $"Cannot change audio because endpoint inspection failed: {before.Detail}", timestamp);
        }

        var after = command.ActionKind switch
        {
            AudioAssistActionKind.SetVolume => _controller.SetDefaultRenderVolume(command.VolumePercent!.Value, timestamp),
            AudioAssistActionKind.Mute => _controller.SetDefaultRenderMute(true, timestamp),
            AudioAssistActionKind.Unmute => _controller.SetDefaultRenderMute(false, timestamp),
            _ => before
        };

        if (!after.IsAvailable)
        {
            return Block(request, $"Audio command did not complete: {after.Detail}", timestamp);
        }

        var report = new AudioAssistExecutionReport(
            "1",
            request.TaskCardId,
            ToolFamily,
            command.ActionKind,
            command.VolumePercent,
            before,
            after,
            BuildSafetyNotes(),
            DidChangeAudio: true,
            DidMutateFiles: false,
            timestamp);

        Directory.CreateDirectory(artifactRoot);
        var jsonPath = Path.Combine(artifactRoot, "audio-assist-execution-report.json");
        var markdownPath = Path.Combine(artifactRoot, "run-summary.md");
        File.WriteAllText(jsonPath, JsonSerializer.Serialize(report, JsonDefaults.Options));
        File.WriteAllText(markdownPath, BuildMarkdown(report));

        return new TaskAdapterResult(
            request.TaskCardId,
            ToolFamily,
            TaskAdapterResultStatus.Completed,
            DidMutate: true,
            ReadPaths: [],
            WrittenPaths: [jsonPath, markdownPath],
            ResultSummary: $"audioAssist execution complete: {markdownPath}",
            AuditLogPath: markdownPath,
            CompletedAtUtc: timestamp);
    }

    private static AudioAssistCommand ParseCommand(string rawText)
    {
        if (Regex.IsMatch(rawText, @"\b(boost|louder than|over\s*100|equalizer|fxsound|apo)\b", RegexOptions.IgnoreCase))
        {
            return new AudioAssistCommand(AudioAssistActionKind.BoostGuide, null);
        }

        if (Regex.IsMatch(rawText, @"\bunmute\b", RegexOptions.IgnoreCase))
        {
            return new AudioAssistCommand(AudioAssistActionKind.Unmute, null);
        }

        if (Regex.IsMatch(rawText, @"\bmute\b", RegexOptions.IgnoreCase))
        {
            return new AudioAssistCommand(AudioAssistActionKind.Mute, null);
        }

        var percentMatch = Regex.Match(rawText, @"\b(?:set|change|turn|put|make)\b.*?\bvolume\b.*?(?<percent>\d{1,3})(?:\s*%)?", RegexOptions.IgnoreCase);
        if (!percentMatch.Success)
        {
            percentMatch = Regex.Match(rawText, @"\bvolume\b.*?\b(?:to|at)\b\s*(?<percent>\d{1,3})(?:\s*%)?", RegexOptions.IgnoreCase);
        }

        if (percentMatch.Success && double.TryParse(percentMatch.Groups["percent"].Value, out var volumePercent))
        {
            return new AudioAssistCommand(AudioAssistActionKind.SetVolume, volumePercent);
        }

        return new AudioAssistCommand(AudioAssistActionKind.InspectVolume, null);
    }

    private static IReadOnlyList<string> BuildSafetyNotes()
    {
        return
        [
            "Audio was changed only because execute mode and an approval-gated write policy were provided.",
            "Only normal Windows endpoint volume/mute state was touched.",
            "Volume is capped to the normal 0% to 100% endpoint range.",
            "No drivers, APOs, equalizers, external enhancer apps, project files, or sprite assets were changed."
        ];
    }

    private static string BuildMarkdown(AudioAssistExecutionReport report)
    {
        var lines = new List<string>
        {
            "# PET TASKS Audio Assist Execution",
            "",
            $"Generated: {report.GeneratedAtUtc:O}",
            $"TaskCard: `{report.TaskCardId}`",
            "",
            "## Summary",
            "",
            $"- Action: {report.ActionKind}",
            $"- Requested volume: {FormatNullablePercent(report.RequestedVolumePercent)}",
            $"- Changed audio: {report.DidChangeAudio}",
            $"- Mutated files: {report.DidMutateFiles}",
            "",
            "## Before",
            "",
            FormatEndpoint(report.BeforeStatus),
            "",
            "## After",
            "",
            FormatEndpoint(report.AfterStatus),
            "",
            "## Safety Notes",
            ""
        };

        lines.AddRange(report.SafetyNotes.Select(note => $"- {note}"));
        return string.Join(Environment.NewLine, lines) + Environment.NewLine;
    }

    private static string FormatEndpoint(AudioEndpointStatus? status)
    {
        if (status is null)
        {
            return "- unavailable";
        }

        return string.Join(Environment.NewLine, [
            $"- Available: {status.IsAvailable}",
            $"- Master volume: {FormatNullablePercent(status.MasterVolumePercent)}",
            $"- Muted: {FormatNullableBool(status.IsMuted)}",
            $"- Endpoint id: `{status.EndpointId}`",
            $"- Detail: {status.Detail}"
        ]);
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

        var slug = timestamp.ToString("yyyyMMdd-HHmmss") + "-audio-assist-execution";
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

    private sealed record AudioAssistCommand(AudioAssistActionKind ActionKind, double? VolumePercent);
}
