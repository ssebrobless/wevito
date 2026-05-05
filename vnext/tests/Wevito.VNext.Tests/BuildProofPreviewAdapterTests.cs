using System.Text.Json;
using Wevito.VNext.Contracts;
using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class BuildProofPreviewAdapterTests
{
    private readonly BuildProofPreviewAdapter _adapter = new();

    [Fact]
    public void BuildPlan_WritesApprovalGatedCommandPlanWithoutRunningCommands()
    {
        var tempRoot = CreateTempRoot();
        var artifactRoot = Path.Combine(tempRoot, "vnext", "artifacts", "pet-tasks", "20260505-141000-build-proof");
        Directory.CreateDirectory(Path.Combine(tempRoot, "vnext"));
        Directory.CreateDirectory(Path.Combine(tempRoot, "vnext", "tests", "Wevito.VNext.Tests"));
        Directory.CreateDirectory(Path.Combine(tempRoot, "tools"));
        File.WriteAllText(Path.Combine(tempRoot, "vnext", "Wevito.VNext.sln"), "");
        File.WriteAllText(Path.Combine(tempRoot, "vnext", "tests", "Wevito.VNext.Tests", "Wevito.VNext.Tests.csproj"), "<Project />");
        File.WriteAllText(Path.Combine(tempRoot, "tools", "build-vnext.ps1"), "");

        var result = _adapter.BuildPlan(BuildRequest(tempRoot, artifactRoot));

        Assert.Equal(TaskAdapterResultStatus.PreviewReady, result.Status);
        Assert.False(result.DidMutate);
        Assert.Contains(result.WrittenPaths ?? [], path => path.EndsWith("build-proof-plan-report.json", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.WrittenPaths ?? [], path => path.EndsWith("run-summary.md", StringComparison.OrdinalIgnoreCase));

        var report = JsonSerializer.Deserialize<BuildProofPlanReport>(
            File.ReadAllText(Path.Combine(artifactRoot, "build-proof-plan-report.json")),
            JsonDefaults.Options);
        Assert.NotNull(report);
        Assert.False(report.DidRunCommands);
        Assert.False(report.DidMutate);
        Assert.Contains(report.Commands, command => command.Command.Contains("-SkipAssetPrep", StringComparison.OrdinalIgnoreCase));
        Assert.All(report.Commands, command => Assert.True(command.RequiresApproval));
    }

    [Fact]
    public void BuildPlan_BlocksExecuteMode()
    {
        var tempRoot = CreateTempRoot();
        var artifactRoot = Path.Combine(tempRoot, "vnext", "artifacts", "pet-tasks", "20260505-141000-build-proof");

        var result = _adapter.BuildPlan(BuildRequest(tempRoot, artifactRoot, TaskAdapterRunMode.Execute));

        Assert.Equal(TaskAdapterResultStatus.Blocked, result.Status);
        Assert.False(result.DidMutate);
        Assert.Contains("dry-run report", result.BlockReason);
    }

    private static TaskAdapterRequest BuildRequest(
        string approvedRoot,
        string artifactRoot,
        TaskAdapterRunMode runMode = TaskAdapterRunMode.DryRunPreview)
    {
        var intent = new TaskIntent(
            Guid.Parse("c2000000-0000-0000-0000-000000000001"),
            "Nix, run a build proof",
            TaskIntentTargetMode.ExplicitPetName,
            TargetPetNameSnapshot: "Nix",
            TaskKind: TaskKind.BuildProof,
            RequestedToolFamily: "buildProof");
        var policy = new ToolPolicy(
            "build-proof-readonly",
            "buildProof",
            ToolAccessMode.ReadOnly,
            ToolRiskLevel.Low,
            ApprovalRequirement.None,
            ApprovedRootPaths: [approvedRoot]);

        return new TaskAdapterRequest(
            Guid.Parse("d2000000-0000-0000-0000-000000000001"),
            intent,
            policy,
            runMode,
            artifactRoot);
    }

    private static string CreateTempRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "wevito-build-proof-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }
}
