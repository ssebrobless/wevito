using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class DailyEvidenceSnapshotServiceTests
{
    [Fact]
    public void Tick_EmitsBudgetAndFocusSnapshotsOncePerDay()
    {
        var root = CreateRoot();
        var ledger = CreateLedger(root);
        var focus = new FocusStealCounter(Path.Combine(root, "focus.json"));
        var budget = new RuntimeBudgetMeter(
            Path.Combine(root, "budget.json"),
            clock: () => DateTimeOffset.Parse("2026-05-13T12:00:00Z"),
            resourceReader: () => new RuntimeResourceSnapshot(3, 100, DateTimeOffset.Parse("2026-05-13T12:00:00Z")));
        var service = new DailyEvidenceSnapshotService(ledger, budget, focus, Path.Combine(root, "daily.json"));
        var now = DateTimeOffset.Parse("2026-05-13T12:00:00Z");

        var first = service.Tick(now, new RuntimeBudgetSnapshot(4, 20, 512));
        var second = service.Tick(now.AddHours(1), new RuntimeBudgetSnapshot(4, 20, 512));

        Assert.True(first.Emitted);
        Assert.Contains("focus_steal=false", first.Summary);
        Assert.Contains("budget_exceeded=false", first.Summary);
        Assert.False(second.Emitted);

        var rows = ledger.Snapshot(now.AddMinutes(-1), now.AddHours(2));
        Assert.Single(rows.Where(row => row.PacketKind == "focus_steal_snapshot"));
        Assert.Single(rows.Where(row => row.PacketKind == "budget_meter_snapshot"));
    }

    [Fact]
    public void Tick_ReportsFocusDeltaAndBudgetExceeded()
    {
        var root = CreateRoot();
        var ledger = CreateLedger(root);
        var focus = new FocusStealCounter(Path.Combine(root, "focus.json"));
        var now = DateTimeOffset.Parse("2026-05-13T12:00:00Z");
        focus.RecordActivation(true, now.AddMinutes(-5));
        var budget = new RuntimeBudgetMeter(
            Path.Combine(root, "budget.json"),
            clock: () => now,
            resourceReader: () => new RuntimeResourceSnapshot(30, 100, now));
        var service = new DailyEvidenceSnapshotService(ledger, budget, focus, Path.Combine(root, "daily.json"));

        var result = service.Tick(now, new RuntimeBudgetSnapshot(4, 20, 512));

        Assert.True(result.Emitted);
        Assert.Contains("focus_steal=true day_delta=1 total=1", result.Summary);
        Assert.Contains("budget_exceeded=true", result.Summary);
    }

    [Fact]
    public void Tick_WhenKillSwitchActive_SuppressesSnapshots()
    {
        var root = CreateRoot();
        var ledger = CreateLedger(root);
        var focus = new FocusStealCounter(Path.Combine(root, "focus.json"));
        var budget = new RuntimeBudgetMeter(Path.Combine(root, "budget.json"));
        var settings = new Dictionary<string, string>
        {
            [KillSwitchService.KillSwitchSetting] = bool.TrueString
        };
        var service = new DailyEvidenceSnapshotService(
            ledger,
            budget,
            focus,
            Path.Combine(root, "daily.json"),
            new KillSwitchService(() => settings));
        var now = DateTimeOffset.Parse("2026-05-13T12:00:00Z");

        var result = service.Tick(now, new RuntimeBudgetSnapshot(4, 20, 512), settings);

        Assert.False(result.Emitted);
        Assert.Empty(ledger.Snapshot(now.AddMinutes(-1), now.AddMinutes(1)));
    }

    private static AuditLedgerService CreateLedger(string root)
    {
        return new AuditLedgerService(Path.Combine(root, "ledger.sqlite"));
    }

    private static string CreateRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "wevito-daily-snapshot-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }
}
