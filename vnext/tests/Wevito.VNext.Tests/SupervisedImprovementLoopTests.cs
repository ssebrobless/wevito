using Wevito.VNext.Contracts;
using Wevito.VNext.Core;
using Wevito.VNext.Core.Audit;
using Wevito.VNext.Core.SelfImprovement;
using Wevito.VNext.Core.SelfImprovement.Experiments;

namespace Wevito.VNext.Tests;

public sealed class SupervisedImprovementLoopTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-05-18T12:00:00Z");

    [Fact]
    public void Settings_DefaultOff()
    {
        var settings = SupervisedImprovementLoopSettings.FromSettings(new Dictionary<string, string>());

        Assert.False(settings.Enabled);
    }

    [Fact]
    public void TryRun_WhenPilotDisabled_DoesNotWriteApprovalPacket()
    {
        var root = TempRoot();
        var ledger = new AuditLedgerService(Path.Combine(root, "ledger.sqlite"));
        var loop = new SupervisedImprovementLoop(ledger);
        var proposalCard = ProposalCard();

        var result = loop.TryRun(Request(root, Settings(pilotEnabled: false, betaEnabled: true, proposalScopeEnabled: true), [proposalCard]));

        Assert.False(result.Ran);
        Assert.Equal($"{SupervisedImprovementLoopSettings.EnabledSetting}=false", result.BlockReason);
        Assert.Empty(ledger.Snapshot(Now.AddMinutes(-1), Now.AddMinutes(1)));
    }

    [Fact]
    public void TryRun_WhenEnabled_WritesOneAwaitingApprovalCardAndPacket()
    {
        var root = TempRoot();
        var ledger = new AuditLedgerService(Path.Combine(root, "ledger.sqlite"));
        var loop = new SupervisedImprovementLoop(ledger);
        var proposalCard = ProposalCard();

        var result = loop.TryRun(Request(root, Settings(pilotEnabled: true, betaEnabled: true, proposalScopeEnabled: true), [proposalCard]));

        Assert.True(result.Ran);
        Assert.False(result.DidMutate);
        var approvalCard = Assert.Single(result.TaskCards.Where(SupervisedImprovementLoop.IsAwaitingApprovalCard));
        Assert.Equal(TaskCardStatus.WaitingForApproval, approvalCard.Status);
        Assert.Equal(SupervisedImprovementLoop.BuildOperationId(proposalCard), approvalCard.ReviewPayload!["operation_id"]);
        Assert.True(File.Exists(approvalCard.AuditLogPath));
        var rows = ledger.Snapshot(Now.AddMinutes(-1), Now.AddMinutes(1));
        Assert.Contains(rows, row => row.PacketKind == SelfImprovementPacketKinds.ApplyAwaitingApproval && !row.DidMutate);
    }

    [Fact]
    public void TryRun_WithOpenApprovalCard_DoesNotDuplicate()
    {
        var root = TempRoot();
        var ledger = new AuditLedgerService(Path.Combine(root, "ledger.sqlite"));
        var loop = new SupervisedImprovementLoop(ledger);
        var proposalCard = ProposalCard();
        var first = loop.TryRun(Request(root, Settings(pilotEnabled: true, betaEnabled: true, proposalScopeEnabled: true), [proposalCard]));

        var second = loop.TryRun(Request(root, Settings(pilotEnabled: true, betaEnabled: true, proposalScopeEnabled: true), first.TaskCards));

        Assert.False(second.Ran);
        Assert.Equal("self_improvement_apply_awaiting_approval already open", second.BlockReason);
        Assert.Single(second.TaskCards.Where(SupervisedImprovementLoop.IsAwaitingApprovalCard));
    }

    [Fact]
    public void HandleApplyApproval_AcceptedApproval_RefusesBecauseApplyRunnerNotImplemented()
    {
        var root = TempRoot();
        var ledger = new AuditLedgerService(Path.Combine(root, "ledger.sqlite"));
        var loop = new SupervisedImprovementLoop(ledger);
        var proposalCard = ProposalCard();
        var tick = loop.TryRun(Request(root, Settings(pilotEnabled: true, betaEnabled: true, proposalScopeEnabled: true), [proposalCard]));
        var approvalCard = Assert.Single(tick.TaskCards.Where(SupervisedImprovementLoop.IsAwaitingApprovalCard));
        var operationId = approvalCard.ReviewPayload!["operation_id"];
        var approval = new UserApplyApproval(true, operationId, Now, AutonomousScopeService.SpriteRepairBatchProposalScopeId, operationId);

        var result = loop.HandleApplyApproval(
            approval,
            AutonomousScopeService.SpriteRepairBatchProposalScopeId,
            operationId,
            approvalCard.Id,
            Now,
            tick.TaskCards);

        var refused = Assert.IsType<ApprovalResult.Refused>(result.ValidationResult);
        Assert.Equal(SupervisedImprovementLoop.ApplyRunnerNotImplementedReason, refused.Reason);
        Assert.False(result.DidMutate);
        var rows = ledger.Snapshot(Now.AddMinutes(-1), Now.AddMinutes(1));
        Assert.Contains(rows, row => row.PacketKind == SelfImprovementPacketKinds.ApplyRefused && row.Error == SupervisedImprovementLoop.ApplyRunnerNotImplementedReason);
    }

    [Fact]
    public void HandleApplyApproval_ValidationRefusal_WritesRefusedPacket()
    {
        var root = TempRoot();
        var ledger = new AuditLedgerService(Path.Combine(root, "ledger.sqlite"));
        var loop = new SupervisedImprovementLoop(ledger);
        var approval = new UserApplyApproval(true, "wrong", Now, AutonomousScopeService.SpriteRepairBatchProposalScopeId, "wrong");

        var result = loop.HandleApplyApproval(
            approval,
            AutonomousScopeService.SpriteRepairBatchProposalScopeId,
            "expected-operation",
            Guid.NewGuid(),
            Now,
            []);

        var refused = Assert.IsType<ApprovalResult.Refused>(result.ValidationResult);
        Assert.Equal("operation_id_mismatch", refused.Reason);
        var rows = ledger.Snapshot(Now.AddMinutes(-1), Now.AddMinutes(1));
        Assert.Contains(rows, row => row.PacketKind == SelfImprovementPacketKinds.ApplyRefused && row.Error == "operation_id_mismatch");
    }

    private static SupervisedImprovementLoopRequest Request(string root, IReadOnlyDictionary<string, string> settings, IReadOnlyList<TaskCard> cards)
    {
        return new SupervisedImprovementLoopRequest(settings, ActiveStatus(), Path.Combine(root, "artifacts"), Now, cards);
    }

    private static RuntimeSupervisorStatus ActiveStatus()
    {
        return new RuntimeSupervisorStatus(RuntimeSupervisorMode.Active, true, true, false, "active", "");
    }

    private static Dictionary<string, string> Settings(bool pilotEnabled, bool betaEnabled, bool proposalScopeEnabled)
    {
        return new Dictionary<string, string>
        {
            [SupervisedImprovementLoopSettings.EnabledSetting] = pilotEnabled.ToString(),
            [AutonomousOperationsConfig.EnabledSetting] = betaEnabled.ToString(),
            [AutonomousOperationsConfig.DailyCapSetting] = "3",
            [AutonomousOperationsConfig.IntervalMinutesSetting] = "10",
            [AutonomousScopeService.BuildEnabledSettingKey(AutonomousScopeService.SpriteRepairBatchProposalScopeId)] = proposalScopeEnabled.ToString()
        };
    }

    private static TaskCard ProposalCard()
    {
        var intent = new TaskIntent(
            Guid.NewGuid(),
            "Review self-improvement sprite repair proposal.",
            TaskIntentTargetMode.RouteToBestHelper,
            TaskKind: TaskKind.ReviewSprites,
            RequestedToolFamily: SpriteRepairBatchProposalDescriptor.Kind,
            NeedsApproval: true);
        return new TaskCard(
            Guid.NewGuid(),
            intent,
            TaskCardStatus.Draft,
            ToolFamily: SpriteRepairBatchProposalDescriptor.Kind,
            CreatedAtUtc: Now,
            UpdatedAtUtc: Now,
            ReviewPayload: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["proposal_path"] = "proposal.json",
                ["dry_run_path"] = "dry-run.json",
                ["eval_path"] = "eval.json"
            });
    }

    private static string TempRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "wevito-supervised-loop-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }
}
