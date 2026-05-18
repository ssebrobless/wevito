namespace Wevito.VNext.Core;

public enum LocalBrainAvailability
{
    Starting,
    Ready,
    Offline,
    Dormant,
    Blocked
}

public sealed record LocalBrainStatus(
    LocalBrainAvailability Availability,
    string RuntimeId,
    string Endpoint,
    string Model,
    string Reason,
    DateTimeOffset LastProbeAtUtc,
    bool DidUseLocalModel)
{
    public static LocalBrainStatus Starting(DateTimeOffset nowUtc) => new(
        LocalBrainAvailability.Starting,
        RuntimeId: "ollama",
        Endpoint: LocalRuntimeProbeService.DefaultOllamaEndpoint,
        Model: LocalRuntimeProbeService.DefaultOllamaModel,
        Reason: "Local brain has not been probed yet.",
        LastProbeAtUtc: nowUtc,
        DidUseLocalModel: false);
}

public sealed class LocalBrainHeartbeatService
{
    public const string HeartbeatPacketKind = "local_brain_heartbeat";
    public static readonly TimeSpan ProbeInterval = TimeSpan.FromSeconds(60);
    public static readonly TimeSpan PacketInterval = TimeSpan.FromMinutes(10);

    private readonly LocalRuntimeProbeService _probeService;
    private readonly AuditLedgerService? _auditLedgerService;
    private readonly KillSwitchService? _killSwitchService;
    private DateTimeOffset? _lastProbeAtUtc;
    private DateTimeOffset? _lastPacketAtUtc;
    private LocalBrainStatus _latestStatus = LocalBrainStatus.Starting(DateTimeOffset.MinValue);

    public LocalBrainHeartbeatService(
        LocalRuntimeProbeService? probeService = null,
        AuditLedgerService? auditLedgerService = null,
        KillSwitchService? killSwitchService = null)
    {
        _probeService = probeService ?? new LocalRuntimeProbeService(killSwitchService: killSwitchService);
        _auditLedgerService = auditLedgerService;
        _killSwitchService = killSwitchService;
    }

    public LocalBrainStatus LatestStatus => _latestStatus;

    public async Task<LocalBrainStatus> TickAsync(
        IReadOnlyDictionary<string, string>? settings,
        RuntimeSupervisorStatus? runtimeStatus = null,
        DateTimeOffset? nowUtc = null,
        CancellationToken cancellationToken = default)
    {
        var now = nowUtc ?? DateTimeOffset.UtcNow;
        if (_killSwitchService?.IsActive() == true || KillSwitchService.IsActive(settings))
        {
            _latestStatus = new LocalBrainStatus(
                LocalBrainAvailability.Blocked,
                "ollama",
                LocalRuntimeProbeService.DefaultOllamaEndpoint,
                Read(settings, LocalRuntimeProbeService.OllamaModelSetting, LocalRuntimeProbeService.DefaultOllamaModel),
                "kill_switch=true",
                now,
                DidUseLocalModel: false);
            return _latestStatus;
        }

        if (_lastProbeAtUtc is not null && now - _lastProbeAtUtc.Value < ProbeInterval)
        {
            return _latestStatus;
        }

        var probe = await _probeService.ProbeAsync(settings, runtimeStatus, now, cancellationToken).ConfigureAwait(false);
        _lastProbeAtUtc = now;
        _latestStatus = FromProbe(probe);

        if (_auditLedgerService is not null &&
            (_lastPacketAtUtc is null || now - _lastPacketAtUtc.Value >= PacketInterval))
        {
            _auditLedgerService.Record(new EvidencePacket(
                Guid.NewGuid(),
                HeartbeatPacketKind,
                TaskCardId: null,
                CreatedAtUtc: now,
                DidUseNetwork: false,
                DidUseHostedAi: false,
                DidUseLocalModel: probe.IsAvailable,
                DidMutate: false,
                ArtifactPath: "",
                Summary: BuildSummary(_latestStatus),
                Status: _latestStatus.Availability.ToString(),
                Error: probe.IsAvailable ? "" : probe.Reason));
            _lastPacketAtUtc = now;
        }

        return _latestStatus;
    }

    public static LocalBrainStatus FromProbe(LocalRuntimeProbeResult probe)
    {
        var availability = probe.WasDormant
            ? LocalBrainAvailability.Dormant
            : probe.IsAvailable
                ? LocalBrainAvailability.Ready
                : LocalBrainAvailability.Offline;
        return new LocalBrainStatus(
            availability,
            probe.RuntimeId,
            probe.Endpoint,
            probe.Model,
            probe.Reason,
            probe.ProbedAtUtc,
            DidUseLocalModel: probe.IsAvailable);
    }

    public static string BuildSummary(LocalBrainStatus status)
    {
        return status.Availability switch
        {
            LocalBrainAvailability.Ready => $"Local brain ready: runtime={status.RuntimeId}; model={status.Model}.",
            LocalBrainAvailability.Dormant => $"Local brain dormant: {status.Reason}",
            LocalBrainAvailability.Blocked => $"Local brain blocked: {status.Reason}",
            _ => $"Local brain offline: {status.Reason}"
        };
    }

    private static string Read(IReadOnlyDictionary<string, string>? settings, string key, string defaultValue)
    {
        return settings is not null && settings.TryGetValue(key, out var raw) && !string.IsNullOrWhiteSpace(raw)
            ? raw
            : defaultValue;
    }
}
