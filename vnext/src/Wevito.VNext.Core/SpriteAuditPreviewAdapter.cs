using System.Security.Cryptography;
using System.Text.Json;
using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed class SpriteAuditPreviewAdapter
{
    private const string ToolFamily = "spriteAudit";

    public TaskAdapterResult BuildReport(TaskAdapterRequest request, DateTimeOffset? nowUtc = null)
    {
        var timestamp = nowUtc ?? DateTimeOffset.UtcNow;
        if (request.RunMode != TaskAdapterRunMode.DryRunPreview)
        {
            return Block(request, "Sprite audit adapter only supports dry-run report mode right now.", timestamp);
        }

        if (!string.Equals(request.PolicySnapshot.ToolFamily, ToolFamily, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(request.Intent.RequestedToolFamily, ToolFamily, StringComparison.OrdinalIgnoreCase))
        {
            return Block(request, "Task intent and policy must both target spriteAudit.", timestamp);
        }

        if (request.PolicySnapshot.AccessMode != ToolAccessMode.ReadOnly)
        {
            return Block(request, "Sprite audit requires a read-only policy.", timestamp);
        }

        var approvedRoots = NormalizeExistingRoots(request.PolicySnapshot.ApprovedRootPaths);
        if (approvedRoots.Count == 0)
        {
            return Block(request, "Sprite audit requires at least one approved root path.", timestamp);
        }

        var artifactRoot = ResolveArtifactRoot(request.ArtifactRoot, timestamp);
        if (!IsSafePetTaskArtifactRoot(artifactRoot))
        {
            return Block(request, "Sprite audit artifacts must be written under a pet-tasks artifact folder.", timestamp);
        }

        var targets = ResolveTargets(request.Intent.TargetPathsOrAssets, approvedRoots);
        if (targets.BlockReason is not null)
        {
            return Block(request, targets.BlockReason, timestamp);
        }

        var frames = targets.Paths
            .SelectMany(EnumeratePngs)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(path => IsInsideAnyRoot(path, approvedRoots))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .Take(300)
            .Select(path => BuildFinding(path, approvedRoots))
            .ToList();

        Directory.CreateDirectory(artifactRoot);
        Directory.CreateDirectory(Path.Combine(artifactRoot, "qa"));
        var report = new SpriteAuditReport(
            "1",
            request.TaskCardId,
            ToolFamily,
            approvedRoots[0],
            targets.Paths,
            frames.Count,
            frames.Count(frame => frame.IssueKinds?.Count > 0),
            frames,
            DidMutate: false,
            timestamp);
        var jsonPath = Path.Combine(artifactRoot, "sprite-audit-report.json");
        var markdownPath = Path.Combine(artifactRoot, "run-summary.md");
        File.WriteAllText(jsonPath, JsonSerializer.Serialize(report, JsonDefaults.Options));
        File.WriteAllText(markdownPath, BuildMarkdown(report, request));

        return new TaskAdapterResult(
            request.TaskCardId,
            ToolFamily,
            TaskAdapterResultStatus.PreviewReady,
            DidMutate: false,
            ReadPaths: frames.Select(frame => frame.Path).ToList(),
            WrittenPaths: [jsonPath, markdownPath],
            PreviewSummary: $"Wrote spriteAudit markdown and JSON reports for {frames.Count} PNG frame(s). No sprite files were changed.",
            ResultSummary: $"spriteAudit report ready: {markdownPath}",
            AuditLogPath: markdownPath,
            CompletedAtUtc: timestamp);
    }

    private static SpriteAuditFrameFinding BuildFinding(string path, IReadOnlyList<string> approvedRoots)
    {
        var (width, height, issueKinds) = TryReadPngHeader(path);
        if (width <= 0 || height <= 0)
        {
            issueKinds.Add("invalid_png_header");
        }

        if (width <= 16 || height <= 16)
        {
            issueKinds.Add("tiny_frame");
        }

        if (width >= 256 || height >= 256)
        {
            issueKinds.Add("large_frame");
        }

        var info = new FileInfo(path);
        return new SpriteAuditFrameFinding(
            path,
            BuildRelativePath(path, approvedRoots),
            ComputeSha256(path),
            width,
            height,
            info.Length,
            issueKinds);
    }

    private static string BuildMarkdown(SpriteAuditReport report, TaskAdapterRequest request)
    {
        var flagged = report.Findings.Where(frame => frame.IssueKinds?.Count > 0).Take(20).ToList();
        var lines = new List<string>
        {
            "# PET TASKS Sprite Audit Report",
            "",
            $"Generated: {report.GeneratedAtUtc:O}",
            $"TaskCard: `{report.TaskCardId}`",
            $"Intent: {request.Intent.RawText}",
            "",
            "## Summary",
            "",
            $"- PNG frames scanned: {report.PngCount}",
            $"- Findings: {report.FindingCount}",
            "- Did mutate sprites: false",
            "- Contact sheet: not generated by this Core report adapter yet",
            "",
            "## Targets",
            ""
        };

        lines.AddRange(report.TargetPaths.Select(path => $"- `{path}`"));
        lines.Add("");
        lines.Add("## Findings");
        lines.Add("");

        if (flagged.Count == 0)
        {
            lines.Add("No structural header findings were detected in the sampled PNG frames.");
        }
        else
        {
            lines.AddRange(flagged.Select(frame => $"- `{frame.RelativePath}`: {string.Join(", ", frame.IssueKinds ?? [])} ({frame.Width}x{frame.Height})"));
        }

        lines.Add("");
        lines.Add("## Safety");
        lines.Add("");
        lines.Add("This adapter only wrote markdown/JSON artifacts in a new pet-task folder. It did not edit runtime PNGs, source boards, prop anchors, shared assets, or visual-side candidate/proof folders.");
        return string.Join(Environment.NewLine, lines) + Environment.NewLine;
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

    private static string ResolveArtifactRoot(string artifactRoot, DateTimeOffset timestamp)
    {
        if (!string.IsNullOrWhiteSpace(artifactRoot))
        {
            return Path.GetFullPath(artifactRoot);
        }

        var slug = timestamp.ToString("yyyyMMdd-HHmmss") + "-sprite-audit";
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
                return new TargetResolution([], $"Target path is outside approved spriteAudit roots: {target}");
            }

            return File.Exists(fullPath) || Directory.Exists(fullPath)
                ? new TargetResolution([fullPath], null)
                : new TargetResolution([], null);
        }

        var paths = new List<string>();
        foreach (var root in approvedRoots)
        {
            var candidate = Path.GetFullPath(Path.Combine(root, target));
            if (!IsInsideAnyRoot(candidate, approvedRoots))
            {
                return new TargetResolution([], $"Relative target path escapes approved spriteAudit roots: {target}");
            }

            if (File.Exists(candidate) || Directory.Exists(candidate))
            {
                paths.Add(candidate);
            }
        }

        return new TargetResolution(paths, null);
    }

    private static IEnumerable<string> EnumeratePngs(string path)
    {
        if (File.Exists(path))
        {
            return Path.GetExtension(path).Equals(".png", StringComparison.OrdinalIgnoreCase)
                ? [Path.GetFullPath(path)]
                : [];
        }

        if (!Directory.Exists(path))
        {
            return [];
        }

        return Directory.EnumerateFiles(path, "*.png", SearchOption.TopDirectoryOnly);
    }

    private static (int Width, int Height, List<string> IssueKinds) TryReadPngHeader(string path)
    {
        var issueKinds = new List<string>();
        try
        {
            using var stream = File.OpenRead(path);
            Span<byte> header = stackalloc byte[24];
            var read = stream.Read(header);
            if (read < 24 ||
                header[0] != 0x89 ||
                header[1] != 0x50 ||
                header[2] != 0x4E ||
                header[3] != 0x47)
            {
                issueKinds.Add("invalid_png_signature");
                return (0, 0, issueKinds);
            }

            var width = ReadBigEndianInt32(header[16..20]);
            var height = ReadBigEndianInt32(header[20..24]);
            return (width, height, issueKinds);
        }
        catch
        {
            issueKinds.Add("unreadable_png");
            return (0, 0, issueKinds);
        }
    }

    private static int ReadBigEndianInt32(ReadOnlySpan<byte> bytes)
    {
        return (bytes[0] << 24) |
               (bytes[1] << 16) |
               (bytes[2] << 8) |
               bytes[3];
    }

    private static string ComputeSha256(string path)
    {
        using var stream = File.OpenRead(path);
        var hash = SHA256.HashData(stream);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string BuildRelativePath(string path, IReadOnlyList<string> approvedRoots)
    {
        var root = approvedRoots.FirstOrDefault(candidate => IsInsideAnyRoot(path, [candidate]));
        return root is null
            ? Path.GetFileName(path)
            : Path.GetRelativePath(root, path);
    }

    private static bool IsInsideAnyRoot(string path, IReadOnlyList<string> approvedRoots)
    {
        var fullPath = Path.GetFullPath(path);
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
