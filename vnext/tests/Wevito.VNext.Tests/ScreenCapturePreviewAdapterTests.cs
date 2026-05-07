using System.Text.Json;
using Wevito.VNext.Contracts;
using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class ScreenCapturePreviewAdapterTests
{
    private readonly ScreenCapturePreviewAdapter _adapter = new();

    [Fact]
    public void BuildPreview_WritesReportWithoutCapturingScreen()
    {
        var tempRoot = CreateTempRoot();
        var artifactRoot = Path.Combine(tempRoot, "vnext", "artifacts", "pet-tasks", "20260505-152000-screen-capture");

        var result = _adapter.BuildPreview(BuildRequest(artifactRoot, "Nix, take a screenshot of the Wevito window"));

        Assert.Equal(TaskAdapterResultStatus.PreviewReady, result.Status);
        Assert.False(result.DidMutate);
        Assert.Contains(result.WrittenPaths ?? [], path => path.EndsWith("screen-capture-preview-report.json", StringComparison.OrdinalIgnoreCase));

        var report = JsonSerializer.Deserialize<ScreenCapturePreviewReport>(
            File.ReadAllText(Path.Combine(artifactRoot, "screen-capture-preview-report.json")),
            JsonDefaults.Options);
        Assert.NotNull(report);
        Assert.False(report.DidCaptureScreen);
        Assert.False(report.DidRecordScreen);
        Assert.False(report.DidMutate);
        Assert.Contains(report.Capabilities, capability => capability.ActionKind == ScreenCaptureActionKind.WindowScreenshot && capability.ApprovalRequirement == ApprovalRequirement.BeforeExecution);
        Assert.Contains(report.Capabilities, capability => capability.ActionKind == ScreenCaptureActionKind.ScreenRecording && capability.Status == ScreenCaptureCapabilityStatus.Blocked);
    }

    [Fact]
    public void BuildPreview_BlocksExecuteMode()
    {
        var tempRoot = CreateTempRoot();
        var artifactRoot = Path.Combine(tempRoot, "vnext", "artifacts", "pet-tasks", "20260505-152000-screen-capture");

        var result = _adapter.BuildPreview(BuildRequest(artifactRoot, "Nix, take a screenshot", TaskAdapterRunMode.Execute));

        Assert.Equal(TaskAdapterResultStatus.Blocked, result.Status);
        Assert.Contains("dry-run report", result.BlockReason);
    }

    [Fact]
    public void BuildPreview_DescribesSelectedRegionWithoutCapturing()
    {
        var tempRoot = CreateTempRoot();
        var artifactRoot = Path.Combine(tempRoot, "vnext", "artifacts", "pet-tasks", "20260505-152000-screen-capture");

        var result = _adapter.BuildPreview(BuildRequest(artifactRoot, "screenshot a region"));

        Assert.Equal(TaskAdapterResultStatus.PreviewReady, result.Status);
        var report = JsonSerializer.Deserialize<ScreenCapturePreviewReport>(
            File.ReadAllText(Path.Combine(artifactRoot, "screen-capture-preview-report.json")),
            JsonDefaults.Options);
        Assert.NotNull(report);
        Assert.Contains("selected-region", report.RequestedCaptureSummary, StringComparison.OrdinalIgnoreCase);
        Assert.False(report.DidCaptureScreen);
    }

    private static TaskAdapterRequest BuildRequest(
        string artifactRoot,
        string rawText,
        TaskAdapterRunMode runMode = TaskAdapterRunMode.DryRunPreview)
    {
        var intent = new TaskIntent(
            Guid.Parse("c5000000-0000-0000-0000-000000000003"),
            rawText,
            TaskIntentTargetMode.ExplicitPetName,
            TargetPetNameSnapshot: "Nix",
            TaskKind: TaskKind.ScreenCapture,
            RequestedToolFamily: "screenCapture");
        var policy = new ToolPolicy(
            "screen-capture-readonly",
            "screenCapture",
            ToolAccessMode.ReadOnly,
            ToolRiskLevel.Low,
            ApprovalRequirement.None);

        return new TaskAdapterRequest(
            Guid.Parse("d5000000-0000-0000-0000-000000000003"),
            intent,
            policy,
            runMode,
            artifactRoot);
    }

    private static string CreateTempRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "wevito-screen-capture-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }
}
