using System.Text.Json;
using Wevito.VNext.Contracts;
using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class AudioBoostHandoffAdapterTests
{
    [Fact]
    public void BuildSetupGuide_DetectsInstalledAndPartialStatesWithoutMutation()
    {
        var tempRoot = CreateTempRoot();
        var artifactRoot = Path.Combine(tempRoot, "vnext", "artifacts", "pet-tasks", "20260507-audio-boost-handoff");
        var adapter = new AudioBoostHandoffAdapter(new FakeAudioBoostEnvironment(
            files: [@"C:\Program Files\EqualizerAPO\config\config.txt"],
            directories: [@"C:\Program Files\FxSound", @"C:\Program Files\EqualizerAPO"],
            processes: ["fxsound"],
            registryDisplayNames: ["FxSound 1.1"]));

        var result = adapter.BuildSetupGuide(BuildRequest(artifactRoot), DateTimeOffset.Parse("2026-05-07T00:00:00Z"));

        Assert.Equal(TaskAdapterResultStatus.PreviewReady, result.Status);
        Assert.False(result.DidMutate);
        Assert.Contains(result.WrittenPaths ?? [], path => path.EndsWith("setup-guide.md", StringComparison.OrdinalIgnoreCase));

        var report = JsonSerializer.Deserialize<AudioBoostHandoffReport>(
            File.ReadAllText(Path.Combine(artifactRoot, "audio-boost-handoff-report.json")),
            JsonDefaults.Options);
        Assert.NotNull(report);
        Assert.False(report.DidInstallOrConfigure);
        Assert.False(report.DidMutate);
        Assert.Equal(AudioBoostDetectionStatus.Installed, report.Tools.Single(tool => tool.ToolName == "FxSound").Status);
        Assert.Equal(AudioBoostDetectionStatus.Installed, report.Tools.Single(tool => tool.ToolName == "Equalizer APO").Status);

        var guide = File.ReadAllText(Path.Combine(artifactRoot, "setup-guide.md"));
        Assert.Contains("WHO", guide);
        Assert.Contains("-1 dBTP", guide);
        Assert.Contains("Wevito will not edit", guide);
    }

    [Fact]
    public void BuildSetupGuide_ReportsPartialEqualizerApoDirectory()
    {
        var tempRoot = CreateTempRoot();
        var artifactRoot = Path.Combine(tempRoot, "vnext", "artifacts", "pet-tasks", "20260507-audio-boost-handoff");
        var adapter = new AudioBoostHandoffAdapter(new FakeAudioBoostEnvironment(
            files: [],
            directories: [@"C:\Program Files\EqualizerAPO"],
            processes: [],
            registryDisplayNames: []));

        adapter.BuildSetupGuide(BuildRequest(artifactRoot), DateTimeOffset.Parse("2026-05-07T00:00:00Z"));

        var report = JsonSerializer.Deserialize<AudioBoostHandoffReport>(
            File.ReadAllText(Path.Combine(artifactRoot, "audio-boost-handoff-report.json")),
            JsonDefaults.Options);
        Assert.NotNull(report);
        Assert.Equal(AudioBoostDetectionStatus.Partial, report.Tools.Single(tool => tool.ToolName == "Equalizer APO").Status);
        Assert.Equal(AudioBoostDetectionStatus.NotInstalled, report.Tools.Single(tool => tool.ToolName == "FxSound").Status);
    }

    [Fact]
    public void Dispatcher_RoutesBoostRequestToHandoffGuide()
    {
        var tempRoot = CreateTempRoot();
        var artifactRoot = Path.Combine(tempRoot, "vnext", "artifacts", "pet-tasks", "20260507-audio-boost-handoff");
        var dispatcher = new PetTaskAdapterPreviewDispatcher(
            audioBoostHandoffAdapter: new AudioBoostHandoffAdapter(new FakeAudioBoostEnvironment([], [], [], [])));

        var result = dispatcher.BuildPreview(BuildRequest(artifactRoot));

        Assert.Equal(TaskAdapterResultStatus.PreviewReady, result.Status);
        Assert.Contains(result.WrittenPaths ?? [], path => path.EndsWith("setup-guide.md", StringComparison.OrdinalIgnoreCase));
        Assert.Contains("boost handoff", result.PreviewSummary, StringComparison.OrdinalIgnoreCase);
    }

    private static TaskAdapterRequest BuildRequest(string artifactRoot)
    {
        var intent = new TaskIntent(
            Guid.Parse("c8000000-0000-0000-0000-000000000001"),
            "help me boost my PC volume",
            TaskIntentTargetMode.RouteToBestHelper,
            TargetPetId: null,
            TargetPetNameSnapshot: "",
            TaskKind: TaskKind.AudioAssist,
            RequestedToolFamily: "audioAssist");
        var policy = new ToolPolicy(
            "audio-assist-readonly",
            "audioAssist",
            ToolAccessMode.ReadOnly,
            ToolRiskLevel.Low,
            ApprovalRequirement.None);
        return new TaskAdapterRequest(
            Guid.Parse("d8000000-0000-0000-0000-000000000001"),
            intent,
            policy,
            TaskAdapterRunMode.DryRunPreview,
            artifactRoot);
    }

    private static string CreateTempRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "wevito-audio-boost-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }

    private sealed class FakeAudioBoostEnvironment : IAudioBoostEnvironment
    {
        private readonly HashSet<string> _files;
        private readonly HashSet<string> _directories;
        private readonly HashSet<string> _processes;
        private readonly IReadOnlyList<string> _registryDisplayNames;

        public FakeAudioBoostEnvironment(
            IReadOnlyList<string> files,
            IReadOnlyList<string> directories,
            IReadOnlyList<string> processes,
            IReadOnlyList<string> registryDisplayNames)
        {
            _files = new HashSet<string>(files, StringComparer.OrdinalIgnoreCase);
            _directories = new HashSet<string>(directories, StringComparer.OrdinalIgnoreCase);
            _processes = new HashSet<string>(processes, StringComparer.OrdinalIgnoreCase);
            _registryDisplayNames = registryDisplayNames;
        }

        public bool FileExists(string path) => _files.Contains(path);

        public bool DirectoryExists(string path) => _directories.Contains(path);

        public bool IsProcessRunning(string processName) => _processes.Contains(processName);

        public IReadOnlyList<string> RegistryDisplayNames() => _registryDisplayNames;
    }
}
