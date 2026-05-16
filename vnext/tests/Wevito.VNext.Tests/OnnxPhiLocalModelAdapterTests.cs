using Wevito.VNext.Contracts;
using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class OnnxPhiLocalModelAdapterTests
{
    [Fact]
    public async Task SuggestAsync_DefaultFeatureFlagOffFallsBackDeterministic()
    {
        var adapter = new OnnxPhiLocalModelAdapter(
            modelFolder: NewTempDirectory(),
            settingsProvider: () => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));

        var response = await adapter.SuggestAsync(BuildRequest());

        Assert.False(adapter.IsFeatureEnabled);
        Assert.False(response.DidCallProvider);
        Assert.Equal(OnnxPhiLocalModelAdapter.Provider, response.Provider);
        Assert.Contains("local_runtime_inproc_enabled=false", response.BlockReason);
        Assert.Contains("No hosted model call was made", response.Summary);
    }

    [Fact]
    public async Task SuggestAsync_FeatureOnButWeightsMissingFallsBackDeterministic()
    {
        var adapter = new OnnxPhiLocalModelAdapter(
            modelFolder: NewTempDirectory(),
            settingsProvider: () => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [ModelProviderModeService.InProcessLocalRuntimeEnabledSetting] = bool.TrueString
            });

        var response = await adapter.SuggestAsync(BuildRequest());

        Assert.True(adapter.IsFeatureEnabled);
        Assert.False(adapter.HasWeights);
        Assert.False(response.DidCallProvider);
        Assert.Contains("weights are missing", response.BlockReason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SuggestAsync_FeatureOnWithWeightsStillDefersRealInference()
    {
        var folder = NewTempDirectory();
        File.WriteAllText(Path.Combine(folder, "model.onnx"), "stub-weights-marker");
        var adapter = new OnnxPhiLocalModelAdapter(
            modelFolder: folder,
            settingsProvider: () => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [ModelProviderModeService.InProcessLocalRuntimeEnabledSetting] = bool.TrueString
            });

        var response = await adapter.SuggestAsync(BuildRequest());

        Assert.True(adapter.HasWeights);
        Assert.False(response.DidCallProvider);
        Assert.Contains("deferred", response.BlockReason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SuggestAsync_KillSwitchBlocksBeforeWeights()
    {
        var killSwitch = new KillSwitchService(() => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [KillSwitchService.KillSwitchSetting] = bool.TrueString
        });
        var adapter = new OnnxPhiLocalModelAdapter(
            modelFolder: NewTempDirectory(),
            settingsProvider: () => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [ModelProviderModeService.InProcessLocalRuntimeEnabledSetting] = bool.TrueString
            },
            killSwitchService: killSwitch);

        var response = await adapter.SuggestAsync(BuildRequest());

        Assert.False(response.DidCallProvider);
        Assert.Equal("kill_switch=true", response.BlockReason);
    }

    private static ModelRequest BuildRequest()
    {
        return new ModelRequest(
            Guid.Parse("79000000-0000-0000-0000-000000000001"),
            "goose 1",
            "ResearchAgent",
            "localResearch",
            "research local runtime setup",
            "preview",
            TrustedContext: ["docs/runtime.md"],
            UntrustedContext: ["user request"],
            ApprovedForModelCall: true,
            ArtifactRoot: Path.Combine(Path.GetTempPath(), "wevito-onnxphi-tests", Guid.NewGuid().ToString("N")),
            RequestedAtUtc: DateTimeOffset.Parse("2026-05-13T12:00:00Z"));
    }

    private static string NewTempDirectory()
    {
        var folder = Path.Combine(Path.GetTempPath(), "wevito-onnxphi-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        return folder;
    }
}
