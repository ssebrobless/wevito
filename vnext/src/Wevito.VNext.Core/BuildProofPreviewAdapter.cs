using System.Text.Json;
using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed class BuildProofPreviewAdapter
{
    private const string ToolFamily = "buildProof";

    public TaskAdapterResult BuildPlan(TaskAdapterRequest request, DateTimeOffset? nowUtc = null)
    {
        var timestamp = nowUtc ?? DateTimeOffset.UtcNow;
        if (request.RunMode != TaskAdapterRunMode.DryRunPreview)
        {
            return Block(request, "Build proof planning only supports dry-run report mode right now.", timestamp);
        }

        if (!string.Equals(request.PolicySnapshot.ToolFamily, ToolFamily, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(request.Intent.RequestedToolFamily, ToolFamily, StringComparison.OrdinalIgnoreCase))
        {
            return Block(request, "Task intent and policy must both target buildProof.", timestamp);
        }

        if (request.PolicySnapshot.AccessMode != ToolAccessMode.ReadOnly)
        {
            return Block(request, "Build proof planning requires a read-only preview policy.", timestamp);
        }

        var artifactRoot = ResolveArtifactRoot(request.ArtifactRoot, timestamp);
        if (!IsSafePetTaskArtifactRoot(artifactRoot))
        {
            return Block(request, "Build proof planning artifacts must be written under a pet-tasks artifact folder.", timestamp);
        }

        var readPaths = ResolveExistingProofInputs(request.PolicySnapshot.ApprovedRootPaths);
        var report = new BuildProofPlanReport(
            "1",
            request.TaskCardId,
            ToolFamily,
            BuildCommands(),
            BuildExpectedArtifacts(),
            BuildSafetyGates(),
            BuildStopConditions(),
            DidRunCommands: false,
            DidMutate: false,
            timestamp);

        Directory.CreateDirectory(artifactRoot);
        var jsonPath = Path.Combine(artifactRoot, "build-proof-plan-report.json");
        var markdownPath = Path.Combine(artifactRoot, "run-summary.md");
        File.WriteAllText(jsonPath, JsonSerializer.Serialize(report, JsonDefaults.Options));
        File.WriteAllText(markdownPath, BuildMarkdown(report, request));

        return new TaskAdapterResult(
            request.TaskCardId,
            ToolFamily,
            TaskAdapterResultStatus.PreviewReady,
            DidMutate: false,
            ReadPaths: readPaths,
            WrittenPaths: [jsonPath, markdownPath],
            PreviewSummary: "Wrote buildProof command plan. No build commands were run.",
            ResultSummary: $"buildProof plan ready: {markdownPath}",
            AuditLogPath: markdownPath,
            CompletedAtUtc: timestamp);
    }

    private static IReadOnlyList<BuildProofCommandPlan> BuildCommands()
    {
        return
        [
            new BuildProofCommandPlan(
                1,
                "dotnet build .\\vnext\\Wevito.VNext.sln",
                "Compile contracts, core, tests, broker, and shell before proofing.",
                ToolRiskLevel.Low,
                RequiresApproval: true,
                MustSkipAssetPrep: false),
            new BuildProofCommandPlan(
                2,
                "dotnet test .\\vnext\\tests\\Wevito.VNext.Tests\\Wevito.VNext.Tests.csproj --no-build",
                "Run deterministic vNext tests against the compiled output.",
                ToolRiskLevel.Low,
                RequiresApproval: true,
                MustSkipAssetPrep: false),
            new BuildProofCommandPlan(
                3,
                "powershell -NoProfile -ExecutionPolicy Bypass -File .\\tools\\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests",
                "Publish the Debug vNext shell/broker without regenerating runtime sprites.",
                ToolRiskLevel.Medium,
                RequiresApproval: true,
                MustSkipAssetPrep: true),
            new BuildProofCommandPlan(
                4,
                "powershell -NoProfile -ExecutionPolicy Bypass -File .\\tools\\probe-vnext-pet-tasks.ps1 -TaskText \"review pet state\" -ExpectedToolFamily petState -SkipBuild",
                "Optional live UI smoke proof after publish, still no asset mutation.",
                ToolRiskLevel.Medium,
                RequiresApproval: true,
                MustSkipAssetPrep: false)
        ];
    }

    private static IReadOnlyList<string> BuildExpectedArtifacts()
    {
        return
        [
            "Console output for build/test/publish commands.",
            "vnext\\artifacts\\shell\\Wevito.VNext.Shell.exe after safe publish.",
            "vnext\\artifacts\\pet-task-probes\\<timestamp>\\summary.json for optional live probe.",
            "No runtime/source PNG changes."
        ];
    }

    private static IReadOnlyList<string> BuildSafetyGates()
    {
        return
        [
            "This adapter must not execute commands.",
            "Every build/proof command requires explicit user approval before a future execution adapter can run it.",
            "Use -SkipAssetPrep for vNext publish while visual cleanup is active.",
            "Do not regenerate sprites_runtime or source assets.",
            "Do not mutate runtime/source PNGs or visual-side candidate/proof folders.",
            "Stop if any command plan would require removing or overwriting unrelated dirty work."
        ];
    }

    private static IReadOnlyList<string> BuildStopConditions()
    {
        return
        [
            "Missing vNext solution, test project, or build script.",
            "Any proof path requires asset prep or sprite normalization.",
            "A live proof would need visual generation/import or PNG mutation.",
            "The working tree has conflicting code changes in the intended proof surface."
        ];
    }

    private static IReadOnlyList<string> ResolveExistingProofInputs(IReadOnlyList<string>? approvedRoots)
    {
        var roots = (approvedRoots ?? [])
            .Where(root => !string.IsNullOrWhiteSpace(root))
            .Select(Path.GetFullPath)
            .ToList();
        var repoRoot = roots.FirstOrDefault(Directory.Exists) ?? Directory.GetCurrentDirectory();
        var candidates = new[]
        {
            Path.Combine(repoRoot, "vnext", "Wevito.VNext.sln"),
            Path.Combine(repoRoot, "vnext", "tests", "Wevito.VNext.Tests", "Wevito.VNext.Tests.csproj"),
            Path.Combine(repoRoot, "tools", "build-vnext.ps1"),
            Path.Combine(repoRoot, "tools", "probe-vnext-pet-tasks.ps1")
        };

        return candidates
            .Where(File.Exists)
            .Select(Path.GetFullPath)
            .ToList();
    }

    private static string BuildMarkdown(BuildProofPlanReport report, TaskAdapterRequest request)
    {
        var lines = new List<string>
        {
            "# PET TASKS Build Proof Plan",
            "",
            $"Generated: {report.GeneratedAtUtc:O}",
            $"TaskCard: `{report.TaskCardId}`",
            $"Intent: {request.Intent.RawText}",
            "",
            "## Summary",
            "",
            $"- Commands planned: {report.Commands.Count}",
            "- Commands run: false",
            "- Did mutate files: false",
            "",
            "## Command Plan",
            ""
        };

        lines.AddRange(report.Commands.Select(command => $"- {command.Order}. `{command.Command}` - {command.Purpose} Requires approval: {command.RequiresApproval}. Skip asset prep: {command.MustSkipAssetPrep}."));
        lines.Add("");
        lines.Add("## Expected Artifacts");
        lines.Add("");
        lines.AddRange(report.ProofArtifactsExpected.Select(item => $"- {item}"));
        lines.Add("");
        lines.Add("## Safety Gates");
        lines.Add("");
        lines.AddRange(report.SafetyGates.Select(item => $"- {item}"));
        lines.Add("");
        lines.Add("## Stop Conditions");
        lines.Add("");
        lines.AddRange(report.StopConditions.Select(item => $"- {item}"));
        lines.Add("");
        lines.Add("## Safety");
        lines.Add("");
        lines.Add("This adapter only wrote markdown/JSON artifacts in a new pet-task folder. It did not run build commands, edit files, mutate assets, or touch visual-side candidate/proof folders.");
        return string.Join(Environment.NewLine, lines) + Environment.NewLine;
    }

    private static string ResolveArtifactRoot(string artifactRoot, DateTimeOffset timestamp)
    {
        if (!string.IsNullOrWhiteSpace(artifactRoot))
        {
            return Path.GetFullPath(artifactRoot);
        }

        var slug = timestamp.ToString("yyyyMMdd-HHmmss") + "-build-proof-plan";
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
}
