using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class CitationEnforcerTests
{
    [Fact]
    public void Citation_KeepsValidCitedSentencesAndMarksUncited()
    {
        var enforcer = new CitationEnforcer();
        var result = enforcer.Enforce(
            "Goose needs pond care [1]. This sentence has no citation. Snake facts [7].",
            [Chunk("a"), Chunk("b")]);

        Assert.Contains("Goose needs pond care [1].", result.Text);
        Assert.Contains("(needs citation)", result.Text);
        Assert.DoesNotContain("Snake facts [7]", result.Text);
        Assert.Equal(3, result.TotalSentences);
        Assert.Equal(1, result.CitedSentences);
        Assert.Equal(1d / 3d, result.CitationCoverageRatio, precision: 6);
    }

    [Fact]
    public void Citation_EmptyTextNeverThrows()
    {
        var result = new CitationEnforcer().Enforce("", [Chunk("a")]);

        Assert.Equal("(needs citation)", result.Text);
        Assert.Equal(0, result.CitationCoverageRatio);
    }

    private static RetrievalChunk Chunk(string id)
    {
        return new RetrievalChunk(id, "doc", $"docs/{id}.md", id, "text", 0, 4, [1, 0, 0, 0], DateTimeOffset.Parse("2026-05-13T12:00:00Z"));
    }
}
