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

    [Fact]
    public void ReadyBackendReturnsOnnxDimensionsAndCachesCopies()
    {
        var root = CreateTempRoot();
        var model = Path.Combine(root, "model.onnx");
        var tokenizer = Path.Combine(root, "tokenizer.json");
        File.Copy(Fixture("model.onnx"), model);
        File.Copy(Fixture("tokenizer.json"), tokenizer);
        var calls = 0;
        using var service = new OnnxTextEmbeddingService(
            new OnnxTextEmbeddingOptions(model, tokenizer),
            backendFactory: _ => new OnnxEmbeddingBackend(text =>
            {
                calls++;
                return BuildVector(text, OnnxTextEmbeddingService.DefaultOnnxDimensions);
            }, OnnxTextEmbeddingService.DefaultOnnxDimensions));

        var first = service.Embed("goose baby female blue hold ball");
        first[0] = 999;
        var second = service.Embed("goose baby female blue hold ball");

        Assert.True(service.IsOnnxReady);
        Assert.Equal(OnnxTextEmbeddingService.DefaultOnnxDimensions, service.Dimensions);
        Assert.Equal(1, calls);
        Assert.NotEqual(999, second[0]);
        Assert.InRange(Math.Sqrt(second.Sum(value => value * value)), 0.999, 1.001);
    }

    [Fact]
    public void MissingTokenizerFallsBackToHashingAndEmitsOneAuditLine()
    {
        var root = CreateTempRoot();
        var model = Path.Combine(root, "model.onnx");
        File.Copy(Fixture("model.onnx"), model);
        var audit = new List<string>();
        using var service = new OnnxTextEmbeddingService(
            new OnnxTextEmbeddingOptions(model),
            auditLine: audit.Add);

        var first = service.Embed("goose baby female blue hold ball");
        var second = service.Embed("rat habitat perch");

        Assert.False(service.IsOnnxReady);
        Assert.Equal(HashingTextEmbeddingService.DefaultDimensions, service.Dimensions);
        Assert.Equal(HashingTextEmbeddingService.DefaultDimensions, first.Length);
        Assert.Equal(HashingTextEmbeddingService.DefaultDimensions, second.Length);
        Assert.Single(audit);
    }

    [Fact]
    public void CorruptBackendFallsBackToHashing()
    {
        var root = CreateTempRoot();
        var model = Path.Combine(root, "model.onnx");
        var tokenizer = Path.Combine(root, "tokenizer.json");
        File.Copy(Fixture("model.onnx"), model);
        File.Copy(Fixture("tokenizer.json"), tokenizer);
        var audit = new List<string>();
        using var service = new OnnxTextEmbeddingService(
            new OnnxTextEmbeddingOptions(model, tokenizer),
            auditLine: audit.Add,
            backendFactory: _ => new OnnxEmbeddingBackend(_ => throw new InvalidOperationException("boom"), OnnxTextEmbeddingService.DefaultOnnxDimensions));

        var embedding = service.Embed("goose baby female blue hold ball");

        Assert.Equal(HashingTextEmbeddingService.DefaultDimensions, embedding.Length);
        Assert.Single(audit);
        Assert.Contains("embedding-degrade", audit[0], StringComparison.OrdinalIgnoreCase);
    }

    private static string Fixture(string name)
    {
        return Path.Combine(AppContext.BaseDirectory, "fakes", "test-embedder", name);
    }

    private static float[] BuildVector(string text, int dimensions)
    {
        var vector = new float[dimensions];
        var seed = text.GetHashCode(StringComparison.Ordinal);
        for (var index = 0; index < vector.Length; index++)
        {
            vector[index] = ((seed + (index * 17)) % 79) - 39;
        }

        return vector;
    }

    private static string CreateTempRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "wevito-onnx-embedding-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }
}
