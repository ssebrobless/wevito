using System.Text.Json;
using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed record CoexistenceResourceSnapshot(
    double NonWevitoCpuPercent = 0,
    double NetworkSaturationPercent = 0);

public sealed record CoexistenceTriggerResult(
    bool IsQuieting,
    IReadOnlyList<string> ActiveTriggers,
    bool MaintenanceCanResume,
    bool ExperimentationCanResume,
    string Reason);

public sealed class CoexistenceTriggerService
{
    public const string TriggerFiredPacketKind = "coexistence_trigger_fired";
    public const string TriggerClearedPacketKind = "coexistence_trigger_cleared";
    public const string PetAnimationForcedPacketKind = "pet_animation_forced";
    public const string AppListSetting = "coexistence_app_list_json";
    public const string FullscreenEnabledSetting = "coexistence_fullscreen_enabled";
    public const string AppListEnabledSetting = "coexistence_app_list_enabled";
    public const string CpuEnabledSetting = "coexistence_cpu_enabled";
    public const string NetworkEnabledSetting = "coexistence_network_enabled";
    public const string CpuThresholdSetting = "coexistence_cpu_threshold_percent";
    public const string NetworkThresholdSetting = "coexistence_network_threshold_percent";
    public const string DefaultSettingsFileName = "coexistence-app-list.json";

    public static readonly IReadOnlyList<string> DefaultQuietAppList =
    [
        "zoom.exe",
        "teams.exe",
        "obs64.exe",
        "discord.exe"
    ];

    private static readonly TimeSpan MaintenanceResumeGrace = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan ExperimentationResumeGrace = TimeSpan.FromMinutes(5);
    private readonly AuditLedgerService? _auditLedgerService;
    private readonly KillSwitchService? _killSwitchService;
    private readonly string _jsonlPath;
    private readonly HashSet<string> _lastActiveTriggers = new(StringComparer.OrdinalIgnoreCase);
    private DateTimeOffset? _lastClearedAtUtc;

    public CoexistenceTriggerService(
        AuditLedgerService? auditLedgerService = null,
        KillSwitchService? killSwitchService = null,
        string? jsonlPath = null)
    {
        _auditLedgerService = auditLedgerService;
        _killSwitchService = killSwitchService;
        _jsonlPath = string.IsNullOrWhiteSpace(jsonlPath) ? ResolveDefaultJsonlPath() : jsonlPath;
    }

    public CoexistenceTriggerResult Evaluate(
        IReadOnlyDictionary<string, string>? settings,
        DesktopContext? desktopContext,
        CoexistenceResourceSnapshot resourceSnapshot,
        DateTimeOffset nowUtc)
    {
        var snapshot = settings ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var active = new List<string>();

        if (ReadBool(snapshot, FullscreenEnabledSetting, true) &&
            desktopContext?.ForegroundWindow is { IsFullscreenApp: true, IsShellSurface: false })
        {
            active.Add("fullscreen");
        }

        if (ReadBool(snapshot, AppListEnabledSetting, true) &&
            IsListedQuietApp(desktopContext?.ForegroundWindow.ProcessName ?? "", ReadQuietAppList(snapshot)))
        {
            active.Add("app_list");
        }

        if (ReadBool(snapshot, CpuEnabledSetting, true) &&
            resourceSnapshot.NonWevitoCpuPercent > ReadDouble(snapshot, CpuThresholdSetting, 80))
        {
            active.Add("cpu_pressure");
        }

        if (ReadBool(snapshot, NetworkEnabledSetting, true) &&
            resourceSnapshot.NetworkSaturationPercent > ReadDouble(snapshot, NetworkThresholdSetting, 80))
        {
            active.Add("network_saturation");
        }

        RecordTransitions(active, nowUtc);
        var elapsedSinceClear = _lastClearedAtUtc is null ? TimeSpan.MaxValue : nowUtc - _lastClearedAtUtc.Value;
        return new CoexistenceTriggerResult(
            active.Count > 0,
            active,
            MaintenanceCanResume: active.Count == 0 && elapsedSinceClear >= MaintenanceResumeGrace,
            ExperimentationCanResume: active.Count == 0 && elapsedSinceClear >= ExperimentationResumeGrace,
            active.Count == 0 ? "" : $"Coexistence quiet active: {string.Join(", ", active)}.");
    }

