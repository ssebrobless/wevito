using System.Text.Json;
using Wevito.VNext.Contracts;
using Wevito.VNext.Core;
using Wevito.VNext.Tests.Fakes;

namespace Wevito.VNext.Tests;

public sealed class ScreenCaptureExecutionAdapterTests
{
    [Fact]
    public async Task ExecuteAsync_WritesScreenshotManifestAndSummary()
    {
        var adapter = new ScreenCaptureExecutionAdapter(new FakeScreenCaptureBackend());
        var tempRoot = CreateTempRoot();
        var artifactRoot = Path.Combine(tempRoot, "vnext", "artifacts", "pet-tasks", "20260506-screen-capture-execute");

        var result = await adapter.ExecuteAsync(BuildRequest(artifactRoot), DateTimeOffset.Parse("2026-05-06T18:00:00Z"));

        Assert.Equal(TaskAdapterResultStatus.Completed, result.Status);
        Assert.False(result.DidMutate);
        Assert.Contains(result.WrittenPaths ?? [], path => path.EndsWith("screenshot.png", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.WrittenPaths ?? [], path => path.EndsWith("manifest.json", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.WrittenPaths ?? [], path => path.EndsWith("run-summary.md", StringComparison.OrdinalIgnoreCase));
        Assert.True(File.Exists(Path.Combine(artifactRoot, "screenshot.png")));

        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(artifactRoot, "manifest.json")));
        var root = manifest.RootElement;
        Assert.Equal("wevitoWindow", root.GetProperty("targetKind").GetString());
        Assert.Equal("screenshotPng", root.GetProperty("outputKind").GetString());
        Assert.Equal("Wevito Home Panel", root.GetProperty("targetWindowTitle").GetString());
        Assert.True(root.GetProperty("indicatorVisible").GetBoolean());
        Assert.Equal("fake backend", root.GetProperty("redactionState").GetString());
    }

    [Fact]
    public async Task ExecuteAsync_BlocksWithoutApprovalGatedPolicy()
    {
        var adapter = new ScreenCaptureExecutionAdapter(new FakeScreenCaptureBackend());
        var tempRoot = CreateTempRoot();
        var artifactRoot = Path.Combine(tempRoot, "vnext", "artifacts", "pet-tasks", "20260506-screen-capture-execute");
        var request = BuildRequest(artifactRoot) with
        {
            PolicySnapshot = new ToolPolicy("screen-capture-readonly", "screenCapture", ToolAccessMode.ReadOnly, ToolRiskLevel.Low, ApprovalRequirement.None)
        };

        var result = await adapter.ExecuteAsync(request, DateTimeOffset.Parse("2026-05-06T18:00:00Z"));

        Assert.Equal(TaskAdapterResultStatus.Blocked, result.Status);
        Assert.Contains("approval-gated", result.BlockReason);
    }

