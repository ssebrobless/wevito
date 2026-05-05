using Wevito.VNext.Contracts;
using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class PetWellbeingInterpreterTests
{
    private readonly PetWellbeingInterpreter _interpreter = new();

    [Fact]
    public void BuildSnapshot_StablePetReportsLowUrgencyAndRelief()
    {
        var pet = new PetActor(
            Guid.Parse("10000000-0000-0000-0000-000000000001"),
            "Goose 1",
            "goose",
            Hunger: 75,
            Thirst: 84,
            Energy: 80,
            Cleanliness: 82,
            Affection: 86,
            Comfort: 86,
            Health: 88,
            Fitness: 86,
            ActiveConditions: [],
            ActiveStatuses: [PetStatusType.Happy]);

        var snapshot = _interpreter.BuildSnapshot(pet);

        Assert.Equal(PetWellbeingUrgency.Stable, snapshot.Urgency);
        Assert.Equal(PetEmotionChannel.Relief, snapshot.DominantEmotion);
        Assert.Equal(PetDriveFamily.SelfMaintenance, snapshot.DominantDrive);
        Assert.Equal("Goose 1 is stable.", snapshot.Summary);
        Assert.Contains(PetStatusType.Happy, snapshot.Statuses);
    }

    [Fact]
    public void BuildSnapshot_LowEnergyMapsToRestAndExhaustion()
    {
        var pet = new PetActor(
            Guid.Parse("10000000-0000-0000-0000-000000000002"),
            "Crow 1",
            "crow",
            Hunger: 70,
            Thirst: 74,
            Energy: 18,
            Cleanliness: 80,
            Affection: 82,
            Comfort: 76,
            Health: 80,
            Fitness: 68,
            ActiveConditions: [],
            ActiveStatuses: [PetStatusType.Sleepy]);

        var snapshot = _interpreter.BuildSnapshot(pet);

        Assert.Equal(PetWellbeingUrgency.Critical, snapshot.Urgency);
        Assert.Equal(PetDriveFamily.Rest, snapshot.DominantDrive);
        Assert.Equal(PetEmotionChannel.Threat, snapshot.DominantEmotion);
        Assert.Equal(82, snapshot.NeedPressures["energy"]);
        Assert.Contains("energy", snapshot.Summary);
    }

    [Fact]
    public void BuildSnapshot_ConditionsOverrideDominantDriveToSafety()
    {
        var pet = new PetActor(
            Guid.Parse("10000000-0000-0000-0000-000000000003"),
            "Mouse 1",
            "mouse",
            Hunger: 80,
            Thirst: 80,
            Energy: 74,
            Cleanliness: 78,
            Affection: 72,
            Comfort: 74,
            Health: 76,
            Fitness: 70,
            ActiveConditions:
            [
                new PetConditionRecord("injury", 3, false),
                new PetConditionRecord("respiratoryProblems", 1, true)
            ],
            ActiveStatuses: [PetStatusType.Sick]);

        var snapshot = _interpreter.BuildSnapshot(pet);

        Assert.Equal(PetWellbeingUrgency.NeedsCare, snapshot.Urgency);
        Assert.Equal(PetDriveFamily.SafetyAvoidance, snapshot.DominantDrive);
        Assert.Equal(PetEmotionChannel.Agitation, snapshot.DominantEmotion);
        Assert.Equal(["injury", "respiratoryProblems"], snapshot.ActiveConditionIds);
        Assert.Contains("injury", snapshot.Summary);
    }

    [Fact]
    public void BuildSnapshot_ExposesPersonalityDescriptorsForAgentContext()
    {
        var pet = new PetActor(
            Guid.Parse("10000000-0000-0000-0000-000000000004"),
            "Rat 1",
            "rat",
            Energy: 82,
            Fitness: 70,
            Personality: new PetPersonalityProfile(
                FoodLove: 35,
                CuddleNeed: -30,
                CleanlinessPreference: 32,
                ActivityLevel: 28,
                Cheerfulness: 40,
                SocialNeed: -28,
                Playfulness: 45,
                Stubbornness: 10),
            ActiveConditions: [],
            ActiveStatuses: []);

        var snapshot = _interpreter.BuildSnapshot(pet);

        Assert.Equal(PetEmotionChannel.Curiosity, snapshot.DominantEmotion);
        Assert.Contains("playful", snapshot.PersonalityDescriptors);
        Assert.Contains("bright", snapshot.PersonalityDescriptors);
        Assert.Contains("solitary", snapshot.PersonalityDescriptors);
        Assert.DoesNotContain("food-motivated", snapshot.PersonalityDescriptors);
    }
}
