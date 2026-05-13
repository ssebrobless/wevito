using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class WindowsPowerHandlerTests
{
    [Theory]
    [InlineData(WindowsPowerRuntimeEvent.Sleep)]
    [InlineData(WindowsPowerRuntimeEvent.Lock)]
    public void ApplyRuntimeEvent_SleepOrLockForcesQuiet(WindowsPowerRuntimeEvent runtimeEvent)
    {
        var handler = new WindowsPowerHandler();
        var settings = new Dictionary<string, string>
        {
            [RuntimeSupervisorService.QuietModeSetting] = bool.FalseString,
            [RuntimeSupervisorService.BackgroundWorkAllowedSetting] = bool.TrueString
        };

        var result = handler.ApplyRuntimeEvent(settings, runtimeEvent, DateTimeOffset.Parse("2026-05-13T12:00:00Z"));

        Assert.True(result.ForcedQuiet);
        Assert.Equal(bool.TrueString, result.SettingsSnapshot[RuntimeSupervisorService.QuietModeSetting]);
        Assert.Equal(bool.FalseString, result.SettingsSnapshot[RuntimeSupervisorService.BackgroundWorkAllowedSetting]);
    }

    [Theory]
    [InlineData(WindowsPowerRuntimeEvent.Resume)]
    [InlineData(WindowsPowerRuntimeEvent.Unlock)]
    public void ApplyRuntimeEvent_ResumeOrUnlockDoesNotAutoSwitchToActive(WindowsPowerRuntimeEvent runtimeEvent)
    {
        var handler = new WindowsPowerHandler();
        var settings = new Dictionary<string, string>
        {
            [RuntimeSupervisorService.QuietModeSetting] = bool.TrueString
        };

        var result = handler.ApplyRuntimeEvent(settings, runtimeEvent, DateTimeOffset.Parse("2026-05-13T12:00:00Z"));

        Assert.False(result.ForcedQuiet);
        Assert.True(result.ResumedWithoutAutoActive);
        Assert.Equal(bool.TrueString, result.SettingsSnapshot[RuntimeSupervisorService.QuietModeSetting]);
    }

    [Theory]
    [InlineData(WindowsPowerRuntimeEvent.Sleep, "power_sleep")]
    [InlineData(WindowsPowerRuntimeEvent.Resume, "power_resume")]
    [InlineData(WindowsPowerRuntimeEvent.Lock, "session_lock")]
    [InlineData(WindowsPowerRuntimeEvent.Unlock, "session_unlock")]
    public void ApplyRuntimeEvent_RecordsCompletedEvidenceRows(WindowsPowerRuntimeEvent runtimeEvent, string packetKind)
    {
        var root = Path.Combine(Path.GetTempPath(), "wevito-power-handler-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var ledger = new AuditLedgerService(Path.Combine(root, "ledger.sqlite"));
        var handler = new WindowsPowerHandler(ledger);
        var now = DateTimeOffset.Parse("2026-05-13T12:00:00Z");

        handler.ApplyRuntimeEvent(new Dictionary<string, string>(), runtimeEvent, now);

        var row = Assert.Single(ledger.Snapshot(now.AddMinutes(-1), now.AddMinutes(1)));
        Assert.Equal(packetKind, row.PacketKind);
        Assert.Equal("Completed", row.Status);
        Assert.Empty(row.Error);
    }
}
