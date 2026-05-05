using System.Text.Json;
using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed class CodePatchPlanPreviewAdapter
{
    private const string ToolFamily = "codePatchPlan";
    private const int MaxFiles = 40;
    private const long MaxFileBytes = 512 * 1024;
    private static readonly string[] SupportedExtensions = [".cs", ".xaml", ".gd", ".ps1", ".py", ".json"];
    private static readonly string[] SkippedDirectories = [".git", ".codex-cache", ".godot", ".vs", "bin", "obj", "artifacts", "node_modules", "sprites_runtime", "sprites_shared_runtime", "source_assets", "builds"];

    public TaskAdapterResult BuildPlan(TaskAdapterRequest request, DateTimeOffset? nowUtc = null)
    {
        var timestamp = nowUtc ?? DateTimeOffset.UtcNow;
        if (request.RunMode != TaskAdapterRunMode.DryRunPreview)
        {
            return Block(request, "Code patch planning only supports dry-run report mode right now.", timestamp);
        }

        if (!string.Equals(request.PolicySnapshot.ToolFamily, ToolFamily, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(request.Intent.RequestedToolFamily, ToolFamily, StringComparison.OrdinalIgnoreCase))
        {
            return Block(request, "Task intent and policy must both target codePatchPlan.", timestamp);
        }

        if (request.PolicySnapshot.AccessMode != ToolAccessMode.ReadOnly)
        {
            return Block(request, "Code patch planning requires a read-only policy.", timestamp);
        }

        var approvedRoots = NormalizeExistingRoots(request.PolicySnapshot.ApprovedRootPaths);
        if (approvedRoots.Count == 0)
        {
            return Block(request, "Code patch planning requires at least one approved root path.", timestamp);
        }

        var artifactRoot = ResolveArtifactRoot(request.ArtifactRoot, timestamp);
        if (!IsSafePetTaskArtifactRoot(artifactRoot))
        {
            return Block(request, "Code patch planning artifacts must be written under a pet-tasks artifact folder.", timestamp);
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
        var candidates = files.Select(path => BuildFileCandidate(path, approvedRoots)).ToList();
        var report = new CodePatchPlanReport(
            "1",
            request.TaskCardId,
            ToolFamily,
            targetPaths,
            candidates,
            request.Intent.RawText,
            BuildProposedScope(request.Intent.RawText, candidates),
            BuildSteps(request.Intent.RawText, candidates),
            BuildValidationPlan(candidates),
            BuildRollbackPlan(),
            BuildSafetyGates(),
            DidMutate: false,
            timestamp);

        Directory.CreateDirectory(artifactRoot);
        var jsonPath = Path.Combine(artifactRoot, "code-patch-plan-report.json");
        var markdownPath = Path.Combine(artifactRoot, "run-summary.md");
        File.WriteAllText(jsonPath, JsonSerializer.Serialize(report, JsonDefaults.Options));
        File.WriteAllText(markdownPath, BuildMarkdown(report));

        return new TaskAdapterResult(
            request.TaskCardId,
            ToolFamily,
            TaskAdapterResultStatus.PreviewReady,
            DidMutate: false,
            ReadPaths: files,
            WrittenPaths: [jsonPath, markdownPath],
            PreviewSummary: $"Wrote codePatchPlan markdown and JSON reports for {candidates.Count} candidate file(s). No files were changed.",
            ResultSummary: $"codePatchPlan report ready: {markdownPath}",
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

    private static CodePatchPlanFileCandidate BuildFileCandidate(string path, IReadOnlyList<string> approvedRoots)
    {
        var lines = File.ReadLines(path).Count();
        var info = new FileInfo(path);
        return new CodePatchPlanFileCandidate(
            path,
            BuildRelativePath(path, approvedRoots),
            ResolveLanguage(path),
            lines,
            info.Length);
    }

    private static string BuildProposedScope(string rawText, IReadOnlyList<CodePatchPlanFileCandidate> candidates)
    {
        var languageSummary = string.Join(", ", candidates
            .GroupBy(file => file.Language)
            .OrderBy(group => group.Key)
            .Select(group => $"{group.Count()} {group.Key}"));
        if (string.IsNullOrWhiteSpace(languageSummary))
        {
            languageSummary = "no matching code files yet";
        }

        return $"Prepare a reversible patch for \"{rawText}\" using the approved code/script roots only. Candidate surface: {languageSummary}.";
    }

    private static IReadOnlyList<CodePatchPlanStep> BuildSteps(string rawText, IReadOnlyList<CodePatchPlanFileCandidate> candidates)
    {
        var normalized = rawText.ToLowerInvariant();
        var steps = new List<CodePatchPlanStep>
        {
            new(1, "Confirm current behavior", "Use the report candidate files to inspect the smallest relevant code path before editing.", ToolRiskLevel.Low),
            new(2, "Choose narrow write scope", "Pick the minimum files required for the fix and avoid unrelated dirty work.", ToolRiskLevel.Low)
        };

        if (candidates.Any(file => file.Language is "C#" or "XAML"))
        {
            steps.Add(new CodePatchPlanStep(3, "Patch vNext code/UI", "Apply the smallest C#/XAML change and add or update focused tests for the behavior.", ToolRiskLevel.Medium));
        }

        if (candidates.Any(file => file.Language == "GDScript") || normalized.Contains("godot", StringComparison.OrdinalIgnoreCase))
        {
            steps.Add(new CodePatchPlanStep(steps.Count + 1, "Patch Godot behavior", "Keep Godot changes isolated and validate controls or runtime behavior with a targeted proof.", ToolRiskLevel.Medium));
        }

        if (candidates.Any(file => file.Language is "PowerShell" or "Python"))
        {
            steps.Add(new CodePatchPlanStep(steps.Count + 1, "Patch tooling", "Keep scripts dry-run/no-mutation by default and preserve explicit safety checks.", ToolRiskLevel.Medium));
        }

        steps.Add(new CodePatchPlanStep(steps.Count + 1, "Validate safely", "Run focused tests first, then broader tests/builds with -SkipAssetPrep while visual cleanup is active.", ToolRiskLevel.Low));
        steps.Add(new CodePatchPlanStep(steps.Count + 1, "Record outcome", "Write the result, artifacts, and residual risk to the implementation checklist or a phase report.", ToolRiskLevel.Low));
        return steps;
    }

    private static IReadOnlyList<string> BuildValidationPlan(IReadOnlyList<CodePatchPlanFileCandidate> candidates)
    {
        var validations = new List<string>();
        if (candidates.Any(file => file.Language is "C#" or "XAML"))
        {
            validations.Add("dotnet build .\\vnext\\Wevito.VNext.sln");
            validations.Add("dotnet test .\\vnext\\tests\\Wevito.VNext.Tests\\Wevito.VNext.Tests.csproj --no-build");
            validations.Add("powershell -NoProfile -ExecutionPolicy Bypass -File .\\tools\\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests");
        }

        if (candidates.Any(file => file.Language == "GDScript"))
        {
            validations.Add("Run a targeted Godot/package proof for the touched behavior before calling the patch done.");
        }

        if (candidates.Any(file => file.Language is "PowerShell" or "Python"))
        {
            validations.Add("Run touched scripts in dry-run/no-mutation mode where available.");
        }

        if (validations.Count == 0)
        {
            validations.Add("Identify a focused validation command before making edits.");
        }

        return validations;
    }

    private static IReadOnlyList<string> BuildRollbackPlan()
    {
        return
        [
            "Use git diff to review every touched code file before applying.",
            "Keep edits scoped so each file can be manually reverted without touching visual assets.",
            "If runtime proof fails, stop and revert only the patch files from this task.",
            "Do not use git reset --hard or checkout unrelated dirty files."
        ];
    }

    private static IReadOnlyList<string> BuildSafetyGates()
    {
        return
        [
            "No runtime/source PNG mutation.",
            "No visual generation or sprite import.",
            "No prop-anchor edits.",
            "No build asset preparation while visual cleanup is active; use -SkipAssetPrep.",
            "No destructive filesystem or git commands.",
            "Implementation requires explicit approval after this plan."
        ];
    }

    private static string BuildMarkdown(CodePatchPlanReport report)
    {
        var lines = new List<string>
        {
            "# PET TASKS Code Patch Plan",
            "",
            $"Generated: {report.GeneratedAtUtc:O}",
            $"TaskCard: `{report.TaskCardId}`",
            $"Requested change: {report.RequestedChange}",
            "",
            "## Summary",
            "",
            $"- Candidate files: {report.CandidateFiles.Count}",
            "- Did mutate code: false",
            $"- Scope: {report.ProposedScope}",
            "",
            "## Candidate Files",
            ""
        };

        if (report.CandidateFiles.Count == 0)
        {
            lines.Add("No candidate files were found in the approved roots.");
        }
        else
        {
            lines.AddRange(report.CandidateFiles.Take(30).Select(file => $"- `{file.RelativePath}`: {file.Language}, {file.LineCount} lines, {file.ByteLength} bytes"));
        }

        lines.Add("");
        lines.Add("## Proposed Steps");
        lines.Add("");
        lines.AddRange(report.Steps.Select(step => $"- {step.Order}. {step.Title}: {step.Detail} Risk: {step.RiskLevel}."));
        lines.Add("");
        lines.Add("## Validation Plan");
        lines.Add("");
        lines.AddRange(report.ValidationPlan.Select(item => $"- `{item}`"));
        lines.Add("");
        lines.Add("## Rollback Plan");
        lines.Add("");
        lines.AddRange(report.RollbackPlan.Select(item => $"- {item}"));
        lines.Add("");
        lines.Add("## Safety Gates");
        lines.Add("");
        lines.AddRange(report.SafetyGates.Select(item => $"- {item}"));
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
                return new TargetResolution([], $"Target path is outside approved codePatchPlan roots: {target}");
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
                return new TargetResolution([], $"Relative target path escapes approved codePatchPlan roots: {target}");
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

        var slug = timestamp.ToString("yyyyMMdd-HHmmss") + "-code-patch-plan";
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
