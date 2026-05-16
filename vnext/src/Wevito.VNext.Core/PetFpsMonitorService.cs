using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed record PetFpsMonitorObservation(
    bool SnapshotEmitted,
    bool ViolationEmitted,
    bool AutonomousThrottleActive,
    double ThrottleMultiplier,
    string PacketKind,
    string Summary);

public sealed class PetFpsMonitorService
{
    public const string SnapshotPacketKind = "pet_fps_snapshot";
    public const string ViolationPacketKind = "pet_fps_violation";
    public static readonly TimeSpan SampleInterval = TimeSpan.FromMilliseconds(250);
    public static readonly TimeSpan SnapshotInterval = TimeSpan.FromHours(1);
    public static readonly TimeSpan ViolationThrottleDuration = TimeSpan.FromMinutes(5);

    private readonly AuditLedgerService? _auditLedgerService;
    private readonly KillSwitchService? _killSwitchService;
    private readonly List<PetFpsSample> _samples = [];
    private DateTimeOffset? _lastAcceptedSampleAtUtc;
    private DateTimeOffset? _lastSnapshotAtUtc;
    private DateTimeOffset? _throttleUntilUtc;

    public PetFpsMonitorService(
        AuditLedgerService? auditLedgerService = null,
        KillSwitchService? killSwitchService = null)
    {
        _auditLedgerService = auditLedgerService;
        _killSwitchService = killSwitchService;
    }

    public bool IsAutonomousThrottleActive(DateTimeOffset nowUtc)
    {
        return _throttleUntilUtc is not null && nowUtc < _throttleUntilUtc.Value;
    }

    public double GetThrottleMultiplier(DateTimeOffset nowUtc)
    {
        return IsAutonomousThrottleActive(nowUtc) ? 0.5 : 1.0;
    }

    public PetFpsMonitorObservation Observe(PetFpsSample sample)
    {
        if (_killSwitchService?.IsActive() == true)
        {
            return new PetFpsMonitorObservation(false, false, false, 1.0, "", "kill_switch=true");
        }

        if (_lastAcceptedSampleAtUtc is not null &&
            sample.CapturedAtUtc - _lastAcceptedSampleAtUtc.Value < SampleInterval)
        {
            return new PetFpsMonitorObservation(
                SnapshotEmitted: false,
                ViolationEmitted: false,
                IsAutonomousThrottleActive(sample.CapturedAtUtc),
                GetThrottleMultiplier(sample.CapturedAtUtc),
                "",
                "sample_ignored_interval");
        }

        _lastAcceptedSampleAtUtc = sample.CapturedAtUtc;
        _samples.Add(sample);
        Prune(sample.CapturedAtUtc);

        if (sample.FramesPerSecond < 30)
        {
            _throttleUntilUtc = sample.CapturedAtUtc + ViolationThrottleDuration;
            var summary = $"fps={sample.FramesPerSecond:0.##} below_30=true autonomous_throttle=50% throttle_minutes=5";
            Record(ViolationPacketKind, sample.CapturedAtUtc, summary, "Blocked");
            return new PetFpsMonitorObservation(false, true, true, 0.5, ViolationPacketKind, summary);
        }

        if (_lastSnapshotAtUtc is null || sample.CapturedAtUtc - _lastSnapshotAtUtc.Value >= SnapshotInterval)
        {
            _lastSnapshotAtUtc = sample.CapturedAtUtc;
            var snapshot = BuildSnapshotSummary();
            Record(SnapshotPacketKind, sample.CapturedAtUtc, snapshot, "Completed");
            return new PetFpsMonitorObservation(true, false, IsAutonomousThrottleActive(sample.CapturedAtUtc), GetThrottleMultiplier(sample.CapturedAtUtc), SnapshotPacketKind, snapshot);
        }

        return new PetFpsMonitorObservation(false, false, IsAutonomousThrottleActive(sample.CapturedAtUtc), GetThrottleMultiplier(sample.CapturedAtUtc), "", "");
    }

    private void Prune(DateTimeOffset nowUtc)
    {
        _samples.RemoveAll(sample => nowUtc - sample.CapturedAtUtc > SnapshotInterval);
    }

    private string BuildSnapshotSummary()
    {
        var ordered = _samples.Select(sample => sample.FramesPerSecond).Order().ToArray();
        if (ordered.Length == 0)
        {
            return "fps_samples=0";
        }

        return $"fps_samples={ordered.Length} p50={Percentile(ordered, 0.50):0.##} p95={Percentile(ordered, 0.95):0.##} min={ordered.Min():0.##}";
    }

    private static double Percentile(double[] ordered, double percentile)
    {
        if (ordered.Length == 1)
        {
            return ordered[0];
        }

        var index = (ordered.Length - 1) * percentile;
        var lower = (int)Math.Floor(index);
        var upper = (int)Math.Ceiling(index);
        if (lower == upper)
        {
            return ordered[lower];
        }

        var weight = index - lower;
        return ordered[lower] * (1 - weight) + ordered[upper] * weight;
    }

    private void Record(string packetKind, DateTimeOffset nowUtc, string summary, string status)
    {
        _auditLedgerService?.Record(new EvidencePacket(
            Guid.NewGuid(),
            packetKind,
            null,
            nowUtc,
            DidUseNetwork: false,
            DidUseHostedAi: false,
            DidUseLocalModel: false,
            DidMutate: false,
            ArtifactPath: "",
            summary,
            status,
            Error: status.Equals("Blocked", StringComparison.OrdinalIgnoreCase) ? "pet_fps_floor_violation" : ""));
    }
}
