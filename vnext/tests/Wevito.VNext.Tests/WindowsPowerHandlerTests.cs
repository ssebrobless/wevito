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
}
