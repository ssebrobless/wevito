using System.Globalization;
using System.Text.Json;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace Wevito.VNext.Core;

public sealed class OnnxEmbeddingBackend : IDisposable
{
    private const int DefaultMaxTokens = 128;

    private readonly InferenceSession? _session;
    private readonly IReadOnlyDictionary<string, long> _vocab;
    private readonly Func<string, float[]>? _testEmbedder;
    private readonly int _dimensions;
    private bool _disposed;

    public OnnxEmbeddingBackend(
        OnnxTextEmbeddingOptions options,
        Func<string, InferenceSession>? sessionFactory = null)
    {
        if (!File.Exists(options.ModelPath))
        {
            throw new FileNotFoundException("Local ONNX embedding model is missing.", options.ModelPath);
        }

        var tokenizerPath = ResolveTokenizerPath(options);
        if (!File.Exists(tokenizerPath))
        {
            throw new FileNotFoundException("Local ONNX tokenizer is missing.", tokenizerPath);
        }

        _vocab = LoadVocabulary(tokenizerPath);
        _session = (sessionFactory ?? (path => new InferenceSession(path, BuildSessionOptions())))(options.ModelPath);
        _dimensions = Math.Max(4, options.OnnxDimensions);
    }

    public OnnxEmbeddingBackend(Func<string, float[]> testEmbedder, int dimensions)
    {
        _testEmbedder = testEmbedder;
        _dimensions = Math.Max(4, dimensions);
        _vocab = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
    }

    public int Dimensions => _dimensions;

    public bool IsReady => _testEmbedder is not null || _session is not null;

    public float[] Embed(string text)
    {
        if (_testEmbedder is not null)
        {
            return NormalizeToDimensions(_testEmbedder(text ?? string.Empty), _dimensions);
        }

        if (_session is null)
        {
            throw new InvalidOperationException("Local ONNX embedding session is not ready.");
        }

        var encoded = Encode(text ?? string.Empty, _vocab, DefaultMaxTokens);
        var shape = new[] { 1, encoded.InputIds.Length };
        using var results = _session.Run([
            NamedOnnxValue.CreateFromTensor("input_ids", new DenseTensor<long>(encoded.InputIds, shape)),
            NamedOnnxValue.CreateFromTensor("attention_mask", new DenseTensor<long>(encoded.AttentionMask, shape)),
            NamedOnnxValue.CreateFromTensor("token_type_ids", new DenseTensor<long>(encoded.TokenTypeIds, shape))
        ]);
        var output = results.FirstOrDefault()?.AsTensor<float>()
            ?? throw new InvalidOperationException("Local ONNX embedding model did not return a float tensor.");
        return MeanPool(output, encoded.AttentionMask, _dimensions);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _session?.Dispose();
        _disposed = true;
    }

    private static EncodedInput Encode(string text, IReadOnlyDictionary<string, long> vocab, int maxTokens)
    {
        var tokens = new List<long> { Lookup(vocab, "[CLS]", 101) };
        foreach (var token in SplitTokens(text).Take(Math.Max(1, maxTokens - 2)))
        {
            tokens.Add(Lookup(vocab, token, Lookup(vocab, "[UNK]", 100)));
        }

        tokens.Add(Lookup(vocab, "[SEP]", 102));
        var length = tokens.Count;
        return new EncodedInput(
            tokens.ToArray(),
            Enumerable.Repeat(1L, length).ToArray(),
            new long[length]);
    }

    private static IEnumerable<string> SplitTokens(string text)
    {
        return text
            .Trim()
            .ToLowerInvariant()
            .Split([' ', '\t', '\r', '\n', '.', ',', ';', ':', '/', '\\', '-', '_', '(', ')', '[', ']'], StringSplitOptions.RemoveEmptyEntries);
    }

    private static long Lookup(IReadOnlyDictionary<string, long> vocab, string token, long fallback)
    {
        return vocab.TryGetValue(token, out var id) ? id : fallback;
    }

    private static IReadOnlyDictionary<string, long> LoadVocabulary(string tokenizerPath)
    {
        if (Path.GetFileName(tokenizerPath).Equals("vocab.txt", StringComparison.OrdinalIgnoreCase))
        {
            return File.ReadLines(tokenizerPath)
                .Select((token, index) => new { token = token.Trim(), id = (long)index })
                .Where(item => item.token.Length > 0)
                .ToDictionary(item => item.token, item => item.id, StringComparer.OrdinalIgnoreCase);
        }

        using var document = JsonDocument.Parse(File.ReadAllText(tokenizerPath));
        if (document.RootElement.TryGetProperty("vocab", out var vocabElement) && vocabElement.ValueKind == JsonValueKind.Object)
        {
            var vocab = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in vocabElement.EnumerateObject())
            {
                if (item.Value.TryGetInt64(out var id))
                {
                    vocab[item.Name] = id;
                }
            }

            if (vocab.Count > 0)
            {
                return vocab;
            }
        }

        throw new InvalidOperationException("Tokenizer file does not contain a readable vocab.");
    }

    private static float[] MeanPool(Tensor<float> output, long[] attentionMask, int dimensions)
    {
        var tensorDimensions = output.Dimensions.ToArray();
        if (tensorDimensions.Length < 3)
        {
            return NormalizeToDimensions(output.ToArray(), dimensions);
        }

        var tokenCount = Math.Min(attentionMask.Length, tensorDimensions[1]);
        var hiddenSize = tensorDimensions[2];
        var pooled = new float[hiddenSize];
        var activeTokens = 0;
        for (var token = 0; token < tokenCount; token++)
        {
            if (attentionMask[token] == 0)
            {
                continue;
            }

            activeTokens++;
            for (var hidden = 0; hidden < hiddenSize; hidden++)
            {
                pooled[hidden] += output[0, token, hidden];
            }
        }

        if (activeTokens > 0)
        {
            for (var index = 0; index < pooled.Length; index++)
            {
                pooled[index] /= activeTokens;
            }
        }

        return NormalizeToDimensions(pooled, dimensions);
    }

    private static float[] NormalizeToDimensions(float[] source, int dimensions)
    {
        var vector = new float[dimensions];
        for (var index = 0; index < vector.Length && index < source.Length; index++)
        {
            vector[index] = float.IsFinite(source[index]) ? source[index] : 0f;
        }

        var length = Math.Sqrt(vector.Sum(value => value * value));
        if (length <= 0)
        {
            vector[0] = 1f;
            return vector;
        }

        for (var index = 0; index < vector.Length; index++)
        {
            vector[index] = (float)(vector[index] / length);
        }

        return vector;
    }

    private static string ResolveTokenizerPath(OnnxTextEmbeddingOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.TokenizerPath))
        {
            return options.TokenizerPath;
        }

        var directory = Path.GetDirectoryName(options.ModelPath) ?? ".";
        var tokenizerJson = Path.Combine(directory, "tokenizer.json");
        return File.Exists(tokenizerJson) ? tokenizerJson : Path.Combine(directory, "vocab.txt");
    }

    private static SessionOptions BuildSessionOptions()
    {
        var options = new SessionOptions();
        try
        {
            options.AppendExecutionProvider_DML();
        }
        catch
        {
            // CPU fallback keeps the local-first path available on machines
            // without DirectML support.
        }

        return options;
    }

    private sealed record EncodedInput(long[] InputIds, long[] AttentionMask, long[] TokenTypeIds);
}
