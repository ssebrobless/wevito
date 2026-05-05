using System.Security.Cryptography;
using System.Text.Json;
using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed class LocalDocsPreviewAdapter
{
    private const string ToolFamily = "localDocs";
    private static readonly string[] DocumentPatterns = ["*.md", "*.txt", "*.json"];

    public TaskAdapterResult BuildPreview(TaskAdapterRequest request, DateTimeOffset? nowUtc = null)
    {
        var timestamp = nowUtc ?? DateTimeOffset.UtcNow;
        if (request.RunMode != TaskAdapterRunMode.DryRunPreview)
        {
            return Block(request, "Local docs adapter only supports dry-run preview right now.", timestamp);
        }

        if (!string.Equals(request.PolicySnapshot.ToolFamily, ToolFamily, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(request.Intent.RequestedToolFamily, ToolFamily, StringComparison.OrdinalIgnoreCase))
        {
            return Block(request, "Task intent and policy must both target localDocs.", timestamp);
        }

        if (request.PolicySnapshot.AccessMode != ToolAccessMode.ReadOnly)
        {
            return Block(request, "Local docs preview requires a read-only policy.", timestamp);
        }

        var approvedRoots = NormalizeExistingRoots(request.PolicySnapshot.ApprovedRootPaths);
        if (approvedRoots.Count == 0)
        {
            return Block(request, "Local docs preview requires at least one approved root path.", timestamp);
        }

        var targets = ResolveTargets(request.Intent.TargetPathsOrAssets, approvedRoots);
        if (targets.BlockReason is not null)
        {
            return Block(request, targets.BlockReason, timestamp);
        }

        var artifactRoot = ResolveArtifactRoot(request.ArtifactRoot, timestamp);
        if (!IsSafePetTaskArtifactRoot(artifactRoot))
        {
            return Block(request, "Local docs artifacts must be written under a pet-tasks artifact folder.", timestamp);
        }

        var documents = targets.Paths
            .SelectMany(path => EnumerateDocuments(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(path => IsInsideAnyRoot(path, approvedRoots))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .Take(12)
            .Select(path => BuildEntry(path, approvedRoots))
            .ToList();

        Directory.CreateDirectory(artifactRoot);
        var report = new LocalDocsPreviewReport(
            "1",
            request.TaskCardId,
            ToolFamily,
            approvedRoots,
            targets.Paths,
            documents.Count,
            documents,
            DidMutate: false,
            timestamp);
        var jsonPath = Path.Combine(artifactRoot, "local-docs-preview-report.json");
        var markdownPath = Path.Combine(artifactRoot, "run-summary.md");
        File.WriteAllText(jsonPath, JsonSerializer.Serialize(report, JsonDefaults.Options));
        File.WriteAllText(markdownPath, BuildMarkdown(report, request));

        var readPaths = documents.Select(document => document.Path).ToList();
        var previewSummary = documents.Count == 0
            ? $"Wrote localDocs markdown and JSON reports; no local docs were found under {targets.Paths.Count} approved target path(s)."
            : $"Wrote localDocs markdown and JSON reports for {documents.Count} local doc file(s). No source files were changed.";

        return new TaskAdapterResult(
            request.TaskCardId,
            ToolFamily,
            TaskAdapterResultStatus.PreviewReady,
            DidMutate: false,
            ReadPaths: readPaths,
            WrittenPaths: [jsonPath, markdownPath],
            PreviewSummary: previewSummary,
            ResultSummary: $"localDocs report ready: {markdownPath}",
            AuditLogPath: markdownPath,
            CompletedAtUtc: timestamp);
    }

    private static IReadOnlyList<string> NormalizeExistingRoots(IReadOnlyList<string>? roots)
    {
        return (roots ?? [])
            .Where(root => !string.IsNullOrWhiteSpace(root))
            .Select(Path.GetFullPath)
            .Where(path => Directory.Exists(path))
            .Select(EnsureTrailingSeparator)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static TargetResolution ResolveTargets(IReadOnlyList<string>? requestedTargets, IReadOnlyList<string> approvedRoots)
    {
        var rawTargets = requestedTargets?.Where(target => !string.IsNullOrWhiteSpace(target)).ToList() ?? [];
        if (rawTargets.Count == 0)
        {
            return new TargetResolution(approvedRoots, null);
        }

        var normalized = new List<string>();
        foreach (var target in rawTargets)
        {
            var fullPath = Path.GetFullPath(target);
            if (!IsInsideAnyRoot(fullPath, approvedRoots))
            {
                return new TargetResolution([], $"Target path is outside approved localDocs roots: {target}");
            }

            if (File.Exists(fullPath) || Directory.Exists(fullPath))
            {
                normalized.Add(fullPath);
            }
        }

        return new TargetResolution(normalized, null);
    }

    private static IEnumerable<string> EnumerateDocuments(string path)
    {
        if (File.Exists(path))
        {
            return IsSupportedDocument(path) ? [Path.GetFullPath(path)] : [];
        }

        if (!Directory.Exists(path))
        {
            return [];
        }

        return DocumentPatterns.SelectMany(pattern => Directory.EnumerateFiles(path, pattern, SearchOption.TopDirectoryOnly));
    }

    private static bool IsSupportedDocument(string path)
    {
        var extension = Path.GetExtension(path);
        return extension.Equals(".md", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".txt", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".json", StringComparison.OrdinalIgnoreCase);
    }

    private static LocalDocPreviewEntry BuildEntry(string path, IReadOnlyList<string> approvedRoots)
    {
        var info = new FileInfo(path);
        return new LocalDocPreviewEntry(
            path,
            BuildRelativePath(path, approvedRoots),
            ComputeSha256(path),
            Path.GetExtension(path).TrimStart('.').ToLowerInvariant(),
            info.Length,
            ReadPreviewLine(path));
    }

    private static string BuildMarkdown(LocalDocsPreviewReport report, TaskAdapterRequest request)
    {
        var lines = new List<string>
        {
            "# PET TASKS Local Docs Preview Report",
            "",
            $"Generated: {report.GeneratedAtUtc:O}",
            $"TaskCard: `{report.TaskCardId}`",
            $"Intent: {request.Intent.RawText}",
            "",
            "## Summary",
            "",
            $"- Documents found: {report.DocumentCount}",
            "- Did mutate files: false",
            "",
            "## Targets",
            ""
        };

        lines.AddRange(report.TargetPaths.Select(path => $"- `{path}`"));
        lines.Add("");
        lines.Add("## Documents");
        lines.Add("");

        if (report.Documents.Count == 0)
        {
            lines.Add("No supported local docs were found under the approved target paths.");
        }
        else
        {
            lines.AddRange(report.Documents.Select(document => $"- `{document.RelativePath}` ({document.Extension}, {document.ByteLength} bytes): {document.PreviewLine}"));
        }

        lines.Add("");
        lines.Add("## Safety");
        lines.Add("");
        lines.Add("This adapter only wrote markdown/JSON artifacts in a new pet-task folder. It did not edit docs, runtime PNGs, source boards, prop anchors, shared assets, or visual-side candidate/proof folders.");
        return string.Join(Environment.NewLine, lines) + Environment.NewLine;
    }

    private static string BuildRelativePath(string path, IReadOnlyList<string> approvedRoots)
    {
        var fullPath = Path.GetFullPath(path);
        var root = approvedRoots
            .OrderByDescending(candidate => candidate.Length)
            .FirstOrDefault(candidate => EnsureTrailingSeparator(fullPath).StartsWith(candidate, StringComparison.OrdinalIgnoreCase));
        return root is null
            ? fullPath
            : Path.GetRelativePath(root, fullPath);
    }

    private static string ComputeSha256(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }

    private static string ReadPreviewLine(string path)
    {
        try
        {
            return File.ReadLines(path)
                .Select(line => line.Trim())
                .FirstOrDefault(line => !string.IsNullOrWhiteSpace(line)) is { } preview
                ? Truncate(preview, 160)
                : "(empty)";
        }
        catch
        {
            return "(unreadable preview)";
        }
    }

    private static string Truncate(string value, int maxLength)
    {
        if (value.Length <= maxLength)
        {
            return value;
        }

        return value[..Math.Max(0, maxLength - 3)] + "...";
    }

    private static string ResolveArtifactRoot(string artifactRoot, DateTimeOffset timestamp)
    {
        if (!string.IsNullOrWhiteSpace(artifactRoot))
        {
            return Path.GetFullPath(artifactRoot);
        }

        var slug = timestamp.ToString("yyyyMMdd-HHmmss") + "-local-docs";
        return Path.GetFullPath(Path.Combine("vnext", "artifacts", "pet-tasks", slug));
    }

    private static bool IsSafePetTaskArtifactRoot(string artifactRoot)
    {
        var fullPath = Path.GetFullPath(artifactRoot);
        var parts = fullPath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return parts.Length >= 2 &&
               parts.Any(part => string.Equals(part, "pet-tasks", StringComparison.OrdinalIgnoreCase)) &&
               !parts.Any(part => part.StartsWith("candidate-frames", StringComparison.OrdinalIgnoreCase) ||
                                  part.StartsWith("backup-before-", StringComparison.OrdinalIgnoreCase) ||
                                  part.StartsWith("godot-packaged-proof-", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsInsideAnyRoot(string path, IReadOnlyList<string> approvedRoots)
    {
        var fullPath = File.Exists(path) || Directory.Exists(path)
            ? Path.GetFullPath(path)
            : Path.GetFullPath(path);
        return approvedRoots.Any(root => EnsureTrailingSeparator(fullPath).StartsWith(root, StringComparison.OrdinalIgnoreCase) ||
                                         string.Equals(fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), StringComparison.OrdinalIgnoreCase));
    }

    private static string EnsureTrailingSeparator(string path)
    {
        var fullPath = Path.GetFullPath(path);
        return fullPath.EndsWith(Path.DirectorySeparatorChar) || fullPath.EndsWith(Path.AltDirectorySeparatorChar)
            ? fullPath
            : fullPath + Path.DirectorySeparatorChar;
    }

    private static TaskAdapterResult Block(TaskAdapterRequest request, string reason, DateTimeOffset timestamp)
    {
        return new TaskAdapterResult(
            request.TaskCardId,
            ToolFamily,
            TaskAdapterResultStatus.Blocked,
            DidMutate: false,
            ReadPaths: [],
            WrittenPaths: [],
            BlockReason: reason,
            CompletedAtUtc: timestamp);
    }

    private sealed record TargetResolution(IReadOnlyList<string> Paths, string? BlockReason);
}
