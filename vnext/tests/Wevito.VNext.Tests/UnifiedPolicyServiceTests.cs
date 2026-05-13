using Wevito.VNext.Contracts;
using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class UnifiedPolicyServiceTests
{
    [Fact]
    public void EvaluateRead_AppendsAuditLedgerRow()
    {
        var root = CreateTempRoot();
        var docs = Path.Combine(root, "docs");
        Directory.CreateDirectory(docs);
        var file = Path.Combine(docs, "plan.md");
        File.WriteAllText(file, "ok");
        var ledgerPath = Path.Combine(root, "ledger.sqlite");
        var ledger = new AuditLedgerService(ledgerPath);
        var service = new UnifiedPolicyService(new LocalToolAccessPolicy(root), ledger);
        var now = DateTimeOffset.Parse("2026-05-12T12:00:00Z");

        var decision = service.EvaluateRead(file, [docs], Guid.Parse("11111111-1111-1111-1111-111111111111"), now);

        Assert.Equal(ToolPolicyDecisionStatus.Allowed, decision.Status);
        var rows = ledger.Snapshot(now.AddMinutes(-1), now.AddMinutes(1));
        Assert.Single(rows);
        Assert.Equal("unified_policy", rows[0].PacketKind);
        Assert.Contains("FileRead", rows[0].Summary);
    }

    [Fact]
    public void EvaluateRead_KillSwitchBlocks()
    {
        var root = CreateTempRoot();
        var docs = Path.Combine(root, "docs");
        Directory.CreateDirectory(docs);
        var file = Path.Combine(docs, "plan.md");
        File.WriteAllText(file, "ok");
        var killSwitch = new KillSwitchService(() => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [KillSwitchService.KillSwitchSetting] = bool.TrueString
        });
        var service = new UnifiedPolicyService(new LocalToolAccessPolicy(root), killSwitchService: killSwitch);

        var decision = service.EvaluateRead(file, [docs]);

        Assert.Equal(ToolPolicyDecisionStatus.Blocked, decision.Status);
        Assert.Equal("kill_switch=true", decision.Reason);
    }

    [Fact]
    public void ToolPolicyEvaluator_DelegatesWithoutChangingDecision()
    {
        var evaluator = new ToolPolicyEvaluator();
        var intent = new TaskIntent(
            Guid.NewGuid(),
            "summarize docs",
            TaskIntentTargetMode.RouteToBestHelper,
            TaskKind: TaskKind.SummarizeDocs,
            RequestedToolFamily: "localDocs");
        var policy = new ToolPolicy(
            "local-docs-readonly",
            "localDocs",
            ToolAccessMode.ReadOnly,
            ToolRiskLevel.Low,
            ApprovalRequirement.None);

        var decision = evaluator.Evaluate(intent, [policy]);

        Assert.Equal(ToolPolicyDecisionStatus.Allowed, decision.Status);
        Assert.Equal(ApprovalRequirement.None, decision.ApprovalRequirement);
    }

    [Fact]
    public void CapturePolicyEvaluator_DelegatesWithoutChangingDecision()
    {
        var evaluator = new CapturePolicyEvaluator();

        var decision = evaluator.Evaluate(new CaptureRequest(
            Guid.NewGuid(),
            CapturePreset.FullDesktop,
            CaptureTargetKind.FullDesktop,
            PrivacyLevel: CapturePrivacyLevel.Desktop));

        Assert.Equal(ToolPolicyDecisionStatus.ApprovalRequired, decision.Status);
        Assert.Equal(ToolRiskLevel.High, decision.RiskLevel);
    }

    private static string CreateTempRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "wevito-unified-policy-service-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }
}
