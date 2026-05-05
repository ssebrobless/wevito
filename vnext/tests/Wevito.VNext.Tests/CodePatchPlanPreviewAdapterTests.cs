using System.Text.Json;
using Wevito.VNext.Contracts;
using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class CodePatchPlanPreviewAdapterTests
{
    private readonly CodePatchPlanPreviewAdapter _adapter = new();

    [Fact]
    public void BuildPlan_WritesMarkdownAndJsonWithoutMutatingCode()
    {
        var tempRoot = CreateTempRoot();
        var sourceRoot = Path.Combine(tempRoot, "vnext", "src");
        var artifactRoot = Path.Combine(tempRoot, "vnext", "artifacts", "pet-tasks", "20260505-140000-code-patch-plan");
        Directory.CreateDirectory(sourceRoot);
        var codePath = Path.Combine(sourceRoot, "Sample.cs");
        File.WriteAllText(codePath, "public sealed class Sample {}\n");
        var beforeText = File.ReadAllText(codePath);

        var result = _adapter.BuildPlan(BuildRequest(tempRoot, artifactRoot, [sourceRoot]));

        Assert.Equal(TaskAdapterResultStatus.PreviewReady, result.Status);
        Assert.False(result.DidMutate);
        Assert.Equal(beforeText, File.ReadAllText(codePath));
        Assert.Contains(codePath, result.ReadPaths ?? [], StringComparer.OrdinalIgnoreCase);
        Assert.Contains(result.WrittenPaths ?? [], path => path.EndsWith("code-patch-plan-report.json", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.WrittenPaths ?? [], path => path.EndsWith("run-summary.md", StringComparison.OrdinalIgnoreCase));

        var report = JsonSerializer.Deserialize<CodePatchPlanReport>(
            File.ReadAllText(Path.Combine(artifactRoot, "code-patch-plan-report.json")),
            JsonDefaults.Options);
        Assert.NotNull(report);
        Assert.Single(report.CandidateFiles);
        Assert.Contains(report.ValidationPlan, step => step.Contains("dotnet build", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(report.SafetyGates, gate => gate.Contains("No runtime/source PNG mutation", StringComparison.OrdinalIgnoreCase));
        Assert.False(report.DidMutate);
    }

    [Fact]
    public void BuildPlan_BlocksExecuteMode()
    {
        var tempRoot = CreateTempRoot();
        var artifactRoot = Path.Combine(tempRoot, "vnext", "artifacts", "pet-tasks", "20260505-140000-code-patch-plan");

        var result = _adapter.BuildPlan(BuildRequest(tempRoot, artifactRoot, [], TaskAdapterRunMode.Execute));

        Assert.Equal(TaskAdapterResultStatus.Blocked, result.Status);
        Assert.False(result.DidMutate);
        Assert.Contains("dry-run report", result.BlockReason);
    }

    [Fact]
    public void BuildPlan_BlocksOutsideApprovedTarget()
    {
        var tempRoot = CreateTempRoot();
        var approvedRoot = Path.Combine(tempRoot, "repo");
        var outsideRoot = Path.Combine(tempRoot, "outside");
        var artifactRoot = Path.Combine(tempRoot, "vnext", "artifacts", "pet-tasks", "20260505-140000-code-patch-plan");
        Directory.CreateDirectory(approvedRoot);
        Directory.CreateDirectory(outsideRoot);

        var result = _adapter.BuildPlan(BuildRequest(approvedRoot, artifactRoot, [outsideRoot]));

        Assert.Equal(TaskAdapterResultStatus.Blocked, result.Status);
        Assert.False(result.DidMutate);
        Assert.Empty(result.WrittenPaths ?? []);
        Assert.Contains("outside approved", result.BlockReason);
    }

    private static TaskAdapterRequest BuildRequest(
        string approvedRoot,
        string artifactRoot,
        IReadOnlyList<string> targets,
        TaskAdapterRunMode runMode = TaskAdapterRunMode.DryRunPreview)
    {
        var intent = new TaskIntent(
            Guid.Parse("c1000000-0000-0000-0000-000000000001"),
            "Pip, plan a code patch",
            TaskIntentTargetMode.ExplicitPetName,
            TargetPetNameSnapshot: "Pip",
            TaskKind: TaskKind.PlanCodePatch,
            RequestedToolFamily: "codePatchPlan",
            TargetPathsOrAssets: targets);
        var policy = new ToolPolicy(
            "code-patch-plan-readonly",
            "codePatchPlan",
            ToolAccessMode.ReadOnly,
            ToolRiskLevel.Low,
            ApprovalRequirement.None,
            ApprovedRootPaths: [approvedRoot]);

        return new TaskAdapterRequest(
            Guid.Parse("d1000000-0000-0000-0000-000000000001"),
            intent,
            policy,
            runMode,
            artifactRoot);
    }

    private static string CreateTempRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "wevito-code-patch-plan-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }
}
