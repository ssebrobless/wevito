using System.Text.Json;
using Wevito.VNext.Contracts;
using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class AssetInventoryPreviewAdapterTests
{
    private readonly AssetInventoryPreviewAdapter _adapter = new();

    [Fact]
    public void BuildReport_WritesMarkdownAndJsonWithoutMutatingAssets()
    {
        var tempRoot = CreateTempRoot();
        var runtimeRoot = Path.Combine(tempRoot, "sprites_runtime");
        var sharedRoot = Path.Combine(tempRoot, "sprites_shared_runtime");
        var rowRoot = Path.Combine(runtimeRoot, "goose", "baby", "female", "blue");
        var itemRoot = Path.Combine(sharedRoot, "items", "toys_a");
        var artifactRoot = Path.Combine(tempRoot, "vnext", "artifacts", "pet-tasks", "20260505-132000-asset-inventory");
        Directory.CreateDirectory(rowRoot);
        Directory.CreateDirectory(itemRoot);
        var runtimePng = Path.Combine(rowRoot, "idle_00.png");
        var sharedPng = Path.Combine(itemRoot, "ball.png");
        var orphanImport = Path.Combine(itemRoot, "missing.png.import");
        File.WriteAllBytes(runtimePng, BuildPngHeaderBytes());
        File.WriteAllBytes(sharedPng, BuildPngHeaderBytes());
        File.WriteAllText(orphanImport, "orphan");
        var beforeBytes = File.ReadAllBytes(runtimePng);

        var result = _adapter.BuildReport(BuildRequest([runtimeRoot, sharedRoot], artifactRoot));

        Assert.Equal(TaskAdapterResultStatus.PreviewReady, result.Status);
        Assert.False(result.DidMutate);
        Assert.Equal(beforeBytes, File.ReadAllBytes(runtimePng));
        Assert.Contains(result.WrittenPaths ?? [], path => path.EndsWith("asset-inventory-report.json", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.WrittenPaths ?? [], path => path.EndsWith("run-summary.md", StringComparison.OrdinalIgnoreCase));

        var report = JsonSerializer.Deserialize<AssetInventoryReport>(
            File.ReadAllText(Path.Combine(artifactRoot, "asset-inventory-report.json")),
            JsonDefaults.Options);
        Assert.NotNull(report);
        Assert.Equal(1, report.RuntimeVariantFolderCount);
        Assert.Equal(1, report.RuntimePngCount);
        Assert.Equal(1, report.SharedPngCount);
        Assert.Contains(report.Findings, finding => finding.Kind == "orphan_png_import");
        Assert.False(report.DidMutate);
    }

    [Fact]
    public void BuildReport_BlocksExecuteMode()
    {
        var tempRoot = CreateTempRoot();
        var runtimeRoot = Path.Combine(tempRoot, "sprites_runtime");
        Directory.CreateDirectory(runtimeRoot);
        var artifactRoot = Path.Combine(tempRoot, "vnext", "artifacts", "pet-tasks", "20260505-132000-asset-inventory");

        var result = _adapter.BuildReport(BuildRequest([runtimeRoot], artifactRoot, TaskAdapterRunMode.Execute));

        Assert.Equal(TaskAdapterResultStatus.Blocked, result.Status);
        Assert.False(result.DidMutate);
        Assert.Contains("dry-run report", result.BlockReason);
        Assert.False(Directory.Exists(artifactRoot));
    }

    [Fact]
    public void BuildReport_BlocksUnsafeArtifactRoot()
    {
        var tempRoot = CreateTempRoot();
        var runtimeRoot = Path.Combine(tempRoot, "sprites_runtime");
        Directory.CreateDirectory(runtimeRoot);
        var artifactRoot = Path.Combine(tempRoot, "vnext", "artifacts", "animation-runs", "candidate-frames");

        var result = _adapter.BuildReport(BuildRequest([runtimeRoot], artifactRoot));

        Assert.Equal(TaskAdapterResultStatus.Blocked, result.Status);
        Assert.False(result.DidMutate);
        Assert.Contains("pet-tasks", result.BlockReason);
    }

    private static TaskAdapterRequest BuildRequest(
        IReadOnlyList<string> approvedRoots,
        string artifactRoot,
        TaskAdapterRunMode runMode = TaskAdapterRunMode.DryRunPreview)
    {
        var intent = new TaskIntent(
            Guid.Parse("a0000000-0000-0000-0000-000000000001"),
            "Bean, inventory assets",
            TaskIntentTargetMode.ExplicitPetName,
            TargetPetNameSnapshot: "Bean",
            TaskKind: TaskKind.InventoryAssets,
            RequestedToolFamily: "assetInventory");
        var policy = new ToolPolicy(
            "asset-inventory-readonly",
            "assetInventory",
            ToolAccessMode.ReadOnly,
            ToolRiskLevel.Low,
            ApprovalRequirement.None,
            ApprovedRootPaths: approvedRoots);

        return new TaskAdapterRequest(
            Guid.Parse("b0000000-0000-0000-0000-000000000001"),
            intent,
            policy,
            runMode,
            artifactRoot);
    }

    private static string CreateTempRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "wevito-asset-inventory-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }

    private static byte[] BuildPngHeaderBytes()
    {
        return Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+/p9sAAAAASUVORK5CYII=");
    }
}
