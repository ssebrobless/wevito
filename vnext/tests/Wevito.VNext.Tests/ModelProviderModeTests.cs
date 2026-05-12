using Wevito.VNext.Core;
using Wevito.VNext.Shell;

namespace Wevito.VNext.Tests;

public sealed class ModelProviderModeTests
{
    [Fact]
    public void ApplyDefaultSettings_DisablesProviderAndHostedApproval()
    {
        var settings = ShellCoordinator.ApplyDefaultSettings(new Dictionary<string, string>());
        var service = new ModelProviderModeService();

        var parsed = service.ReadSettings(settings);

        Assert.Equal(ModelProviderMode.Disabled, parsed.Mode);
        Assert.False(parsed.HostedProviderApproved);
        Assert.False(parsed.LocalProviderAvailable);
        Assert.Equal("deterministic-local", parsed.LocalProviderId);
        Assert.Equal("none", parsed.HostedProviderId);
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
}
