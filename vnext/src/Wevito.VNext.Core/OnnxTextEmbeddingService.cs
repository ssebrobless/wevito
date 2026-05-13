using Microsoft.ML.OnnxRuntime;

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
    private readonly Func<string, InferenceSession> _sessionFactory;
    private readonly Dictionary<string, float[]> _cache = new(StringComparer.Ordinal);
    private readonly Lazy<InferenceSession?> _session;
    private bool _degradeEmitted;
    private bool _disposed;

    public OnnxTextEmbeddingService(
        OnnxTextEmbeddingOptions options,
        ITextEmbeddingService? fallback = null,
        Action<string>? auditLine = null,
        Func<string, InferenceSession>? sessionFactory = null)
    {
        _options = options;
        _fallback = fallback ?? new HashingTextEmbeddingService();
        _auditLine = auditLine;
        _sessionFactory = sessionFactory ?? (path => new InferenceSession(path, BuildSessionOptions()));
        _session = new Lazy<InferenceSession?>(TryLoadSession);
    }

    public int Dimensions => IsOnnxReady ? Math.Max(4, _options.OnnxDimensions) : _fallback.Dimensions;

    public bool IsOnnxReady => _session.Value is not null && File.Exists(ResolveTokenizerPath());

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

        if (_session.IsValueCreated)
        {
            _session.Value?.Dispose();
        }

        _disposed = true;
    }

    private float[] EmbedWithOnnxFallback(string text)
    {
        // The approved phase installs model weights only. Tokenizer/inference graph
        // contracts remain explicit so Wevito never guesses how to feed a model.
        return DegradeToFallback("local ONNX embedding runtime loaded, but tokenizer inference is not yet configured; hashing fallback is active", text);
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

    private InferenceSession? TryLoadSession()
    {
        try
        {
            if (!File.Exists(_options.ModelPath))
            {
                return null;
            }

            return _sessionFactory(_options.ModelPath);
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
        return Path.Combine(directory, "tokenizer.json");
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
}
