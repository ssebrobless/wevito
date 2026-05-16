using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class CodexLoopRunnerServiceTests
{
    [Fact]
    public void TimeoutAfterTwoHoursWhenUserPresent()
    {
        var decision = CodexLoopRunnerService.Evaluate(Snapshot(
            now: DateTimeOffset.Parse("2026-05-15T14:01:00Z"),
            started: DateTimeOffset.Parse("2026-05-15T12:00:00Z"),
            lastCommit: DateTimeOffset.Parse("2026-05-15T13:50:00Z"),
            idle: TimeSpan.FromMinutes(1)));

        Assert.True(decision.ShouldHalt);
        Assert.Equal("phase_timeout_2h_user_present", decision.Reason);
    }

    [Fact]
    public void ExtendedTimeoutWhenUserIdle()
    {
        var decision = CodexLoopRunnerService.Evaluate(Snapshot(
            now: DateTimeOffset.Parse("2026-05-15T18:00:00Z"),
            started: DateTimeOffset.Parse("2026-05-15T12:00:00Z"),
            lastCommit: DateTimeOffset.Parse("2026-05-15T17:45:00Z"),
            idle: TimeSpan.FromMinutes(15)));

        Assert.False(decision.ShouldHalt);
        Assert.Equal("continue", decision.Reason);
    }

    [Fact]
    public void StuckDetectionHaltsOn30MinNoCommit()
    {
        var decision = CodexLoopRunnerService.Evaluate(Snapshot(
            now: DateTimeOffset.Parse("2026-05-15T12:45:00Z"),
            started: DateTimeOffset.Parse("2026-05-15T12:00:00Z"),
            lastCommit: DateTimeOffset.Parse("2026-05-15T12:10:00Z"),
            idle: TimeSpan.FromMinutes(2)));

        Assert.True(decision.ShouldHalt);
        Assert.Equal("stuck_no_commit_30min", decision.Reason);
    }

    [Fact]
    public void RetryOnceWithFailureContext()
    {
        var decision = CodexLoopRunnerService.Evaluate(Snapshot(testsPassed: false, failureCount: 0));

        Assert.True(decision.ShouldRetry);
        Assert.Equal("retry_once_with_failure_context", decision.Reason);
    }

    [Fact]
    public void OpensRemediationCardAfterSecondFailure()
    {
        var decision = CodexLoopRunnerService.Evaluate(Snapshot(testsPassed: false, failureCount: 1));

        Assert.True(decision.ShouldHalt);
        Assert.True(decision.ShouldOpenRemediationCard);
        Assert.Equal("second_failure_open_remediation_card", decision.Reason);
    }

    [Fact]
    public void AutoRebaseOnFastForwardConflict()
    {
        var decision = CodexLoopRunnerService.Evaluate(Snapshot(mergeConflict: true, trivialRebaseAvailable: true));

        Assert.True(decision.ShouldAutoRebase);
        Assert.Equal("auto_rebase_trivial_conflict", decision.Reason);
    }

    [Fact]
    public void HaltsOnSemanticConflict()
    {
        var decision = CodexLoopRunnerService.Evaluate(Snapshot(mergeConflict: true, trivialRebaseAvailable: false));

        Assert.True(decision.ShouldHalt);
        Assert.Equal("semantic_merge_conflict", decision.Reason);
    }

    [Fact]
    public void MarkCompletedRequiresPassingTests()
    {
        using var temp = CodexLoopTempWorkspace.Create();
        var queue = new CodexPhaseQueueService(temp.DocsRoot);
        var runner = new CodexLoopRunnerService(queue);

        Assert.Throws<InvalidOperationException>(() => runner.MarkCompleted("C-PHASE 114", DateTimeOffset.Parse("2026-05-15T12:00:00Z"), testsPassed: false));
    }

    private static CodexLoopPolicySnapshot Snapshot(
        DateTimeOffset? now = null,
        DateTimeOffset? started = null,
        DateTimeOffset? lastCommit = null,
        TimeSpan? idle = null,
        int failureCount = 0,
        bool testsPassed = true,
        bool autoContinue = true,
        bool mergeConflict = false,
        bool trivialRebaseAvailable = false)
    {
        return new CodexLoopPolicySnapshot(
            now ?? DateTimeOffset.Parse("2026-05-15T12:05:00Z"),
            started ?? DateTimeOffset.Parse("2026-05-15T12:00:00Z"),
            lastCommit ?? DateTimeOffset.Parse("2026-05-15T12:04:00Z"),
            idle ?? TimeSpan.FromMinutes(1),
            failureCount,
            testsPassed,
            autoContinue,
            mergeConflict,
            trivialRebaseAvailable);
    }
}
