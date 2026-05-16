using Wevito.VNext.Core;
using Wevito.VNext.Shell;

namespace Wevito.VNext.Tests;

public sealed class ModelProviderModeTests
{
    [Fact]
    public void ApplyDefaultSettings_UsesLocalOnlyProviderAndBlocksHostedApproval()
    {
        var settings = ShellCoordinator.ApplyDefaultSettings(new Dictionary<string, string>());
        var service = new ModelProviderModeService();

        var parsed = service.ReadSettings(settings);

        Assert.Equal(ModelProviderMode.LocalOnly, parsed.Mode);
        Assert.False(parsed.HostedProviderApproved);
        Assert.False(parsed.LocalProviderAvailable);
        Assert.Equal("ollama", parsed.LocalProviderId);
        Assert.Equal("none", parsed.HostedProviderId);
        Assert.False(parsed.InProcessLocalRuntimeEnabled);
    }

    [Fact]
    public void CanUseHostedProvider_RequiresApprovedCloudAndExplicitApproval()
    {
        var service = new ModelProviderModeService();
        var settings = new ModelProviderSettings(
            ModelProviderMode.ApprovedCloud,
            HostedProviderApproved: false,
            LocalProviderAvailable: true,
            LocalProviderId: "deterministic-local",
            HostedProviderId: "anthropic");

        var allowed = service.CanUseHostedProvider(settings, out var reason);

        Assert.False(allowed);
        Assert.Contains("explicit approval", reason);
    }

    [Fact]
    public void CanUseLocalProvider_ReportsUnavailableLocalRuntime()
    {
        var service = new ModelProviderModeService();
        var settings = new ModelProviderSettings(
            ModelProviderMode.LocalOnly,
            HostedProviderApproved: false,
            LocalProviderAvailable: false,
            LocalProviderId: "deterministic-local",
            HostedProviderId: "none");

        var allowed = service.CanUseLocalProvider(settings, out var reason);

        Assert.False(allowed);
        Assert.Contains("deterministic local fallback", reason);
    }

    [Fact]
    public void DecideRoute_DisabledReturnsNoAdapter()
    {
        var service = new ModelProviderModeService();
        var settings = new ModelProviderSettings(
            ModelProviderMode.Disabled,
            HostedProviderApproved: false,
            LocalProviderAvailable: false,
            LocalProviderId: "deterministic-local",
            HostedProviderId: "none");

        var route = service.DecideRoute(settings);

        Assert.Equal(ModelProviderRoute.Disabled, route.Route);
        Assert.False(route.DidSelectHostedProvider);
    }

    [Fact]
    public void DecideRoute_LocalOnlyAvailableProbeRoutesOllama()
    {
        var service = new ModelProviderModeService();
        var settings = new ModelProviderSettings(
            ModelProviderMode.LocalOnly,
            HostedProviderApproved: false,
            LocalProviderAvailable: true,
            LocalProviderId: "deterministic-local",
            HostedProviderId: "none");
        var probe = new LocalRuntimeProbeResult(
            IsAvailable: true,
            WasDormant: false,
            RuntimeId: "ollama",
            Endpoint: LocalRuntimeProbeService.DefaultOllamaEndpoint,
            Model: LocalRuntimeProbeService.DefaultOllamaModel,
            Reason: "ok",
            ProbedAtUtc: DateTimeOffset.Parse("2026-05-13T12:00:00Z"));

        var route = service.DecideRoute(settings, probe);

        Assert.Equal(ModelProviderRoute.Ollama, route.Route);
        Assert.Equal("ollama", route.ProviderId);
        Assert.False(route.DidSelectHostedProvider);
    }

    [Fact]
    public void DecideRoute_LocalOnlyInProcessWeightsRoutesOnnxPhi()
    {
        var service = new ModelProviderModeService();
        var settings = new ModelProviderSettings(
            ModelProviderMode.LocalOnly,
            HostedProviderApproved: false,
            LocalProviderAvailable: false,
            LocalProviderId: "deterministic-local",
            HostedProviderId: "none",
            InProcessLocalRuntimeEnabled: true);

        var route = service.DecideRoute(settings, probeResult: null, onnxPhiWeightsPresent: true);

        Assert.Equal(ModelProviderRoute.OnnxPhi, route.Route);
        Assert.Equal("onnx-phi", route.ProviderId);
        Assert.False(route.DidSelectHostedProvider);
    }

    [Fact]
    public void DecideRoute_LocalOnlyWithoutRuntimeFallsBackDeterministic()
    {
        var service = new ModelProviderModeService();
        var settings = new ModelProviderSettings(
            ModelProviderMode.LocalOnly,
            HostedProviderApproved: false,
            LocalProviderAvailable: false,
            LocalProviderId: "deterministic-local",
            HostedProviderId: "none");

        var route = service.DecideRoute(settings);

        Assert.Equal(ModelProviderRoute.DeterministicLocal, route.Route);
        Assert.False(route.DidSelectHostedProvider);
    }

    [Fact]
    public void DecideRoute_ApprovedCloudStaysBlockedInThisPhase()
    {
        var service = new ModelProviderModeService();
        var settings = new ModelProviderSettings(
            ModelProviderMode.ApprovedCloud,
            HostedProviderApproved: true,
            LocalProviderAvailable: true,
            LocalProviderId: "ollama",
            HostedProviderId: "anthropic");

        var route = service.DecideRoute(settings);

        Assert.Equal(ModelProviderRoute.HostedCloudBlocked, route.Route);
        Assert.False(route.DidSelectHostedProvider);
    }
}
