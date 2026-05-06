using Wevito.VNext.Contracts;
using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class AutoCareIntentTests
{
    private readonly PetSimulationEngine _engine = new();

    [Fact]
    public void ApplyAutoCare_LowThirstUsesDrinkVisualIntent()
    {
        var now = DateTimeOffset.UtcNow;
        var pet = new PetActor(
            Guid.NewGuid(),
            "Goose 1",
            "goose",
            Thirst: 18,
            CurrentAnimationState: PetAnimationState.Idle,
            ActiveStatuses: []);

        var updated = _engine.ApplyAutoCare(pet, now);

        Assert.True(updated.Thirst > pet.Thirst);
        Assert.Equal("water", updated.LastActionId);
        Assert.Equal(PetAnimationState.Eat, updated.CurrentAnimationState);
        Assert.NotNull(updated.CurrentActionVisualIntent);
        Assert.Equal(AnimationFamily.Drink, updated.CurrentActionVisualIntent!.Family);
    }

    [Fact]
    public void ApplyAutoCare_ActiveDrinkIntentDoesNotRetrigger()
    {
        var now = DateTimeOffset.UtcNow;
        var activeDrink = new ActionVisualIntent(AnimationFamily.Drink);
        var pet = new PetActor(
            Guid.NewGuid(),
            "Goose 1",
            "goose",
            Thirst: 18,
            CurrentAnimationState: PetAnimationState.Eat,
            OverrideAnimationState: PetAnimationState.Eat,
            OverrideAnimationEndsAtUtc: now.AddSeconds(1),
            LastActionId: "water",
            LastActionAtUtc: now.AddSeconds(-0.25),
            ActiveStatuses: [],
            CurrentActionVisualIntent: activeDrink);

        var updated = _engine.ApplyAutoCare(pet, now);

        Assert.Equal(pet.Thirst, updated.Thirst);
        Assert.Equal(pet.LastActionAtUtc, updated.LastActionAtUtc);
        Assert.Same(activeDrink, updated.CurrentActionVisualIntent);
    }
}
