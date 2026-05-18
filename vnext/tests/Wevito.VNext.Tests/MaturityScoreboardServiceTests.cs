using Wevito.VNext.Core;
using Wevito.VNext.Core.Audit;
using Wevito.VNext.Core.SelfImprovement.Maturity;

namespace Wevito.VNext.Tests;

public sealed class MaturityScoreboardServiceTests
{
    [Fact]
    public void BuildScoreboard_CountsOnlyApprovedProgressPacketKinds()
    {
        var path = CreateDatabasePath();
        var ledger = new AuditLedgerService(path);
        var now = DateTimeOffset.Parse("2026-05-18T12:00:00Z");
        ledger.Record(Packet(SelfImprovementPacketKinds.DryRunCompleted, now.AddMinutes(-5)));
        ledger.Record(Packet(SelfImprovementPacketKinds.ApplyCompleted, now.AddMinutes(-4), didMutate: true));
        ledger.Record(Packet(SelfImprovementPacketKinds.RollbackVerified, now.AddMinutes(-3)));
        ledger.Record(Packet(SelfImprovementPacketKinds.EvalCompleted, now.AddMinutes(-2), summary: "all applicable gates Passed"));
        ledger.Record(Packet("unknown_self_improvement_noise", now.AddMinutes(-1)));
        var service = new MaturityScoreboardService(path);

        var clock = service.BuildScoreboard(now);

        Assert.Equal(4, clock.ProgressIncrements);
        Assert.Equal(1, clock.DryRunCompletedCount);
        Assert.Equal(1, clock.ApplyCompletedCount);
        Assert.Equal(1, clock.RollbackVerifiedCount);
        Assert.Equal(1, clock.EvalCompletedPassedCount);
        Assert.Empty(clock.ResetReasons);
    }

    [Fact]
    public void BuildScoreboard_DoesNotClaimNotApplicableEvalAsProgress()
    {
        var path = CreateDatabasePath();
        var ledger = new AuditLedgerService(path);
        var now = DateTimeOffset.Parse("2026-05-18T12:00:00Z");
        ledger.Record(Packet(SelfImprovementPacketKinds.EvalCompleted, now, summary: "gate=HeldOutEval status=NotApplicable"));
        var service = new MaturityScoreboardService(path);

        var clock = service.BuildScoreboard(now);

        Assert.Equal(0, clock.ProgressIncrements);
        Assert.Equal(0, clock.EvalCompletedPassedCount);
    }

    [Fact]
    public void BuildScoreboard_RecordsOnlyMaturityResetPacketsForResetConditions()
    {
        var path = CreateDatabasePath();
        var ledger = new AuditLedgerService(path);
        var now = DateTimeOffset.Parse("2026-05-18T12:00:00Z");
        ledger.Record(Packet("unexpected_writer", now.AddMinutes(-1), didMutate: true));
        var service = new MaturityScoreboardService(path, ledger);

        var clock = service.BuildScoreboard(now);

        Assert.Contains(MaturityClockResetReason.SilentMutationDetected, clock.ResetReasons);
        var rows = ledger.Snapshot(now.AddHours(-1), now.AddHours(1));
        var reset = Assert.Single(rows.Where(row => row.PacketKind == SelfImprovementPacketKinds.MaturityClockReset));
        Assert.False(reset.DidMutate);
        Assert.False(reset.DidUseNetwork);
        Assert.False(reset.DidUseHostedAi);
        Assert.Contains(nameof(MaturityClockResetReason.SilentMutationDetected), reset.Summary);
    }