    [Fact]
    public async Task ExecuteAsync_SelectedRegion_WritesRegionManifest()
    {
        var adapter = new ScreenCaptureExecutionAdapter(new FakeScreenCaptureBackend());
        var tempRoot = CreateTempRoot();
        var artifactRoot = Path.Combine(tempRoot, "vnext", "artifacts", "pet-tasks", "20260506-region-capture-execute");
        var request = BuildRequest(artifactRoot, "screenshot a region");
        var region = new CaptureRegion(42, 64, 320, 180);

        var result = await adapter.ExecuteAsync(request, DateTimeOffset.Parse("2026-05-06T18:00:00Z"), region);

        Assert.Equal(TaskAdapterResultStatus.Completed, result.Status);
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(artifactRoot, "manifest.json")));
        var root = manifest.RootElement;
        Assert.Equal("selectedRegion", root.GetProperty("targetKind").GetString());
        Assert.Equal("selectedRegion", root.GetProperty("privacyLevel").GetString());
        Assert.Equal("Selected Region", root.GetProperty("targetWindowTitle").GetString());
        Assert.Equal(42, root.GetProperty("targetWindowRect").GetProperty("x").GetInt32());
        Assert.Equal(64, root.GetProperty("targetWindowRect").GetProperty("y").GetInt32());
        Assert.Equal(320, root.GetProperty("targetWindowRect").GetProperty("width").GetInt32());
        Assert.Equal(180, root.GetProperty("targetWindowRect").GetProperty("height").GetInt32());
    }

    [Fact]
    public async Task ExecuteAsync_LastRegion_BlocksWhenRegionMissing()
    {
        var adapter = new ScreenCaptureExecutionAdapter(new FakeScreenCaptureBackend());
        var tempRoot = CreateTempRoot();
        var artifactRoot = Path.Combine(tempRoot, "vnext", "artifacts", "pet-tasks", "20260506-region-capture-execute");
        var request = BuildRequest(artifactRoot, "screenshot last region");

        var result = await adapter.ExecuteAsync(request, DateTimeOffset.Parse("2026-05-06T18:00:00Z"));

        Assert.Equal(TaskAdapterResultStatus.Blocked, result.Status);
        Assert.Contains("requires a selected region", result.BlockReason);
    }

    [Fact]
    public async Task ExecuteAsync_WevitoRecording_WritesClipManifestAndSummary()
    {
        var adapter = new ScreenCaptureExecutionAdapter(new FakeScreenCaptureBackend());
        var tempRoot = CreateTempRoot();
        var artifactRoot = Path.Combine(tempRoot, "vnext", "artifacts", "pet-tasks", "20260506-screencapture-clip");
        var request = BuildRequest(artifactRoot, "record the Wevito window for 5 seconds");
        var progressReports = new RecordingProgress();

        var result = await adapter.ExecuteAsync(
            request,
            DateTimeOffset.Parse("2026-05-06T18:00:00Z"),
            recordingProgress: progressReports);

        Assert.Equal(TaskAdapterResultStatus.Completed, result.Status);
        Assert.Contains(result.WrittenPaths ?? [], path => path.EndsWith("clip.mp4", StringComparison.OrdinalIgnoreCase));
        Assert.True(File.Exists(Path.Combine(artifactRoot, "clip.mp4")));
        Assert.Contains(TimeSpan.Zero, progressReports.Values);

        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(artifactRoot, "manifest.json")));
        var root = manifest.RootElement;
        Assert.Equal("shortRecording", root.GetProperty("preset").GetString());
        Assert.Equal("clipMp4", root.GetProperty("outputKind").GetString());
        Assert.Equal("wevitoWindow", root.GetProperty("targetKind").GetString());
        Assert.Equal("fake clip backend", root.GetProperty("redactionState").GetString());
    }

    [Fact]
    public async Task ExecuteAsync_RegionRecording_Blocks()
    {
        var adapter = new ScreenCaptureExecutionAdapter(new FakeScreenCaptureBackend());
        var tempRoot = CreateTempRoot();
        var artifactRoot = Path.Combine(tempRoot, "vnext", "artifacts", "pet-tasks", "20260506-screencapture-clip");
        var request = BuildRequest(artifactRoot, "record a selected region for 5 seconds");

        var result = await adapter.ExecuteAsync(
            request,
            DateTimeOffset.Parse("2026-05-06T18:00:00Z"),
            new CaptureRegion(1, 2, 100, 100));

        Assert.Equal(TaskAdapterResultStatus.Blocked, result.Status);
        Assert.Contains("Wevito window", result.BlockReason);
    }

    private static TaskAdapterRequest BuildRequest(string artifactRoot, string rawText = "screenshot the Wevito window")
    {
        var intent = new TaskIntent(
            Guid.Parse("c6000000-0000-0000-0000-000000000003"),
            rawText,
            TaskIntentTargetMode.RouteToBestHelper,
            TaskKind: TaskKind.ScreenCapture,
            RequestedToolFamily: "screenCapture");
        var policy = new ToolPolicy(
            "screen-capture-wevito-window-approval",
            "screenCapture",
            ToolAccessMode.ReadOnly,
            ToolRiskLevel.Medium,
            ApprovalRequirement.BeforeExecution);

        return new TaskAdapterRequest(
            Guid.Parse("d6000000-0000-0000-0000-000000000003"),
            intent,
            policy,
            TaskAdapterRunMode.Execute,
            artifactRoot);
    }

    private static string CreateTempRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "wevito-screen-capture-execution-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }

    private sealed class RecordingProgress : IProgress<TimeSpan>
    {
        public List<TimeSpan> Values { get; } = [];

        public void Report(TimeSpan value)
        {
            Values.Add(value);
        }
    }
}
