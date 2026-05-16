using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed class CodexLoopRunnerService
{
    public const string LoopHeartbeatPacketKind = "codex_loop_heartbeat";
    public static readonly TimeSpan UserPresentTimeout = TimeSpan.FromHours(2);
    public static readonly TimeSpan IdleExtensionThreshold = TimeSpan.FromMinutes(10);
    public static readonly TimeSpan NoCommitStuckThreshold = TimeSpan.FromMinutes(30);

    private readonly CodexPhaseQueueService _queueService;
    private readonly AuditLedgerService? _auditLedgerService;
    private readonly KillSwitchService? _killSwitchService;

    public CodexLoopRunnerService(
        CodexPhaseQueueService queueService,
        AuditLedgerService? auditLedgerService = null,
        KillSwitchService? killSwitchService = null)
    {
        _queueService = queueService;
        _auditLedgerService = auditLedgerService;
        _killSwitchService = killSwitchService;
    }

    public Task<CodexLoopRunDecision> RunLoopAsync(CodexLoopPolicySnapshot snapshot, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (_killSwitchService?.IsActive() == true)
        {
            return Task.FromResult(Halt("kill_switch=true"));
        }

        RecordHeartbeat(snapshot.NowUtc, snapshot);
        return Task.FromResult(Evaluate(snapshot));
    }

    public static CodexLoopRunDecision Evaluate(CodexLoopPolicySnapshot snapshot)
    {
        if (!snapshot.TestsPassed)
        {
            return snapshot.FailureCount <= 0
                ? new CodexLoopRunDecision(false, true, false, false, "retry_once_with_failure_context")
                : new CodexLoopRunDecision(true, false, true, false, "second_failure_open_remediation_card");
        }

        if (snapshot.MergeConflict)
        {
            return snapshot.TrivialRebaseAvailable
                ? new CodexLoopRunDecision(false, false, false, true, "auto_rebase_trivial_conflict")
                : Halt("semantic_merge_conflict");
        }

        if (!snapshot.AutoContinue)
        {
            return Halt("auto_continue_disabled_user_review_required");
        }

        if (snapshot.UserIdleTime < IdleExtensionThreshold &&
            snapshot.NowUtc - snapshot.PhaseStartedAtUtc >= UserPresentTimeout)
        {
            return Halt("phase_timeout_2h_user_present");
        }

        if (snapshot.NowUtc - snapshot.LastCommitAtUtc >= NoCommitStuckThreshold)
        {
            return Halt("stuck_no_commit_30min");
        }

        return new CodexLoopRunDecision(false, false, false, false, "continue");
    }

    public void MarkCompleted(string phaseId, DateTimeOffset nowUtc, bool testsPassed)
    {
        if (!testsPassed)
        {
            throw new InvalidOperationException("Cannot advance Codex loop past a phase whose tests did not pass.");
        }

        _queueService.CompletePhase(phaseId, nowUtc, "phase completed with passing validation");
    }

    private void RecordHeartbeat(DateTimeOffset nowUtc, CodexLoopPolicySnapshot snapshot)
    {
        _auditLedgerService?.Record(new EvidencePacket(
            Guid.NewGuid(),
            LoopHeartbeatPacketKind,
            null,
            nowUtc,
            DidUseNetwork: false,
            DidUseHostedAi: false,
            DidUseLocalModel: false,
            DidMutate: false,
            "",
            $"codex_loop heartbeat auto_continue={snapshot.AutoContinue} idle_minutes={snapshot.UserIdleTime.TotalMinutes:0}",
            "Running"));
    }

    private static CodexLoopRunDecision Halt(string reason) => new(
        ShouldHalt: true,
        ShouldRetry: false,
        ShouldOpenRemediationCard: false,
        ShouldAutoRebase: false,
        reason);
}
