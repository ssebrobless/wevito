using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class RuntimeSessionTrackerTests
{
    [Fact]
    public void Tick_EmitsStartThenHourlyHeartbeatWithFourHourProof()
    {
        var root = CreateRoot();
        var ledger = CreateLedger(root);
        var tracker = new RuntimeSessionTracker(ledger, Path.Combine(root, "runtime-session.json"));
        var start = DateTimeOffset.Parse("2026-05-13T12:00:00Z");

        var started = tracker.Tick(start);
        var quiet = tracker.Tick(start.AddMinutes(30));
        var heartbeat = tracker.Tick(start.AddHours(4));

        Assert.True(started.Emitted);
        Assert.Equal("runtime_session_start", started.PacketKind);
        Assert.False(quiet.Emitted);
        Assert.True(heartbeat.Emitted);
        Assert.Equal("runtime_session_heartbeat", heartbeat.PacketKind);
        Assert.Contains("uptime_hours=4", heartbeat.Summary);
        Assert.Contains("uptime_hours>=4", heartbeat.Summary);

        var rows = ledger.Snapshot(start.AddMinutes(-1), start.AddHours(5));
        Assert.Contains(rows, row => row.PacketKind == "runtime_session_start");
        Assert.Contains(rows, row => row.PacketKind == "runtime_session_heartbeat");
    }

    [Fact]
    public void Tick_ClampsHeartbeatIntervalToMinimumFifteenMinutes()
    {
        var root = CreateRoot();
        var ledger = CreateLedger(root);
        var tracker = new RuntimeSessionTracker(ledger, Path.Combine(root, "runtime-session.json"));
        var start = DateTimeOffset.Parse("2026-05-13T12:00:00Z");
        var settings = new Dictionary<string, string>
        {
            [RuntimeSessionTracker.HeartbeatMinutesSetting] = "1"
        };

        tracker.Tick(start, settings);
        var tooSoon = tracker.Tick(start.AddMinutes(14), settings);
        var emitted = tracker.Tick(start.AddMinutes(15), settings);

        Assert.False(tooSoon.Emitted);
        Assert.True(emitted.Emitted);
        Assert.Equal("runtime_session_heartbeat", emitted.PacketKind);
    }

    [Fact]
    public void End_EmitsEndAndClearsSession()
    {
        var root = CreateRoot();
        var ledger = CreateLedger(root);
        var tracker = new RuntimeSessionTracker(ledger, Path.Combine(root, "runtime-session.json"));
        var start = DateTimeOffset.Parse("2026-05-13T12:00:00Z");

        tracker.Tick(start);
        var ended = tracker.End(start.AddHours(1));
        var duplicate = tracker.End(start.AddHours(2));

        Assert.True(ended.Emitted);
        Assert.Equal("runtime_session_end", ended.PacketKind);
        Assert.False(duplicate.Emitted);
    }

    [Fact]
    public void Tick_WhenKillSwitchActive_EmitsPausedWithoutStartingSession()
    {
        var root = CreateRoot();
        var ledger = CreateLedger(root);
        var settings = new Dictionary<string, string>
        {
            [KillSwitchService.KillSwitchSetting] = bool.TrueString
        };
        var tracker = new RuntimeSessionTracker(
            ledger,
            Path.Combine(root, "runtime-session.json"),
            new KillSwitchService(() => settings));

        var result = tracker.Tick(DateTimeOffset.Parse("2026-05-13T12:00:00Z"), settings);

        Assert.True(result.Emitted);
        Assert.Equal("runtime_session_paused", result.PacketKind);
        Assert.Contains("kill_switch_active=true", result.Summary);
    }

    private static AuditLedgerService CreateLedger(string root)
    {
        return new AuditLedgerService(Path.Combine(root, "ledger.sqlite"));
    }

    private static string CreateRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "wevito-runtime-session-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }
}
