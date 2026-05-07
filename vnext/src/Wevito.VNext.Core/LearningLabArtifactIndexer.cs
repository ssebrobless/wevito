using System.Text.Json;

namespace Wevito.VNext.Core;

public sealed record LearningLabArtifactIndexRequest(
    string RepoRoot,
    DateTimeOffset? IndexedAtUtc = null);

public sealed record LearningLabArtifactIndex(
    string SchemaVersion,
    string RepoRoot,
    DateTimeOffset IndexedAtUtc,
    LearningLabMetrics Metrics,
    IReadOnlyList<LearningLabArtifactRecord> Artifacts);

public sealed record LearningLabMetrics(
    int Raw,
    int Cleaned,
    int Labeled,
    int Bundled,
    int Eval,
    int MarkdownFiles,
    int JsonFiles);

public sealed record LearningLabArtifactRecord(
    string Id,
    string SourceRoot,
    string RelativePath,
    string AbsolutePath,
    string FileName,
    string Extension,
    string ArtifactKind,
    string Title,
    string Status,
    string Target,
    string ParseStatus,
    DateTimeOffset IndexedAtUtc);

public sealed class LearningLabArtifactIndexer
{
    private static readonly string[] ScanRoots =
    [
        Path.Combine("vnext", "artifacts", "visual-review"),
        Path.Combine("vnext", "artifacts", "animation-runs")
    ];

    public LearningLabArtifactIndex Index(LearningLabArtifactIndexRequest request)
    {
        var repoRoot = Path.GetFullPath(request.RepoRoot);
        var indexedAtUtc = request.IndexedAtUtc ?? DateTimeOffset.UtcNow;
        var artifacts = ScanRoots
            .SelectMany(root => ReadRoot(repoRoot, root, indexedAtUtc))
            .OrderByDescending(item => item.IndexedAtUtc)
            .ThenBy(item => item.RelativePath, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var metrics = new LearningLabMetrics(
            Raw: artifacts.Count,
            Cleaned: 0,
            Labeled: 0,
            Bundled: 0,
            Eval: 0,
            MarkdownFiles: artifacts.Count(item => item.Extension.Equals(".md", StringComparison.OrdinalIgnoreCase)),
            JsonFiles: artifacts.Count(item => item.Extension.Equals(".json", StringComparison.OrdinalIgnoreCase)));

        return new LearningLabArtifactIndex("1", repoRoot, indexedAtUtc, metrics, artifacts);
    }

    private static IReadOnlyList<LearningLabArtifactRecord> ReadRoot(string repoRoot, string relativeRoot, DateTimeOffset indexedAtUtc)
    {
        var root = Path.Combine(repoRoot, relativeRoot);
        if (!Directory.Exists(root))
        {
            return [];
        }

        return Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories)
            .Where(path => path.EndsWith(".md", StringComparison.OrdinalIgnoreCase) ||
                           path.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            .Select(path => ReadArtifact(repoRoot, root, relativeRoot, path, indexedAtUtc))
            .ToList();
    }

    private static LearningLabArtifactRecord ReadArtifact(
        string repoRoot,
        string absoluteScanRoot,
        string relativeScanRoot,
        string absolutePath,
        DateTimeOffset indexedAtUtc)
    {
        var extension = Path.GetExtension(absolutePath);
        var relativePath = Path.GetRelativePath(repoRoot, absolutePath).Replace(Path.DirectorySeparatorChar, '/');
        var kind = relativeScanRoot.Contains("animation-runs", StringComparison.OrdinalIgnoreCase)
            ? "optional-animation-candidate"
            : "visual-review";

        var metadata = extension.Equals(".json", StringComparison.OrdinalIgnoreCase)
            ? ReadJsonMetadata(absolutePath)
            : ReadMarkdownMetadata(absolutePath);

        var id = Path.GetRelativePath(absoluteScanRoot, absolutePath)
            .Replace(Path.DirectorySeparatorChar, '/')
            .Replace(Path.AltDirectorySeparatorChar, '/');

        return new LearningLabArtifactRecord(
            id,
            relativeScanRoot.Replace(Path.DirectorySeparatorChar, '/'),
            relativePath,
            Path.GetFullPath(absolutePath),
            Path.GetFileName(absolutePath),
            extension.ToLowerInvariant(),
            kind,
            metadata.Title,
            metadata.Status,
            metadata.Target,
            metadata.ParseStatus,
            indexedAtUtc);
    }

    private static ArtifactMetadata ReadMarkdownMetadata(string path)
    {
        try
        {
            var title = File.ReadLines(path)
                .Select(line => line.Trim())
                .FirstOrDefault(line => line.StartsWith("# ", StringComparison.Ordinal));
            return new ArtifactMetadata(
                string.IsNullOrWhiteSpace(title) ? Path.GetFileNameWithoutExtension(path) : title[2..].Trim(),
                "review-needed",
                "unknown",
                "markdown");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return new ArtifactMetadata(Path.GetFileNameWithoutExtension(path), "unreadable", "unknown", ex.GetType().Name);
        }
    }

    private static ArtifactMetadata ReadJsonMetadata(string path)
    {
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            var root = document.RootElement;
            var title = ReadString(root, "title") ??
                        ReadString(root, "name") ??
                        Path.GetFileNameWithoutExtension(path);
            var status = ReadString(root, "status") ?? "review-needed";
            var target = ReadTarget(root);
            return new ArtifactMetadata(title, status, target, "json");
        }
        catch (JsonException)
        {
            return new ArtifactMetadata(Path.GetFileNameWithoutExtension(path), "parse-error", "unknown", "invalid-json");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return new ArtifactMetadata(Path.GetFileNameWithoutExtension(path), "unreadable", "unknown", ex.GetType().Name);
        }
    }

    private static string? ReadString(JsonElement root, string propertyName)
    {
        return root.ValueKind == JsonValueKind.Object &&
               root.TryGetProperty(propertyName, out var value) &&
               value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }

    private static string ReadTarget(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object ||
            !root.TryGetProperty("target", out var target) ||
            target.ValueKind != JsonValueKind.Object)
        {
            return "unknown";
        }

        var parts = new[]
        {
            ReadString(target, "species"),
            ReadString(target, "age"),
            ReadString(target, "gender"),
            ReadString(target, "color"),
            ReadString(target, "family")
        }.Where(part => !string.IsNullOrWhiteSpace(part));

        var formatted = string.Join("/", parts);
        return string.IsNullOrWhiteSpace(formatted) ? "unknown" : formatted;
    }

    private sealed record ArtifactMetadata(
        string Title,
        string Status,
        string Target,
        string ParseStatus);
}
