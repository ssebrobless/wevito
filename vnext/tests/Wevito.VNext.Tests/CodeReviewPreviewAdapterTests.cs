using System.Text.Json;
using Wevito.VNext.Contracts;
using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class CodeReviewPreviewAdapterTests
{
    private readonly CodeReviewPreviewAdapter _adapter = new();

    [Fact]
    public void BuildReport_WritesMarkdownAndJsonWithoutMutatingCode()
    {
        var tempRoot = CreateTempRoot();
        var sourceRoot = Path.Combine(tempRoot, "vnext", "src");
        var artifactRoot = Path.Combine(tempRoot, "vnext", "artifacts", "pet-tasks", "20260505-133000-code-review");
        Directory.CreateDirectory(sourceRoot);
        var codePath = Path.Combine(sourceRoot, "Sample.cs");
        File.WriteAllText(codePath, "public sealed class Sample\n{\n    // TODO: tighten this later\n}\n");
        var beforeText = File.ReadAllText(codePath);

        var result = _adapter.BuildReport(BuildRequest(tempRoot, artifactRoot, [sourceRoot]));

        Assert.Equal(TaskAdapterResultStatus.PreviewReady, result.Status);
        Assert.False(result.DidMutate);
        Assert.Equal(beforeText, File.ReadAllText(codePath));
        Assert.Contains(codePath, result.ReadPaths ?? [], StringComparer.OrdinalIgnoreCase);
        Assert.Contains(result.WrittenPaths ?? [], path => path.EndsWith("code-review-report.json", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.WrittenPaths ?? [], path => path.EndsWith("run-summary.md", StringComparison.OrdinalIgnoreCase));

        var report = JsonSerializer.Deserialize<CodeReviewReport>(
            File.ReadAllText(Path.Combine(artifactRoot, "code-review-report.json")),
            JsonDefaults.Options);
        Assert.NotNull(report);
        Assert.Equal(1, report.FilesScanned);
        Assert.Contains(report.Findings, finding => finding.Kind == "todo_or_fixme");
        Assert.False(report.DidMutate);
    }

    [Fact]
    public void BuildReport_BlocksOutsideApprovedTarget()
    {
        var tempRoot = CreateTempRoot();
        var approvedRoot = Path.Combine(tempRoot, "repo");
        var outsideRoot = Path.Combine(tempRoot, "outside");
        var artifactRoot = Path.Combine(tempRoot, "vnext", "artifacts", "pet-tasks", "20260505-133000-code-review");
        Directory.CreateDirectory(approvedRoot);
        Directory.CreateDirectory(outsideRoot);

        var result = _adapter.BuildReport(BuildRequest(approvedRoot, artifactRoot, [outsideRoot]));

        Assert.Equal(TaskAdapterResultStatus.Blocked, result.Status);
        Assert.False(result.DidMutate);
        Assert.Empty(result.WrittenPaths ?? []);
        Assert.Contains("outside approved", result.BlockReason);
    }

    [Fact]
    public void BuildReport_BlocksExecuteMode()
    {
        var tempRoot = CreateTempRoot();
        var artifactRoot = Path.Combine(tempRoot, "vnext", "artifacts", "pet-tasks", "20260505-133000-code-review");

        var result = _adapter.BuildReport(BuildRequest(tempRoot, artifactRoot, [], TaskAdapterRunMode.Execute));

        Assert.Equal(TaskAdapterResultStatus.Blocked, result.Status);
        Assert.False(result.DidMutate);
        Assert.Contains("dry-run report", result.BlockReason);
    }

    [Fact]
    public void BuildReport_SkipsLocalCacheDirectories()
    {
        var tempRoot = CreateTempRoot();
        var sourceRoot = Path.Combine(tempRoot, "vnext", "src");
        var cacheRoot = Path.Combine(tempRoot, ".codex-cache", "chrome-debug-profile");
        var artifactRoot = Path.Combine(tempRoot, "vnext", "artifacts", "pet-tasks", "20260505-133000-code-review");
        Directory.CreateDirectory(sourceRoot);
        Directory.CreateDirectory(cacheRoot);
        var codePath = Path.Combine(sourceRoot, "Sample.cs");
        var cachePath = Path.Combine(cacheRoot, "manifest.json");
        File.WriteAllText(codePath, "public sealed class Sample {}\n");
        File.WriteAllText(cachePath, "{ \"long\": \"" + new string('x', 240) + "\" }");

        var result = _adapter.BuildReport(BuildRequest(tempRoot, artifactRoot, []));
        var report = JsonSerializer.Deserialize<CodeReviewReport>(
            File.ReadAllText(Path.Combine(artifactRoot, "code-review-report.json")),
            JsonDefaults.Options);

        Assert.Equal(TaskAdapterResultStatus.PreviewReady, result.Status);
        Assert.NotNull(report);
        Assert.Contains(report.Files, file => file.Path.Equals(codePath, StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(report.Files, file => file.Path.Equals(cachePath, StringComparison.OrdinalIgnoreCase));
    }

    private static TaskAdapterRequest BuildRequest(
        string approvedRoot,
        string artifactRoot,
        IReadOnlyList<string> targets,
        TaskAdapterRunMode runMode = TaskAdapterRunMode.DryRunPreview)
    {
        var intent = new TaskIntent(
            Guid.Parse("c0000000-0000-0000-0000-000000000001"),
            "Pip, review code",
            TaskIntentTargetMode.ExplicitPetName,
            TargetPetNameSnapshot: "Pip",
            TaskKind: TaskKind.ReviewCode,
            RequestedToolFamily: "codeReview",
            TargetPathsOrAssets: targets);
        var policy = new ToolPolicy(
            "code-review-readonly",
            "codeReview",
            ToolAccessMode.ReadOnly,
            ToolRiskLevel.Low,
            ApprovalRequirement.None,
            ApprovedRootPaths: [approvedRoot]);

        return new TaskAdapterRequest(
            Guid.Parse("d0000000-0000-0000-0000-000000000001"),
            intent,
            policy,
            runMode,
            artifactRoot);
    }

    private static string CreateTempRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "wevito-code-review-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }
}
