using Wevito.VNext.Contracts;
using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class ChatInputBarServiceTests
{
    private readonly ChatInputBarService _service = new();
    private readonly Guid _beanId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private readonly Guid _pipId = Guid.Parse("10000000-0000-0000-0000-000000000002");
    private readonly Guid _nixId = Guid.Parse("10000000-0000-0000-0000-000000000003");

    [Fact]
    public void BuildInitialState_CapsActiveHelpersAtThree()
    {
        var state = _service.BuildInitialState([
            new AgentSlotProfile(_beanId, "Bean", 0),
            new AgentSlotProfile(_pipId, "Pip", 1),
            new AgentSlotProfile(_nixId, "Nix", 2),
            new AgentSlotProfile(Guid.NewGuid(), "Juniper", 3)
        ]);

        Assert.Equal(PetAgentContractLimits.MaxActiveHelpers, state.ActiveHelpers.Count);
        Assert.DoesNotContain(state.ActiveHelpers, helper => helper.PetNameSnapshot == "Juniper");
        Assert.Contains("3 agent task slots", state.StatusMessage);
    }

    [Fact]
    public void AgentSlotService_UsesPetNamesAndAgentFallbacks()
    {
        var service = new AgentSlotService();
        var pet = new PetActor(Guid.NewGuid(), "Pebble", "goose");
        var roster = service.BuildRoster([pet]);

        Assert.Equal(PetAgentContractLimits.MaxActiveHelpers, roster.Slots.Count);
        Assert.Collection(
            roster.Slots,
            slot => Assert.Equal("Pebble", slot.Name),
            slot => Assert.Equal("Agent 2", slot.Name),
            slot => Assert.Equal("Agent 3", slot.Name));
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

    private IReadOnlyList<AgentSlotProfile> Helpers()
    {
        return
        [
            new AgentSlotProfile(_beanId, "Bean", 0),
            new AgentSlotProfile(_pipId, "Pip", 1),
            new AgentSlotProfile(_nixId, "Nix", 2)
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
