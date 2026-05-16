using System.Text.Json;
using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class DoNotDisturbScheduleServiceTests
{
    [Fact]
    public void ScheduleWindowHonored()
    {
        var schedule = JsonSerializer.Serialize(new[]
        {
            new DoNotDisturbWindow("09:00", "17:00")
        });
        var settings = new Dictionary<string, string>
        {
            [DoNotDisturbScheduleService.EnabledSetting] = bool.TrueString,
            [DoNotDisturbScheduleService.ScheduleSetting] = schedule
        };
        var service = new DoNotDisturbScheduleService();

        var state = service.Evaluate(settings, DateTimeOffset.Parse("2026-05-15T12:00:00Z"));

        Assert.True(state.IsActive);
        Assert.Contains("schedule", state.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void QuickToggleOverrides()
    {
        var service = new DoNotDisturbScheduleService();

        var state = service.ApplyQuickToggle(new Dictionary<string, string>(), DoNotDisturbQuickToggle.OneHour, Now);

        Assert.True(state.IsActive);
        Assert.True(state.SettingsSnapshot.ContainsKey(DoNotDisturbScheduleService.QuickToggleUntilUtcSetting));
    }

    [Fact]
    public void UntilTomorrowClearsAtMidnight()
    {
        var service = new DoNotDisturbScheduleService();
        var start = DateTimeOffset.Parse("2026-05-15T22:00:00Z");
        var state = service.ApplyQuickToggle(new Dictionary<string, string>(), DoNotDisturbQuickToggle.UntilTomorrow, start);

        var beforeMidnight = service.Evaluate(state.SettingsSnapshot, DateTimeOffset.Parse("2026-05-15T23:59:00Z"));
        var afterMidnight = service.Evaluate(state.SettingsSnapshot, DateTimeOffset.Parse("2026-05-16T00:01:00Z"));

        Assert.True(beforeMidnight.IsActive);
        Assert.False(afterMidnight.IsActive);
    }

    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-05-15T12:00:00Z");
}

