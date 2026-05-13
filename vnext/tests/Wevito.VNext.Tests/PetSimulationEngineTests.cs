using Wevito.VNext.Contracts;
using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class PetSimulationEngineTests
{
    private readonly PetSimulationEngine _engine = new();

    [Fact]
    public void CreatePet_UsesSpeciesAgeGenderAndColorConfiguration()
    {
        var species = new SpeciesDefinition("rat", "Rat", "#AAA", 100, DefaultEnvironmentId: "rat-home", InnateConditionId: "respiratoryProblems");

        var pet = _engine.CreatePet(species, PetAgeStage.Teen, PetGender.Male, "violet", "Rat Dev", DateTimeOffset.UtcNow);

        Assert.Equal("rat", pet.SpeciesId);
        Assert.Equal(PetAgeStage.Teen, pet.AgeStage);
        Assert.Equal(PetGender.Male, pet.Gender);
        Assert.Equal("violet", pet.ColorVariant);
        Assert.Equal("rat-home", pet.SelectedEnvironmentId);
        Assert.True(pet.Speed > 100);
        Assert.Contains(pet.ActiveConditions!, condition => condition.Id == "respiratoryProblems" && condition.IsInnate);
    }

    [Fact]
    public void ReconfigurePet_ResetsIdentityAndAnimationState()
    {
        var fox = new SpeciesDefinition("fox", "Fox", "#F80", 94, DefaultEnvironmentId: "fox-home");
        var rat = new PetActor(Guid.NewGuid(), "Rat 1", "rat", AgeStage: PetAgeStage.Baby, Gender: PetGender.Female, ColorVariant: "red", CurrentAnimationState: PetAnimationState.Walk, OverrideAnimationState: PetAnimationState.Happy, OverrideAnimationEndsAtUtc: DateTimeOffset.UtcNow.AddSeconds(2), ActiveStatuses: []);

        var updated = _engine.ReconfigurePet(rat, fox, PetAgeStage.Adult, PetGender.Male, "blue", DateTimeOffset.UtcNow);

        Assert.Equal("fox", updated.SpeciesId);
        Assert.Equal(PetAgeStage.Adult, updated.AgeStage);
        Assert.Equal(PetGender.Male, updated.Gender);
        Assert.Equal("blue", updated.ColorVariant);
        Assert.Equal("fox-home", updated.SelectedEnvironmentId);
        Assert.Equal(PetAnimationState.Idle, updated.CurrentAnimationState);
        Assert.Null(updated.OverrideAnimationState);
        Assert.Null(updated.OverrideAnimationEndsAtUtc);
        Assert.NotNull(updated.Personality);
        Assert.NotNull(updated.HabitProfile);
    }

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

    [Theory]
    [InlineData(PetAnimationState.Waving)]
    [InlineData(PetAnimationState.Jumping)]
    [InlineData(PetAnimationState.Failed)]
    [InlineData(PetAnimationState.Waiting)]
    [InlineData(PetAnimationState.Review)]
    public void Tick_PreservesActiveWorkCompanionAnimationOverrides(PetAnimationState animationState)
    {
        var now = DateTimeOffset.UtcNow;
        var pet = new PetActor(
            Guid.NewGuid(),
            "Crow 1",
            "crow",
            CurrentAnimationState: PetAnimationState.Idle,
            OverrideAnimationState: animationState,
            OverrideAnimationEndsAtUtc: now.AddSeconds(3),
            ActiveStatuses: []);

        var updated = _engine.Tick(
            [pet],
            CompanionMode.Focused,
            new RectInt(0, 922, 1920, 118),
            now,
            0.2).Single();

        Assert.Equal(animationState, updated.CurrentAnimationState);
        Assert.Equal(animationState, updated.OverrideAnimationState);
        Assert.Equal(pet.OverrideAnimationEndsAtUtc, updated.OverrideAnimationEndsAtUtc);
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
            BiologicalAgeMinutes: 75,
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

    [Fact]
    public void Tick_AdvancesAdultPetsToSeniorOverTime()
    {
        var start = DateTimeOffset.UtcNow.AddMinutes(-25);
        var pet = new PetActor(
            Guid.NewGuid(),
            "Goose 1",
            "goose",
            AgeStage: PetAgeStage.Adult,
            AgeStageStartedAtUtc: start,
            BiologicalAgeMinutes: 481,
            ActiveStatuses: []);

        var updated = _engine.Tick(
            [pet],
            CompanionMode.Focused,
            new RectInt(0, 922, 1920, 118),
            DateTimeOffset.UtcNow,
            0.2).Single();

        Assert.Equal(PetAgeStage.Senior, updated.AgeStage);
        Assert.True(updated.AgeStageStartedAtUtc > start);
    }

    [Fact]
    public void Tick_WhenCareReachesDeathThreshold_StartsSadMemorialFlow()
    {
        var now = DateTimeOffset.Parse("2026-05-07T10:00:00Z");
        var pet = new PetActor(
            Guid.NewGuid(),
            "Goose 1",
            "goose",
            CurrentX: 123,
            CurrentY: 234,
            Hunger: 0,
            ActiveStatuses: []);

        var updated = _engine.Tick(
            [pet],
            CompanionMode.Focused,
            new RectInt(0, 922, 1920, 118),
            now,
            0.2).Single();

        Assert.True(updated.IsDead);
        Assert.False(updated.IsGhost);
        Assert.Equal(PetAnimationState.Sad, updated.CurrentAnimationState);
        Assert.Equal(PetStatusType.Dead, Assert.Single(updated.ActiveStatuses!));
        Assert.Equal("memorial_object", updated.MemorialObjectId);
        Assert.Equal(now.AddDays(1), updated.MemorialExpiresAtUtc);
        Assert.Equal(123, updated.MemorialX);
        Assert.Equal(234, updated.MemorialY);
    }

    [Fact]
    public void Tick_PassivePetAtRoamTargetIdlesInsteadOfWalkingInPlace()
    {
        var now = DateTimeOffset.Parse("2026-05-13T22:00:00Z");
        var pet = new PetActor(
            Guid.NewGuid(),
            "Fox 1",
            "fox",
            CurrentX: 400,
            CurrentY: 1000,
            TargetX: 400,
            TargetY: 1000,
            BehaviorState: PetBehaviorState.Roaming,
            NextDecisionAtUtc: now.AddSeconds(10),
            CurrentAnimationState: PetAnimationState.Walk,
            ActiveStatuses: []);

        var updated = _engine.Tick(
            [pet],
            CompanionMode.Passive,
            new RectInt(0, 922, 1920, 118),
            now,
            0.2).Single();

        Assert.Equal(PetAnimationState.Idle, updated.CurrentAnimationState);
    }

    [Fact]
    public void Tick_PassivePetMovingTowardTargetUsesWalkAnimation()
    {
        var now = DateTimeOffset.Parse("2026-05-13T22:00:00Z");
        var pet = new PetActor(
            Guid.NewGuid(),
            "Fox 1",
            "fox",
            CurrentX: 400,
            CurrentY: 1000,
            TargetX: 700,
            TargetY: 1000,
            Speed: 120,
            BehaviorState: PetBehaviorState.Roaming,
            NextDecisionAtUtc: now.AddSeconds(10),
            CurrentAnimationState: PetAnimationState.Idle,
            ActiveStatuses: []);

        var updated = _engine.Tick(
            [pet],
            CompanionMode.Passive,
            new RectInt(0, 922, 1920, 118),
            now,
            0.2).Single();

        Assert.Equal(PetAnimationState.Walk, updated.CurrentAnimationState);
    }

    [Fact]
    public void Tick_DeadPetTransitionsToGhostAfterSadWindow()
    {
        var now = DateTimeOffset.Parse("2026-05-07T10:00:00Z");
        var pet = new PetActor(
            Guid.NewGuid(),
            "Goose 1",
            "goose",
            IsDead: true,
            DiedAtUtc: now.AddSeconds(-3),
            MemorialExpiresAtUtc: now.AddHours(2),
            MemorialObjectId: "memorial_object",
            ActiveStatuses: []);

        var updated = _engine.Tick(
            [pet],
            CompanionMode.Focused,
            new RectInt(0, 922, 1920, 118),
            now,
            0.2).Single();

        Assert.True(updated.IsGhost);
        Assert.Equal(PetAnimationState.Idle, updated.CurrentAnimationState);
        Assert.Equal(PetStatusType.Ghost, Assert.Single(updated.ActiveStatuses!));
        Assert.Equal("memorial_object", updated.MemorialObjectId);
    }

    [Fact]
    public void Tick_ExpiredMemorialClearsMarkerButKeepsGhostPet()
    {
        var now = DateTimeOffset.Parse("2026-05-07T10:00:00Z");
        var pet = new PetActor(
            Guid.NewGuid(),
            "Goose 1",
            "goose",
            IsDead: true,
            IsGhost: true,
            DiedAtUtc: now.AddDays(-2),
            MemorialExpiresAtUtc: now.AddSeconds(-1),
            MemorialObjectId: "memorial_object",
            ActiveStatuses: []);

        var updated = _engine.Tick(
            [pet],
            CompanionMode.Focused,
            new RectInt(0, 922, 1920, 118),
            now,
            0.2).Single();

        Assert.True(updated.IsDead);
        Assert.True(updated.IsGhost);
        Assert.Equal(string.Empty, updated.MemorialObjectId);
        Assert.Null(updated.MemorialExpiresAtUtc);
    }

    [Fact]
    public void ApplyAction_DeadPetIgnoresCareActions()
    {
        var now = DateTimeOffset.Parse("2026-05-07T10:00:00Z");
        var pet = new PetActor(
            Guid.NewGuid(),
            "Goose 1",
            "goose",
            IsDead: true,
            DiedAtUtc: now,
            Hunger: 5,
            Thirst: 6,
            ActiveStatuses: []);

        var updated = _engine.ApplyAction("feed", [pet], now).Single();

        Assert.Equal(5, updated.Hunger);
        Assert.Equal(6, updated.Thirst);
        Assert.True(updated.IsDead);
        Assert.True(string.IsNullOrWhiteSpace(updated.LastActionId));
    }

    [Fact]
    public void Tick_PoorCareAcceleratesBiologicalAgeMoreThanStrongCare()
    {
        var poorCare = new PetActor(
            Guid.NewGuid(),
            "Crow 1",
            "crow",
            AgeStage: PetAgeStage.Teen,
            BiologicalAgeMinutes: 120,
            Hunger: 10,
            Thirst: 12,
            Energy: 14,
            Cleanliness: 16,
            Affection: 18,
            Comfort: 16,
            Health: 32,
            Fitness: 18,
            HabitProfile: new PetHabitProfile(18, 20, 14, 16, 18, 16, 24, 82),
            ActiveConditions: [new PetConditionRecord("anxiety", 2, false), new PetConditionRecord("jointPain", 2, false)],
            ActiveStatuses: []);
        var strongCare = poorCare with
        {
            Id = Guid.NewGuid(),
            Name = "Crow 2",
            Hunger = 86,
            Thirst = 88,
            Energy = 82,
            Cleanliness = 84,
            Affection = 86,
            Comfort = 84,
            Health = 92,
            Fitness = 80,
            HabitProfile = new PetHabitProfile(84, 84, 80, 82, 84, 82, 86, 14),
            ActiveConditions = []
        };

        var poorRate = _engine.CalculateAgingRate(poorCare);
        var strongRate = _engine.CalculateAgingRate(strongCare);

        Assert.True(poorRate > strongRate);
    }

    [Fact]
    public void DescribeAging_UsesAsciiSeparatorForShellText()
    {
        var pet = new PetActor(
            Guid.NewGuid(),
            "Crow 1",
            "crow",
            AgeStage: PetAgeStage.Teen,
            BiologicalAgeMinutes: 120,
            ActiveStatuses: []);

        var description = _engine.DescribeAging(pet);

        Assert.Contains(" - ", description);
        Assert.DoesNotContain("·", description);
    }

    [Fact]
    public void ApplyAction_PlayBuildsFitnessAndPlayfulPersonality()
    {
        var pet = new PetActor(
            Guid.NewGuid(),
            "Rat 1",
            "rat",
            Fitness: 32,
            Personality: new PetPersonalityProfile(),
            HabitProfile: new PetHabitProfile(),
            ActiveConditions: [],
            ActiveStatuses: []);

        var updated = _engine.ApplyAction("play", [pet], DateTimeOffset.UtcNow).Single();

        Assert.True(updated.Fitness > pet.Fitness);
        Assert.True(updated.Personality!.Playfulness > pet.Personality!.Playfulness);
        Assert.True(updated.HabitProfile!.Exercise > pet.HabitProfile!.Exercise);
    }

    [Fact]
    public void Tick_LowFitnessAndNeglectCreateAcquiredConditions()
    {
        var pet = new PetActor(
            Guid.NewGuid(),
            "Fox 1",
            "fox",
            Hunger: 95,
            Energy: 8,
            Cleanliness: 12,
            Affection: 12,
            Comfort: 12,
            Health: 42,
            Fitness: 10,
            ActiveConditions: [],
            ActiveStatuses: []);

        var updated = _engine.Tick([pet], CompanionMode.Focused, new RectInt(0, 922, 1920, 118), DateTimeOffset.UtcNow, 2).Single();

        Assert.Contains(updated.ActiveConditions!, condition => condition.Id == "obesity");
        Assert.Contains(updated.ActiveConditions!, condition => condition.Id == "depression" || condition.Id == "anxiety");
        Assert.Contains(updated.ActiveConditions!, condition => condition.Id == "jointPain");
        Assert.Contains(updated.ActiveConditions!, condition => condition.Id == "exhaustion");
    }

    [Fact]
    public void MedicineAndDoctorReduceConditionSeverity()
    {
        var pet = new PetActor(
            Guid.NewGuid(),
            "Goose 1",
            "goose",
            Health: 40,
            ActiveConditions:
            [
                new PetConditionRecord("injury", 2, false),
                new PetConditionRecord("respiratoryProblems", 2, true)
            ],
            ActiveStatuses: []);

        var afterMedicine = _engine.ApplyAction("medicine", [pet], DateTimeOffset.UtcNow).Single();
        var afterDoctor = _engine.ApplyAction("doctor", [afterMedicine], DateTimeOffset.UtcNow).Single();

        Assert.True(afterMedicine.ActiveConditions!.First(condition => condition.Id == "injury").Severity < 2);
        Assert.True(afterDoctor.Health > afterMedicine.Health);
        Assert.True(afterDoctor.ActiveConditions!.First(condition => condition.Id == "respiratoryProblems").Severity <= 1);
    }
}
