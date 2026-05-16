namespace Wevito.VNext.Core;

public sealed record CodexLoopStatus(
    string State,
    string CurrentPhaseId,
    DateTimeOffset? StartedAtUtc,
    DateTimeOffset? LastHeartbeatUtc,
    string LastReason = "",
    int AttemptCount = 0)
{
    public static CodexLoopStatus Idle { get; } = new("idle", "", null, null);
}

public sealed record CodexLoopRunDecision(
    bool ShouldHalt,
    bool ShouldRetry,
    bool ShouldOpenRemediationCard,
    bool ShouldAutoRebase,
    string Reason);

public sealed record CodexLoopPolicySnapshot(
    DateTimeOffset NowUtc,
    DateTimeOffset PhaseStartedAtUtc,
    DateTimeOffset LastCommitAtUtc,
    TimeSpan UserIdleTime,
    int FailureCount,
    bool TestsPassed,
    bool AutoContinue,
    bool MergeConflict,
    bool TrivialRebaseAvailable);
