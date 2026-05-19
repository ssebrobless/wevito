using Wevito.VNext.Core;
using Wevito.VNext.Core.Audit;
using Wevito.VNext.Core.SelfImprovement;
using Wevito.VNext.Core.SelfImprovement.Invariants;
using Wevito.VNext.Core.Settings;

namespace Wevito.VNext.Tests;

public sealed class CapabilityFlagInventoryTests
{
    [Fact]
    public void Entries_DefaultValuesAreOnlyFalseOrEmpty()
    {
        Assert.All(CapabilityFlagInventory.Entries, entry =>
        {
            Assert.True(
                string.Equals(entry.DefaultValue, bool.FalseString, StringComparison.Ordinal) ||
                string.Equals(entry.DefaultValue, "", StringComparison.Ordinal),
                $"Capability flag {entry.Name} has unsupported default '{entry.DefaultValue}'.");
        });
    }

    [Fact]
    public void Entries_DoNotDuplicateNames()
    {
        var duplicates = CapabilityFlagInventory.Entries
            .GroupBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();

        Assert.Empty(duplicates);
    }

    [Fact]
    public void Entries_ContainKnownAutonomyAndModelGates()
    {
        var names = CapabilityFlagInventory.Entries
            .Select(entry => entry.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Contains(KillSwitchService.KillSwitchSetting, names);
        Assert.Contains(AutonomousOperationsConfig.EnabledSetting, names);
        Assert.Contains(AutonomousScopeService.BuildEnabledSettingKey(AutonomousScopeService.SpriteRepairTriageScopeId), names);
        Assert.Contains(AutonomousScopeService.BuildEnabledSettingKey(AutonomousScopeService.AuditLedgerCleanupScopeId), names);
        Assert.Contains(AutonomousScopeService.BuildEnabledSettingKey(AutonomousScopeService.SpriteRepairBatchProposalScopeId), names);
        Assert.Contains(SupervisedImprovementLoopSettings.EnabledSetting, names);
        Assert.Contains(InvariantViolationWatchdog.EnabledSetting, names);
        Assert.Contains(AutonomousTaskScheduler.SchedulerEnabledSetting, names);
        Assert.Contains(AutonomousTaskScheduler.SchedulerPreviewDispatchApprovedSetting, names);
        Assert.Contains(CapabilityFlagInventory.PetModelAdapterEnabledSetting, names);
        Assert.Contains(CapabilityFlagInventory.PetModelFirstCallApprovedSetting, names);
        Assert.Contains(ModelProviderModeService.HostedProviderApprovedSetting, names);
        Assert.Contains(ModelProviderModeService.LocalProviderAvailableSetting, names);
        Assert.Contains(ModelProviderModeService.InProcessLocalRuntimeEnabledSetting, names);
        Assert.Contains(WebResearchConnector.WebSearchEnabledSetting, names);
        Assert.Contains(SettingKeys.LocalDocumentRetrievalEnabled, names);
        Assert.Contains(CapabilityFlagInventory.LocalFileReadEnabledSetting, names);
        Assert.Contains(CapabilityFlagInventory.LocalToolExecutionEnabledSetting, names);
    }
}
