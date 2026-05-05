using System.Buffers.Binary;
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
        var speciesDirectories = Directory
            .EnumerateDirectories(spriteRoot)
            .Where(path => !Path.GetFileName(path).StartsWith("_", StringComparison.OrdinalIgnoreCase))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();

        Assert.Equal(10, speciesDirectories.Count);

        foreach (var speciesDirectory in speciesDirectories)
        {
            var variantDirectories = Directory
                .EnumerateDirectories(speciesDirectory, "*", SearchOption.AllDirectories)
                .Where(path =>
                {
                    var relative = Path.GetRelativePath(spriteRoot, path);
                    var parts = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    return parts.Length == 4;
                })
                .ToList();

            Assert.Equal(3 * 2 * 6, variantDirectories.Count);
            Assert.True(
                variantDirectories.Any(directory => Directory.EnumerateFiles(directory, "*.png", SearchOption.TopDirectoryOnly).Any()),
                $"Expected exported frames for {Path.GetFileName(speciesDirectory)}.");
        }

        if (File.Exists(summaryPath))
        {
            Assert.True(new FileInfo(summaryPath).Length > 0, "Expected non-empty sprite runtime generation summary when present.");
        }
    }

    [Fact]
    public void RuntimeSpriteFrames_AreSequenceStableTransparentPngs()
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

                (uint Width, uint Height)? sequenceCanvas = null;
                foreach (var frame in frames)
                {
                    var png = ReadPngMetadata(frame);
                    Assert.Contains(png.ColorType, new byte[] { 4, 6 });
                    Assert.True(png.Width > 0, $"Expected non-empty PNG width for {frame}.");
                    Assert.True(png.Height > 0, $"Expected non-empty PNG height for {frame}.");

                    sequenceCanvas ??= (png.Width, png.Height);
                    Assert.Equal(sequenceCanvas.Value.Width, png.Width);
                    Assert.Equal(sequenceCanvas.Value.Height, png.Height);
                }
            }
        }
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
