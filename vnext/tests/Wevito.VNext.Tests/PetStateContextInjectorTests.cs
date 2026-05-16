using Wevito.VNext.Contracts;
using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class PetStateContextInjectorTests
{
    [Fact]
    public void InjectsAlertForCriticalThreshold()
    {
        var now = DateTimeOffset.Parse("2026-05-15T12:00:00Z");
        var injector = new PetStateContextInjector();
        var pet = BuildPet(now, hunger: 8);

        var lines = injector.BuildContextLines([pet], new Dictionary<string, string>(), now, now);

        Assert.Single(lines);
        Assert.Contains("hunger", lines[0], StringComparison.OrdinalIgnoreCase);
        Assert.Contains("asks about pets", lines[0], StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DoesNotInjectAboveThreshold()
    {
        var now = DateTimeOffset.Parse("2026-05-15T12:00:00Z");
        var injector = new PetStateContextInjector();
        var pet = BuildPet(now, hunger: 65, thirst: 66, energy: 64, health: 80);

        var lines = injector.BuildContextLines([pet], new Dictionary<string, string>(), now, now);

        Assert.Empty(lines);
    }

    [Fact]
    public void RespectsUserDisabledSetting()
    {
        var now = DateTimeOffset.Parse("2026-05-15T12:00:00Z");
        var injector = new PetStateContextInjector();
        var pet = BuildPet(now, health: 20);
        var settings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [PetStateContextInjector.AiMentionsPetStateSetting] = bool.FalseString
        };

        var lines = injector.BuildContextLines([pet], settings, now, now);

        Assert.Empty(lines);
    }

    [Fact]
    public void DoesNotInjectStaleSnapshot()
    {
        var now = DateTimeOffset.Parse("2026-05-15T12:00:00Z");
        var injector = new PetStateContextInjector(TimeSpan.FromMinutes(5));
        var pet = BuildPet(now, health: 20);

        var lines = injector.BuildContextLines([pet], new Dictionary<string, string>(), now.AddMinutes(-6), now);

        Assert.Empty(lines);
    }

    private static PetActor BuildPet(
        DateTimeOffset now,
        double hunger = 84,
        double thirst = 82,
        double energy = 76,
        double health = 78)
    {
        return new PetSimulationEngine().CreatePet(
            new SpeciesDefinition("goose", "Goose", "#ffffff", 90, "pond"),
            PetAgeStage.Baby,
            PetGender.Female,
            "blue",
            "Goose 1",
            now,
            hunger: hunger,
            thirst: thirst,
            energy: energy,
            health: health);
    }
}
