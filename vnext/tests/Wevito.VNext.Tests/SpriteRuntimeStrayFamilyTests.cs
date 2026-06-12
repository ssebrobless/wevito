namespace Wevito.VNext.Tests;

/// <summary>
/// C-PHASE 201 stray-family pin. Every animation frame under sprites_runtime must
/// belong to a contracted family (required or optional). This is the C# twin of the
/// strict check in tools/audit_sprite_contract.py, so dead families can never
/// silently accumulate again (the 504 unreachable play_* frames removed in
/// C-PHASE 201 are the motivating case).
/// </summary>
public sealed class SpriteRuntimeStrayFamilyTests
{
    private static readonly IReadOnlySet<string> RequiredFamilies = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "idle", "walk", "eat", "happy", "sad", "sleep", "sick", "bathe", "groom"
    };

    private static readonly IReadOnlySet<string> OptionalFamilies = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "drink", "play_ball", "hold_ball", "pickup_ball", "drop_ball", "carry_ball_walk", "carry_ball_run"
    };

    [Fact]
    public void RuntimeSpriteTree_ContainsOnlyContractedFamilies()
    {
        var spriteRoot = FindPath("sprites_runtime");
        var strays = new List<string>();

        foreach (var speciesDirectory in Directory.EnumerateDirectories(spriteRoot))
        {
            if (Path.GetFileName(speciesDirectory).StartsWith("_", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            foreach (var framePath in Directory.EnumerateFiles(speciesDirectory, "*.png", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(spriteRoot, framePath);
                var depth = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Length;
                if (depth != 5)
                {
                    continue; // only species/age/gender/color/frame.png leaves are variant frames
                }

                var stem = Path.GetFileNameWithoutExtension(framePath);
                var separator = stem.LastIndexOf('_');
                var family = separator > 0 ? stem[..separator] : string.Empty;
                var frameIndex = separator > 0 ? stem[(separator + 1)..] : string.Empty;

                if (family.Length == 0 || frameIndex.Length == 0 || !frameIndex.All(char.IsAsciiDigit))
                {
                    strays.Add($"{relative} (naming contract)");
                    continue;
                }

                if (!RequiredFamilies.Contains(family) && !OptionalFamilies.Contains(family))
                {
                    strays.Add($"{relative} (uncontracted family '{family}')");
                }
            }
        }

        Assert.True(strays.Count == 0, $"Stray runtime sprite files found:{Environment.NewLine}{string.Join(Environment.NewLine, strays.Take(25))}");
    }

    [Fact]
    public void RequiredFamilies_IncludeGroom_RegisteredInCPhase201()
    {
        Assert.Contains("groom", RequiredFamilies);
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

        throw new DirectoryNotFoundException($"Could not locate {Path.Combine(segments)} above {AppContext.BaseDirectory}.");
    }
}
