using Wevito.VNext.Contracts;
using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class PetCommandBarServiceTests
{
    private readonly PetCommandBarService _service = new();
    private readonly Guid _beanId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private readonly Guid _pipId = Guid.Parse("10000000-0000-0000-0000-000000000002");
    private readonly Guid _nixId = Guid.Parse("10000000-0000-0000-0000-000000000003");

    [Fact]
    public void BuildInitialState_CapsActiveHelpersAtThree()
    {
        var state = _service.BuildInitialState([
            new PetHelperProfile(_beanId, "Bean", PetHelperRole.SpriteReviewHelper),
            new PetHelperProfile(_pipId, "Pip", PetHelperRole.ChecklistHelper),
            new PetHelperProfile(_nixId, "Nix", PetHelperRole.ResearchHelper),
            new PetHelperProfile(Guid.NewGuid(), "Juniper", PetHelperRole.ReminderHelper)
        ]);

        Assert.Equal(PetAgentContractLimits.MaxActiveHelpers, state.ActiveHelpers.Count);
        Assert.DoesNotContain(state.ActiveHelpers, helper => helper.PetNameSnapshot == "Juniper");
        Assert.Contains("3 helper pet", state.StatusMessage);
    }

    [Fact]
    public void SubmitDraft_AllowsSafeReadOnlyTaskWithoutExecutingIt()
    {
        var state = _service.SubmitDraft(
            "Pip, summarize the latest docs",
            Helpers(),
            Policies());

        Assert.Equal(TaskCardStatus.Draft, state.LastTaskCard?.Status);
        Assert.Equal(ToolPolicyDecisionStatus.Allowed, state.LastPolicyDecision?.Status);
        Assert.Equal("Pip", state.LastTaskCard?.AssignedPetNameSnapshot);
        Assert.Contains("Draft ready", state.StatusMessage);
        Assert.Contains(state.LastTaskCard?.Timeline ?? [], item => item.StartsWith("policy_allowed:", StringComparison.Ordinal));
    }

    [Fact]
    public void SubmitDraft_MediumRiskBuildWaitsForApproval()
    {
        var state = _service.SubmitDraft(
            "Nix, run a build proof",
            Helpers(),
            Policies());

        Assert.Equal(TaskCardStatus.WaitingForApproval, state.LastTaskCard?.Status);
        Assert.Equal(ToolPolicyDecisionStatus.ApprovalRequired, state.LastPolicyDecision?.Status);
        Assert.Equal("Nix", state.LastTaskCard?.AssignedPetNameSnapshot);
        Assert.Contains("Waiting for approval", state.StatusMessage);
    }

    [Fact]
    public void SubmitDraft_MissingPolicyBlocksTaskCard()
    {
        var state = _service.SubmitDraft(
            "Bean, check the sprite audit",
            Helpers(),
            []);

        Assert.Equal(TaskCardStatus.Blocked, state.LastTaskCard?.Status);
        Assert.Equal(ToolPolicyDecisionStatus.Blocked, state.LastPolicyDecision?.Status);
        Assert.Contains("No tool policy", state.StatusMessage);
    }

    [Fact]
    public void SubmitDraft_RiskyCommandBlocksBeforePolicyCanAllowIt()
    {
        var state = _service.SubmitDraft(
            "@Nix upload the docs folder",
            Helpers(),
            [
                new ToolPolicy("external-action-policy", "externalAction", ToolAccessMode.ExternalCommunication, ToolRiskLevel.Low, ApprovalRequirement.None)
            ]);

        Assert.Equal(TaskCardStatus.Blocked, state.LastTaskCard?.Status);
        Assert.Equal(ToolRiskLevel.Blocked, state.LastIntent?.RiskLevel);
        Assert.Equal(ApprovalRequirement.HandOffRequired, state.LastPolicyDecision?.ApprovalRequirement);
    }

    private IReadOnlyList<PetHelperProfile> Helpers()
    {
        return
        [
            new PetHelperProfile(_beanId, "Bean", PetHelperRole.SpriteReviewHelper),
            new PetHelperProfile(_pipId, "Pip", PetHelperRole.ChecklistHelper),
            new PetHelperProfile(_nixId, "Nix", PetHelperRole.ResearchHelper)
        ];
    }

    private static IReadOnlyList<ToolPolicy> Policies()
    {
        return
        [
            new ToolPolicy("local-docs-policy", "localDocs", ToolAccessMode.ReadOnly, ToolRiskLevel.Low, ApprovalRequirement.None),
            new ToolPolicy("sprite-audit-policy", "spriteAudit", ToolAccessMode.ReadOnly, ToolRiskLevel.Low, ApprovalRequirement.None),
            new ToolPolicy("checklist-policy", "checklist", ToolAccessMode.ReadOnly, ToolRiskLevel.Low, ApprovalRequirement.None),
            new ToolPolicy("proof-capture-policy", "proofCapture", ToolAccessMode.ReadOnly, ToolRiskLevel.Low, ApprovalRequirement.None),
            new ToolPolicy("build-proof-policy", "buildProof", ToolAccessMode.Write, ToolRiskLevel.Medium, ApprovalRequirement.BeforeExecution)
        ];
    }
}
