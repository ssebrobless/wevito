using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class RuntimeBudgetMeterTests
{
    [Fact]
    public void TryReserve_PersistsUsageAcrossRestart()
    {
        var root = BuildTempRoot();
        var path = Path.Combine(root, "budget-meter.json");
        var now = DateTimeOffset.Parse("2026-05-12T12:15:00Z");
        var budget = new RuntimeBudgetSnapshot(2, 90, 1024);

        var first = new RuntimeBudgetMeter(path, () => now, () => Snapshot(now));
        var firstReservation = first.TryReserve(budget);

        var restarted = new RuntimeBudgetMeter(path, () => now.AddMinutes(5), () => Snapshot(now.AddMinutes(5)));
        var secondReservation = restarted.TryReserve(budget);
        var thirdReservation = restarted.TryReserve(budget);

        Assert.True(firstReservation.Allowed, firstReservation.Reason);
        Assert.True(secondReservation.Allowed, secondReservation.Reason);
        Assert.False(thirdReservation.Allowed);
        Assert.Equal(2, thirdReservation.UsedThisHour);
        Assert.Contains("exhausted", thirdReservation.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TryReserve_RollsOverHourly()
    {
        var root = BuildTempRoot();
        var path = Path.Combine(root, "budget-meter.json");
        var now = DateTimeOffset.Parse("2026-05-12T12:59:00Z");
        var budget = new RuntimeBudgetSnapshot(1, 90, 1024);
        var currentTime = now;
        var meter = new RuntimeBudgetMeter(path, () => currentTime, () => Snapshot(currentTime));

        var first = meter.TryReserve(budget);
        var blocked = meter.TryReserve(budget);
        currentTime = now.AddMinutes(2);
        var afterRollover = meter.TryReserve(budget);

        Assert.True(first.Allowed, first.Reason);
        Assert.False(blocked.Allowed);
        Assert.True(afterRollover.Allowed, afterRollover.Reason);
        Assert.Equal(1, afterRollover.UsedThisHour);
    }

    [Fact]
    public void TryReserve_BlocksWhenResourceBudgetsAreExceeded()
    {
        var root = BuildTempRoot();
        var path = Path.Combine(root, "budget-meter.json");
        var now = DateTimeOffset.Parse("2026-05-12T12:15:00Z");
        var meter = new RuntimeBudgetMeter(path, () => now, () => new RuntimeResourceSnapshot(91, 512, now));

        var reservation = meter.TryReserve(new RuntimeBudgetSnapshot(2, 90, 1024));

        Assert.False(reservation.Allowed);
        Assert.Contains("CPU", reservation.Reason);
    }

    private static RuntimeResourceSnapshot Snapshot(DateTimeOffset now)
    {
        return new RuntimeResourceSnapshot(0, 128, now);
    }

    private static string BuildTempRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "wevito-budget-meter-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }
}
