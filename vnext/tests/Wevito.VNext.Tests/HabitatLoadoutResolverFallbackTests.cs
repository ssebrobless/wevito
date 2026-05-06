using Wevito.VNext.Contracts;
using Wevito.VNext.Core;
using Wevito.VNext.Shell;

namespace Wevito.VNext.Tests;

public sealed class HabitatLoadoutResolverFallbackTests
{
    [Fact]
    public async Task Resolve_UsesManifestStagePropsWhenSpeciesLoadoutExists()
    {
        var repository = new ContentRepository(FindPath("vnext", "content"));
        var content = await repository.LoadAsync();
        var state = CreateState("goose");

        var loadout = HabitatLoadoutResolver.Resolve(state, content);

        Assert.Equal(["pond_dish", "ball", "pebble_cluster"], loadout.DynamicStageProps.Select(prop => prop.AssetId).ToArray());
        Assert.Contains(loadout.DynamicStageProps, prop =>
            prop.AssetId == "pond_dish" &&
            prop.CategoryFolder == "containers" &&
            prop.DepthBand == DepthBand.GroundContact &&
            prop.OcclusionMode == OcclusionMode.BodyOnly &&
            prop.ContactShadowMode == ContactShadowMode.Soft);
    }

    [Fact]
    public async Task Resolve_FallsBackWhenHabitatManifestIsMissing()
    {
        var repository = new ContentRepository(FindPath("vnext", "content"));
        var content = await repository.LoadAsync();
        content = content with { HabitatLoadouts = null };
        var state = CreateState("goose");

        var loadout = HabitatLoadoutResolver.Resolve(state, content);

        Assert.NotEmpty(loadout.DynamicStageProps);
        Assert.NotEqual(["pond_dish", "ball", "pebble_cluster"], loadout.DynamicStageProps.Select(prop => prop.AssetId).ToArray());
    }

    [Fact]
    public async Task Resolve_FallsBackWhenSpeciesLoadoutIsMissing()
    {
        var repository = new ContentRepository(FindPath("vnext", "content"));
        var content = await repository.LoadAsync();
        Assert.NotNull(content.HabitatLoadouts);
        content = content with
        {
            HabitatLoadouts = content.HabitatLoadouts
                .Where(loadout => !string.Equals(loadout.SpeciesId, "goose", StringComparison.OrdinalIgnoreCase))
                .ToList()
        };
        var state = CreateState("goose");

        var loadout = HabitatLoadoutResolver.Resolve(state, content);

        Assert.NotEmpty(loadout.DynamicStageProps);
        Assert.NotEqual(["pond_dish", "ball", "pebble_cluster"], loadout.DynamicStageProps.Select(prop => prop.AssetId).ToArray());
    }

    private static CompanionState CreateState(string speciesId)
    {
        var pet = new PetActor(
            Guid.NewGuid(),
            $"{speciesId} 1",
            speciesId,
            AgeStage: PetAgeStage.Baby,
            Gender: PetGender.Female,
            ColorVariant: "blue");

        return new CompanionState(
            CompanionMode.Focused,
            IsPinned: false,
            ActiveEnvironmentId: speciesId,
            ActiveTool: new ToolSession("home", IsOpen: false),
            ActivePets: [pet],
            BasketItems: [],
            SettingsSnapshot: new Dictionary<string, string>());
    }

    private static string FindPath(params string[] segments)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, Path.Combine(segments));
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException($"Could not locate path: {Path.Combine(segments)}");
    }
}
