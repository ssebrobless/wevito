using System.Text.Json;
using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class LocalTrainingPlanServiceTests
{
    [Fact]
    public void CreatePlan_RefusesLoraWhenFlagFalse()
    {
        var root = CreateTempRoot();
        var service = new LocalTrainingPlanService();

        var result = service.CreatePlan(Request(root, new Dictionary<string, string>
        {
            [LocalTrainingPlanService.LoraEnabledSetting] = "false"
        }));

        Assert.True(result.Succeeded, result.Message);
        var lora = Assert.Single(result.Plan.Stages.Where(stage => stage.Stage == LocalTrainingStage.LoraPilot));
        Assert.Equal(LocalTrainingStageStatus.Blocked, lora.Status);
        Assert.False(lora.MayMutate);
        Assert.Contains("false", lora.Reason, StringComparison.OrdinalIgnoreCase);
        Assert.True(File.Exists(result.PlanPath));
        Assert.True(File.Exists(result.SummaryPath));
    }

    [Fact]
    public void CreatePlan_DryRunWritesOnlyUnderArtifactRoot()
    {
        var root = CreateTempRoot();
        var artifactRoot = Path.Combine(root, "vnext", "artifacts", "training-plan");
        var service = new LocalTrainingPlanService();

        var result = service.CreatePlan(Request(root, new Dictionary<string, string>(), artifactRoot: artifactRoot));

        Assert.True(result.Succeeded, result.Message);
        Assert.StartsWith(Path.GetFullPath(artifactRoot), Path.GetFullPath(result.PlanPath), StringComparison.OrdinalIgnoreCase);
        Assert.False(File.Exists(Path.Combine(root, "vnext", "content", "local-ai", "rerank-head.json")));
    }

    [Fact]
    public void CreatePlan_BlocksArtifactRootOutsideAllowlist()
    {
        var root = CreateTempRoot();
        var service = new LocalTrainingPlanService();

        var ex = Assert.Throws<InvalidOperationException>(() => service.CreatePlan(Request(root, new Dictionary<string, string>(), artifactRoot: Path.Combine(root, "outside"))));

        Assert.Contains("vnext", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LoraScripts_DoNotInvokePythonDirectly()
    {
        var repoRoot = FindRepoRoot();
        var ps1 = File.ReadAllText(Path.Combine(repoRoot, "tools", "run-unsloth-lora.ps1")).ToLowerInvariant();
        var bat = File.ReadAllText(Path.Combine(repoRoot, "tools", "run-unsloth-lora.bat")).ToLowerInvariant();

        Assert.DoesNotContain("python", ps1);
        Assert.DoesNotContain("python", bat);
        Assert.DoesNotContain("py.exe", ps1);
        Assert.DoesNotContain("py.exe", bat);
    }

    private static LocalTrainingPlanRequest Request(
        string root,
        IReadOnlyDictionary<string, string> settings,
        string? artifactRoot = null)
    {
        return new LocalTrainingPlanRequest(
            root,
            "v0001-20260512-120000",
            Path.Combine(root, "vnext", "content"),
            artifactRoot ?? Path.Combine(root, "vnext", "artifacts", "training-plan"),
            settings,
            DateTimeOffset.Parse("2026-05-12T12:00:00Z"));
    }

    private static string CreateTempRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "wevito-training-plan-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "vnext", "content"));
        Directory.CreateDirectory(Path.Combine(root, "vnext", "artifacts"));
        return root;
    }

    private static string FindRepoRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var gitPath = Path.Combine(current.FullName, ".git");
            if (Directory.Exists(gitPath) || File.Exists(gitPath) || File.Exists(Path.Combine(current.FullName, "wevito.code-workspace")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repo root.");
    }
}
