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
        var inspector = helpers.Single(helper => helper.PetNameSnapshot == "Inspector");
        store.AddExample(inspector.PetId, "spriteAudit", "review goose baby female blue sprites", "goose sprite QA");
        var router = new PetMemoryRouter(store);
        var intent = new TaskIntent(
            Guid.NewGuid(),
            "please inspect goose baby female blue sprite frames",
            TaskIntentTargetMode.RouteToBestHelper,
            TaskKind: TaskKind.ReviewSprites,
            RequestedToolFamily: "spriteAudit");

        var decision = router.Route(intent, helpers);

        Assert.True(decision.UsedMemory);
        Assert.Equal("Inspector", decision.Helper?.PetNameSnapshot);
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
        var inspector = helpers.Single(helper => helper.PetNameSnapshot == "Inspector");
        store.AddExample(inspector.PetId, "spriteAudit", "goose baby female blue sprite review", "goose sprite QA");
        var service = new PetCommandBarService(new PetCommandParser(), new ToolPolicyEvaluator(), new PetMemoryRouter(store));

        var state = service.SubmitDraft(
            "review goose baby female blue sprites",
            helpers,
            [new ToolPolicy("sprite-audit-policy", "spriteAudit", ToolAccessMode.ReadOnly, ToolRiskLevel.Low, ApprovalRequirement.None)]);

        Assert.Equal("Inspector", state.LastTaskCard?.AssignedPetNameSnapshot);
        Assert.Contains(state.LastTaskCard?.Timeline ?? [], entry => entry.StartsWith("memory_routed:", StringComparison.Ordinal));
        Assert.Equal(ToolPolicyDecisionStatus.Allowed, state.LastPolicyDecision?.Status);
    }

    private static IReadOnlyList<PetHelperProfile> Helpers()
    {
        return
        [
            new PetHelperProfile(Guid.Parse("11111111-1111-1111-1111-111111111111"), "Scout", PetHelperRole.ResearchHelper),
            new PetHelperProfile(Guid.Parse("22222222-2222-2222-2222-222222222222"), "Inspector", PetHelperRole.SpriteReviewHelper),
            new PetHelperProfile(Guid.Parse("33333333-3333-3333-3333-333333333333"), "Builder", PetHelperRole.ChecklistHelper)
        ];
    }

    private static string CreateTempRoot()
    {
        return Path.Combine(Path.GetTempPath(), "wevito-pet-memory-router-tests", Guid.NewGuid().ToString("N"));
    }
}
