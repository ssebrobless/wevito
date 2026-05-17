using System.Text.Json;

namespace Wevito.VNext.Core;

public sealed class SpriteRepairQueueReader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly IReadOnlyDictionary<string, int> PriorityOrder = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
    {
        ["P0"] = 0,
        ["P1"] = 1,
        ["P2"] = 2,
        ["P3"] = 3
    };

    private readonly KillSwitchService? _killSwitchService;

    public SpriteRepairQueueReader(KillSwitchService? killSwitchService = null)
    {
        _killSwitchService = killSwitchService;
    }

    public SpriteRepairQueueManifest Load(string queuePath, string? repoRoot = null)
    {
        if (_killSwitchService?.IsActive() == true)
        {
            return SpriteRepairQueueManifest.Empty(queuePath);
        }

        if (string.IsNullOrWhiteSpace(queuePath))
        {
            throw new ArgumentException("Queue path is required.", nameof(queuePath));
        }

        var fullPath = Path.GetFullPath(queuePath);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException("Sprite repair queue was not found.", fullPath);
        }

        var manifest = JsonSerializer.Deserialize<SpriteRepairQueueManifest>(
            File.ReadAllText(fullPath),
            JsonOptions) ?? throw new InvalidOperationException("Sprite repair queue could not be parsed.");

        var root = repoRoot is null ? ResolveRepoRoot(fullPath) : Path.GetFullPath(repoRoot);
        Validate(manifest, root);

        return manifest with
        {
            Rows = manifest.Rows
                .OrderBy(row => PriorityRank(row.Priority))
                .ThenBy(row => row.SpeciesId, StringComparer.OrdinalIgnoreCase)
                .ThenBy(row => AgeRank(row.LifeStage))
                .ThenBy(row => row.Gender, StringComparer.OrdinalIgnoreCase)
                .ToArray()
        };
    }

    private static void Validate(SpriteRepairQueueManifest manifest, string repoRoot)
    {
        foreach (var row in manifest.Rows)
        {
            if (row.LifeStage.Equals("senior", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Repair queue row references unsupported Senior stage: {row.RowId}");
            }

            foreach (var tool in row.RecommendedTools)
            {
                var toolPath = Path.Combine(repoRoot, tool.Replace('/', Path.DirectorySeparatorChar));
                if (!File.Exists(toolPath))
                {
                    throw new FileNotFoundException($"Repair queue row {row.RowId} references missing repair tool.", toolPath);
                }
            }
        }
    }

    private static string ResolveRepoRoot(string path)
    {
        var directory = new DirectoryInfo(Path.GetDirectoryName(path) ?? Directory.GetCurrentDirectory());
        while (directory is not null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, ".git")) ||
                File.Exists(Path.Combine(directory.FullName, "wevito.godot")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return Directory.GetCurrentDirectory();
    }

    private static int PriorityRank(string priority)
    {
        return PriorityOrder.TryGetValue(priority, out var rank) ? rank : int.MaxValue;
    }

    private static int AgeRank(string ageStage)
    {
        return ageStage.ToLowerInvariant() switch
        {
            "baby" => 0,
            "teen" => 1,
            "adult" => 2,
            _ => 99
        };
    }
}

public sealed record SpriteRepairQueueManifest(
    string SchemaVersion,
    DateTimeOffset GeneratedAtUtc,
    string SourceManifestPath,
    string SourceManifestGeneratedAtUtc,
    int ExpectedFamilyCount,
    int RowCount,
    IReadOnlyDictionary<string, int> PriorityCounts,
    IReadOnlyDictionary<string, int> TagCounts,
    IReadOnlyList<SpriteRepairQueueRow> Rows)
{
    public static SpriteRepairQueueManifest Empty(string sourcePath)
    {
        return new SpriteRepairQueueManifest(
            "1.0",
            DateTimeOffset.UtcNow,
            sourcePath,
            "",
            0,
            0,
            new Dictionary<string, int>(),
            new Dictionary<string, int>(),
            []);
    }
}

public sealed record SpriteRepairQueueRow(
    string RowId,
    string SpeciesId,
    string LifeStage,
    string Gender,
    string Priority,
    string Status,
    int IssueCount,
    IReadOnlyList<string> ColorsAffected,
    IReadOnlyList<string> AnimationsAffected,
    IReadOnlyList<string> RecommendedTools,
    IReadOnlyList<SpriteRepairQueueIssue> Issues);

public sealed record SpriteRepairQueueIssue(
    string ColorVariant,
    string AnimationFamily,
    string Severity,
    IReadOnlyList<string> Tags,
    IReadOnlyList<string> Warnings,
    string RepairTool,
    string Reason,
    string? SourcePath,
    string? CapturePath);