    [Fact]
    public void BuildScoreboard_DoesNotRepeatResetPacketForSameReason()
    {
        var path = CreateDatabasePath();
        var ledger = new AuditLedgerService(path);
        var now = DateTimeOffset.Parse("2026-05-18T12:00:00Z");
        ledger.Record(Packet("unexpected_network", now.AddMinutes(-2), didUseNetwork: true));
        var service = new MaturityScoreboardService(path, ledger);

        service.BuildScoreboard(now.AddMinutes(-1));
        service.BuildScoreboard(now);

        var rows = ledger.Snapshot(now.AddHours(-1), now.AddHours(1));
        Assert.Single(rows.Where(row => row.PacketKind == SelfImprovementPacketKinds.MaturityClockReset));
    }

    [Fact]
    public void BuildScoreboard_RespectsKillSwitch()
    {
        var path = CreateDatabasePath();
        var settings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [KillSwitchService.KillSwitchSetting] = bool.TrueString
        };
        var service = new MaturityScoreboardService(path, killSwitchService: new KillSwitchService(() => settings));

        var clock = service.BuildScoreboard(DateTimeOffset.Parse("2026-05-18T12:00:00Z"));

        Assert.True(clock.IsBlocked);
        Assert.Equal("kill_switch=true", clock.StatusMessage);
    }

    [Fact]
    public void BuildScoreboard_DoesNotIssueUpdateOrDelete()
    {
        var path = CreateDatabasePath();
        var ledger = new AuditLedgerService(path);
        ledger.Record(Packet(SelfImprovementPacketKinds.DryRunCompleted, DateTimeOffset.Parse("2026-05-18T12:00:00Z")));
        var commands = new List<string>();
        var service = new MaturityScoreboardService(path, commandObserver: commands.Add);

        service.BuildScoreboard(DateTimeOffset.Parse("2026-05-18T12:00:00Z"));

        Assert.All(commands, command =>
        {
            Assert.DoesNotContain("UPDATE", command, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("DELETE", command, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public void BuildScoreboard_DetectsAllResetReasons()
    {
        var path = CreateDatabasePath();
        var ledger = new AuditLedgerService(path);
        var now = DateTimeOffset.Parse("2026-05-18T12:00:00Z");
        ledger.Record(Packet("invariant_guard", now.AddMinutes(-6), error: "Invariant violation"));
        ledger.Record(Packet("network_guard", now.AddMinutes(-5), didUseNetwork: true));
        ledger.Record(Packet("hosted_guard", now.AddMinutes(-4), didUseHostedAi: true));
        ledger.Record(Packet(SelfImprovementPacketKinds.RollbackVerified, now.AddMinutes(-3), status: "Failed", error: "rollback failed"));
        ledger.Record(Packet(SelfImprovementPacketKinds.ApplyRefused, now.AddMinutes(-2), summary: "reason=user_rejection"));
        var service = new MaturityScoreboardService(path);

        var clock = service.BuildScoreboard(now);

        Assert.Contains(MaturityClockResetReason.InvariantViolation, clock.ResetReasons);
        Assert.Contains(MaturityClockResetReason.SilentNetworkDetected, clock.ResetReasons);
        Assert.Contains(MaturityClockResetReason.SilentHostedAiDetected, clock.ResetReasons);
        Assert.Contains(MaturityClockResetReason.FailedRollback, clock.ResetReasons);
        Assert.Contains(MaturityClockResetReason.UserRejectedProposal, clock.ResetReasons);
    }

    private static EvidencePacket Packet(
        string kind,
        DateTimeOffset timestamp,
        bool didUseNetwork = false,
        bool didUseHostedAi = false,
        bool didUseLocalModel = false,
        bool didMutate = false,
        string summary = "test packet",
        string status = "Completed",
        string error = "")
    {
        return new EvidencePacket(
            Guid.NewGuid(),
            kind,
            null,
            timestamp,
            didUseNetwork,
            didUseHostedAi,
            didUseLocalModel,
            didMutate,
            "",
            summary,
            status,
            error);
    }

    private static string CreateDatabasePath()
    {
        var root = Path.Combine(Path.GetTempPath(), "wevito-maturity-scoreboard-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return Path.Combine(root, "ledger.sqlite");
    }
}
