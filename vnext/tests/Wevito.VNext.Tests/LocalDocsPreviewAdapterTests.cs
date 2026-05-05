using Wevito.VNext.Contracts;
using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class LocalDocsPreviewAdapterTests
{
    private readonly LocalDocsPreviewAdapter _adapter = new();

    [Fact]
    public void BuildPreview_ReturnsReadOnlyPreviewForApprovedDocsRoot()
    {
        var tempRoot = CreateTempRoot();
        var docsRoot = Path.Combine(tempRoot, "docs");
        Directory.CreateDirectory(docsRoot);
        var planPath = Path.Combine(docsRoot, "plan.md");
        var notesPath = Path.Combine(docsRoot, "notes.txt");
        File.WriteAllText(planPath, "phase plan");
        File.WriteAllText(notesPath, "notes");

        var result = _adapter.BuildPreview(BuildRequest(docsRoot, [docsRoot]));

        Assert.Equal(TaskAdapterResultStatus.PreviewReady, result.Status);
        Assert.False(result.DidMutate);
        Assert.Equal(2, result.WrittenPaths?.Count);
        Assert.EndsWith("run-summary.md", result.AuditLogPath, StringComparison.OrdinalIgnoreCase);
        Assert.All(result.WrittenPaths ?? [], path => Assert.True(File.Exists(path), path));
        Assert.Contains(planPath, result.ReadPaths ?? [], StringComparer.OrdinalIgnoreCase);
        Assert.Contains(notesPath, result.ReadPaths ?? [], StringComparer.OrdinalIgnoreCase);
        Assert.Contains("No source files were changed", result.PreviewSummary);
    }

    [Fact]
    public void BuildPreview_BlocksTargetOutsideApprovedRoot()
    {
        var tempRoot = CreateTempRoot();
        var docsRoot = Path.Combine(tempRoot, "docs");
        var outsideRoot = Path.Combine(tempRoot, "outside");
        Directory.CreateDirectory(docsRoot);
        Directory.CreateDirectory(outsideRoot);
        var outsidePath = Path.Combine(outsideRoot, "secret.md");
        File.WriteAllText(outsidePath, "nope");

        var result = _adapter.BuildPreview(BuildRequest(docsRoot, [outsidePath]));

        Assert.Equal(TaskAdapterResultStatus.Blocked, result.Status);
        Assert.False(result.DidMutate);
        Assert.Empty(result.ReadPaths ?? []);
        Assert.Contains("outside approved", result.BlockReason);
    }

    [Fact]
    public void BuildPreview_BlocksWhenPolicyHasNoApprovedRoots()
    {
        var tempRoot = CreateTempRoot();
        var result = _adapter.BuildPreview(BuildRequest(tempRoot, [], approvedRoots: []));

        Assert.Equal(TaskAdapterResultStatus.Blocked, result.Status);
        Assert.Contains("approved root", result.BlockReason);
        Assert.False(result.DidMutate);
    }

    [Fact]
    public void BuildPreview_BlocksUnsafeArtifactRoot()
    {
        var tempRoot = CreateTempRoot();
        var docsRoot = Path.Combine(tempRoot, "docs");
        Directory.CreateDirectory(docsRoot);
        var unsafeRoot = Path.Combine(tempRoot, "candidate-frames");

        var result = _adapter.BuildPreview(BuildRequest(docsRoot, [docsRoot], artifactRoot: unsafeRoot));

        Assert.Equal(TaskAdapterResultStatus.Blocked, result.Status);
        Assert.Contains("pet-tasks", result.BlockReason);
        Assert.False(result.DidMutate);
    }

    [Fact]
    public void BuildPreview_BlocksExecuteModeUntilExecutionAdapterExists()
    {
        var tempRoot = CreateTempRoot();
        Directory.CreateDirectory(Path.Combine(tempRoot, "docs"));
        var request = BuildRequest(Path.Combine(tempRoot, "docs"), [], runMode: TaskAdapterRunMode.Execute);

        var result = _adapter.BuildPreview(request);

        Assert.Equal(TaskAdapterResultStatus.Blocked, result.Status);
        Assert.Contains("dry-run preview", result.BlockReason);
        Assert.False(result.DidMutate);
    }

    private static TaskAdapterRequest BuildRequest(
        string docsRoot,
        IReadOnlyList<string> targets,
        IReadOnlyList<string>? approvedRoots = null,
        TaskAdapterRunMode runMode = TaskAdapterRunMode.DryRunPreview,
        string? artifactRoot = null)
    {
        var intent = new TaskIntent(
            Guid.Parse("40000000-0000-0000-0000-000000000001"),
            "Bean, summarize local docs",
            TaskIntentTargetMode.ExplicitPetName,
            TargetPetNameSnapshot: "Bean",
            TaskKind: TaskKind.SummarizeDocs,
            RequestedToolFamily: "localDocs",
            TargetPathsOrAssets: targets);
        var policy = new ToolPolicy(
            "local-docs-readonly",
            "localDocs",
            ToolAccessMode.ReadOnly,
            ToolRiskLevel.Low,
            ApprovalRequirement.None,
            ApprovedRootPaths: approvedRoots ?? [docsRoot]);

        return new TaskAdapterRequest(
            Guid.Parse("50000000-0000-0000-0000-000000000001"),
            intent,
            policy,
            runMode,
            ArtifactRoot: artifactRoot ?? Path.Combine(docsRoot, "vnext", "artifacts", "pet-tasks", "local-docs-test"));
    }

    private static string CreateTempRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "wevito-local-docs-preview-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }
}
