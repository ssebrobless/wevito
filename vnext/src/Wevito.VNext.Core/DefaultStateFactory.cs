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
        var environment = content.Environments.FirstOrDefault()?.Id ?? "corner-garden";
        return new CompanionState(
            CompanionMode.Focused,
            false,
            environment,
            new ToolSession("basket", false),
            _petSimulationEngine.CreateDefaultPets(content),
            [],
            new Dictionary<string, string>
            {
                ["theme"] = "slate",
                ["roamBandHeight"] = "112"
            });
    }
}
