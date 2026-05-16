using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class FirstLaunchWizardTests
{
    [Fact]
    public void RunsOnceOnly()
    {
        var service = new FirstLaunchWizardStateService();
        var settings = service.CompleteWizard(new Dictionary<string, string>());

        Assert.False(service.ShouldRun(settings));
    }

    [Fact]
    public void EachStepCompletesIndependently()
    {
        var service = new FirstLaunchWizardStateService();
        var settings = new Dictionary<string, string>();

        settings = new Dictionary<string, string>(service.CompleteIdentityStep(settings, "Wisp"));
        Assert.Equal(bool.TrueString, settings["first_launch_step_1_completed"]);
        Assert.Equal("Wisp", settings[AiIdentityService.AiIdentityNameSetting]);

        settings = new Dictionary<string, string>(service.CompleteAgentNamesStep(settings, ["Scout", "Inspector", "Builder"]));
        Assert.Equal(bool.TrueString, settings["first_launch_step_2_completed"]);
        Assert.Equal("Scout", settings[FirstLaunchWizardStateService.BuildAgentSlotNameSetting(0)]);
    }

    [Fact]
    public void SeedDefaultRegistryOption2()
    {
        var service = new FirstLaunchWizardStateService();

        var settings = service.CompleteBackgroundChoiceStep(
            new Dictionary<string, string>(),
            FirstLaunchBackgroundChoice.HelpWithSpriteCleanup);

        Assert.Equal(
            FirstLaunchWizardStateService.SpriteTemplateCandidateGenerationSeed,
            settings[FirstLaunchWizardStateService.ExperimentRegistrySeedSetting]);
        Assert.Equal(bool.TrueString, settings["first_launch_step_3_completed"]);
    }

    [Fact]
    public void WritesFirstLaunchCompletedPacket()
    {
        var root = Path.Combine(Path.GetTempPath(), "wevito-first-launch-tests", Guid.NewGuid().ToString("N"));
        var ledger = new AuditLedgerService(Path.Combine(root, "ledger.sqlite"));
        var service = new FirstLaunchWizardStateService(auditLedgerService: ledger);
        var timestamp = DateTimeOffset.Parse("2026-05-15T12:00:00Z");

        var settings = service.CompleteFirstChatStep(new Dictionary<string, string>(), timestamp);

        Assert.Equal(bool.TrueString, settings[FirstLaunchWizardStateService.CompletedSetting]);
        var rows = ledger.Snapshot(timestamp.AddMinutes(-1), timestamp.AddMinutes(1));
        Assert.Contains(rows, row => row.PacketKind == "first_launch_step_completed");
        Assert.Contains(rows, row => row.PacketKind == "first_launch_completed");
    }
}
