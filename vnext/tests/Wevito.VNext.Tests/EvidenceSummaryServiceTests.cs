using Wevito.VNext.Core;
using Wevito.VNext.Core.Audit;

namespace Wevito.VNext.Tests;

public sealed class EvidenceSummaryServiceTests
{
    [Fact]
    public void EvidenceSummaryService_ReturnsExpectedRollups()
    {
        var path = CreateDatabasePath();
        var ledger = new AuditLedgerService(path);
        var now = DateTimeOffset.Parse("2026-05-18T12:00:00Z");
        ledger.Record(Packet("spriteAudit", now.AddMinutes(-3), didMutate: false, status: "Completed"));
        ledger.Record(Packet("spriteAudit", now.AddMinutes(-2), didMutate: true, status: "Blocked", error: "blocked"));
        ledger.Record(Packet("web_fetch", now.AddMinutes(-1), didUseNetwork: true, status: "Completed"));
        var service = new EvidenceSummaryService(path);

        var summary = service.GetSummary(new EvidenceSummaryQuery(now.AddHours(-1), now, 100));

        var sprite = Assert.Single(summary.Rows.Where(row => row.PacketKind == "spriteAudit"));
        Assert.Equal(2, sprite.Count);
        Assert.Equal(1, sprite.MutationYesCount);
        Assert.Equal(1, sprite.RefusalCount);
        var web = Assert.Single(summary.Rows.Where(row => row.PacketKind == "web_fetch"));
        Assert.Equal(1, web.NetworkYesCount);
        Assert.Empty(summary.UnknownPacketKinds);
    }

    [Fact]
    public void EvidenceSummaryService_RespectsDateRange()
    {
        var path = CreateDatabasePath();
        var ledger = new AuditLedgerService(path);
        var now = DateTimeOffset.Parse("2026-05-18T12:00:00Z");
        ledger.Record(Packet("spriteAudit", now.AddDays(-2)));
        ledger.Record(Packet("petState", now.AddMinutes(-10)));
        var service = new EvidenceSummaryService(path);

        var summary = service.GetSummary(new EvidenceSummaryQuery(now.AddHours(-1), now, 100));

        Assert.DoesNotContain(summary.Rows, row => row.PacketKind == "spriteAudit");
        Assert.Contains(summary.Rows, row => row.PacketKind == "petState");
    }

    [Fact]
    public void EvidenceSummaryService_CapsAtMaxPackets()
    {
        var path = CreateDatabasePath();
        var ledger = new AuditLedgerService(path);
        var now = DateTimeOffset.Parse("2026-05-18T12:00:00Z");
        for (var index = 0; index < 1005; index++)
        {
            ledger.Record(Packet("petState", now.AddSeconds(-index)));
        }
        var service = new EvidenceSummaryService(path);

        var summary = service.GetSummary(new EvidenceSummaryQuery(null, now, 5000));

        Assert.Equal(EvidenceSummaryService.MaxAllowedPackets, summary.Query.MaxPackets);
        Assert.Equal(EvidenceSummaryService.MaxAllowedPackets, summary.Rows.Sum(row => row.Count));
    }

    [Fact]
    public void EvidenceSummaryService_DoesNotIssueUpdateOrDelete()
    {
        var path = CreateDatabasePath();
        var ledger = new AuditLedgerService(path);
        ledger.Record(Packet("petState", DateTimeOffset.Parse("2026-05-18T12:00:00Z")));
        var commands = new List<string>();
        var service = new EvidenceSummaryService(path, commandObserver: commands.Add);

        service.GetSummary(new EvidenceSummaryQuery(MaxPackets: 100));

        Assert.All(commands, command =>
        {
            Assert.DoesNotContain("UPDATE", command, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("DELETE", command, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public void EvidenceDashboard_Export_WritesOnlyToArtifactsDir()
    {
        var path = CreateDatabasePath();
        var ledger = new AuditLedgerService(path);
        var now = DateTimeOffset.Parse("2026-05-18T12:00:00Z");
        ledger.Record(Packet("petState", now));
        var service = new EvidenceSummaryService(path, ledger);
        var root = Path.Combine(Path.GetTempPath(), "wevito-tests", Guid.NewGuid().ToString("N"), "vnext", "artifacts", "c-phase-141-evidence-dashboard");

        var result = service.ExportSummary(new EvidenceSummaryQuery(MaxPackets: 100), root, now);

        Assert.True(result.Exported, result.BlockReason);
        Assert.True(File.Exists(result.Path));
        Assert.StartsWith(Path.GetFullPath(root), Path.GetFullPath(result.Path), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EvidenceDashboard_RespectsKillSwitch()
    {
        var path = CreateDatabasePath();
        var settings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [KillSwitchService.KillSwitchSetting] = bool.TrueString
        };
        var service = new EvidenceSummaryService(path, killSwitchService: new KillSwitchService(() => settings));

        var summary = service.GetSummary(new EvidenceSummaryQuery());
        var export = service.ExportSummary(new EvidenceSummaryQuery(), Path.Combine(Path.GetTempPath(), "vnext", "artifacts", "c-phase-141-evidence-dashboard"), DateTimeOffset.UtcNow);

        Assert.True(summary.IsBlocked);
        Assert.False(export.Exported);
        Assert.Equal("kill_switch=true", export.BlockReason);
    }

    [Fact]
    public void PlainLanguageExplainer_KnowsEvidenceDashboardExport()
    {
        var text = new PlainLanguageExplainer().ExplainPacketKind(EvidenceSummaryService.ExportedPacketKind);

        Assert.Contains(EvidenceSummaryService.ExportedPacketKind, PlainLanguageExplainer.KnownPacketKinds);
        Assert.Contains("Evidence", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Unknown", text, StringComparison.OrdinalIgnoreCase);
    }

    private static EvidencePacket Packet(
        string kind,
        DateTimeOffset timestamp,
        bool didUseNetwork = false,
        bool didUseHostedAi = false,
        bool didUseLocalModel = false,
        bool didMutate = false,
        string status = "Completed",
        string error = "")
    {
        return new EvidencePacket(
            Guid.NewGuid(),
            kind,
            null,
            timestamp,
            didUseNetwork,
            didUseHostedAi,
            didUseLocalModel,
            didMutate,
            "",
            $"{kind} test packet",
            status,
            error);
    }

    private static string CreateDatabasePath()
    {
        var root = Path.Combine(Path.GetTempPath(), "wevito-evidence-summary-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return Path.Combine(root, "ledger.sqlite");
    }
}
