using Wevito.VNext.Contracts;
using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class ContentCoverageTests
{
    [Fact]
    public async Task ContentRepository_LoadsExpandedCatalog()
    {
        var contentRoot = FindPath("vnext", "content");
        var repository = new ContentRepository(contentRoot);

        var content = await repository.LoadAsync();

        Assert.Equal(10, content.Species.Count);
        Assert.Equal(9, content.Actions.Count(action => action.IsPrimaryAction));
        Assert.True(content.Environments.Count >= 10);
        Assert.Equal(7, content.Needs.Count);
        Assert.Equal(8, content.Statuses.Count);
        Assert.True(content.Items.Count >= 7);
    }

    [Fact]
    public void SpriteInventory_ContainsRequiredFramesForEnabledVariants()
    {
        var spriteRoot = FindPath("sprites");

        var minimumFrameCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["idle"] = 4,
            ["walk"] = 4,
            ["eat"] = 4,
            ["happy"] = 4,
            ["sad"] = 2,
            ["sleep"] = 2,
            ["sick"] = 4,
            ["bathe"] = 4
        };

        var variantDirectories = Directory
            .EnumerateDirectories(spriteRoot)
            .SelectMany(speciesDirectory => Directory.EnumerateDirectories(speciesDirectory, "*", SearchOption.AllDirectories))
            .Where(path =>
            {
                var relative = Path.GetRelativePath(spriteRoot, path);
                var parts = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                return parts.Length >= 4 && !parts.Contains("obj", StringComparer.OrdinalIgnoreCase);
            })
            .ToList();

        Assert.NotEmpty(variantDirectories);

        foreach (var directory in variantDirectories)
        {
            foreach (var pair in minimumFrameCounts)
            {
                var frames = Directory
                    .EnumerateFiles(directory, $"{pair.Key}_*.png", SearchOption.TopDirectoryOnly)
                    .Where(path => !path.EndsWith(".import", StringComparison.OrdinalIgnoreCase))
                    .ToList();
                Assert.True(frames.Count >= pair.Value, $"Expected at least {pair.Value} frame(s) for '{pair.Key}' in {directory}, found {frames.Count}.");
            }
        }
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
