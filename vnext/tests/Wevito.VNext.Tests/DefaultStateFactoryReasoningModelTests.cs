using Wevito.VNext.Contracts;
using Wevito.VNext.Core;
using Wevito.VNext.Shell;

namespace Wevito.VNext.Tests;

public sealed class DefaultStateFactoryReasoningModelTests
{
    [Fact]
    public void ModelProviderModeDefaultsToLocalOnly()
    {
        var settings = ShellCoordinator.ApplyDefaultSettings(new Dictionary<string, string>());
        var parsed = new ModelProviderModeService().ReadSettings(settings);

        Assert.Equal(ModelProviderMode.LocalOnly, parsed.Mode);
        Assert.Equal("ollama", parsed.LocalProviderId);
        Assert.False(parsed.HostedProviderApproved);
    }

    [Fact]
    public void DefaultEndpointIsLoopback()
    {
        var state = new DefaultStateFactory(new PetSimulationEngine()).Create(BuildContent());

        Assert.Equal("http://127.0.0.1:11434", state.SettingsSnapshot[ModelProviderModeService.LocalRuntimeEndpointSetting]);
    }

    [Fact]
    public void DefaultModelIsQwen25_7b()
    {
        var state = new DefaultStateFactory(new PetSimulationEngine()).Create(BuildContent());

        Assert.Equal("qwen2.5:7b-instruct-q4_K_M", state.SettingsSnapshot[ModelProviderModeService.LocalRuntimeModelSetting]);
    }

    private static GameContent BuildContent()
    {
        return new GameContent(
            [new SpeciesDefinition("goose", "Goose", "#ffffff", 90, "pond")],
            [new ActionDefinition("feed", "Feed", "Feed pets")],
            [new EnvironmentDefinition("pond", "Pond", "#223344", "#112233")],
            [],
            [],
            [],
            [],
            []);
    }
}
