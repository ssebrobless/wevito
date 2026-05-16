using System.Runtime.InteropServices;
using System.Text.Json;
using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed record DiskIoBudgetSnapshot(
    double WriteMegabytesPerSecond,
    DateTimeOffset CapturedAtUtc);

public sealed record DiskIoBudgetDecision(
    bool ShouldThrottle,
    TimeSpan SuggestedDelay,
    double CapMegabytesPerSecond,
    string Reason);

public sealed class DiskIoBudgetService
{
    public const string EnabledSetting = "resource_disk_io_budget_enabled";
    public const string DiskIoBudgetThrottledPacketKind = "disk_io_budget_throttled";
    public const double DefaultActiveUserCapMegabytesPerSecond = 20;
    private static readonly TimeSpan ActiveUserWindow = TimeSpan.FromSeconds(30);

    private readonly Func<DateTimeOffset> _clock;
    private readonly Func<DateTimeOffset> _lastInputReader;
    private readonly Func<DiskIoBudgetSnapshot> _diskSnapshotReader;
    private readonly AuditLedgerService? _auditLedgerService;
    private readonly KillSwitchService? _killSwitchService;
    private readonly string _jsonlPath;

    public DiskIoBudgetService(
        Func<DateTimeOffset>? clock = null,
        Func<DateTimeOffset>? lastInputReader = null,
        Func<DiskIoBudgetSnapshot>? diskSnapshotReader = null,
        AuditLedgerService? auditLedgerService = null,
        KillSwitchService? killSwitchService = null,
        string? jsonlPath = null)
    {
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
        _lastInputReader = lastInputReader ?? ReadLastInputUtc;
        _diskSnapshotReader = diskSnapshotReader ?? (() => new DiskIoBudgetSnapshot(0, DateTimeOffset.UtcNow));
        _auditLedgerService = auditLedgerService;
        _killSwitchService = killSwitchService;
        _jsonlPath = string.IsNullOrWhiteSpace(jsonlPath) ? ResolveDefaultJsonlPath() : jsonlPath;
    }

    public DiskIoBudgetDecision Evaluate(IReadOnlyDictionary<string, string>? settings = null)
    {
        if (_killSwitchService?.IsActive() == true)
        {
            return new DiskIoBudgetDecision(false, TimeSpan.Zero, DefaultActiveUserCapMegabytesPerSecond, "kill_switch=true");
        }

        if (!ReadBool(settings, EnabledSetting, true))
        {
            return new DiskIoBudgetDecision(false, TimeSpan.Zero, DefaultActiveUserCapMegabytesPerSecond, "disk I/O budget disabled.");
        }

        var now = _clock();
        var lastInput = _lastInputReader();
        if (now - lastInput > ActiveUserWindow)
        {
            return new DiskIoBudgetDecision(false, TimeSpan.Zero, DefaultActiveUserCapMegabytesPerSecond, "user idle; disk writes are not capped.");
        }

        var snapshot = _diskSnapshotReader();
        if (snapshot.WriteMegabytesPerSecond <= DefaultActiveUserCapMegabytesPerSecond)
        {
            return new DiskIoBudgetDecision(false, TimeSpan.Zero, DefaultActiveUserCapMegabytesPerSecond, "");
        }

        var decision = new DiskIoBudgetDecision(
            true,
            TimeSpan.FromSeconds(1),
            DefaultActiveUserCapMegabytesPerSecond,
            $"disk write rate {snapshot.WriteMegabytesPerSecond:0.0} MB/s exceeds active-user cap {DefaultActiveUserCapMegabytesPerSecond:0.0} MB/s.");
        Record(now, decision.Reason);
        return decision;
    }

    public async Task YieldIfNeededAsync(IReadOnlyDictionary<string, string>? settings = null, CancellationToken cancellationToken = default)
    {
        var decision = Evaluate(settings);
        if (decision.ShouldThrottle && decision.SuggestedDelay > TimeSpan.Zero)
        {
            await Task.Delay(decision.SuggestedDelay, cancellationToken).ConfigureAwait(false);
        }
    }

    private void Record(DateTimeOffset nowUtc, string summary)
    {
        if (_killSwitchService?.IsActive() == true)
        {
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(_jsonlPath) ?? ".");
        File.AppendAllText(_jsonlPath, JsonSerializer.Serialize(new
        {
            packet_kind = DiskIoBudgetThrottledPacketKind,
            created_at_utc = nowUtc,
            did_use_network = false,
            did_use_hosted_ai = false,
            did_use_local_model = false,
            did_mutate = false,
            summary
        }) + Environment.NewLine);

        _auditLedgerService?.Record(new EvidencePacket(
            Guid.NewGuid(),
            DiskIoBudgetThrottledPacketKind,
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

    private static DateTimeOffset ReadLastInputUtc()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return DateTimeOffset.MinValue;
        }

        return DateTimeOffset.UtcNow;
    }

    private static string ResolveDefaultJsonlPath()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Wevito",
            "audit",
            "disk-io-throttle-events.jsonl");
    }
}
