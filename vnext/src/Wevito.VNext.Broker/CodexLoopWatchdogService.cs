namespace Wevito.VNext.Broker;

public sealed record CodexLoopHeartbeatSnapshot(
    bool LoopRunning,
    DateTimeOffset? LastHeartbeatUtc,
    DateTimeOffset ObservedAtUtc);

public sealed record CodexLoopWatchdogDecision(
    bool RestartRequested,
    string PacketKind,
    string Summary);

public sealed class CodexLoopWatchdogService
{
    public const string LoopHeartbeatPacketKind = "codex_loop_heartbeat";
    public static readonly TimeSpan MissedHeartbeatThreshold = TimeSpan.FromMinutes(10);

    private readonly Func<bool>? _killSwitchActive;
    private readonly Action<CodexLoopWatchdogDecision>? _recordDecision;

    public CodexLoopWatchdogService(
        Func<bool>? killSwitchActive = null,
        Action<CodexLoopWatchdogDecision>? recordDecision = null)
    {
        _killSwitchActive = killSwitchActive;
        _recordDecision = recordDecision;
    }

    public CodexLoopWatchdogDecision Observe(CodexLoopHeartbeatSnapshot snapshot)
    {
        if (_killSwitchActive?.Invoke() == true)
        {
            return new CodexLoopWatchdogDecision(false, LoopHeartbeatPacketKind, "codex_loop_watchdog kill_switch=true restart_requested=false");
        }

        if (!snapshot.LoopRunning)
        {
            return new CodexLoopWatchdogDecision(false, "", "");
        }

        if (snapshot.LastHeartbeatUtc is null ||
            snapshot.ObservedAtUtc - snapshot.LastHeartbeatUtc.Value > MissedHeartbeatThreshold)
        {
            var decision = new CodexLoopWatchdogDecision(true, LoopHeartbeatPacketKind, "codex_loop_watchdog missed_heartbeat restart_requested=true");
            _recordDecision?.Invoke(decision);
            return decision;
        }

        return new CodexLoopWatchdogDecision(false, "", "");
    }
}
