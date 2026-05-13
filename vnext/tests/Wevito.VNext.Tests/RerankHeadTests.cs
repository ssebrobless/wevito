using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class RerankHeadTests
{
    [Fact]
    public void Score_BoostsRequestedToolFamilyMatches()
    {
        var head = RerankHead.CreateDefault("v0001-20260512-120000", DateTimeOffset.Parse("2026-05-12T12:00:00Z"));
        var match = Result("spriteAudit", 0.5);
        var other = Result("localDocs", 0.53);

        Assert.True(head.Score(match, "spriteAudit") > head.Score(other, "spriteAudit"));
    }

    [Fact]
    public void PetMemoryStore_ApplyRerankHeadReordersSearchResults()
    {
        var root = Path.Combine(Path.GetTempPath(), "wevito-rerank-tests", Guid.NewGuid().ToString("N"));
        var petId = Guid.Parse("55555555-5555-5555-5555-555555555555");
        var store = new PetMemoryStore(root, new TestEmbeddingService());
        store.AddExample(petId, "localDocs", "goose docs", "docs", DateTimeOffset.Parse("2026-05-12T12:00:00Z"));
        store.AddExample(petId, "spriteAudit", "goose sprites", "sprites", DateTimeOffset.Parse("2026-05-12T12:01:00Z"));
        var head = RerankHead.CreateDefault("v0001-20260512-120000", DateTimeOffset.Parse("2026-05-12T12:02:00Z"));

        var result = store.ApplyRerankHead(petId, "goose", head, "spriteAudit", topK: 1);

        Assert.True(result.Succeeded);
        Assert.Equal("spriteAudit", Assert.Single(result.Results).Example.Kind);
    }

    private static PetMemorySearchResult Result(string kind, double score)
    {
        return new PetMemorySearchResult(
            new PetMemoryExample(1, kind, "content", "label", [1], DateTimeOffset.Parse("2026-05-12T12:00:00Z")),
            score,
            1 - score);
    }

    private sealed class TestEmbeddingService : ITextEmbeddingService
    {
        public int Dimensions => 4;

        public float[] Embed(string text)
        {
            return text.Contains("goose", StringComparison.OrdinalIgnoreCase)
                ? [1, 0, 0, 0]
                : [0, 1, 0, 0];
        }
    }
}
