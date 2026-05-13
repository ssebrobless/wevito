using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class ActivitySummaryServiceTests
{
    [Fact]
    public void BuildDaily_GroupsRowsAndFlagsSensitiveActivity()
    {
        var root = CreateTempRoot();
        var ledger = new AuditLedgerService(Path.Combine(root, "ledger.sqlite"));
        ledger.Record(Packet("localDocs", DateTimeOffset.Parse("2026-05-12T09:00:00Z"), network: false, hosted: false, localModel: false, mutate: false));
        ledger.Record(Packet("learning_promotion", DateTimeOffset.Parse("2026-05-12T10:00:00Z"), network: false, hosted: false, localModel: true, mutate: true));
        ledger.Record(Packet("localDocs", DateTimeOffset.Parse("2026-05-11T10:00:00Z"), network: true, hosted: false, localModel: false, mutate: false));
        var service = new ActivitySummaryService(ledger);

        var summary = service.BuildDaily(DateTimeOffset.Parse("2026-05-12T12:00:00Z"));

        Assert.Equal(2, summary.TotalRows);
        var docs = Assert.Single(summary.Buckets.Where(bucket => bucket.PacketKind == "localDocs"));
        Assert.Equal(1, docs.Count);
        var learning = Assert.Single(summary.Buckets.Where(bucket => bucket.PacketKind == "learning_promotion"));
        Assert.Equal(1, learning.LocalModelCount);
        Assert.Equal(1, learning.MutationCount);
        Assert.Contains("Sensitive flags: 1", ActivitySummaryService.FormatOneLine(summary));
    }

    private static EvidencePacket Packet(string kind, DateTimeOffset createdAt, bool network, bool hosted, bool localModel, bool mutate)
    {
        return new EvidencePacket(
            Guid.NewGuid(),
            kind,
            Guid.NewGuid(),
            createdAt,
            network,
            hosted,
            localModel,
            mutate,
            "artifact",
            "summary",
            "Completed");
    }

    private static string CreateTempRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "wevito-activity-summary-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }
}
