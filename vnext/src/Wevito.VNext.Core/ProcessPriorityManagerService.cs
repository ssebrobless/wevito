using System.Diagnostics;
using System.ComponentModel;
using System.Text.Json;
using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed record ProcessPriorityTarget(
    int ProcessId,
    string ProcessName,
    ProcessPriorityClass CurrentPriority,
    Action<ProcessPriorityClass> SetPriority);

public sealed record ProcessPriorityChange(
    bool Applied,
    int ProcessId,
    string ProcessName,
    ProcessPriorityClass RequestedPriority,
    ProcessPriorityClass EffectivePriority,
    string Reason);

public sealed class ProcessPriorityManagerService
{
    public const string EnabledSetting = "resource_priority_below_normal_enabled";
    public const string ProcessPrioritySetPacketKind = "process_priority_set";
    public const string ProcessPriorityBoostedPacketKind = "process_priority_boosted";

    private readonly AuditLedgerService? _auditLedgerService;
    private readonly KillSwitchService? _killSwitchService;
    private readonly Func<DateTimeOffset> _clock;
    private readonly string _jsonlPath;
    private readonly Dictionary<int, DateTimeOffset> _boostExpiresAtUtc = new();

    public ProcessPriorityManagerService(
        AuditLedgerService? auditLedgerService = null,
        KillSwitchService? killSwitchService = null,
        Func<DateTimeOffset>? clock = null,
        string? jsonlPath = null)
    {
        _auditLedgerService = auditLedgerService;
        _killSwitchService = killSwitchService;
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
        _jsonlPath = string.IsNullOrWhiteSpace(jsonlPath) ? ResolveDefaultJsonlPath() : jsonlPath;
    }

    public ProcessPriorityChange ApplyBelowNormal(ProcessPriorityTarget target, IReadOnlyDictionary<string, string>? settings = null)
    {
        if (_killSwitchService?.IsActive() == true)
        {
            return Blocked(target, ProcessPriorityClass.BelowNormal, "kill_switch=true");
        }

        if (!ReadBool(settings, EnabledSetting, true))
        {
            return Blocked(target, ProcessPriorityClass.BelowNormal, "resource priority policy disabled.");
        }

        return Apply(target, ProcessPriorityClass.BelowNormal, ProcessPrioritySetPacketKind);
    }

    public ProcessPriorityChange ApplyBelowNormalToCurrentProcess(IReadOnlyDictionary<string, string>? settings = null)
    {
        using var process = Process.GetCurrentProcess();
        return ApplyBelowNormal(FromProcess(process), settings);
    }

    public ProcessPriorityChange BoostForForeground(ProcessPriorityTarget target, TimeSpan duration)
    {
        if (_killSwitchService?.IsActive() == true)
        {
            return Blocked(target, ProcessPriorityClass.Normal, "kill_switch=true");
        }

        var change = Apply(target, ProcessPriorityClass.Normal, ProcessPriorityBoostedPacketKind);
        if (change.Applied)
        {
            _boostExpiresAtUtc[target.ProcessId] = _clock().Add(duration);
        }

        return change;
    }

    public IReadOnlyList<ProcessPriorityChange> DecayExpiredBoosts(IReadOnlyList<ProcessPriorityTarget> targets)
    {
        if (_killSwitchService?.IsActive() == true)
        {
            return [];
        }

        var now = _clock();
        var changes = new List<ProcessPriorityChange>();
        foreach (var target in targets)
        {
            if (_boostExpiresAtUtc.TryGetValue(target.ProcessId, out var expiresAt) && now >= expiresAt)
            {
                _boostExpiresAtUtc.Remove(target.ProcessId);
                changes.Add(Apply(target, ProcessPriorityClass.BelowNormal, ProcessPrioritySetPacketKind));
            }
        }

        return changes;
    }

    public static ProcessPriorityChange RefuseUnsafePriority(ProcessPriorityTarget target, ProcessPriorityClass requestedPriority)
    {
        return requestedPriority is ProcessPriorityClass.AboveNormal or ProcessPriorityClass.High or ProcessPriorityClass.RealTime
            ? new ProcessPriorityChange(false, target.ProcessId, target.ProcessName, requestedPriority, target.CurrentPriority, "Wevito never sets itself above Normal priority.")
            : new ProcessPriorityChange(true, target.ProcessId, target.ProcessName, requestedPriority, requestedPriority, "");
    }

    public static ProcessPriorityTarget FromProcess(Process process)
    {
        return new ProcessPriorityTarget(
            process.Id,
            process.ProcessName,
            process.PriorityClass,
            priority => process.PriorityClass = priority);
    }

    private ProcessPriorityChange Apply(ProcessPriorityTarget target, ProcessPriorityClass requestedPriority, string packetKind)
    {
        var safety = RefuseUnsafePriority(target, requestedPriority);
        if (!safety.Applied)
        {
            return safety;
        }

        try
        {
            target.SetPriority(requestedPriority);
            var change = new ProcessPriorityChange(true, target.ProcessId, target.ProcessName, requestedPriority, requestedPriority, "");
            Record(packetKind, _clock(), $"process={target.ProcessName} pid={target.ProcessId} priority={requestedPriority}");
            return change;
        }
        catch (Exception exception) when (exception is InvalidOperationException or Win32Exception or PlatformNotSupportedException)
        {
            return new ProcessPriorityChange(false, target.ProcessId, target.ProcessName, requestedPriority, target.CurrentPriority, exception.Message);
        }
    }

    private static ProcessPriorityChange Blocked(ProcessPriorityTarget target, ProcessPriorityClass requestedPriority, string reason)
    {
        return new ProcessPriorityChange(false, target.ProcessId, target.ProcessName, requestedPriority, target.CurrentPriority, reason);
    }

    private void Record(string packetKind, DateTimeOffset nowUtc, string summary)
    {
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

    private static bool ReadBool(IReadOnlyDictionary<string, string>? settings, string key, bool defaultValue)
    {
        return settings is not null &&
            settings.TryGetValue(key, out var raw) &&
            bool.TryParse(raw, out var parsed)
            ? parsed
            : defaultValue;
    }

    private static string ResolveDefaultJsonlPath()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Wevito",
            "audit",
            "process-priority-events.jsonl");
    }
}
