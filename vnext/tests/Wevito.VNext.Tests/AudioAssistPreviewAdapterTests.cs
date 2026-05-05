using System.Text.Json;
using Wevito.VNext.Contracts;
using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class AudioAssistPreviewAdapterTests
{
    private readonly AudioAssistPreviewAdapter _adapter = new(new FakeAudioEndpointStatusReader(
        new AudioEndpointStatus(
            "fake test endpoint",
            IsAvailable: true,
            MasterVolumePercent: 42.5,
            IsMuted: false,
            EndpointId: "fake-endpoint-id",
            Detail: "Fake endpoint volume is 42.5% and mute is false.",
            DateTimeOffset.Parse("2026-05-05T14:50:00Z"))));

    [Fact]
    public void BuildStatusReport_WritesCapabilitiesWithoutChangingAudio()
    {
        var tempRoot = CreateTempRoot();
        var artifactRoot = Path.Combine(tempRoot, "vnext", "artifacts", "pet-tasks", "20260505-145000-audio-assist");

        var result = _adapter.BuildStatusReport(BuildRequest(artifactRoot));

        Assert.Equal(TaskAdapterResultStatus.PreviewReady, result.Status);
        Assert.False(result.DidMutate);
        Assert.Contains(result.WrittenPaths ?? [], path => path.EndsWith("audio-assist-status-report.json", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.WrittenPaths ?? [], path => path.EndsWith("run-summary.md", StringComparison.OrdinalIgnoreCase));

        var report = JsonSerializer.Deserialize<AudioAssistStatusReport>(
            File.ReadAllText(Path.Combine(artifactRoot, "audio-assist-status-report.json")),
            JsonDefaults.Options);
        Assert.NotNull(report);
        Assert.True(report.DidInspectSystemAudio);
        Assert.False(report.DidChangeAudio);
        Assert.False(report.DidMutate);
        Assert.NotNull(report.EndpointStatus);
        Assert.True(report.EndpointStatus.IsAvailable);
        Assert.Equal(42.5, report.EndpointStatus.MasterVolumePercent);
        Assert.False(report.EndpointStatus.IsMuted);
        Assert.Contains(report.Capabilities, capability => capability.ActionKind == AudioAssistActionKind.BoostGuide && capability.Status == AudioAssistCapabilityStatus.Available);
        Assert.Contains(report.Capabilities, capability => capability.ActionKind == AudioAssistActionKind.InspectVolume && capability.Status == AudioAssistCapabilityStatus.Available);
        Assert.Contains(report.Capabilities, capability => capability.ActionKind == AudioAssistActionKind.SetVolume && capability.ApprovalRequirement == ApprovalRequirement.BeforeExecution);
    }

    [Fact]
    public void BuildStatusReport_BlocksExecuteMode()
    {
        var tempRoot = CreateTempRoot();
        var artifactRoot = Path.Combine(tempRoot, "vnext", "artifacts", "pet-tasks", "20260505-145000-audio-assist");

        var result = _adapter.BuildStatusReport(BuildRequest(artifactRoot, TaskAdapterRunMode.Execute));

        Assert.Equal(TaskAdapterResultStatus.Blocked, result.Status);
        Assert.False(result.DidMutate);
        Assert.Contains("dry-run report", result.BlockReason);
    }

    private static TaskAdapterRequest BuildRequest(
        string artifactRoot,
        TaskAdapterRunMode runMode = TaskAdapterRunMode.DryRunPreview)
    {
        var intent = new TaskIntent(
            Guid.Parse("c5000000-0000-0000-0000-000000000001"),
            "Nix, check audio volume",
            TaskIntentTargetMode.ExplicitPetName,
            TargetPetNameSnapshot: "Nix",
            TaskKind: TaskKind.AudioAssist,
            RequestedToolFamily: "audioAssist");
        var policy = new ToolPolicy(
            "audio-assist-readonly",
            "audioAssist",
            ToolAccessMode.ReadOnly,
            ToolRiskLevel.Low,
            ApprovalRequirement.None);

        return new TaskAdapterRequest(
            Guid.Parse("d5000000-0000-0000-0000-000000000001"),
            intent,
            policy,
            runMode,
            artifactRoot);
    }

    private static string CreateTempRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "wevito-audio-assist-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }

    private sealed class FakeAudioEndpointStatusReader(AudioEndpointStatus status) : IAudioEndpointStatusReader
    {
        public AudioEndpointStatus ReadDefaultRenderEndpoint(DateTimeOffset inspectedAtUtc)
        {
            return status with { InspectedAtUtc = inspectedAtUtc };
        }
    }
}
