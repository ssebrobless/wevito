using Wevito.VNext.Contracts;
using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class PetSimulationEngineTests
{
    private readonly PetSimulationEngine _engine = new();

    [Fact]
    public void ApplyAction_Feed_RaisesHungerAndSetsEatAnimation()
    {
        var pet = new PetActor(
            Guid.NewGuid(),
            "Rat 1",
            "rat",
            Hunger: 20,
            CurrentAnimationState: PetAnimationState.Idle,
            ActiveStatuses: []);

        var updated = _engine.ApplyAction("feed", [pet], DateTimeOffset.UtcNow).Single();

        Assert.True(updated.Hunger > pet.Hunger);
        Assert.Equal(PetAnimationState.Eat, updated.CurrentAnimationState);
        Assert.Equal(PetAnimationState.Eat, updated.OverrideAnimationState);
    }

    [Fact]
    public void Tick_WhenNeedsAreLow_AddsExpectedStatuses()
    {
        var pet = new PetActor(
            Guid.NewGuid(),
            "Crow 1",
            "crow",
            Hunger: 18,
            Thirst: 16,
            Energy: 22,
            Cleanliness: 20,
            Affection: 18,
            Health: 35,
            ActiveStatuses: []);

        var updated = _engine.Tick(
            [pet],
            CompanionMode.Focused,
            new RectInt(0, 922, 1920, 118),
            DateTimeOffset.UtcNow,
            0.2).Single();

        Assert.Contains(PetStatusType.Hungry, updated.ActiveStatuses!);
        Assert.Contains(PetStatusType.Thirsty, updated.ActiveStatuses!);
        Assert.Contains(PetStatusType.Sleepy, updated.ActiveStatuses!);
        Assert.Contains(PetStatusType.Dirty, updated.ActiveStatuses!);
        Assert.Contains(PetStatusType.Lonely, updated.ActiveStatuses!);
        Assert.Contains(PetStatusType.Sick, updated.ActiveStatuses!);
    }

    [Theory]
    [InlineData("medicine", 40, true)]
    [InlineData("doctor", 70, true)]
    [InlineData("feed", 100, false)]
    public void IsActionEnabled_RespondsToPetState(string actionId, double healthOrNeed, bool expected)
    {
        var pet = actionId == "feed"
            ? new PetActor(Guid.NewGuid(), "Fox 1", "fox", Hunger: healthOrNeed, ActiveStatuses: [])
            : new PetActor(Guid.NewGuid(), "Fox 1", "fox", Health: healthOrNeed, ActiveStatuses: []);

        var enabled = _engine.IsActionEnabled(actionId, [pet]);

        Assert.Equal(expected, enabled);
    }

    [Fact]
    public void Tick_AdvancesBabyPetsToTeenOverTime()
    {
        var start = DateTimeOffset.UtcNow.AddMinutes(-25);
        var pet = new PetActor(
            Guid.NewGuid(),
            "Frog 1",
            "frog",
            AgeStage: PetAgeStage.Baby,
            AgeStageStartedAtUtc: start,
            Speed: 80,
            ActiveStatuses: []);

        var updated = _engine.Tick(
            [pet],
            CompanionMode.Focused,
            new RectInt(0, 922, 1920, 118),
            DateTimeOffset.UtcNow,
            0.2).Single();

        Assert.Equal(PetAgeStage.Teen, updated.AgeStage);
        Assert.True(updated.AgeStageStartedAtUtc > start);
    }
}