    public PetAnimationState ResolveVisiblePetAnimation(PetAnimationState requested, CoexistenceTriggerResult coexistence, DoNotDisturbState dnd)
    {
        return coexistence.IsQuieting || dnd.IsActive ? PetAnimationState.Sleep : requested;
    }

    public void RecordPetAnimationForced(DateTimeOffset nowUtc, string reason)
    {
        Record(PetAnimationForcedPacketKind, nowUtc, reason);
    }

    public string EnsureDefaultAppListFile()
    {
        var path = ResolveDefaultAppListPath();
        if (_killSwitchService?.IsActive() == true)
        {
            return path;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
        if (!File.Exists(path))
        {
            File.WriteAllText(path, JsonSerializer.Serialize(DefaultQuietAppList, new JsonSerializerOptions { WriteIndented = true }));
        }

        return path;
    }

    private void RecordTransitions(IReadOnlyList<string> active, DateTimeOffset nowUtc)
    {
        var next = active.ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var trigger in next.Where(trigger => !_lastActiveTriggers.Contains(trigger)))
        {
            Record(TriggerFiredPacketKind, nowUtc, $"trigger={trigger}");
        }

        foreach (var trigger in _lastActiveTriggers.Where(trigger => !next.Contains(trigger)).ToList())
        {
            Record(TriggerClearedPacketKind, nowUtc, $"trigger={trigger}");
        }

        if (_lastActiveTriggers.Count > 0 && next.Count == 0)
        {
            _lastClearedAtUtc = nowUtc;
        }

        _lastActiveTriggers.Clear();
        foreach (var trigger in next)
        {
            _lastActiveTriggers.Add(trigger);
        }
    }

    private static bool IsListedQuietApp(string processName, IReadOnlyList<string> quietApps)
    {
        return quietApps.Any(app => string.Equals(app, processName, StringComparison.OrdinalIgnoreCase));
    }

    private static IReadOnlyList<string> ReadQuietAppList(IReadOnlyDictionary<string, string> settings)
    {
        if (!settings.TryGetValue(AppListSetting, out var raw) || string.IsNullOrWhiteSpace(raw))
        {
            return DefaultQuietAppList;
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(raw) ?? DefaultQuietAppList;
        }
        catch (JsonException)
        {
            return DefaultQuietAppList;
        }
    }

    private static bool ReadBool(IReadOnlyDictionary<string, string> settings, string key, bool defaultValue)
    {
        return settings.TryGetValue(key, out var raw) && bool.TryParse(raw, out var parsed)
            ? parsed
            : defaultValue;
    }

    private static double ReadDouble(IReadOnlyDictionary<string, string> settings, string key, double defaultValue)
    {
        return settings.TryGetValue(key, out var raw) && double.TryParse(raw, out var parsed)
            ? parsed
            : defaultValue;
    }

    private void Record(string packetKind, DateTimeOffset nowUtc, string summary)
    {
        if (_killSwitchService?.IsActive() == true)
        {
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(_jsonlPath) ?? ".");
        File.AppendAllText(_jsonlPath, JsonSerializer.Serialize(new
        {
            packet_kind = packetKind,
            created_at_utc = nowUtc,
            did_use_network = false,
            did_use_hosted_ai = false,
            did_use_local_model = false,
            did_mutate = false,
            summary
        }) + Environment.NewLine);

        _auditLedgerService?.Record(new EvidencePacket(
            Guid.NewGuid(),
            packetKind,
            null,
            nowUtc,
            DidUseNetwork: false,
            DidUseHostedAi: false,
            DidUseLocalModel: false,
            DidMutate: false,
            ArtifactPath: _jsonlPath,
            Summary: summary,
            Status: "Completed"));
    }

    private static string ResolveDefaultJsonlPath()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Wevito",
            "audit",
            "coexistence-events.jsonl");
    }

    private static string ResolveDefaultAppListPath()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Wevito",
            "settings",
            DefaultSettingsFileName);
    }
}
