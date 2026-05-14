using Wevito.VNext.Contracts;
using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class DefaultStateFactoryTests
{
    [Fact]
    public void Create_StartsFreshSaveAtStarterEggChoice()
    {
        var state = new DefaultStateFactory(new PetSimulationEngine()).Create(BuildContent());

        Assert.Empty(state.ActivePets);
        Assert.Equal("pond", state.ActiveEnvironmentId);
        Assert.True(bool.Parse(state.SettingsSnapshot["starter_egg_pending"]));
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
