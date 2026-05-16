using System.IO;
using System.Text.Json;
using Wevito.VNext.Contracts;

namespace Wevito.VNext.Shell;

public sealed record StarterEggOption(
    string ColorVariant,
    string Label,
    string HexColor,
    string SpeciesId,
    bool IsEnabled = true,
    string DisabledReason = "");

public static class StarterEggCatalog
{
    private static readonly Lazy<IReadOnlyList<StarterEggOption>> LoadedEggs = new(() => LoadFromPath(ResolveDefaultManifestPath()));

    public static IReadOnlyList<StarterEggOption> Eggs => LoadedEggs.Value;

    public static string ManifestPath => ResolveDefaultManifestPath();

    internal static IReadOnlyList<StarterEggOption> LoadFromPath(string path)
    {
        if (!File.Exists(path))
        {
            return FallbackEggs;
        }

        try
        {
            using var stream = File.OpenRead(path);
            var manifest = JsonSerializer.Deserialize<StarterEggManifest>(stream, JsonDefaults.Options);
            var eggs = manifest?.Eggs?
                .Where(egg => !string.IsNullOrWhiteSpace(egg.Color) &&
                              !string.IsNullOrWhiteSpace(egg.Label) &&
                              !string.IsNullOrWhiteSpace(egg.Hex) &&
                              !string.IsNullOrWhiteSpace(egg.Species))
                .Select(egg => new StarterEggOption(
                    egg.Color.Trim(),
                    egg.Label.Trim(),
                    egg.Hex.Trim(),
                    egg.Species.Trim(),
                    egg.Enabled,
                    egg.DisabledReason?.Trim() ?? ""))
                .ToList();

            return eggs is { Count: > 0 } ? eggs : FallbackEggs;
        }
        catch (JsonException)
        {
            return FallbackEggs;
        }
        catch (IOException)
        {
            return FallbackEggs;
        }
    }

    private static IReadOnlyList<StarterEggOption> FallbackEggs { get; } =
    [
        new("red", "Red egg", "#D94A42", "fox"),
        new("orange", "Orange egg", "#E98635", "squirrel"),
        new("yellow", "Yellow egg", "#E8C84E", "goose"),
        new("green", "Green egg", "#62B45D", "deer", false, "Green runtime sprites are not installed yet."),
        new("blue", "Blue egg", "#4C8FE8", "frog"),
        new("indigo", "Indigo egg", "#4B5BC8", "crow"),
        new("violet", "Violet egg", "#8C58D8", "raccoon")
    ];

    public static StarterEggOption? Resolve(string colorVariant)
    {
        return Eggs.FirstOrDefault(egg => string.Equals(egg.ColorVariant, colorVariant, StringComparison.OrdinalIgnoreCase));
    }

    private static string ResolveDefaultManifestPath()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "vnext", "content", "starter_eggs.json");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        return Path.Combine(AppContext.BaseDirectory, "vnext", "content", "starter_eggs.json");
    }

    private sealed record StarterEggManifest(string Version, IReadOnlyList<StarterEggManifestEntry>? Eggs);

    private sealed record StarterEggManifestEntry(
        string Color,
        string Label,
        string Hex,
        string Species,
        bool Enabled = true,
        string? DisabledReason = "");
}
