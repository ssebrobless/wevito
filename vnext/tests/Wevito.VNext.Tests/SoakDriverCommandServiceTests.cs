using Wevito.VNext.Core;
using Wevito.VNext.Shell;

namespace Wevito.VNext.Tests;

public sealed class SoakDriverCommandServiceTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-05-13T12:00:00Z");

    [Fact]
    public void Heartbeat_WritesExactlyOneLedgerRow()
    {
        var root = CreateRoot();
        var ledger = CreateLedger(root);
        var service = new SoakDriverCommandService(ledger, Path.Combine(root, "soak"), () => Now);

        var result = service.Heartbeat("scheduled", SoakDriverCommandService.BuildDefaultSettingsSnapshot());

        Assert.True(result.Succeeded, result.Message);
        var row = Assert.Single(ledger.Snapshot(Now.AddMinutes(-1), Now.AddMinutes(1)));
        Assert.Equal("runtime_session_heartbeat", row.PacketKind);
        Assert.False(row.DidUseNetwork);
        Assert.False(row.DidUseHostedAi);
        Assert.False(row.DidMutate);
    }

    [Fact]
    public void Heartbeat_WhenKillSwitchActive_RefusesToWrite()
    {
        var root = CreateRoot();
        var ledger = CreateLedger(root);
        var settings = new Dictionary<string, string>(SoakDriverCommandService.BuildDefaultSettingsSnapshot(), StringComparer.OrdinalIgnoreCase)
        {
            [KillSwitchService.KillSwitchSetting] = bool.TrueString
        };
        var service = new SoakDriverCommandService(
            ledger,
            Path.Combine(root, "soak"),
            () => Now,
            new KillSwitchService(() => settings));

        var result = service.Heartbeat("scheduled", settings);

        Assert.False(result.Succeeded);
        Assert.Equal("kill_switch=true", result.Message);
        Assert.Empty(ledger.Snapshot(Now.AddMinutes(-1), Now.AddMinutes(1)));
    }

    [Fact]
    public void DayEnd_WritesSelfImprovementReport()
    {
        var root = CreateRoot();
        var ledger = CreateLedger(root);
        ledger.Record(Packet("localDocs", Now.AddMinutes(-30), "preview", "PreviewReady"));
        var service = new SoakDriverCommandService(ledger, Path.Combine(root, "soak"), () => Now);

        var result = service.DayEnd();

        Assert.True(result.Succeeded, result.Message);
        Assert.Contains(ledger.Snapshot(Now.AddDays(-1), Now.AddMinutes(1)), row => row.PacketKind == AuditLedgerService.SelfImprovementReportPacketKind);
    }

    [Fact]
    public void WindowEnd_WritesSoakWindowEndRow()
    {
        var root = CreateRoot();
        var ledger = CreateLedger(root);
        var service = new SoakDriverCommandService(ledger, Path.Combine(root, "soak"), () => Now);

        var result = service.WindowEnd("manual");

        Assert.True(result.Succeeded, result.Message);
        Assert.True(File.Exists(result.ArtifactPath));
        Assert.Contains(ledger.Snapshot(Now.AddMinutes(-1), Now.AddMinutes(1)), row => row.PacketKind == SoakDriverCommandService.SoakWindowEndPacketKind);
    }

    [Fact]
    public void Status_ReturnsSettingsSnapshotKeys()
    {
        var root = CreateRoot();
        var service = new SoakDriverCommandService(CreateLedger(root), Path.Combine(root, "soak"), () => Now);
        var settings = SoakDriverCommandService.BuildDefaultSettingsSnapshot();

        var result = service.Status(settings);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Status);
        Assert.Contains("runtime_autonomous_beta_enabled", settings.Keys);
        Assert.Contains(KillSwitchService.KillSwitchSetting, settings.Keys);
    }

    [Fact]
    public void FormatEvidenceCollectionPanel_UsesCountsNotPrivateSummaryText()
    {
        var status = new EvidenceCollectionStatus(
            Active: true,
            HasManifest: true,
            StartedAtUtc: Now.AddDays(-1),
            DayN: 2,
            DayMax: 7,
            RowsToday: 3,
            FlaggedRowsToday: 0,
            LastReportAtUtc: Now,
            LastReadinessLabel: "day_2_of_7",
            HeartbeatCountToday: 1,
            FocusStealDeltaToday: 0,
            BudgetExceededToday: false,
            ManifestPath: "manifest",
            Days:
            [
                new EvidenceCollectionDayStatus(DateOnly.FromDateTime(Now.UtcDateTime), 1, 0, 0, false, Now)
            ]);

        var text = ToolPopupWindow.FormatEvidenceCollectionPanel(status);

        Assert.Contains("Day 2 of 7", text);
        Assert.Contains("heartbeats=1", text);
        Assert.DoesNotContain("secret", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("private", text, StringComparison.OrdinalIgnoreCase);
    }

    private static AuditLedgerService CreateLedger(string root)
    {
        return new AuditLedgerService(Path.Combine(root, "ledger.sqlite"));
    }

    private static EvidencePacket Packet(string kind, DateTimeOffset createdAt, string summary, string status)
    {
        return new EvidencePacket(Guid.NewGuid(), kind, null, createdAt, false, false, false, false, "artifact", summary, status);
    }

    private static string CreateRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "wevito-soak-driver-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }
}
