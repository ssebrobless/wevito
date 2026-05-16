using Wevito.VNext.Shell;

namespace Wevito.VNext.Tests;

public sealed class StarterEggCatalogTests
{
    [Fact]
    public void EggsLoadedFromStarterEggsJson()
    {
        Assert.EndsWith(Path.Combine("vnext", "content", "starter_eggs.json"), StarterEggCatalog.ManifestPath);
        Assert.True(File.Exists(StarterEggCatalog.ManifestPath));

        var fromFile = StarterEggCatalog.LoadFromPath(StarterEggCatalog.ManifestPath);

        Assert.Equal(7, fromFile.Count);
        Assert.Equal("fox", fromFile.Single(egg => egg.ColorVariant == "red").SpeciesId);
    }

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

    [Fact]
    public void OrangeEggMapsToSquirrelSpecies()
    {
        Assert.Equal("squirrel", StarterEggCatalog.Resolve("orange")?.SpeciesId);
    }

    [Fact]
    public void FallbackUsedWhenJsonMissing()
    {
        var eggs = StarterEggCatalog.LoadFromPath(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "missing.json"));

        Assert.Equal(["red", "orange", "yellow", "green", "blue", "indigo", "violet"], eggs.Select(egg => egg.ColorVariant).ToArray());
        Assert.Equal("fox", eggs.Single(egg => egg.ColorVariant == "red").SpeciesId);
    }

    [Fact]
    public void GodotAndCSharpReadFromSameStarterEggSource()
    {
        var gameManager = File.ReadAllText(FindRepoFile("scripts", "game_manager.gd"));
        var mainScene = File.ReadAllText(FindRepoFile("scripts", "main_scene.gd"));

        Assert.Contains("STARTER_EGGS_PATH", gameManager);
        Assert.Contains("starter_eggs.json", gameManager);
        Assert.Contains("_resolve_starter_egg_species", gameManager);
        Assert.DoesNotContain("pd.animal_type = ANIMAL_TYPES.pick_random()", gameManager);
        Assert.Contains("get_starter_egg_options", mainScene);
        Assert.DoesNotContain("var egg_colors = [\"red\", \"orange\", \"yellow\", \"blue\", \"indigo\", \"violet\"]", mainScene);
    }

    private static string FindRepoFile(params string[] relativeParts)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(new[] { directory.FullName }.Concat(relativeParts).ToArray());
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException($"Could not find {string.Join(Path.DirectorySeparatorChar, relativeParts)} from {AppContext.BaseDirectory}.");
    }
}
