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
            Assert.Equal(
                ["primary", "bed-left", "bed-center", "bed-right", "food", "water"],
                loadout.Slots.Select(slot => slot.SlotId).ToArray());
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
    public async Task AllSpecies_UseUniversalHomeContractWithThreeBeds()
    {
        var repository = new ContentRepository(FindPath("vnext", "content"));

        var content = await repository.LoadAsync();

        Assert.NotNull(content.HabitatLoadouts);
        foreach (var loadout in content.HabitatLoadouts)
        {
            AssertUniversalHome(loadout);
        }
    }

    private static void AssertUniversalHome(HabitatLoadoutDefinition loadout)
    {
        Assert.Equal("log_shelter", loadout.Slots.Single(slot => slot.SlotId == "primary").AssetId);
        Assert.Equal(["moss_bed", "moss_bed", "moss_bed"], loadout.Slots
            .Where(slot => slot.SlotId.StartsWith("bed-", StringComparison.OrdinalIgnoreCase))
            .Select(slot => slot.AssetId)
            .ToArray());
        Assert.Equal("snack_bowl", loadout.Slots.Single(slot => slot.SlotId == "food").AssetId);
        Assert.Equal("water_bowl", loadout.Slots.Single(slot => slot.SlotId == "water").AssetId);
        Assert.Equal(DepthBand.GroundContact, loadout.Slots.Single(slot => slot.SlotId == "primary").DepthBand);
        Assert.All(loadout.Slots.Where(slot => slot.SlotId.StartsWith("bed-", StringComparison.OrdinalIgnoreCase)), slot =>
            Assert.Equal(DepthBand.GroundContact, slot.DepthBand));
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
