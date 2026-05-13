using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class OnnxEmbeddingBackendTests
{
    [Fact]
    public void TestBackend_ReturnsDeterministicNormalizedVector()
    {
        using var backend = new OnnxEmbeddingBackend(text => BuildVector(text, 384), 384);

        var first = backend.Embed("goose sprite review");
        var second = backend.Embed("goose sprite review");

        Assert.Equal(384, backend.Dimensions);
        Assert.True(backend.IsReady);
        Assert.Equal(first, second);
        Assert.InRange(Length(first), 0.999, 1.001);
    }

    [Fact]
    public void TestBackend_ReturnsDifferentVectorsForDifferentInputs()
    {
        using var backend = new OnnxEmbeddingBackend(text => BuildVector(text, 384), 384);

        var first = backend.Embed("goose sprite review");
        var second = backend.Embed("snake habitat review");

        Assert.NotEqual(first, second);
        Assert.NotEqual(1.0, Cosine(first, second), precision: 6);
    }

    [Fact]
    public void RealBackend_RequiresReadableTokenizer()
    {
        var root = CreateTempRoot();
        var model = Path.Combine(root, "model.onnx");
        var tokenizer = Path.Combine(root, "tokenizer.json");
        File.Copy(Fixture("model.onnx"), model);
        File.WriteAllText(tokenizer, "{}");

        var ex = Assert.Throws<InvalidOperationException>(() =>
            new OnnxEmbeddingBackend(new OnnxTextEmbeddingOptions(model, tokenizer)).Dispose());

        Assert.Contains("Tokenizer", ex.Message, StringComparison.OrdinalIgnoreCase);
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
            vector[index] = ((seed + (index * 31)) % 97) - 48;
        }

        return vector;
    }

    private static double Length(float[] vector)
    {
        return Math.Sqrt(vector.Sum(value => value * value));
    }

    private static double Cosine(float[] left, float[] right)
    {
        double dot = 0;
        for (var index = 0; index < Math.Min(left.Length, right.Length); index++)
        {
            dot += left[index] * right[index];
        }

        return dot / (Length(left) * Length(right));
    }

    private static string CreateTempRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "wevito-onnx-backend-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }
}
