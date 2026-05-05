using System.Text.Json;
using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed class AssetInventoryPreviewAdapter
{
    private const string ToolFamily = "assetInventory";

    public TaskAdapterResult BuildReport(TaskAdapterRequest request, DateTimeOffset? nowUtc = null)
    {
        var timestamp = nowUtc ?? DateTimeOffset.UtcNow;
        if (request.RunMode != TaskAdapterRunMode.DryRunPreview)
        {
            return Block(request, "Asset inventory only supports dry-run report mode right now.", timestamp);
        }

        if (!string.Equals(request.PolicySnapshot.ToolFamily, ToolFamily, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(request.Intent.RequestedToolFamily, ToolFamily, StringComparison.OrdinalIgnoreCase))
        {
            return Block(request, "Task intent and policy must both target assetInventory.", timestamp);
        }

        if (request.PolicySnapshot.AccessMode != ToolAccessMode.ReadOnly)
        {
            return Block(request, "Asset inventory requires a read-only policy.", timestamp);
        }

        var approvedRoots = NormalizeExistingRoots(request.PolicySnapshot.ApprovedRootPaths);
        if (approvedRoots.Count == 0)
        {
            return Block(request, "Asset inventory requires at least one approved root path.", timestamp);
        }

        var artifactRoot = ResolveArtifactRoot(request.ArtifactRoot, timestamp);
        if (!IsSafePetTaskArtifactRoot(artifactRoot))
        {
            return Block(request, "Asset inventory artifacts must be written under a pet-tasks artifact folder.", timestamp);
        }

        var targets = ResolveTargets(request.Intent.TargetPathsOrAssets, approvedRoots);
        if (targets.BlockReason is not null)
        {
            return Block(request, targets.BlockReason, timestamp);
        }

        var roots = targets.Paths.Count > 0 ? targets.Paths : approvedRoots;
        var summaries = roots
            .Where(Directory.Exists)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(BuildRootSummary)
            .ToList();
        var findings = roots
            .Where(Directory.Exists)
            .SelectMany(BuildFindings)
            .Take(100)
            .ToList();

        var runtimeRoots = summaries.Where(summary => summary.RootName.Equals("sprites_runtime", StringComparison.OrdinalIgnoreCase)).ToList();
        var sharedRoots = summaries.Where(summary => summary.RootName.Equals("sprites_shared_runtime", StringComparison.OrdinalIgnoreCase)).ToList();
        var runtimeVariantFolders = roots
            .Where(root => Path.GetFileName(root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)).Equals("sprites_runtime", StringComparison.OrdinalIgnoreCase))
            .Sum(CountRuntimeVariantFolders);
        var report = new AssetInventoryReport(
            "1",
            request.TaskCardId,
            ToolFamily,
            summaries,
            runtimeVariantFolders,
            runtimeRoots.Sum(root => root.PngCount),
            sharedRoots.Sum(root => root.PngCount),
            findings.Count,
            findings,
            DidMutate: false,
            timestamp);

        Directory.CreateDirectory(artifactRoot);
        var jsonPath = Path.Combine(artifactRoot, "asset-inventory-report.json");
        var markdownPath = Path.Combine(artifactRoot, "run-summary.md");
        File.WriteAllText(jsonPath, JsonSerializer.Serialize(report, JsonDefaults.Options));
        File.WriteAllText(markdownPath, BuildMarkdown(report, request));

        return new TaskAdapterResult(
            request.TaskCardId,
            ToolFamily,
            TaskAdapterResultStatus.PreviewReady,
            DidMutate: false,
            ReadPaths: roots,
            WrittenPaths: [jsonPath, markdownPath],
            PreviewSummary: $"Wrote assetInventory markdown and JSON reports for {summaries.Count} root(s). No asset files were changed.",
            ResultSummary: $"assetInventory report ready: {markdownPath}",
            AuditLogPath: markdownPath,
            CompletedAtUtc: timestamp);
    }

    private static AssetInventoryRootSummary BuildRootSummary(string root)
    {
        var files = Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories).ToList();
        var directories = Directory.EnumerateDirectories(root, "*", SearchOption.AllDirectories).ToList();
        return new AssetInventoryRootSummary(
            root,
            Path.GetFileName(root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)),
            directories.Count,
            files.Count,
            files.Count(path => Path.GetExtension(path).Equals(".png", StringComparison.OrdinalIgnoreCase)),
            files.Count(path => path.EndsWith(".png.import", StringComparison.OrdinalIgnoreCase)),
            files.Count(path => Path.GetExtension(path).Equals(".json", StringComparison.OrdinalIgnoreCase)),
            files.Sum(path => new FileInfo(path).Length));
    }

    private static IEnumerable<AssetInventoryFinding> BuildFindings(string root)
    {
        foreach (var importPath in Directory.EnumerateFiles(root, "*.png.import", SearchOption.AllDirectories))
        {
            var pngPath = importPath[..^".import".Length];
            if (!File.Exists(pngPath))
            {
                yield return new AssetInventoryFinding(
                    "orphan_png_import",
                    importPath,
                    "Import sidecar exists without the matching PNG.");
            }
        }

        foreach (var directory in Directory.EnumerateDirectories(root, "*", SearchOption.AllDirectories))
        {
            if (!Directory.EnumerateFileSystemEntries(directory).Any())
            {
                yield return new AssetInventoryFinding(
                    "empty_directory",
                    directory,
                    "Directory is empty and may be stale.");
            }
        }
    }

    private static int CountRuntimeVariantFolders(string runtimeRoot)
    {
        var root = Path.GetFullPath(runtimeRoot);
        return Directory.EnumerateDirectories(root, "*", SearchOption.AllDirectories)
            .Count(directory =>
            {
                var relative = Path.GetRelativePath(root, directory);
                var parts = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                return parts.Length == 4 &&
                       Directory.EnumerateFiles(directory, "*.png", SearchOption.TopDirectoryOnly).Any();
            });
    }

    private static string BuildMarkdown(AssetInventoryReport report, TaskAdapterRequest request)
    {
        var lines = new List<string>
        {
            "# PET TASKS Asset Inventory Report",
            "",
            $"Generated: {report.GeneratedAtUtc:O}",
            $"TaskCard: `{report.TaskCardId}`",
            $"Intent: {request.Intent.RawText}",
            "",
            "## Summary",
            "",
            $"- Roots scanned: {report.Roots.Count}",
            $"- Runtime variant folders: {report.RuntimeVariantFolderCount}",
            $"- Runtime PNGs: {report.RuntimePngCount}",
            $"- Shared PNGs: {report.SharedPngCount}",
            $"- Findings: {report.FindingCount}",
            "- Did mutate assets: false",
            "",
            "## Roots",
            ""
        };

        lines.AddRange(report.Roots.Select(root => $"- `{root.RootPath}`: {root.PngCount} PNG, {root.ImportSidecarCount} import sidecar, {root.JsonCount} JSON, {root.DirectoryCount} dirs"));
        lines.Add("");
        lines.Add("## Findings");
        lines.Add("");
        if (report.Findings.Count == 0)
        {
            lines.Add("No stale sidecar or empty-directory findings were detected.");
        }
        else
        {
            lines.AddRange(report.Findings.Take(30).Select(finding => $"- `{finding.Kind}`: `{finding.Path}` - {finding.Detail}"));
        }

        lines.Add("");
        lines.Add("## Safety");
        lines.Add("");
        lines.Add("This adapter only wrote markdown/JSON artifacts in a new pet-task folder. It did not edit sprites, shared assets, source boards, prop anchors, generated files, or visual-side candidate/proof folders.");
        return string.Join(Environment.NewLine, lines) + Environment.NewLine;
    }

    private static IReadOnlyList<string> NormalizeExistingRoots(IReadOnlyList<string>? roots)
    {
        return (roots ?? [])
            .Where(root => !string.IsNullOrWhiteSpace(root))
            .Select(Path.GetFullPath)
            .Where(Directory.Exists)
            .Select(EnsureTrailingSeparator)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static TargetResolution ResolveTargets(IReadOnlyList<string>? requestedTargets, IReadOnlyList<string> approvedRoots)
    {
        var rawTargets = requestedTargets?.Where(target => !string.IsNullOrWhiteSpace(target)).ToList() ?? [];
        if (rawTargets.Count == 0)
        {
            return new TargetResolution([], null);
        }

        var normalized = new List<string>();
        foreach (var target in rawTargets)
        {
            var candidates = ResolveTargetCandidates(target, approvedRoots);
            if (candidates.BlockReason is not null)
            {
                return new TargetResolution([], candidates.BlockReason);
            }

            normalized.AddRange(candidates.Paths);
        }

        return new TargetResolution(normalized, null);
    }

    private static TargetResolution ResolveTargetCandidates(string target, IReadOnlyList<string> approvedRoots)
    {
        if (Path.IsPathFullyQualified(target))
        {
            var fullPath = Path.GetFullPath(target);
            if (!IsInsideAnyRoot(fullPath, approvedRoots))
            {
                return new TargetResolution([], $"Target path is outside approved assetInventory roots: {target}");
            }

            return Directory.Exists(fullPath)
                ? new TargetResolution([EnsureTrailingSeparator(fullPath)], null)
                : new TargetResolution([], null);
        }

        var paths = new List<string>();
        foreach (var root in approvedRoots)
        {
            var candidate = Path.GetFullPath(Path.Combine(root, target));
            if (!IsInsideAnyRoot(candidate, approvedRoots))
            {
                return new TargetResolution([], $"Relative target path escapes approved assetInventory roots: {target}");
            }

            if (Directory.Exists(candidate))
            {
                paths.Add(EnsureTrailingSeparator(candidate));
            }
        }

        return new TargetResolution(paths, null);
    }

    private static string ResolveArtifactRoot(string artifactRoot, DateTimeOffset timestamp)
    {
        if (!string.IsNullOrWhiteSpace(artifactRoot))
        {
            return Path.GetFullPath(artifactRoot);
        }

        var slug = timestamp.ToString("yyyyMMdd-HHmmss") + "-asset-inventory";
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
        var fullPath = Path.GetFullPath(path);
        return approvedRoots.Any(root => fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase));
    }

    private static string EnsureTrailingSeparator(string path)
    {
        var fullPath = Path.GetFullPath(path);
        return fullPath.EndsWith(Path.DirectorySeparatorChar)
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

    private sealed record TargetResolution(
        IReadOnlyList<string> Paths,
        string? BlockReason);
}
