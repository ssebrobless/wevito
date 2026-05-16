using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed record MonitorDescriptor(
    string Id,
    bool IsPrimary);

public sealed record MonitorResolution(
    string MonitorId,
    bool FellBackToPrimary,
    string Reason);

public sealed class MultiMonitorService
{
    public const string PreferredMonitorSetting = "pet_preferred_monitor";
    public const string MultiMonitorPreferenceSetPacketKind = "multi_monitor_preference_set";

    private readonly AuditLedgerService? _auditLedgerService;
    private readonly KillSwitchService? _killSwitchService;

    public MultiMonitorService(
        AuditLedgerService? auditLedgerService = null,
        KillSwitchService? killSwitchService = null)
    {
        _auditLedgerService = auditLedgerService;
        _killSwitchService = killSwitchService;
    }

    public MonitorResolution ResolvePreferredMonitor(
        IReadOnlyDictionary<string, string>? settings,
        IReadOnlyList<MonitorDescriptor> availableMonitors)
    {
        var primary = availableMonitors.FirstOrDefault(item => item.IsPrimary) ?? availableMonitors.FirstOrDefault();
        if (primary is null)
        {
            return new MonitorResolution("", true, "no_monitor_available");
        }

        var preferred = settings is not null && settings.TryGetValue(PreferredMonitorSetting, out var raw)
            ? raw
            : "";
        if (!string.IsNullOrWhiteSpace(preferred) &&
            availableMonitors.Any(item => string.Equals(item.Id, preferred, StringComparison.OrdinalIgnoreCase)))
        {
            return new MonitorResolution(preferred, false, "preferred_monitor_connected");
        }

        return new MonitorResolution(primary.Id, !string.IsNullOrWhiteSpace(preferred), "fallback_to_primary");
    }

    public IReadOnlyDictionary<string, string> SetPreference(
        IReadOnlyDictionary<string, string> settings,
        string monitorId,
        DateTimeOffset nowUtc)
    {
        var next = new Dictionary<string, string>(settings, StringComparer.OrdinalIgnoreCase)
        {
            [PreferredMonitorSetting] = monitorId
        };
        Record(nowUtc, $"Set preferred pet monitor to {monitorId}.");
        return next;
    }

    private void Record(DateTimeOffset timestamp, string summary)
    {
        if (_killSwitchService?.IsActive() == true)
        {
            return;
        }

        _auditLedgerService?.Record(new EvidencePacket(
            Guid.NewGuid(),
            MultiMonitorPreferenceSetPacketKind,
            null,
            timestamp,
            DidUseNetwork: false,
            DidUseHostedAi: false,
            DidUseLocalModel: false,
            DidMutate: false,
            ArtifactPath: "",
            Summary: summary,
            Status: "Saved",
            Error: ""));
    }
}
