using System.Text.Json;
using Wevito.VNext.Contracts;
using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class EvidenceCollectionStatusServiceTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-05-13T12:00:00Z");

    [Fact]
    public void Read_ReturnsInactiveWhenNoManifestExists()
    {
        var root = CreateRoot();
        var service = new EvidenceCollectionStatusService(CreateLedger(root), Path.Combine(root, "soak"), () => Now);

        var status = service.Read();

        Assert.False(status.Active);
        Assert.False(status.HasManifest);
        Assert.Equal(0, status.DayN);
        Assert.Equal("not_started", status.LastReadinessLabel);
    }

    [Fact]
    public void Read_ReturnsDayNumberAndDailyCounts()
    {
        var root = CreateRoot();
        var artifactRoot = Path.Combine(root, "soak");
        WriteManifest(Path.Combine(artifactRoot, "20260511-soak-window"), Now.AddDays(-2), 7);
        var ledger = CreateLedger(root);
        ledger.Record(Packet("runtime_session_heartbeat", Now.AddMinutes(-30), "runtime_session uptime_hours=4 uptime_hours>=4 heartbeat=true", "Completed"));
        ledger.Record(Packet("focus_steal_snapshot", Now.AddMinutes(-20), "focus_steal=true day_delta=2 total=4", "Completed"));
        ledger.Record(Packet("budget_meter_snapshot", Now.AddMinutes(-10), "budget_exceeded=true used_this_hour=4 max_this_hour=4", "Completed"));
        ledger.Record(Packet(AuditLedgerService.SelfImprovementReportPacketKind, Now.AddMinutes(-5), "Self-improvement report wrote 3 rows.", "Completed"));
        var service = new EvidenceCollectionStatusService(ledger, artifactRoot, () => Now);

        var status = service.Read();

        Assert.True(status.Active);
        Assert.True(status.HasManifest);
        Assert.Equal(3, status.DayN);
        Assert.Equal(7, status.DayMax);
        Assert.Equal(4, status.RowsToday);
        Assert.Equal(1, status.HeartbeatCountToday);
        Assert.Equal(2, status.FocusStealDeltaToday);
        Assert.True(status.BudgetExceededToday);
        Assert.Equal(Now.AddMinutes(-5), status.LastReportAtUtc);
    }

    [Fact]
    public void Read_WhenKillSwitchActive_ReturnsInactiveEvenWithManifest()
    {
        var root = CreateRoot();
        var artifactRoot = Path.Combine(root, "soak");
        WriteManifest(Path.Combine(artifactRoot, "20260513-soak-window"), Now, 7);
        var settings = new Dictionary<string, string>
        {
            [KillSwitchService.KillSwitchSetting] = bool.TrueString
        };
        var service = new EvidenceCollectionStatusService(
            CreateLedger(root),
            artifactRoot,
            () => Now,
            new KillSwitchService(() => settings));

        var status = service.Read();

        Assert.False(status.Active);
        Assert.Equal(0, status.DayN);
        Assert.Equal("blocked_by_kill_switch", status.LastReadinessLabel);
    }

    [Fact]
    public void SoakWindowEnd_IsKnownPlainLanguageKind()
    {
        Assert.Contains(SoakDriverCommandService.SoakWindowEndPacketKind, PlainLanguageExplainer.KnownPacketKinds);
    }

    private static AuditLedgerService CreateLedger(string root)
    {
        return new AuditLedgerService(Path.Combine(root, "ledger.sqlite"));
    }

    private static EvidencePacket Packet(string kind, DateTimeOffset createdAt, string summary, string status)
    {
        return new EvidencePacket(
            Guid.NewGuid(),
            kind,
            null,
            createdAt,
            DidUseNetwork: false,
            DidUseHostedAi: false,
            DidUseLocalModel: false,
            DidMutate: false,
            ArtifactPath: "artifact",
            summary,
            status);
    }

    private static void WriteManifest(string folder, DateTimeOffset startedAtUtc, int days)
    {
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, "manifest.json"), JsonSerializer.Serialize(new
        {
            schema_version = "1",
            started_at_utc = startedAtUtc,
            requested_days = days,
            heartbeat_minutes = 60,
            artifact_root = folder,
            initial_settings_snapshot_sha256 = "test"
        }, JsonDefaults.Options));
    }

    private static string CreateRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "wevito-evidence-status-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }
}
