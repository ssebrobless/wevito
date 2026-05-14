using Wevito.VNext.Shell;

namespace Wevito.VNext.Tests;

public sealed class StarterEggCatalogTests
{
    [Fact]
    public void Eggs_AreRoygbivColorChoicesNotSpeciesChoices()
    {
        var colors = StarterEggCatalog.Eggs.Select(egg => egg.ColorVariant).ToArray();
        var labels = StarterEggCatalog.Eggs.Select(egg => egg.Label).ToArray();

        Assert.Equal(["red", "orange", "yellow", "green", "blue", "indigo", "violet"], colors);
        Assert.Equal(["Red egg", "Orange egg", "Yellow egg", "Green egg", "Blue egg", "Indigo egg", "Violet egg"], labels);
    }

    [Fact]
    public void Eggs_HideSpeciesOutcomeUntilHatch()
    {
        Assert.All(StarterEggCatalog.Eggs, egg => Assert.DoesNotContain(egg.SpeciesId, egg.Label, StringComparison.OrdinalIgnoreCase));
        Assert.Equal("fox", StarterEggCatalog.Resolve("red")?.SpeciesId);
    }

    [Fact]
    public void Eggs_DoNotEnableGreenUntilRuntimeGreenSpritesExist()
    {
        var green = StarterEggCatalog.Resolve("green");

        Assert.NotNull(green);
        Assert.False(green!.IsEnabled);
        Assert.Equal("Green runtime sprites are not installed yet.", green.DisabledReason);
    }
}
