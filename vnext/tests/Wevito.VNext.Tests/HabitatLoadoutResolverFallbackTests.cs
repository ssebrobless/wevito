using Wevito.VNext.Contracts;
using Wevito.VNext.Core;
using Wevito.VNext.Shell;

namespace Wevito.VNext.Tests;

public sealed class HabitatLoadoutResolverFallbackTests
{
    [Fact]
    public async Task Resolve_AttachesVisualMappingMetadataToRecommendedAndActionItems()
    {
        var repository = new ContentRepository(FindPath("vnext", "content"));
        var content = await repository.LoadAsync();
        var state = CreateState("goose", pet => pet with
        {
            Health = 42,
            Cleanliness = 35,
            ActiveConditions = [new PetConditionRecord("injury", 3, false)]
        });

        var loadout = HabitatLoadoutResolver.Resolve(state, content);
        var actionItems = loadout.ActionOptions.Values.SelectMany(items => items).ToArray();

        Assert.All(loadout.RecommendedItems, item => Assert.False(string.IsNullOrWhiteSpace(item.VisualMappingId)));
        Assert.All(actionItems, item => Assert.False(string.IsNullOrWhiteSpace(item.VisualMappingId)));
    }

    [Fact]
    public void EnrichForVisualMapping_PreservesSmallIconSafetyForNarrowCareAssets()
    {
        var item = new HabitatDisplayItem(
            "care:medicine_dropper",
            "Medicine Dropper",
            "care",
            "medicine_dropper",
            "Medicine",
            ActionId: "medicine");
        var mappings = new[]
        {
            new ItemVisualMapping(
                "care-medicine-dropper",
                "Medicine Dropper",
                "care",
                "items/care/medicine_dropper",
                SmallIconSafe: false)
        };

        var enriched = HabitatLoadoutResolver.EnrichForVisualMapping(item, mappings);

        Assert.Equal("care-medicine-dropper", enriched.VisualMappingId);
        Assert.False(enriched.IsSmallIconSafe);
    }

    [Fact]
    public async Task Resolve_UsesManifestStagePropsWhenSpeciesLoadoutExists()
    {
        var repository = new ContentRepository(FindPath("vnext", "content"));
        var content = await repository.LoadAsync();
        var state = CreateState("goose");

        var loadout = HabitatLoadoutResolver.Resolve(state, content);

        Assert.Equal(["moss_bed", "moss_bed", "moss_bed"], loadout.DynamicStageProps.Select(prop => prop.AssetId).ToArray());
        Assert.All(loadout.DynamicStageProps, prop =>
        {
            Assert.Equal("toys_b", prop.CategoryFolder);
            Assert.Equal(DepthBand.GroundContact, prop.DepthBand);
            Assert.Equal(OcclusionMode.None, prop.OcclusionMode);
            Assert.Equal(ContactShadowMode.Soft, prop.ContactShadowMode);
        });
        Assert.Equal(3, loadout.DynamicStageProps.Count(prop => prop.SlotId.StartsWith("bed-", StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public async Task Resolve_ShowsUniversalHomePropsWithoutNeedGating()
    {
        var repository = new ContentRepository(FindPath("vnext", "content"));
        var content = await repository.LoadAsync();
        var state = CreateState("goose", pet => pet with
        {
            LastActionId = "play",
            LastActionAtUtc = DateTimeOffset.UtcNow
        });

        var loadout = HabitatLoadoutResolver.Resolve(state, content);

        Assert.Equal(["moss_bed", "moss_bed", "moss_bed"], loadout.DynamicStageProps.Select(prop => prop.AssetId).ToArray());
        Assert.DoesNotContain(loadout.DynamicStageProps, prop => prop.SlotId is "food" or "water" or "primary");
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
        Assert.NotEqual(["moss_bed", "moss_bed", "moss_bed"], loadout.DynamicStageProps.Select(prop => prop.AssetId).ToArray());
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
        Assert.NotEqual(["moss_bed", "moss_bed", "moss_bed"], loadout.DynamicStageProps.Select(prop => prop.AssetId).ToArray());
    }

    private static CompanionState CreateState(string speciesId, Func<PetActor, PetActor>? configurePet = null)
    {
        var pet = new PetActor(
            Guid.NewGuid(),
            $"{speciesId} 1",
            speciesId,
            AgeStage: PetAgeStage.Baby,
            Gender: PetGender.Female,
            ColorVariant: "blue");
        pet = configurePet?.Invoke(pet) ?? pet;

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
