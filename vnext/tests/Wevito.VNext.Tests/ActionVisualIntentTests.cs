using Wevito.VNext.Contracts;
using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class ActionVisualIntentTests
{
    [Fact]
    public void WaterWithOptionalDrinkResolvesDrinkFamily()
    {
        var action = new ActionDefinition(
            "water",
            "Water",
            "Restores thirst.",
            AnimationState: PetAnimationState.Eat,
            OptionalAnimationFamily: "drink");

        var intent = PetSimulationEngine.ResolveActionVisualIntent(action);

        Assert.Equal(AnimationFamily.Drink, intent.Family);
        Assert.Equal(PropOverlayKind.None, intent.Overlay);
        Assert.False(intent.LoopUntilStopped);
    }

    [Fact]
    public void WaterWithoutOptionalFallsBackToBaseEatFamily()
    {
        var action = new ActionDefinition(
            "water",
            "Water",
            "Restores thirst.",
            AnimationState: PetAnimationState.Eat);

        var intent = PetSimulationEngine.ResolveActionVisualIntent(action);

        Assert.Equal(AnimationFamily.Eat, intent.Family);
        Assert.Equal(PropOverlayKind.None, intent.Overlay);
    }

    [Fact]
    public void ApplyActionStoresResolvedVisualIntentOnPet()
    {
        var engine = new PetSimulationEngine();
        var action = new ActionDefinition(
            "play",
            "Play",
            "Play with the pet.",
            AnimationState: PetAnimationState.Happy,
            OptionalAnimationFamily: "play_ball",
            PropOverlay: "ball");
        var pet = new PetActor(Guid.NewGuid(), "Goose 1", "goose", ActiveStatuses: []);

        var updated = engine.ApplyAction(action, [pet], DateTimeOffset.UtcNow).Single();

        Assert.NotNull(updated.CurrentActionVisualIntent);
        Assert.Equal(AnimationFamily.PlayBall, updated.CurrentActionVisualIntent!.Family);
        Assert.Equal(PropOverlayKind.Ball, updated.CurrentActionVisualIntent.Overlay);
        Assert.Equal(PetAnimationState.Happy, updated.CurrentAnimationState);
    }

    [Theory]
    [InlineData("feed", PetAnimationState.Eat, AnimationFamily.Eat)]
    [InlineData("water", PetAnimationState.Drink, AnimationFamily.Drink)]
    [InlineData("play", PetAnimationState.Happy, AnimationFamily.Happy)]
    [InlineData("groom", PetAnimationState.Groom, AnimationFamily.Happy)]
    [InlineData("medicine", PetAnimationState.Sick, AnimationFamily.Sick)]
    [InlineData("doctor", PetAnimationState.Doctor, AnimationFamily.Sick)]
    public void ImplicitCareActionsHaveDistinctConfirmationStates(string actionId, PetAnimationState expectedState, AnimationFamily expectedFamily)
    {
        var engine = new PetSimulationEngine();
        var pet = new PetActor(Guid.NewGuid(), "Goose 1", "goose", ActiveStatuses: []);

        var updated = engine.ApplyAction(actionId, [pet], DateTimeOffset.UtcNow).Single();

        Assert.Equal(expectedState, updated.CurrentAnimationState);
        Assert.Equal(expectedState, updated.OverrideAnimationState);
        Assert.Equal(expectedFamily, updated.CurrentActionVisualIntent!.Family);
        Assert.Equal(actionId, updated.LastActionId);
    }
}
