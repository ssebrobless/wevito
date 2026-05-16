using Wevito.VNext.Contracts;
using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class PetMemoryRouterTests
{
    [Fact]
    public void Route_UsesStrongestPerPetMemoryMatch()
    {
        var root = CreateTempRoot();
        var store = new PetMemoryStore(root);
        var helpers = Helpers();
        var visualAgent = helpers.Single(helper => helper.PetNameSnapshot == "goose 1");
        store.AddExample(visualAgent.PetId, "spriteAudit", "review goose baby female blue sprites", "goose sprite QA");
        var router = new PetMemoryRouter(store);
        var intent = new TaskIntent(
            Guid.NewGuid(),
            "please inspect goose baby female blue sprite frames",
            TaskIntentTargetMode.RouteToBestHelper,
            TaskKind: TaskKind.ReviewSprites,
            RequestedToolFamily: "spriteAudit");

        var decision = router.Route(intent, helpers);

        Assert.True(decision.UsedMemory);
        Assert.Equal("goose 1", decision.Helper?.PetNameSnapshot);
    }

    [Fact]
    public void Route_FallsBackRoundRobinWhenNoMemoryMatches()
    {
        var router = new PetMemoryRouter(new PetMemoryStore(CreateTempRoot()));
        var helpers = Helpers();
        var intent = new TaskIntent(
            Guid.NewGuid(),
            "unknown helper task",
            TaskIntentTargetMode.RouteToBestHelper,
            TaskKind: TaskKind.Unknown,
            RequestedToolFamily: "draft");

        var first = router.Route(intent, helpers);
        var second = router.Route(intent, helpers);

        Assert.False(first.UsedMemory);
        Assert.False(second.UsedMemory);
        Assert.NotEqual(first.Helper?.PetId, second.Helper?.PetId);
    }

    [Fact]
    public void SubmitDraft_CanUseMemoryRouterWithoutChangingPolicy()
    {
        var root = CreateTempRoot();
        var store = new PetMemoryStore(root);
        var helpers = Helpers();
        var visualAgent = helpers.Single(helper => helper.PetNameSnapshot == "goose 1");
        store.AddExample(visualAgent.PetId, "spriteAudit", "goose baby female blue sprite review", "goose sprite QA");
        var service = new ChatInputBarService(new ChatPromptParser(), new ToolPolicyEvaluator(), new PetMemoryRouter(store));

        var state = service.SubmitDraft(
            "review goose baby female blue sprites",
            helpers,
            [new ToolPolicy("sprite-audit-policy", "spriteAudit", ToolAccessMode.ReadOnly, ToolRiskLevel.Low, ApprovalRequirement.None)]);

        Assert.Equal("goose 1", state.LastTaskCard?.AssignedPetNameSnapshot);
        Assert.Contains(state.LastTaskCard?.Timeline ?? [], entry => entry.StartsWith("memory_routed:", StringComparison.Ordinal));
        Assert.Equal(ToolPolicyDecisionStatus.Allowed, state.LastPolicyDecision?.Status);
    }

    private static IReadOnlyList<AgentSlotProfile> Helpers()
    {
        return
        [
            new AgentSlotProfile(AgentSlotService.BuildSlotId(0), "goose 1", 0),
            new AgentSlotProfile(AgentSlotService.BuildSlotId(1), "fox 1", 1),
            new AgentSlotProfile(AgentSlotService.BuildSlotId(2), "frog 1", 2)
        ];
    }

    private static string CreateTempRoot()
    {
        return Path.Combine(Path.GetTempPath(), "wevito-pet-memory-router-tests", Guid.NewGuid().ToString("N"));
    }
}
