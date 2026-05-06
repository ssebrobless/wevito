using Wevito.VNext.Contracts;
using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class HabitatLoadoutManifestTests
{
    [Fact]
    public async Task ContentRepository_LoadsHabitatLoadoutsForEverySpecies()
    {
        var repository = new ContentRepository(FindPath("vnext", "content"));

        var content = await repository.LoadAsync();

        Assert.NotNull(content.HabitatLoadouts);
        Assert.Equal(10, content.HabitatLoadouts.Count);
        Assert.Equal(
            content.Species.Select(species => species.Id).Order(StringComparer.OrdinalIgnoreCase),
            content.HabitatLoadouts.Select(loadout => loadout.SpeciesId).Order(StringComparer.OrdinalIgnoreCase));
        Assert.All(content.HabitatLoadouts, loadout =>
        {
            Assert.Equal(loadout.SpeciesId, loadout.EnvironmentId);
            Assert.Equal(["primary", "interaction", "decor"], loadout.Slots.Select(slot => slot.SlotId).ToArray());
            Assert.All(loadout.Slots, slot =>
            {
                Assert.False(string.IsNullOrWhiteSpace(slot.AssetId));
                Assert.True(slot.DefaultRect.Width > 0);
                Assert.True(slot.DefaultRect.Height > 0);
                Assert.True(slot.PriorityTier >= 0);
            });
        });
    }

    [Fact]
    public async Task PilotSpecies_MatchRefinedHabitatContract()
    {
        var repository = new ContentRepository(FindPath("vnext", "content"));

        var content = await repository.LoadAsync();

        Assert.NotNull(content.HabitatLoadouts);
        AssertPilot(content.HabitatLoadouts, "goose", "pond_dish", "ball", "pebble_cluster");
        AssertPilot(content.HabitatLoadouts, "rat", "crate_hideout", "snack_bowl", "storage_basket");
        AssertPilot(content.HabitatLoadouts, "crow", "branch_perch", "seed_tray", "shiny_reward");
        AssertPilot(content.HabitatLoadouts, "snake", "rock_basking_spot", "shallow_water_dish", "moss_patch");
        AssertPilot(content.HabitatLoadouts, "frog", "pond_dish", "bug_treat", "moss_patch");
    }

    private static void AssertPilot(
        IReadOnlyList<HabitatLoadoutDefinition> loadouts,
        string speciesId,
        string primaryAsset,
        string interactionAsset,
        string decorAsset)
    {
        var loadout = loadouts.Single(candidate => candidate.SpeciesId == speciesId);
        Assert.Equal(primaryAsset, loadout.Slots.Single(slot => slot.SlotId == "primary").AssetId);
        Assert.Equal(interactionAsset, loadout.Slots.Single(slot => slot.SlotId == "interaction").AssetId);
        Assert.Equal(decorAsset, loadout.Slots.Single(slot => slot.SlotId == "decor").AssetId);
        Assert.Equal(DepthBand.GroundContact, loadout.Slots.Single(slot => slot.SlotId == "primary").DepthBand);
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
