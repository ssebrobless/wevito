namespace Wevito.VNext.Core;

public sealed record OnnxTextEmbeddingOptions(
    string ModelPath,
    string TokenizerPath = "",
    int OnnxDimensions = 384);

public sealed class OnnxTextEmbeddingService : ITextEmbeddingService, IDisposable
{
    public const int DefaultOnnxDimensions = 384;

    private readonly OnnxTextEmbeddingOptions _options;
    private readonly ITextEmbeddingService _fallback;
    private readonly Action<string>? _auditLine;
    private readonly Func<OnnxTextEmbeddingOptions, OnnxEmbeddingBackend> _backendFactory;
    private readonly Dictionary<string, float[]> _cache = new(StringComparer.Ordinal);
    private readonly Lazy<OnnxEmbeddingBackend?> _backend;
    private bool _degradeEmitted;
    private bool _disposed;

    public OnnxTextEmbeddingService(
        OnnxTextEmbeddingOptions options,
        ITextEmbeddingService? fallback = null,
        Action<string>? auditLine = null,
        Func<OnnxTextEmbeddingOptions, OnnxEmbeddingBackend>? backendFactory = null)
    {
        _options = options;
        _fallback = fallback ?? new HashingTextEmbeddingService();
        _auditLine = auditLine;
        _backendFactory = backendFactory ?? (backendOptions => new OnnxEmbeddingBackend(backendOptions));
        _backend = new Lazy<OnnxEmbeddingBackend?>(TryLoadBackend);
    }

    public int Dimensions => IsOnnxReady ? _backend.Value?.Dimensions ?? Math.Max(4, _options.OnnxDimensions) : _fallback.Dimensions;

    public bool IsOnnxReady => _backend.Value?.IsReady == true;

    public float[] Embed(string text)
    {
        var key = text ?? string.Empty;
        if (_cache.TryGetValue(key, out var cached))
        {
            return cached.ToArray();
        }

        var embedding = IsOnnxReady
            ? EmbedWithOnnxFallback(key)
            : DegradeToFallback("local ONNX embedding model or tokenizer is missing; hashing fallback is active", key);
        _cache[key] = embedding;
        return embedding.ToArray();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (_backend.IsValueCreated)
        {
            _backend.Value?.Dispose();
        }

        _disposed = true;
    }

    private float[] EmbedWithOnnxFallback(string text)
    {
        try
        {
            return _backend.Value?.Embed(text) ??
                DegradeToFallback("local ONNX embedding backend is unavailable; hashing fallback is active", text);
        }
        catch (Exception ex)
        {
            return DegradeToFallback($"local ONNX embedding inference failed: {ex.GetType().Name}", text);
        }
    }

    private float[] DegradeToFallback(string reason, string text)
    {
        if (!_degradeEmitted)
        {
            _auditLine?.Invoke($"embedding-degrade | {reason}");
            _degradeEmitted = true;
        }

        return _fallback.Embed(text);
    }

    private OnnxEmbeddingBackend? TryLoadBackend()
    {
        try
        {
            if (!File.Exists(_options.ModelPath))
            {
                DegradeToFallback("local ONNX embedding model is missing; hashing fallback is active", "");
                return null;
            }

            if (!File.Exists(ResolveTokenizerPath()))
            {
                DegradeToFallback("local ONNX embedding tokenizer is missing; hashing fallback is active", "");
                return null;
            }

            return _backendFactory(_options);
        }
        catch (Exception ex)
        {
            _auditLine?.Invoke($"embedding-degrade | failed to load local ONNX embedding model: {ex.GetType().Name}");
            _degradeEmitted = true;
            return null;
        }
    }

    private string ResolveTokenizerPath()
    {
        if (!string.IsNullOrWhiteSpace(_options.TokenizerPath))
        {
            return _options.TokenizerPath;
        }

        var directory = Path.GetDirectoryName(_options.ModelPath) ?? ".";
        var tokenizerJson = Path.Combine(directory, "tokenizer.json");
        return File.Exists(tokenizerJson) ? tokenizerJson : Path.Combine(directory, "vocab.txt");
    }
}
