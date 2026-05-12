using System.Text.Json;
using Wevito.VNext.Contracts;
using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class AudioAssistExecutionAdapterTests
{
    [Fact]
    public void Execute_SetsNormalWindowsVolumeWithApprovalGate()
    {
        var tempRoot = CreateTempRoot();
        var artifactRoot = Path.Combine(tempRoot, "vnext", "artifacts", "pet-tasks", "20260505-151000-audio-assist-execute");
        var controller = new FakeAudioEndpointController(volumePercent: 25, isMuted: false);
        var adapter = new AudioAssistExecutionAdapter(controller);

        var result = adapter.Execute(BuildRequest(artifactRoot, "Nix, set volume to 40%"));

        Assert.Equal(TaskAdapterResultStatus.Completed, result.Status);
        Assert.True(result.DidMutate);
        Assert.Equal(40, controller.VolumePercent);
        Assert.False(controller.IsMuted);
        Assert.Contains(result.WrittenPaths ?? [], path => path.EndsWith("audio-assist-execution-report.json", StringComparison.OrdinalIgnoreCase));

        var report = JsonSerializer.Deserialize<AudioAssistExecutionReport>(
            File.ReadAllText(Path.Combine(artifactRoot, "audio-assist-execution-report.json")),
            JsonDefaults.Options);
        Assert.NotNull(report);
        Assert.Equal(AudioAssistActionKind.SetVolume, report.ActionKind);
        Assert.Equal(40, report.RequestedVolumePercent);
        Assert.Equal(25, report.BeforeStatus?.MasterVolumePercent);
        Assert.Equal(40, report.AfterStatus?.MasterVolumePercent);
        Assert.True(report.DidChangeAudio);
        Assert.False(report.DidMutateFiles);

        var markdown = File.ReadAllText(Path.Combine(artifactRoot, "run-summary.md"));
        Assert.Contains("normal Windows endpoint volume/mute only", markdown);
        Assert.Contains("no booster, APO, driver, enhancer, or config file changes", markdown);
    }

    [Fact]
    public void Execute_MutesAndUnmutesWithApprovalGate()
    {
        var tempRoot = CreateTempRoot();
        var controller = new FakeAudioEndpointController(volumePercent: 35, isMuted: false);
        var adapter = new AudioAssistExecutionAdapter(controller);

        var mute = adapter.Execute(BuildRequest(
            Path.Combine(tempRoot, "vnext", "artifacts", "pet-tasks", "20260505-151000-audio-assist-mute"),
            "Nix, mute audio"));
        var unmute = adapter.Execute(BuildRequest(
            Path.Combine(tempRoot, "vnext", "artifacts", "pet-tasks", "20260505-151001-audio-assist-unmute"),
            "Nix, unmute audio"));

        Assert.Equal(TaskAdapterResultStatus.Completed, mute.Status);
        Assert.Equal(TaskAdapterResultStatus.Completed, unmute.Status);
        Assert.False(controller.IsMuted);
    }

    [Fact]
    public void Execute_BlocksBoostAndOutOfRangeVolume()
    {
        var tempRoot = CreateTempRoot();
        var adapter = new AudioAssistExecutionAdapter(new FakeAudioEndpointController(volumePercent: 25, isMuted: false));

        var boost = adapter.Execute(BuildRequest(
            Path.Combine(tempRoot, "vnext", "artifacts", "pet-tasks", "20260505-151000-audio-assist-boost"),
            "Nix, boost volume over 100"));
        var tooHigh = adapter.Execute(BuildRequest(
            Path.Combine(tempRoot, "vnext", "artifacts", "pet-tasks", "20260505-151001-audio-assist-high"),
            "Nix, set volume to 140%"));

        Assert.Equal(TaskAdapterResultStatus.Blocked, boost.Status);
        Assert.Contains("boost beyond normal", boost.BlockReason);
        Assert.Equal(TaskAdapterResultStatus.Blocked, tooHigh.Status);
        Assert.Contains("between 0% and 100%", tooHigh.BlockReason);
    }

    [Fact]
    public void Execute_BlocksWithoutWriteApprovalPolicy()
    {
        var tempRoot = CreateTempRoot();
        var adapter = new AudioAssistExecutionAdapter(new FakeAudioEndpointController(volumePercent: 25, isMuted: false));
        var request = BuildRequest(
            Path.Combine(tempRoot, "vnext", "artifacts", "pet-tasks", "20260505-151000-audio-assist-readonly"),
            "Nix, set volume to 40%",
            policy: new ToolPolicy("audio-assist-readonly", "audioAssist", ToolAccessMode.ReadOnly, ToolRiskLevel.Low, ApprovalRequirement.None));

        var result = adapter.Execute(request);

        Assert.Equal(TaskAdapterResultStatus.Blocked, result.Status);
        Assert.Contains("approval-gated write policy", result.BlockReason);
    }

    private static TaskAdapterRequest BuildRequest(string artifactRoot, string rawText, ToolPolicy? policy = null)
    {
        var intent = new TaskIntent(
            Guid.Parse("c5000000-0000-0000-0000-000000000002"),
            rawText,
            TaskIntentTargetMode.ExplicitPetName,
            TargetPetNameSnapshot: "Nix",
            TaskKind: TaskKind.AudioAssist,
            RequestedToolFamily: "audioAssist");

        return new TaskAdapterRequest(
            Guid.Parse("d5000000-0000-0000-0000-000000000002"),
            intent,
            policy ?? new ToolPolicy("audio-assist-write-approval", "audioAssist", ToolAccessMode.Write, ToolRiskLevel.Medium, ApprovalRequirement.BeforeExecution),
            TaskAdapterRunMode.Execute,
            artifactRoot);
    }

    private static string CreateTempRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "wevito-audio-assist-execution-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }

    private sealed class FakeAudioEndpointController(double volumePercent, bool isMuted) : IAudioEndpointController
    {
        public double VolumePercent { get; private set; } = volumePercent;

        public bool IsMuted { get; private set; } = isMuted;

        public AudioEndpointStatus ReadDefaultRenderEndpoint(DateTimeOffset inspectedAtUtc)
        {
            return BuildStatus(inspectedAtUtc);
        }

        public AudioEndpointStatus SetDefaultRenderVolume(double volumePercent, DateTimeOffset changedAtUtc)
        {
            VolumePercent = volumePercent;
            return BuildStatus(changedAtUtc);
        }

        public AudioEndpointStatus SetDefaultRenderMute(bool isMuted, DateTimeOffset changedAtUtc)
        {
            IsMuted = isMuted;
            return BuildStatus(changedAtUtc);
        }

        private AudioEndpointStatus BuildStatus(DateTimeOffset timestamp)
        {
            return new AudioEndpointStatus(
                "fake test endpoint",
                IsAvailable: true,
                MasterVolumePercent: VolumePercent,
                IsMuted: IsMuted,
                EndpointId: "fake-endpoint-id",
                Detail: $"Fake endpoint volume is {VolumePercent}% and mute is {IsMuted}.",
                timestamp);
        }
    }
}
