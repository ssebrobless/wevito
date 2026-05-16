using System;
using System.Collections.Generic;

namespace Wevito.VNext.Broker;

public sealed record WatchedProcessState(
    string ProcessKey,
    bool IsRunning,
    DateTimeOffset ObservedAtUtc);

public sealed record ProcessCrashRecoveryEvent(
    string ProcessKey,
    bool RestartRequested,
    bool RestartCapReached,
    TimeSpan Backoff,
    string PacketKind,
    string Summary);

public sealed class WevitoProcessWatchdogService
{
    public const string PacketKind = "process_crash_recovery";
    public static readonly TimeSpan RestartWindow = TimeSpan.FromMinutes(5);
    public static readonly TimeSpan[] BackoffSchedule =
    [
        TimeSpan.FromSeconds(1),
        TimeSpan.FromSeconds(5),
        TimeSpan.FromSeconds(30)
    ];

    private readonly Func<bool>? _killSwitchActive;
    private readonly Action<ProcessCrashRecoveryEvent>? _recordRecovery;
    private readonly Dictionary<string, List<DateTimeOffset>> _restartHistory = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _previouslyRunning = new(StringComparer.OrdinalIgnoreCase);

    public WevitoProcessWatchdogService(
        Func<bool>? killSwitchActive = null,
        Action<ProcessCrashRecoveryEvent>? recordRecovery = null)
    {
        _killSwitchActive = killSwitchActive;
        _recordRecovery = recordRecovery;
    }

    public ProcessCrashRecoveryEvent Observe(WatchedProcessState state)
    {
        if (_killSwitchActive?.Invoke() == true)
        {
            return new ProcessCrashRecoveryEvent(
                state.ProcessKey,
                RestartRequested: false,
                RestartCapReached: false,
                TimeSpan.Zero,
                PacketKind,
                $"process={state.ProcessKey} kill_switch=true restart_requested=false");
        }

        if (state.IsRunning)
        {
            _previouslyRunning.Add(state.ProcessKey);
            return new ProcessCrashRecoveryEvent(state.ProcessKey, false, false, TimeSpan.Zero, "", "");
        }

        if (!_previouslyRunning.Contains(state.ProcessKey))
        {
            return new ProcessCrashRecoveryEvent(state.ProcessKey, false, false, TimeSpan.Zero, "", "");
        }

        var history = PrunedHistory(state.ProcessKey, state.ObservedAtUtc);
        if (history.Count >= 3)
        {
            var capped = new ProcessCrashRecoveryEvent(
                state.ProcessKey,
                RestartRequested: false,
                RestartCapReached: true,
                TimeSpan.Zero,
                PacketKind,
                $"process={state.ProcessKey} restart_cap_reached=true restarts_in_5m={history.Count}");
            _recordRecovery?.Invoke(capped);
            return capped;
        }

        var backoff = BackoffSchedule[Math.Min(history.Count, BackoffSchedule.Length - 1)];
        history.Add(state.ObservedAtUtc);
        var recovered = new ProcessCrashRecoveryEvent(
            state.ProcessKey,
            RestartRequested: true,
            RestartCapReached: false,
            backoff,
            PacketKind,
            $"process={state.ProcessKey} restart_requested=true backoff_seconds={backoff.TotalSeconds:0} restarts_in_5m={history.Count}");
        _recordRecovery?.Invoke(recovered);
        return recovered;
    }

    private List<DateTimeOffset> PrunedHistory(string processKey, DateTimeOffset nowUtc)
    {
        if (!_restartHistory.TryGetValue(processKey, out var history))
        {
            history = [];
            _restartHistory[processKey] = history;
        }

        history.RemoveAll(timestamp => nowUtc - timestamp > RestartWindow);
        return history;
    }
}
