using System.Text.Json;
using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed class CodeReviewPreviewAdapter
{
    private const string ToolFamily = "codeReview";
    private const int MaxFiles = 80;
    private const long MaxFileBytes = 512 * 1024;
    private static readonly string[] SupportedExtensions = [".cs", ".xaml", ".gd", ".ps1", ".py", ".json"];
    private static readonly string[] SkippedDirectories = [".git", ".codex-cache", ".godot", ".vs", "bin", "obj", "artifacts", "node_modules", "sprites_runtime", "sprites_shared_runtime", "source_assets", "builds"];

    public TaskAdapterResult BuildReport(TaskAdapterRequest request, DateTimeOffset? nowUtc = null)
    {
        var timestamp = nowUtc ?? DateTimeOffset.UtcNow;
        if (request.RunMode != TaskAdapterRunMode.DryRunPreview)
        {
            return Block(request, "Code review only supports dry-run report mode right now.", timestamp);
        }

        if (!string.Equals(request.PolicySnapshot.ToolFamily, ToolFamily, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(request.Intent.RequestedToolFamily, ToolFamily, StringComparison.OrdinalIgnoreCase))
        {
            return Block(request, "Task intent and policy must both target codeReview.", timestamp);
        }

        if (request.PolicySnapshot.AccessMode != ToolAccessMode.ReadOnly)
        {
            return Block(request, "Code review requires a read-only policy.", timestamp);
        }

        var approvedRoots = NormalizeExistingRoots(request.PolicySnapshot.ApprovedRootPaths);
        if (approvedRoots.Count == 0)
        {
            return Block(request, "Code review requires at least one approved root path.", timestamp);
        }

        var artifactRoot = ResolveArtifactRoot(request.ArtifactRoot, timestamp);
        if (!IsSafePetTaskArtifactRoot(artifactRoot))
        {
            return Block(request, "Code review artifacts must be written under a pet-tasks artifact folder.", timestamp);
        }

        var targets = ResolveTargets(request.Intent.TargetPathsOrAssets, approvedRoots);
        if (targets.BlockReason is not null)
        {
            return Block(request, targets.BlockReason, timestamp);
        }

        var targetPaths = targets.Paths.Count > 0 ? targets.Paths : approvedRoots;
        var files = targetPaths
            .SelectMany(path => EnumerateCodeFiles(path, approvedRoots))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .Take(MaxFiles)
            .ToList();
        var summaries = files.Select(path => BuildFileSummary(path, approvedRoots)).ToList();
        var findings = files.SelectMany(path => BuildFindings(path, approvedRoots)).Take(200).ToList();
        var report = new CodeReviewReport(
            "1",
            request.TaskCardId,
            ToolFamily,
            targetPaths,
            summaries.Count,
            findings.Count,
            summaries,
            findings,
            BuildSuggestedTests(summaries),
            DidMutate: false,
            timestamp);

        Directory.CreateDirectory(artifactRoot);
        var jsonPath = Path.Combine(artifactRoot, "code-review-report.json");
        var markdownPath = Path.Combine(artifactRoot, "run-summary.md");
        File.WriteAllText(jsonPath, JsonSerializer.Serialize(report, JsonDefaults.Options));
        File.WriteAllText(markdownPath, BuildMarkdown(report, request));

        return new TaskAdapterResult(
            request.TaskCardId,
            ToolFamily,
            TaskAdapterResultStatus.PreviewReady,
            DidMutate: false,
            ReadPaths: files,
            WrittenPaths: [jsonPath, markdownPath],
            PreviewSummary: $"Wrote codeReview markdown and JSON reports for {summaries.Count} file(s). No code files were changed.",
            ResultSummary: $"codeReview report ready: {markdownPath}",
            AuditLogPath: markdownPath,
            CompletedAtUtc: timestamp);
    }

    private static IEnumerable<string> EnumerateCodeFiles(string path, IReadOnlyList<string> approvedRoots)
    {
        if (File.Exists(path))
        {
            return IsSupportedCodeFile(path) && IsInsideAnyRoot(path, approvedRoots)
                ? [Path.GetFullPath(path)]
                : [];
        }

        if (!Directory.Exists(path))
        {
            return [];
        }

        return Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories)
            .Where(IsSupportedCodeFile)
            .Where(file => !IsInSkippedDirectory(file, path))
            .Where(file => new FileInfo(file).Length <= MaxFileBytes)
            .Where(file => IsInsideAnyRoot(file, approvedRoots));
    }

    private static CodeReviewFileSummary BuildFileSummary(string path, IReadOnlyList<string> approvedRoots)
    {
        var lines = File.ReadLines(path).Count();
        var info = new FileInfo(path);
        return new CodeReviewFileSummary(
            path,
            BuildRelativePath(path, approvedRoots),
            ResolveLanguage(path),
            lines,
            info.Length);
    }

    private static IEnumerable<CodeReviewFinding> BuildFindings(string path, IReadOnlyList<string> approvedRoots)
    {
        var relative = BuildRelativePath(path, approvedRoots);
        var lineNumber = 0;
        foreach (var line in File.ReadLines(path))
        {
            lineNumber++;
            if (line.Contains("<<<<<<<", StringComparison.Ordinal) ||
                line.Contains("=======", StringComparison.Ordinal) ||
                line.Contains(">>>>>>>", StringComparison.Ordinal))
            {
                yield return new CodeReviewFinding("merge_conflict_marker", relative, lineNumber, "Potential unresolved merge conflict marker.");
            }

            if (line.Contains("TODO", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("FIXME", StringComparison.OrdinalIgnoreCase))
            {
                yield return new CodeReviewFinding("todo_or_fixme", relative, lineNumber, "TODO/FIXME marker found.");
            }

            if (line.Length > 180)
            {
                yield return new CodeReviewFinding("long_line", relative, lineNumber, $"Line is {line.Length} characters.");
            }

            if (line.EndsWith(' ') || line.EndsWith('\t'))
            {
                yield return new CodeReviewFinding("trailing_whitespace", relative, lineNumber, "Line ends with whitespace.");
            }

            if (Path.GetExtension(path).Equals(".ps1", StringComparison.OrdinalIgnoreCase) &&
                line.Contains("Remove-Item", StringComparison.OrdinalIgnoreCase) &&
                line.Contains("-Recurse", StringComparison.OrdinalIgnoreCase))
            {
                yield return new CodeReviewFinding("destructive_recursive_command", relative, lineNumber, "PowerShell recursive delete requires careful path checks.");
            }
        }
    }

    private static IReadOnlyList<string> BuildSuggestedTests(IReadOnlyList<CodeReviewFileSummary> summaries)
    {
        var suggestions = new List<string>();
        if (summaries.Any(file => file.Language == "C#"))
        {
            suggestions.Add("dotnet test .\\vnext\\tests\\Wevito.VNext.Tests\\Wevito.VNext.Tests.csproj --no-build");
        }

        if (summaries.Any(file => file.Language is "PowerShell" or "GDScript"))
        {
            suggestions.Add("Run the relevant script/probe manually in dry-run or no-mutation mode.");
        }

        if (suggestions.Count == 0)
        {
            suggestions.Add("Review the generated report and choose a targeted validation command before editing.");
        }

        return suggestions;
    }

    private static string BuildMarkdown(CodeReviewReport report, TaskAdapterRequest request)
    {
        var lines = new List<string>
        {
            "# PET TASKS Code Review Report",
            "",
            $"Generated: {report.GeneratedAtUtc:O}",
            $"TaskCard: `{report.TaskCardId}`",
            $"Intent: {request.Intent.RawText}",
            "",
            "## Summary",
            "",
            $"- Files scanned: {report.FilesScanned}",
            $"- Findings: {report.FindingCount}",
            "- Did mutate code: false",
            "",
            "## Files",
            ""
        };

        lines.AddRange(report.Files.Take(30).Select(file => $"- `{file.RelativePath}`: {file.Language}, {file.LineCount} lines, {file.ByteLength} bytes"));
        lines.Add("");
        lines.Add("## Findings");
        lines.Add("");
        if (report.Findings.Count == 0)
        {
            lines.Add("No simple static review findings were detected.");
        }
        else
        {
            lines.AddRange(report.Findings.Take(40).Select(finding => $"- `{finding.Path}` line {finding.Line}: {finding.Kind} - {finding.Detail}"));
        }

        lines.Add("");
        lines.Add("## Suggested Tests");
        lines.Add("");
        lines.AddRange(report.SuggestedTests.Select(test => $"- `{test}`"));
        lines.Add("");
        lines.Add("## Safety");
        lines.Add("");
        lines.Add("This adapter only wrote markdown/JSON artifacts in a new pet-task folder. It did not edit code, run commands, mutate assets, or touch visual-side candidate/proof folders.");
        return string.Join(Environment.NewLine, lines) + Environment.NewLine;
    }

    private static bool IsSupportedCodeFile(string path)
    {
        return SupportedExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);
    }

    private static bool IsInSkippedDirectory(string file, string root)
    {
        var relative = Path.GetRelativePath(root, file);
        var parts = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return parts.Any(part => SkippedDirectories.Contains(part, StringComparer.OrdinalIgnoreCase));
    }

    private static string ResolveLanguage(string path)
    {
        return Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".cs" => "C#",
            ".xaml" => "XAML",
            ".gd" => "GDScript",
            ".ps1" => "PowerShell",
            ".py" => "Python",
            ".json" => "JSON",
            _ => "Text"
        };
    }

    private static IReadOnlyList<string> NormalizeExistingRoots(IReadOnlyList<string>? roots)
    {
        return (roots ?? [])
            .Where(root => !string.IsNullOrWhiteSpace(root))
            .Select(Path.GetFullPath)
            .Where(path => Directory.Exists(path) || File.Exists(path))
            .Select(path => Directory.Exists(path) ? EnsureTrailingSeparator(path) : path)
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
                return new TargetResolution([], $"Target path is outside approved codeReview roots: {target}");
            }

            return File.Exists(fullPath) || Directory.Exists(fullPath)
                ? new TargetResolution([Directory.Exists(fullPath) ? EnsureTrailingSeparator(fullPath) : fullPath], null)
                : new TargetResolution([], null);
        }

        var paths = new List<string>();
        foreach (var root in approvedRoots.Where(Directory.Exists))
        {
            var candidate = Path.GetFullPath(Path.Combine(root, target));
            if (!IsInsideAnyRoot(candidate, approvedRoots))
            {
                return new TargetResolution([], $"Relative target path escapes approved codeReview roots: {target}");
            }

            if (File.Exists(candidate) || Directory.Exists(candidate))
            {
                paths.Add(Directory.Exists(candidate) ? EnsureTrailingSeparator(candidate) : candidate);
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

        var slug = timestamp.ToString("yyyyMMdd-HHmmss") + "-code-review";
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
        return approvedRoots.Any(root =>
        {
            var normalizedRoot = Directory.Exists(root) ? EnsureTrailingSeparator(root) : Path.GetFullPath(root);
            return fullPath.Equals(normalizedRoot, StringComparison.OrdinalIgnoreCase) ||
                   fullPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase);
        });
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
