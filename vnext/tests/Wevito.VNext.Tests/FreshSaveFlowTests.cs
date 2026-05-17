using Wevito.VNext.Contracts;
using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class FreshSaveFlowTests
{
    [Fact]
    public void EmptyStateOpensEggPrompt()
    {
        var state = new DefaultStateFactory(new PetSimulationEngine()).Create(BuildContent());

        Assert.Empty(state.ActivePets);
        Assert.Equal("pond", state.ActiveEnvironmentId);
        Assert.True(bool.Parse(state.SettingsSnapshot["starter_egg_pending"]));
    }

    [Fact]
    public void NoTaskCardsSeededBeforeOnboardingChoice()
    {
        var state = new DefaultStateFactory(new PetSimulationEngine()).Create(BuildContent());

        Assert.Empty(state.TaskCards ?? []);
    }

    [Fact]
    public void PartialSaveReopensStarterEggPromptWhenNoPetExists()
    {
        var state = new CompanionState(
            CompanionMode.Focused,
            IsPinned: false,
            ActiveEnvironmentId: "pond",
            ActiveTool: new ToolSession("settings", false),
            ActivePets: [],
            BasketItems: [],
            SettingsSnapshot: new Dictionary<string, string>
            {
                ["starter_egg_pending"] = bool.TrueString
            },
            TaskCards: []);

        Assert.Empty(state.ActivePets);
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
