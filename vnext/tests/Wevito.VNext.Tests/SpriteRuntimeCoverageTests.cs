using System.Buffers.Binary;
using System.Text.Json;

namespace Wevito.VNext.Tests;

public sealed class SpriteRuntimeCoverageTests
{
    private static readonly IReadOnlyDictionary<string, int> RequiredAnimations = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
    {
        ["idle"] = 4,
        ["walk"] = 6,
        ["eat"] = 4,
        ["happy"] = 4,
        ["sad"] = 2,
        ["sleep"] = 2,
        ["sick"] = 4,
        ["bathe"] = 4
    };

    [Fact]
    public void RuntimeSpriteTree_HasExpectedShape()
    {
        var spriteRoot = FindPath("sprites_runtime");
        var summaryPath = Path.Combine(spriteRoot, "generation-summary.json");
        Assert.True(File.Exists(summaryPath), "Expected sprite runtime generation summary.");

        using var document = JsonDocument.Parse(File.ReadAllText(summaryPath));
        var speciesCounts = document.RootElement.GetProperty("species_counts");
        Assert.Equal(10, speciesCounts.EnumerateObject().Count());

        foreach (var species in speciesCounts.EnumerateObject())
        {
            Assert.True(species.Value.GetInt32() > 0, $"Expected exported frames for {species.Name}.");
        }
    }

    [Fact]
    public void RuntimeSpriteFrames_AreCanonicalTransparentPngs()
    {
        var spriteRoot = FindPath("sprites_runtime");
        var variantDirectories = Directory
            .EnumerateDirectories(spriteRoot)
            .SelectMany(speciesDirectory => Directory.EnumerateDirectories(speciesDirectory, "*", SearchOption.AllDirectories))
            .Where(path =>
            {
                var relative = Path.GetRelativePath(spriteRoot, path);
                var parts = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                return parts.Length == 4;
            })
            .ToList();

        Assert.Equal(10 * 3 * 2 * 6, variantDirectories.Count);

        foreach (var directory in variantDirectories)
        {
            foreach (var requirement in RequiredAnimations)
            {
                var frames = Directory
                    .EnumerateFiles(directory, $"{requirement.Key}_*.png", SearchOption.TopDirectoryOnly)
                    .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                Assert.Equal(requirement.Value, frames.Count);

                foreach (var frame in frames)
                {
                    var png = ReadPngMetadata(frame);
                    var expectedCanvas = ResolveExpectedCanvas(spriteRoot, frame);
                    Assert.Equal(expectedCanvas.Width, png.Width);
                    Assert.Equal(expectedCanvas.Height, png.Height);
                    Assert.Contains(png.ColorType, new byte[] { 4, 6 });
                }
            }
        }
    }

    private static (uint Width, uint Height) ResolveExpectedCanvas(string spriteRoot, string framePath)
    {
        var relative = Path.GetRelativePath(spriteRoot, framePath);
        var parts = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (parts.Length < 5)
        {
            return (72u, 64u);
        }

        var species = parts[0];
        var ageStage = parts[1];
        var animationName = Path.GetFileNameWithoutExtension(parts[^1]).Split('_', 2)[0];
        if (string.Equals(species, "snake", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(animationName, "walk", StringComparison.OrdinalIgnoreCase))
        {
            return ageStage.ToLowerInvariant() switch
            {
                "baby" => (104u, 64u),
                "teen" => (112u, 64u),
                _ => (120u, 64u)
            };
        }

        return (72u, 64u);
    }

    private static (uint Width, uint Height, byte ColorType) ReadPngMetadata(string path)
    {
        using var stream = File.OpenRead(path);
        Span<byte> header = stackalloc byte[33];
        var bytesRead = stream.Read(header);
        Assert.True(bytesRead >= 33, $"Expected full PNG header in {path}.");

        var signature = header[..8].ToArray();
        Assert.True(signature.SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }), $"Invalid PNG signature: {path}");

        var ihdrName = header[12..16].ToArray();
        Assert.True(ihdrName.SequenceEqual(new byte[] { 73, 72, 68, 82 }), $"Missing IHDR chunk in {path}");

        var width = BinaryPrimitives.ReadUInt32BigEndian(header[16..20]);
        var height = BinaryPrimitives.ReadUInt32BigEndian(header[20..24]);
        var colorType = header[25];
        return (width, height, colorType);
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
