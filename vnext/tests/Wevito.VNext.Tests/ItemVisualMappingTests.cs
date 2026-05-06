using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class ItemVisualMappingTests
{
    [Fact]
    public async Task ContentRepository_LoadsEverySharedItemVisualMapping()
    {
        var contentRoot = FindPath("vnext", "content");
        var repository = new ContentRepository(contentRoot);

        var content = await repository.LoadAsync();

        Assert.NotNull(content.ItemVisualMappings);
        Assert.Equal(81, content.ItemVisualMappings.Count);
        Assert.All(content.ItemVisualMappings, mapping =>
        {
            Assert.False(string.IsNullOrWhiteSpace(mapping.Id));
            Assert.False(string.IsNullOrWhiteSpace(mapping.DisplayName));
            Assert.False(string.IsNullOrWhiteSpace(mapping.Category));
            Assert.StartsWith("items/", mapping.VisualAssetId, StringComparison.Ordinal);
        });
        Assert.Equal(
            content.ItemVisualMappings.Count,
            content.ItemVisualMappings.Select(mapping => mapping.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Fact]
    public async Task ItemVisualMappings_PointAtExistingSharedRuntimePngs()
    {
        var contentRoot = FindPath("vnext", "content");
        var sharedRoot = FindPath("sprites_shared_runtime");
        var repository = new ContentRepository(contentRoot);

        var content = await repository.LoadAsync();

        Assert.NotNull(content.ItemVisualMappings);
        foreach (var mapping in content.ItemVisualMappings)
        {
            var relativeAssetPath = mapping.VisualAssetId.Replace('/', Path.DirectorySeparatorChar);
            var assetPath = Path.Combine(sharedRoot, $"{relativeAssetPath}.png");
            Assert.True(File.Exists(assetPath), $"Missing shared item visual for '{mapping.Id}' at {assetPath}.");
        }
    }

    [Fact]
    public async Task NarrowMedicineCareVisuals_AreNotMarkedSmallIconSafe()
    {
        var contentRoot = FindPath("vnext", "content");
        var repository = new ContentRepository(contentRoot);

        var content = await repository.LoadAsync();

        Assert.NotNull(content.ItemVisualMappings);
        Assert.False(content.ItemVisualMappings.Single(mapping => mapping.Id == "care-medicine-dropper").SmallIconSafe);
        Assert.False(content.ItemVisualMappings.Single(mapping => mapping.Id == "care-thermometer").SmallIconSafe);
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
