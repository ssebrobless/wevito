using Wevito.VNext.Contracts;
using Wevito.VNext.Shell;

namespace Wevito.VNext.Tests;

public sealed class DevControlSnapshotBuilderTests
{
    [Fact]
    public void Build_ReturnsThreeSlotsAndOptions()
    {
        var content = CreateContent();
        var pets = new[]
        {
            new PetActor(Guid.Parse("11111111-1111-1111-1111-111111111111"), "Fox 1", "fox", AgeStage: PetAgeStage.Baby, Gender: PetGender.Female, ColorVariant: "blue"),
            new PetActor(Guid.Parse("22222222-2222-2222-2222-222222222222"), "Goose 1", "goose", AgeStage: PetAgeStage.Adult, Gender: PetGender.Male, ColorVariant: "red")
        };

        var snapshot = DevControlSnapshotBuilder.Build(pets, content, DateTimeOffset.Parse("2026-05-14T12:00:00Z"));

        Assert.Equal(3, snapshot.Slots.Count);
        Assert.Equal("Fox 1", snapshot.Slots[0].Name);
        Assert.Equal("goose", snapshot.Slots[1].SpeciesId);
        Assert.Equal("N/A", snapshot.Slots[2].DisplayText);
        Assert.Contains("fox", snapshot.Options.SpeciesIds);
        Assert.Contains("baby", snapshot.Options.LifeStages);
        Assert.Contains(snapshot.Options.Actions, action => action.ActionId == "feed");
    }

    [Fact]
    public void TryResolveSlotRejectsPetMismatch()
    {
        var pets = new[]
        {
            new PetActor(Guid.Parse("11111111-1111-1111-1111-111111111111"), "Fox 1", "fox")
        };

        var result = DevControlSnapshotBuilder.TryResolveSlot(
            pets,
            0,
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            out _,
            out var message);

        Assert.False(result);
        Assert.Contains("changed", message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SnapshotIncludesAllSpeciesAndAgesAndColors()
    {
        var snapshot = DevControlSnapshotBuilder.Build([], CreateContent(), DateTimeOffset.Parse("2026-05-16T12:00:00Z"));

        Assert.Contains("fox", snapshot.Options.SpeciesIds);
        Assert.Contains("goose", snapshot.Options.SpeciesIds);
        Assert.Contains("baby", snapshot.Options.LifeStages);
        Assert.Contains("teen", snapshot.Options.LifeStages);
        Assert.Contains("adult", snapshot.Options.LifeStages);
        Assert.Contains("female", snapshot.Options.Genders);
        Assert.Contains("male", snapshot.Options.Genders);
        Assert.Contains("blue", snapshot.Options.ColorVariants);
        Assert.Contains("red", snapshot.Options.ColorVariants);
    }

    private static GameContent CreateContent()
    {
        return new GameContent(
            [
                new SpeciesDefinition("fox", "Fox", "#f87", 96, SupportedAgeStages: [PetAgeStage.Baby, PetAgeStage.Adult], SupportedGenders: [PetGender.Female, PetGender.Male], SupportedColors: ["blue", "red"]),
                new SpeciesDefinition("goose", "Goose", "#fff", 82, SupportedAgeStages: [PetAgeStage.Baby, PetAgeStage.Teen, PetAgeStage.Adult], SupportedGenders: [PetGender.Female, PetGender.Male], SupportedColors: ["blue", "red"])
            ],
            [
                new ActionDefinition("feed", "Feed", "Feed pet", AnimationState: PetAnimationState.Eat),
                new ActionDefinition("water", "Water", "Water pet", AnimationState: PetAnimationState.Idle)
            ],
            [],
            [],
            [],
            [],
            [],
            []);
    }
}
