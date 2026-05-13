using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class OnnxTextEmbeddingServiceTests
{
    [Fact]
    public void MissingModelFallsBackToHashingAndEmitsOneAuditLine()
    {
        var root = CreateTempRoot();
        var audit = new List<string>();
        using var service = new OnnxTextEmbeddingService(
            new OnnxTextEmbeddingOptions(Path.Combine(root, "missing.onnx")),
            auditLine: audit.Add);

        var first = service.Embed("goose baby female blue hold ball");
        var second = service.Embed("goose baby female blue hold ball");

        Assert.Equal(HashingTextEmbeddingService.DefaultDimensions, service.Dimensions);
        Assert.Equal(first, second);
        Assert.Single(audit);
        Assert.Contains("embedding-degrade", audit[0], StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CachedEmbeddingsReturnCopies()
    {
        var root = CreateTempRoot();
        using var service = new OnnxTextEmbeddingService(new OnnxTextEmbeddingOptions(Path.Combine(root, "missing.onnx")));

        var first = service.Embed("rat habitat perch");
        first[0] = 999;
        var second = service.Embed("rat habitat perch");

        Assert.NotEqual(999, second[0]);
    }

    private static string CreateTempRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "wevito-onnx-embedding-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }
}
