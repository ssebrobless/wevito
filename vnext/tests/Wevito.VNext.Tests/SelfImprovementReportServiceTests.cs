using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class SelfImprovementReportServiceTests
{
    [Fact]
    public void Report_HasZeroFlaggedRowsWhenOfflineModeHeld()
    {
        var root = CreateTempRoot();
        var ledger = new AuditLedgerService(Path.Combine(root, "ledger.sqlite"));
        ledger.Record(Packet("localDocs", "2026-05-13T08:00:00Z"));
        ledger.Record(Packet("activity_summary", "2026-05-13T09:00:00Z"));
        var service = new SelfImprovementReportService(ledger);

        var result = service.Run(Request(root, "2026-05-13T00:00:00Z", "2026-05-13T23:59:00Z"));

        Assert.True(result.Succeeded);
        Assert.Equal(2, result.TotalRows);
        Assert.Equal(0, result.FlaggedRows);
        Assert.Contains("Flagged rows: 0", File.ReadAllText(result.MarkdownPath));
    }

    [Fact]
    public void Report_GroupsKindsAcrossMultiDayWindow()
    {
        var root = CreateTempRoot();
        var ledger = new AuditLedgerService(Path.Combine(root, "ledger.sqlite"));
        ledger.Record(Packet("localDocs", "2026-05-12T08:00:00Z"));
        ledger.Record(Packet("localDocs", "2026-05-13T08:00:00Z"));
        ledger.Record(Packet(AuditLedgerService.GoldenEvalPacketKind, "2026-05-13T09:00:00Z"));
        var service = new SelfImprovementReportService(ledger);

        var result = service.Run(Request(root, "2026-05-11T00:00:00Z", "2026-05-13T23:59:00Z"));

        var docs = Assert.Single(result.Buckets.Where(bucket => bucket.PacketKind == "localDocs"));
        Assert.Equal(2, docs.Count);
        Assert.Contains("golden_eval", File.ReadAllText(result.MarkdownPath));
    }

    [Fact]
    public void ReportMarkdown_IsTextOnlyWithoutJsonDump()
    {
        var root = CreateTempRoot();
        var ledger = new AuditLedgerService(Path.Combine(root, "ledger.sqlite"));
        ledger.Record(Packet("localDocs", "2026-05-13T08:00:00Z"));
        var service = new SelfImprovementReportService(ledger);

        var result = service.Run(Request(root, "2026-05-13T00:00:00Z", "2026-05-13T23:59:00Z"));

        var markdown = File.ReadAllText(result.MarkdownPath);
        Assert.DoesNotContain("{", markdown);
        Assert.DoesNotContain("\"packetKind\"", markdown, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void KillSwitch_BlocksReport()
    {
        var root = CreateTempRoot();
        var ledger = new AuditLedgerService(Path.Combine(root, "ledger.sqlite"));
        var killSwitch = new KillSwitchService(() => new Dictionary<string, string> { [KillSwitchService.KillSwitchSetting] = "true" });
        var service = new SelfImprovementReportService(ledger, killSwitch);

        var result = service.Run(Request(root, "2026-05-13T00:00:00Z", "2026-05-13T23:59:00Z"));

        Assert.False(result.Succeeded);
        Assert.Equal("kill_switch=true", result.Message);
    }

    private static SelfImprovementReportRequest Request(string root, string since, string until)
    {
        return new SelfImprovementReportRequest(
            DateTimeOffset.Parse(since),
            DateTimeOffset.Parse(until),
            Path.Combine(root, "vnext", "artifacts", "pet-tasks"),
            DateTimeOffset.Parse("2026-05-13T12:00:00Z"));
    }

    private static EvidencePacket Packet(string kind, string createdAt)
    {
        return new EvidencePacket(
            Guid.NewGuid(),
            kind,
            TaskCardId: null,
            DateTimeOffset.Parse(createdAt),
            DidUseNetwork: false,
            DidUseHostedAi: false,
            DidUseLocalModel: false,
            DidMutate: false,
            ArtifactPath: "artifact",
            Summary: "offline summary",
            Status: "Completed");
    }

    private static string CreateTempRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "wevito-self-improvement-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }
}
