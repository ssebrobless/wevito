using Blake3;
using SkiaSharp;
using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed class SpriteWorkflowManifestReader
{
    private static readonly IReadOnlyDictionary<string, PetAgeStage> AgeMap = new Dictionary<string, PetAgeStage>(StringComparer.OrdinalIgnoreCase)
    {
        ["baby"] = PetAgeStage.Baby,
        ["teen"] = PetAgeStage.Teen,
        ["adult"] = PetAgeStage.Adult
    };

    private static readonly IReadOnlyDictionary<string, PetGender> GenderMap = new Dictionary<string, PetGender>(StringComparer.OrdinalIgnoreCase)
    {
        ["female"] = PetGender.Female,
        ["male"] = PetGender.Male
    };

    public SpriteWorkflowManifestSnapshot Read(string repoRoot, DateTimeOffset? nowUtc = null)
    {
        var canonicalRepoRoot = Path.GetFullPath(repoRoot);
        var rows = ReadRows(canonicalRepoRoot);
        return new SpriteWorkflowManifestSnapshot(
            "1",
            canonicalRepoRoot,
            rows,
            nowUtc ?? DateTimeOffset.UtcNow);
    }

    private static IReadOnlyList<SpriteWorkflowQueueRow> ReadRows(string repoRoot)
    {
        var evidenceByRow = new SortedDictionary<string, List<SpriteWorkflowRowEvidence>>(StringComparer.OrdinalIgnoreCase);
        foreach (var root in ResolveRoots(repoRoot))
        {
            if (!Directory.Exists(root.RootPath))
            {
                continue;
            }

            foreach (var colorDirectory in Directory.EnumerateDirectories(root.RootPath, "*", SearchOption.AllDirectories))
            {
                if (!TryParseVariantDirectory(root.RootPath, colorDirectory, out var species, out var age, out var gender, out var color))
                {
                    continue;
                }

                var frameGroups = Directory.EnumerateFiles(colorDirectory, "*.png", SearchOption.TopDirectoryOnly)
                    .Select(path => BuildFrameEntry(root.RootKind, root.RootPath, path))
                    .Where(entry => entry is not null)
                    .Cast<SpriteWorkflowFrameEntry>()
                    .GroupBy(entry => ParseFamily(entry.FrameId), StringComparer.OrdinalIgnoreCase);

                foreach (var group in frameGroups)
                {
                    var key = new SpriteRowKey(species, age, gender, color, group.Key);
                    var rowId = BuildRowId(key);
                    if (!evidenceByRow.TryGetValue(rowId, out var evidence))
                    {
                        evidence = [];
                        evidenceByRow[rowId] = evidence;
                    }

                    evidence.Add(new SpriteWorkflowRowEvidence(
                        root.RootKind,
                        root.RootPath,
                        group.OrderBy(entry => entry.FrameId, StringComparer.OrdinalIgnoreCase).ToList()));
                }
            }
        }

        return evidenceByRow
            .Select(pair =>
            {
                var key = ParseRowId(pair.Key);
                var findings = BuildFindings(pair.Value);
                return new SpriteWorkflowQueueRow(key, pair.Key, pair.Value, findings);
            })
            .ToList();
    }

    private static IReadOnlyList<SpriteWorkflowRoot> ResolveRoots(string repoRoot)
    {
        return
        [
            new SpriteWorkflowRoot(SpriteWorkflowRootKind.Runtime, Path.Combine(repoRoot, "sprites_runtime")),
            new SpriteWorkflowRoot(SpriteWorkflowRootKind.Authored, Path.Combine(repoRoot, "sprites_authored")),
            new SpriteWorkflowRoot(SpriteWorkflowRootKind.AuthoredVerified, Path.Combine(repoRoot, "sprites_authored_verified"))
        ];
    }

    private static bool TryParseVariantDirectory(
        string rootPath,
        string directory,
        out string species,
        out PetAgeStage age,
        out PetGender gender,
        out string color)
    {
        species = "";
        age = PetAgeStage.Baby;
        gender = PetGender.Female;
        color = "";

        var relativeParts = Path.GetRelativePath(rootPath, directory).Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (relativeParts.Length != 4)
        {
            return false;
        }

        if (!AgeMap.TryGetValue(relativeParts[1], out age) ||
            !GenderMap.TryGetValue(relativeParts[2], out gender))
        {
            return false;
        }

        species = relativeParts[0];
        color = relativeParts[3];
        return true;
    }

    private static SpriteWorkflowFrameEntry? BuildFrameEntry(SpriteWorkflowRootKind rootKind, string rootPath, string absolutePath)
    {
        var fileName = Path.GetFileNameWithoutExtension(absolutePath);
        if (string.IsNullOrWhiteSpace(fileName) || !fileName.Contains('_', StringComparison.Ordinal))
        {
            return null;
        }

        var geometry = ReadGeometry(absolutePath);
        return new SpriteWorkflowFrameEntry(
            rootKind,
            fileName,
            Path.GetRelativePath(rootPath, absolutePath).Replace(Path.DirectorySeparatorChar, '/'),
            Path.GetFullPath(absolutePath),
            ComputeBlake3(absolutePath),
            geometry);
    }

    private static SpriteFrameGeometry ReadGeometry(string absolutePath)
    {
        using var bitmap = SKBitmap.Decode(absolutePath);
        return bitmap is null
            ? new SpriteFrameGeometry(0, 0)
            : new SpriteFrameGeometry(bitmap.Width, bitmap.Height);
    }

    private static string ComputeBlake3(string absolutePath)
    {
        var hash = Hasher.Hash(File.ReadAllBytes(absolutePath));
        return Convert.ToHexString(hash.AsSpan()).ToLowerInvariant();
    }

    private static string ParseFamily(string frameId)
    {
        var index = frameId.LastIndexOf('_');
        return index <= 0 ? frameId : frameId[..index];
    }

    public static string BuildRowId(SpriteRowKey key)
    {
        return $"{key.Species}/{FormatAge(key.AgeStage)}/{FormatGender(key.Gender)}/{key.ColorVariant}/{key.Family}";
    }

    private static SpriteRowKey ParseRowId(string rowId)
    {
        var parts = rowId.Split('/');
        return new SpriteRowKey(
            parts[0],
            AgeMap[parts[1]],
            GenderMap[parts[2]],
            parts[3],
            parts[4]);
    }

    private static IReadOnlyList<string> BuildFindings(IReadOnlyList<SpriteWorkflowRowEvidence> evidence)
    {
        var findings = new List<string>();
        var runtime = evidence.FirstOrDefault(item => item.RootKind == SpriteWorkflowRootKind.Runtime);
        if (runtime is null)
        {
            findings.Add("Missing runtime row.");
        }

        foreach (var group in evidence)
        {
            if (group.Frames.Count == 0)
            {
                findings.Add($"{group.RootKind}: no frames.");
                continue;
            }

            var sizes = group.Frames.Select(frame => frame.Geometry).Distinct().ToList();
            if (sizes.Count > 1)
            {
                findings.Add($"{group.RootKind}: mixed frame geometry.");
            }

            if (group.Frames.Any(frame => frame.Geometry.Width <= 0 || frame.Geometry.Height <= 0))
            {
                findings.Add($"{group.RootKind}: invalid PNG geometry.");
            }
        }

        return findings.Count == 0 ? ["No read-only findings."] : findings;
    }

    private static string FormatAge(PetAgeStage age)
    {
        return age.ToString().ToLowerInvariant();
    }

    private static string FormatGender(PetGender gender)
    {
        return gender.ToString().ToLowerInvariant();
    }

    private sealed record SpriteWorkflowRoot(SpriteWorkflowRootKind RootKind, string RootPath);
}
