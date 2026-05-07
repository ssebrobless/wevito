using System.Diagnostics;
using System.Text.Json;
using Microsoft.Win32;
using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed class AudioBoostHandoffAdapter
{
    private const string ToolFamily = "audioAssist";
    private readonly IAudioBoostEnvironment _environment;

    public AudioBoostHandoffAdapter()
        : this(new WindowsAudioBoostEnvironment())
    {
    }

    public AudioBoostHandoffAdapter(IAudioBoostEnvironment environment)
    {
        _environment = environment;
    }

    public TaskAdapterResult BuildSetupGuide(TaskAdapterRequest request, DateTimeOffset? nowUtc = null)
    {
        var timestamp = nowUtc ?? DateTimeOffset.UtcNow;
        if (request.RunMode != TaskAdapterRunMode.DryRunPreview)
        {
            return Block(request, "Audio boost handoff only supports dry-run preview mode.", timestamp);
        }

        if (!string.Equals(request.PolicySnapshot.ToolFamily, ToolFamily, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(request.Intent.RequestedToolFamily, ToolFamily, StringComparison.OrdinalIgnoreCase))
        {
            return Block(request, "Task intent and policy must both target audioAssist.", timestamp);
        }

        if (request.PolicySnapshot.AccessMode != ToolAccessMode.ReadOnly)
        {
            return Block(request, "Audio boost handoff requires a read-only policy.", timestamp);
        }

        var artifactRoot = ResolveArtifactRoot(request.ArtifactRoot, timestamp);
        if (!IsSafePetTaskArtifactRoot(artifactRoot))
        {
            return Block(request, "Audio boost handoff artifacts must be written under a pet-tasks artifact folder.", timestamp);
        }

        var report = new AudioBoostHandoffReport(
            "1",
            request.TaskCardId,
            ToolFamily,
            DetectTools(),
            BuildSafetyNotes(),
            DidInstallOrConfigure: false,
            DidMutate: false,
            timestamp);

        Directory.CreateDirectory(artifactRoot);
        var jsonPath = Path.Combine(artifactRoot, "audio-boost-handoff-report.json");
        var guidePath = Path.Combine(artifactRoot, "setup-guide.md");
        var boostPlanPath = Path.Combine(artifactRoot, "boost-plan.txt");
        File.WriteAllText(jsonPath, JsonSerializer.Serialize(report, JsonDefaults.Options));
        File.WriteAllText(guidePath, BuildMarkdown(report));
        File.WriteAllText(boostPlanPath, BuildBoostPlanText(report));

        return new TaskAdapterResult(
            request.TaskCardId,
            ToolFamily,
            TaskAdapterResultStatus.PreviewReady,
            DidMutate: false,
            ReadPaths: [],
            WrittenPaths: [jsonPath, guidePath, boostPlanPath],
            PreviewSummary: "Wrote audio boost handoff guide and conservative preset plans. No installer/config/elevation action was run.",
            ResultSummary: $"audioAssist boost handoff ready: {guidePath}",
            AuditLogPath: guidePath,
            CompletedAtUtc: timestamp);
    }

    private IReadOnlyList<AudioBoostToolStatus> DetectTools()
    {
        var fxEvidence = new List<string>();
        if (_environment.RegistryDisplayNames().Any(name => name.Contains("FxSound", StringComparison.OrdinalIgnoreCase)))
        {
            fxEvidence.Add("FxSound uninstall registry entry found.");
        }

        if (_environment.IsProcessRunning("fxsound"))
        {
            fxEvidence.Add("FxSound process is running.");
        }

        if (_environment.DirectoryExists(@"C:\Program Files\FxSound"))
        {
            fxEvidence.Add(@"C:\Program Files\FxSound exists.");
        }

        var equalizerEvidence = new List<string>();
        if (_environment.FileExists(@"C:\Program Files\EqualizerAPO\config\config.txt"))
        {
            equalizerEvidence.Add(@"C:\Program Files\EqualizerAPO\config\config.txt exists.");
        }
        else if (_environment.DirectoryExists(@"C:\Program Files\EqualizerAPO"))
        {
            equalizerEvidence.Add(@"C:\Program Files\EqualizerAPO exists, but config.txt was not found.");
        }

        return
        [
            new AudioBoostToolStatus(
                "FxSound",
                fxEvidence.Count switch
                {
                    >= 2 => AudioBoostDetectionStatus.Installed,
                    1 => AudioBoostDetectionStatus.Partial,
                    _ => AudioBoostDetectionStatus.NotInstalled
                },
                fxEvidence.Count == 0 ? ["No FxSound registry/process/path evidence found."] : fxEvidence,
                "https://www.fxsound.com/",
                "External enhancer handoff only; Wevito does not install or configure FxSound."),
            new AudioBoostToolStatus(
                "Equalizer APO",
                equalizerEvidence.Any(evidence => evidence.EndsWith("config.txt exists.", StringComparison.Ordinal))
                    ? AudioBoostDetectionStatus.Installed
                    : equalizerEvidence.Count == 1 ? AudioBoostDetectionStatus.Partial : AudioBoostDetectionStatus.NotInstalled,
                equalizerEvidence.Count == 0 ? ["No Equalizer APO config path evidence found."] : equalizerEvidence,
                "https://sourceforge.net/projects/equalizerapo/",
                "APO handoff only; Wevito never edits Equalizer APO config.txt.")
        ];
    }

    private static IReadOnlyList<string> BuildSafetyNotes()
    {
        return
        [
            "No installer was launched.",
            "No driver, APO, enhancer, or Equalizer APO config was modified.",
            "No elevation was requested.",
            "True system-wide boosting should be handled by explicit external tools, not silent Wevito automation.",
            "Safe-listening reminder: WHO guidance warns that high volume over time can damage hearing; keep levels modest.",
            "Loudness ceiling reminder: EBU R 128 style mastering typically uses a -1 dBTP true-peak ceiling to avoid clipping."
        ];
    }

    private static string BuildMarkdown(AudioBoostHandoffReport report)
    {
        var lines = new List<string>
        {
            "# PET TASKS Audio Boost Handoff",
            "",
            $"Generated: {report.GeneratedAtUtc:O}",
            $"TaskCard: `{report.TaskCardId}`",
            "",
            "## Summary",
            "",
            "- Wevito did not install software, edit APO/driver configuration, or request elevation.",
            "- This guide only reports detected external tools and safe setup direction.",
            "",
            "## Tool Status",
            ""
        };

        foreach (var tool in report.Tools)
        {
            lines.Add($"### {tool.ToolName}");
            lines.Add("");
            lines.Add($"- Status: {tool.Status}");
            lines.Add($"- Official URL: {tool.OfficialUrl}");
            lines.Add($"- Detail: {tool.Detail}");
            lines.Add("- Evidence:");
            lines.AddRange(tool.Evidence.Select(evidence => $"  - {evidence}"));
            lines.Add("");
        }

        lines.Add("## Setup Guidance");
        lines.Add("");
        lines.Add("- FxSound: install only from the official FxSound site if you choose to use it. Reboot may be required for audio effects to settle.");
        lines.Add(@"- Equalizer APO: install from SourceForge, then configure manually at `C:\Program Files\EqualizerAPO\config\config.txt`. Wevito will not edit this file.");
        lines.Add("- Keep gain conservative. If audio distorts, reduce gain immediately.");
        lines.Add("");
        lines.Add("## Safety Notes");
        lines.Add("");
        lines.AddRange(report.SafetyNotes.Select(note => $"- {note}"));
        return string.Join(Environment.NewLine, lines) + Environment.NewLine;
    }

    private static string BuildBoostPlanText(AudioBoostHandoffReport report)
    {
        var lines = new List<string>
        {
            "PET TASKS Audio Boost Preset Plans",
            $"Generated: {report.GeneratedAtUtc:O}",
            "",
            "IMPORTANT SAFETY AND CONTROL NOTES",
            "User applies manually; Wevito does not edit your config.",
            "WHO safe-listening note: keep exposure around or below 80 dB(A) for 40 h/week where possible. Source: https://www.who.int/news-room/questions-and-answers/item/deafness-and-hearing-loss-safe-listening",
            "EBU R 128 true-peak note: keep a -1 dBTP true-peak ceiling to reduce clipping risk. Source: https://tech.ebu.ch/publications/r128",
            "All gain values in these starter plans are conservative and capped below +6 dB.",
            "",
            "PRESET: Warmth",
            "Purpose: gentle low-shelf body without chasing loudness.",
            "Equalizer APO style block:",
            "Preamp: 0 dB",
            "Filter: ON LSC Fc 200 Hz Gain +1.5 dB Q 0.7",
            "Limiter note: if this causes distortion, reduce preamp by -1 dB before increasing any gain.",
            "",
            "PRESET: Speech clarity",
            "Purpose: modest presence lift for voices without harsh treble.",
            "Equalizer APO style block:",
            "Preamp: -1 dB",
            "Filter: ON PK Fc 3000 Hz Gain +2 dB Q 1.0",
            "Filter: ON HSC Fc 6000 Hz Gain +0.75 dB Q 0.7",
            "Limiter note: keep final output below a -1 dBTP true-peak ceiling.",
            "",
            "PRESET: Modest +3 dB safe boost",
            "Purpose: small overall lift while leaving clipping headroom guidance visible.",
            "Equalizer APO style block:",
            "Preamp: +3 dB",
            "Filter: ON PK Fc 120 Hz Gain +0 dB Q 1.0",
            "Limiter note: use an external limiter or reduce app/player volume so true peaks stay at or below -1 dBTP.",
            "",
            "DO NOT PASTE BLINDLY",
            "Read the Equalizer APO documentation and apply manually only if you understand the change.",
            @"Equalizer APO config path, if installed: C:\Program Files\EqualizerAPO\config\config.txt",
            "Wevito never edits that file, never installs APOs, and never requests elevation for these plans."
        };

        return string.Join(Environment.NewLine, lines) + Environment.NewLine;
    }

    private static string ResolveArtifactRoot(string artifactRoot, DateTimeOffset timestamp)
    {
        if (!string.IsNullOrWhiteSpace(artifactRoot))
        {
            return Path.GetFullPath(artifactRoot);
        }

        var slug = timestamp.ToString("yyyyMMdd-HHmmss") + "-audio-boost-handoff";
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

public interface IAudioBoostEnvironment
{
    bool FileExists(string path);

    bool DirectoryExists(string path);

    bool IsProcessRunning(string processName);

    IReadOnlyList<string> RegistryDisplayNames();
}

public sealed class WindowsAudioBoostEnvironment : IAudioBoostEnvironment
{
    public bool FileExists(string path) => File.Exists(path);

    public bool DirectoryExists(string path) => Directory.Exists(path);

    public bool IsProcessRunning(string processName)
    {
        return Process.GetProcessesByName(processName).Length > 0;
    }

    public IReadOnlyList<string> RegistryDisplayNames()
    {
        if (!OperatingSystem.IsWindows())
        {
            return [];
        }

        var names = new List<string>();
        foreach (var root in new[] { Registry.LocalMachine, Registry.CurrentUser })
        {
            using var uninstall = root.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");
            if (uninstall is null)
            {
                continue;
            }

            foreach (var subKeyName in uninstall.GetSubKeyNames())
            {
                using var subKey = uninstall.OpenSubKey(subKeyName);
                var displayName = subKey?.GetValue("DisplayName")?.ToString();
                if (!string.IsNullOrWhiteSpace(displayName))
                {
                    names.Add(displayName);
                }
            }
        }

        return names;
    }
}
