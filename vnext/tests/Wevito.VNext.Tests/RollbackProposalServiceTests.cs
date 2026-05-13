using Wevito.VNext.Contracts;
using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class RollbackProposalServiceTests
{
    [Fact]
    public void Proposal_EmitsOnlyWhenRegressionFollowsApplyWithin24Hours()
    {
        var root = CreateTempRoot();
        var ledger = new AuditLedgerService(Path.Combine(root, "ledger.sqlite"));
        ledger.Record(Packet(AuditLedgerService.TuningApplyPacketKind, "2026-05-13T08:00:00Z", "Completed", "applied"));
        ledger.Record(Packet(AuditLedgerService.GoldenEvalPacketKind, "2026-05-13T09:00:00Z", "Regression", "golden eval regression"));
        var service = new RollbackProposalService(ledger);

        var result = service.Run(Request(root));

        Assert.True(result.Succeeded);
        Assert.True(result.ProposalCreated);
        Assert.NotNull(result.TaskCard);
        Assert.Equal(TaskCardStatus.Draft, result.TaskCard.Status);
        Assert.Equal(RollbackProposalService.ToolFamily, result.TaskCard.ToolFamily);
        Assert.Contains("rollbackExecuted\":false", File.ReadAllText(result.ProposalPath));
    }

    [Fact]
    public void Proposal_IsNotEmittedOutside24HourWindow()
    {
        var root = CreateTempRoot();
        var ledger = new AuditLedgerService(Path.Combine(root, "ledger.sqlite"));
        ledger.Record(Packet(AuditLedgerService.TuningApplyPacketKind, "2026-05-12T08:00:00Z", "Completed", "applied"));
        ledger.Record(Packet(AuditLedgerService.GoldenEvalPacketKind, "2026-05-13T09:30:00Z", "Regression", "golden eval regression"));
        var service = new RollbackProposalService(ledger);

        var result = service.Run(Request(root));

        Assert.True(result.Succeeded);
        Assert.False(result.ProposalCreated);
    }

    [Fact]
    public void Proposal_NeverAutoExecutesRollback()
    {
        var root = CreateTempRoot();
        var ledger = new AuditLedgerService(Path.Combine(root, "ledger.sqlite"));
        ledger.Record(Packet(AuditLedgerService.TuningApplyPacketKind, "2026-05-13T08:00:00Z", "Completed", "applied"));
        ledger.Record(Packet(AuditLedgerService.GoldenEvalPacketKind, "2026-05-13T09:00:00Z", "Regression", "golden eval regression"));
        var service = new RollbackProposalService(ledger);

        var result = service.Run(Request(root));

        Assert.True(result.ProposalCreated);
        Assert.NotNull(result.TaskCard);
        Assert.Equal(TaskCardStatus.Draft, result.TaskCard.Status);
        Assert.Contains(result.TaskCard.Timeline ?? [], line => line.Contains("rollback_not_executed", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(ledger.Snapshot(DateTimeOffset.Parse("2026-05-13T00:00:00Z"), DateTimeOffset.Parse("2026-05-13T23:59:00Z")), row => row.PacketKind == AuditLedgerService.TuningRollbackPacketKind);
    }

    [Fact]
    public void KillSwitch_BlocksProposal()
    {
        var root = CreateTempRoot();
        var ledger = new AuditLedgerService(Path.Combine(root, "ledger.sqlite"));
        var killSwitch = new KillSwitchService(() => new Dictionary<string, string> { [KillSwitchService.KillSwitchSetting] = "true" });
        var service = new RollbackProposalService(ledger, killSwitch);

        var result = service.Run(Request(root));

        Assert.False(result.Succeeded);
        Assert.Equal("kill_switch=true", result.Message);
    }

    private static RollbackProposalRequest Request(string root)
    {
        return new RollbackProposalRequest(
            DateTimeOffset.Parse("2026-05-12T00:00:00Z"),
            DateTimeOffset.Parse("2026-05-13T23:59:00Z"),
            Path.Combine(root, "vnext", "artifacts", "pet-tasks"),
            DateTimeOffset.Parse("2026-05-13T12:00:00Z"));
    }

    private static EvidencePacket Packet(string kind, string createdAt, string status, string summary)
    {
        return new EvidencePacket(
            Guid.NewGuid(),
            kind,
            TaskCardId: null,
            DateTimeOffset.Parse(createdAt),
            DidUseNetwork: false,
            DidUseHostedAi: false,
            DidUseLocalModel: false,
            DidMutate: kind == AuditLedgerService.TuningApplyPacketKind,
            ArtifactPath: "artifact",
            Summary: summary,
            Status: status);
    }

    private static string CreateTempRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "wevito-rollback-proposal-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }
}
