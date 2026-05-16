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
                ["starter_egg_pending"] = bool.TrueString,
                [ModelProviderModeService.ProviderModeSetting] = ModelProviderModeService.LocalOnlyModeValue,
                [ModelProviderModeService.LocalProviderIdSetting] = ModelProviderModeService.DefaultLocalProviderId,
                [ModelProviderModeService.LocalProviderAvailableSetting] = bool.FalseString,
                [ModelProviderModeService.InProcessLocalRuntimeEnabledSetting] = bool.FalseString,
                [ModelProviderModeService.LocalRuntimeEndpointSetting] = LocalRuntimeProbeService.DefaultOllamaEndpoint,
                [ModelProviderModeService.LocalRuntimeModelSetting] = LocalRuntimeProbeService.DefaultOllamaModel,
                [ModelProviderModeService.HostedProviderIdSetting] = "none",
                [ModelProviderModeService.HostedProviderApprovedSetting] = bool.FalseString,
                [CoexistenceTriggerService.FullscreenEnabledSetting] = bool.TrueString,
                [CoexistenceTriggerService.AppListEnabledSetting] = bool.TrueString,
                [CoexistenceTriggerService.CpuEnabledSetting] = bool.TrueString,
                [CoexistenceTriggerService.NetworkEnabledSetting] = bool.TrueString,
                [CoexistenceTriggerService.CpuThresholdSetting] = "80",
                [CoexistenceTriggerService.NetworkThresholdSetting] = "80",
                [DoNotDisturbScheduleService.EnabledSetting] = bool.FalseString,
                [DoNotDisturbScheduleService.ScheduleSetting] = "[]",
                [DoNotDisturbScheduleService.QuickToggleUntilUtcSetting] = ""
            },
            []);
    }
}
