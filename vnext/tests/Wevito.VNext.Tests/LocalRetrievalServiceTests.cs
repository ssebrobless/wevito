using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class LocalRetrievalServiceTests
{
    [Fact]
    public void Retrieval_HybridRecallAt3BeatsDenseOnlyFixture()
    {
        var root = NewTempDirectory();
        var docs = Path.Combine(root, "docs");
        Directory.CreateDirectory(docs);
        for (var index = 0; index < 5; index++)
        {
            File.WriteAllText(Path.Combine(docs, $"dense-bait-{index}.md"), $"alpha dense bait {index} unrelated");
        }

        var target = Path.Combine(docs, "target.md");
        File.WriteAllText(target, "target-hydration hydration goose pond care");
        var embedding = new FixtureEmbeddingService();
        var memory = new PetMemoryStore(Path.Combine(root, "memory"), embedding);
        var policy = new UnifiedPolicyService(new LocalToolAccessPolicy(root, [docs]));
        var petId = Guid.Parse("80000000-0000-0000-0000-000000000003");
        var ingest = new LocalDocumentIngestService(memory, policy, embeddingService: embedding);
        ingest.Ingest(new LocalDocumentIngestRequest(
            petId,
            [docs],
            [docs],
            ArtifactRoot: Path.Combine(root, "artifacts"),
            RequestedAtUtc: DateTimeOffset.Parse("2026-05-13T12:00:00Z")));

        var denseTop3 = memory.SearchDocumentChunksDense(petId, "hydration goose", topK: 3);
        var retrieval = new LocalRetrievalService(memory, RerankHead.CreateDefault("fixture", DateTimeOffset.Parse("2026-05-13T12:00:00Z")));
        var hybrid = retrieval.Retrieve(petId, "hydration goose", "localResearch", denseTopK: 3, keywordTopK: 3, rerankTopN: 3, nowUtc: DateTimeOffset.Parse("2026-05-13T12:00:00Z"));

        Assert.DoesNotContain(denseTop3, result => result.Chunk.Path.Equals(Path.GetFullPath(target), StringComparison.OrdinalIgnoreCase));
        Assert.Contains(hybrid.Chunks, chunk => chunk.Path.Equals(Path.GetFullPath(target), StringComparison.OrdinalIgnoreCase));
        Assert.Contains(hybrid.MethodTrace, trace => trace.Contains("rrf_k=60", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Retrieval_KillSwitchBlocksAndWritesAuditRow()
    {
        var root = NewTempDirectory();
        var ledger = new AuditLedgerService(Path.Combine(root, "ledger.sqlite"));
        var killSwitch = new KillSwitchService(() => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [KillSwitchService.KillSwitchSetting] = bool.TrueString
        }, ledger);
        var service = new LocalRetrievalService(new PetMemoryStore(Path.Combine(root, "memory")), auditLedgerService: ledger, killSwitchService: killSwitch);
        var now = DateTimeOffset.Parse("2026-05-13T12:00:00Z");

        var result = service.Retrieve(Guid.Parse("80000000-0000-0000-0000-000000000004"), "anything", nowUtc: now);

        Assert.Empty(result.Chunks);
        Assert.Contains(result.MethodTrace, trace => trace.Contains("kill_switch=true", StringComparison.OrdinalIgnoreCase));
        var rows = ledger.Snapshot(now.AddMinutes(-1), now.AddMinutes(1));
        Assert.Contains(rows, row => row.PacketKind == "local_retrieval" && row.Status == "Blocked");
    }

    [Fact]
    public void RerankHead_ApplyIsDeterministic()
    {
        var head = RerankHead.CreateDefault("fixture", DateTimeOffset.Parse("2026-05-13T12:00:00Z"));
        var chunks = new[]
        {
            Chunk("1", "frog jump water"),
            Chunk("2", "goose hydration pond"),
            Chunk("3", "crow perch branch")
        };

        var first = head.Apply(chunks, "goose pond");
        var second = head.Apply(chunks, "goose pond");

        Assert.Equal(first.Select(chunk => chunk.ChunkId), second.Select(chunk => chunk.ChunkId));
        Assert.Equal("2", first[0].ChunkId);
    }

    private static RetrievalChunk Chunk(string id, string text)
    {
        return new RetrievalChunk(id, "doc", $"docs/{id}.md", id, text, 0, text.Length, [0, 1, 0, 0], DateTimeOffset.Parse("2026-05-13T12:00:00Z"));
    }

    private static string NewTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "wevito-retrieval-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private sealed class FixtureEmbeddingService : ITextEmbeddingService
    {
        public int Dimensions => 4;

        public float[] Embed(string text)
        {
            return text.Contains("target-hydration", StringComparison.OrdinalIgnoreCase)
                ? [0, 1, 0, 0]
                : [1, 0, 0, 0];
        }
    }
}
