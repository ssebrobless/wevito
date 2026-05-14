using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed class DefaultStateFactory
{
    private readonly PetSimulationEngine _petSimulationEngine;

    public DefaultStateFactory(PetSimulationEngine petSimulationEngine)
    {
        _petSimulationEngine = petSimulationEngine;
    }

    public CompanionState Create(GameContent content)
    {
        var environment = content.Species.FirstOrDefault()?.DefaultEnvironmentId
            ?? content.Environments.FirstOrDefault()?.Id
            ?? "rat";
        return new CompanionState(
            CompanionMode.Focused,
            false,
            environment,
            new ToolSession("settings", false),
            [],
            [],
            new Dictionary<string, string>
            {
                ["theme"] = "slate",
                ["roamBandHeight"] = "112",
                ["starter_egg_pending"] = bool.TrueString
            },
            []);
    }
}
