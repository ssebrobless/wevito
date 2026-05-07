using Wevito.VNext.Contracts;
using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class ToolPolicyEvaluatorTests
{
    private readonly ToolPolicyEvaluator _evaluator = new();

    [Fact]
    public void Evaluate_AllowsEnabledReadOnlyPolicyWithoutApproval()
    {
        var decision = _evaluator.Evaluate(
            Intent("localDocs", TaskKind.SummarizeDocs),
            [Policy("localDocs", ToolAccessMode.ReadOnly, ToolRiskLevel.Low, ApprovalRequirement.None)]);

        Assert.Equal(ToolPolicyDecisionStatus.Allowed, decision.Status);
        Assert.Equal(ApprovalRequirement.None, decision.ApprovalRequirement);
        Assert.Equal(ToolRiskLevel.Low, decision.RiskLevel);
    }

    [Fact]
    public void Evaluate_RequiresApprovalWhenIntentNeedsApproval()
    {
        var decision = _evaluator.Evaluate(
            Intent("buildProof", TaskKind.BuildProof, ToolRiskLevel.Medium, needsApproval: true),
            [Policy("buildProof", ToolAccessMode.Write, ToolRiskLevel.Medium, ApprovalRequirement.None)]);

        Assert.Equal(ToolPolicyDecisionStatus.ApprovalRequired, decision.Status);
        Assert.Equal(ApprovalRequirement.BeforeExecution, decision.ApprovalRequirement);
        Assert.Equal(ToolRiskLevel.Medium, decision.RiskLevel);
    }

    [Fact]
    public void Evaluate_RequiresPolicyApprovalEvenWhenIntentIsLowRisk()
    {
        var decision = _evaluator.Evaluate(
            Intent("proofCapture", TaskKind.CaptureProof),
            [Policy("proofCapture", ToolAccessMode.ReadOnly, ToolRiskLevel.Low, ApprovalRequirement.ActionTime)]);

        Assert.Equal(ToolPolicyDecisionStatus.ApprovalRequired, decision.Status);
        Assert.Equal(ApprovalRequirement.ActionTime, decision.ApprovalRequirement);
    }

    [Fact]
    public void Evaluate_BlocksMissingPolicy()
    {
        var decision = _evaluator.Evaluate(Intent("spriteAudit", TaskKind.ReviewSprites), []);

        Assert.Equal(ToolPolicyDecisionStatus.Blocked, decision.Status);
        Assert.Contains("No tool policy", decision.Reason);
    }

    [Fact]
    public void Evaluate_BlocksDisabledPolicy()
    {
        var decision = _evaluator.Evaluate(
            Intent("externalAction", TaskKind.ExternalAction),
            [Policy("externalAction", ToolAccessMode.ExternalCommunication, ToolRiskLevel.High, ApprovalRequirement.ActionTime, isEnabled: false, blockReason: "External messages are not enabled.")]);

        Assert.Equal(ToolPolicyDecisionStatus.Blocked, decision.Status);
        Assert.Equal("External messages are not enabled.", decision.Reason);
    }

    [Fact]
    public void Evaluate_BlocksIntentRefusalBeforePolicy()
    {
        var intent = Intent(
            "externalAction",
            TaskKind.ExternalAction,
            ToolRiskLevel.Blocked,
            needsApproval: true,
            refusalReason: "Human handoff required.");

        var decision = _evaluator.Evaluate(
            intent,
            [Policy("externalAction", ToolAccessMode.ExternalCommunication, ToolRiskLevel.High, ApprovalRequirement.ActionTime)]);

        Assert.Equal(ToolPolicyDecisionStatus.Blocked, decision.Status);
        Assert.Equal(ApprovalRequirement.HandOffRequired, decision.ApprovalRequirement);
        Assert.Equal("Human handoff required.", decision.Reason);
    }

    [Fact]
    public void Evaluate_DoesNotChangePolicyDecisionByHelperIdentityOrRole()
    {
        var policies = new[]
        {
            Policy("localDocs", ToolAccessMode.ReadOnly, ToolRiskLevel.Low, ApprovalRequirement.None)
        };
        var helperNames = new[] { "Scout", "Inspector", "Builder" };

        var decisions = helperNames
            .Select(name => _evaluator.Evaluate(
                Intent("localDocs", TaskKind.SummarizeDocs, targetPetName: name),
                policies))
            .ToList();

        Assert.All(decisions, decision =>
        {
            Assert.Equal(ToolPolicyDecisionStatus.Allowed, decision.Status);
            Assert.Equal(ApprovalRequirement.None, decision.ApprovalRequirement);
            Assert.Equal(ToolRiskLevel.Low, decision.RiskLevel);
        });
    }

    private static TaskIntent Intent(
        string toolFamily,
        TaskKind taskKind,
        ToolRiskLevel riskLevel = ToolRiskLevel.Low,
        bool needsApproval = false,
        string refusalReason = "",
        string targetPetName = "")
    {
        return new TaskIntent(
            Guid.NewGuid(),
            "test command",
            TaskIntentTargetMode.RouteToBestHelper,
            TargetPetNameSnapshot: targetPetName,
            TaskKind: taskKind,
            RequestedToolFamily: toolFamily,
            RiskLevel: riskLevel,
            NeedsApproval: needsApproval,
            RefusalOrClarificationReason: refusalReason);
    }

    private static ToolPolicy Policy(
        string toolFamily,
        ToolAccessMode accessMode,
        ToolRiskLevel riskLevel,
        ApprovalRequirement approvalRequirement,
        bool isEnabled = true,
        string blockReason = "")
    {
        return new ToolPolicy(
            toolFamily + "-policy",
            toolFamily,
            accessMode,
            riskLevel,
            approvalRequirement,
            isEnabled,
            BlockReason: blockReason);
    }
}
