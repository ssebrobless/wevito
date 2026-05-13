using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class LocalDocumentIngestServiceTests
{
    [Fact]
    public void DocIngest_ChunksUtf8MixedLineEndingsAndWritesManifest()
    {
        var root = NewTempDirectory();
        var docs = Path.Combine(root, "docs");
        Directory.CreateDirectory(docs);
        var path = Path.Combine(docs, "care.md");
        File.WriteAllText(path, "goose care\r\nhydration pond\nclean food\r\n" + string.Join(" ", Enumerable.Range(0, 70).Select(index => $"token{index}")), System.Text.Encoding.UTF8);
        var memory = new PetMemoryStore(Path.Combine(root, "memory"));
        var policy = new UnifiedPolicyService(new LocalToolAccessPolicy(root, [docs]));
        var service = new LocalDocumentIngestService(memory, policy);
        var petId = Guid.Parse("80000000-0000-0000-0000-000000000001");

        var result = service.Ingest(new LocalDocumentIngestRequest(
            petId,
            [path],
            [docs],
            ArtifactRoot: Path.Combine(root, "artifacts"),
            ChunkSizeTokens: 3,
            ChunkOverlapTokens: 1,
            RequestedAtUtc: DateTimeOffset.Parse("2026-05-13T12:00:00Z")));

        Assert.True(result.Succeeded);
        Assert.Equal(1, result.FilesIndexed);
        Assert.True(result.ChunksWritten >= 2);
        Assert.True(File.Exists(result.ManifestPath));
        var retrieval = memory.SearchDocumentChunksKeyword(petId, "hydration", topK: 3);
        Assert.Contains(retrieval, item => item.Chunk.Path.Equals(Path.GetFullPath(path), StringComparison.OrdinalIgnoreCase));
        Assert.All(retrieval, item => Assert.NotEmpty(item.Chunk.Sha256));
    }

    [Fact]
    public void DocIngest_HonorsUnifiedPolicyAndSkipsDeniedPath()
    {
        var root = NewTempDirectory();
        var allowed = Path.Combine(root, "docs");
        var outside = Path.Combine(root, "outside");
        Directory.CreateDirectory(allowed);
        Directory.CreateDirectory(outside);
        var outsideFile = Path.Combine(outside, "private.md");
        File.WriteAllText(outsideFile, "secret text");
        var ledgerPath = Path.Combine(root, "ledger.sqlite");
        var ledger = new AuditLedgerService(ledgerPath);
        var service = new LocalDocumentIngestService(
            new PetMemoryStore(Path.Combine(root, "memory")),
            new UnifiedPolicyService(new LocalToolAccessPolicy(root, [allowed]), ledger),
            ledger);

        var result = service.Ingest(new LocalDocumentIngestRequest(
            Guid.Parse("80000000-0000-0000-0000-000000000002"),
            [outsideFile],
            [allowed],
            ArtifactRoot: Path.Combine(root, "artifacts"),
            RequestedAtUtc: DateTimeOffset.Parse("2026-05-13T12:00:00Z")));

        Assert.Equal(0, result.FilesIndexed);
        Assert.Contains(result.SkippedPaths, path => path.EndsWith("private.md", StringComparison.OrdinalIgnoreCase));
        var rows = ledger.Snapshot(DateTimeOffset.Parse("2026-05-13T11:59:00Z"), DateTimeOffset.Parse("2026-05-13T12:01:00Z"));
        Assert.Contains(rows, row => row.PacketKind == "local_access_blocked");
    }

    private static string NewTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "wevito-doc-ingest-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
