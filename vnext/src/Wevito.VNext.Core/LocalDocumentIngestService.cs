using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed record LocalDocumentIngestRequest(
    Guid PetId,
    IReadOnlyList<string> Paths,
    IReadOnlyList<string> ApprovedRoots,
    Guid? TaskCardId = null,
    string ArtifactRoot = "",
    int ChunkSizeTokens = 512,
    int ChunkOverlapTokens = 64,
    DateTimeOffset RequestedAtUtc = default);

public sealed record LocalDocumentIngestResult(
    bool Succeeded,
    int FilesIndexed,
    int ChunksWritten,
    IReadOnlyList<string> ReadPaths,
    IReadOnlyList<string> SkippedPaths,
    string ManifestPath,
    string Message);

public sealed class LocalDocumentIngestService
{
    private static readonly string[] TextExtensions = [".cs", ".xaml", ".gd", ".ps1", ".py", ".json", ".md", ".txt", ".yaml", ".yml", ".xml"];
    private readonly PetMemoryStore _memoryStore;
    private readonly UnifiedPolicyService _policyService;
    private readonly AuditLedgerService? _auditLedgerService;
    private readonly ITextEmbeddingService _embeddingService;

    public LocalDocumentIngestService(
        PetMemoryStore? memoryStore = null,
        UnifiedPolicyService? policyService = null,
        AuditLedgerService? auditLedgerService = null,
        ITextEmbeddingService? embeddingService = null)
    {
        _embeddingService = embeddingService ?? new CachingTextEmbeddingService();
        _memoryStore = memoryStore ?? new PetMemoryStore(embeddingService: _embeddingService);
        _policyService = policyService ?? new UnifiedPolicyService();
        _auditLedgerService = auditLedgerService;
    }

    public LocalDocumentIngestResult Ingest(LocalDocumentIngestRequest request)
    {
        var timestamp = request.RequestedAtUtc == default ? DateTimeOffset.UtcNow : request.RequestedAtUtc;
        var artifactRoot = ResolveArtifactRoot(request.ArtifactRoot, timestamp);
        Directory.CreateDirectory(artifactRoot);
        var readPaths = new List<string>();
        var skipped = new List<string>();
        var filesIndexed = 0;
        var chunksWritten = 0;

        foreach (var path in ResolveFiles(request.Paths))
        {
            var decision = _policyService.EvaluateRead(path, request.ApprovedRoots, request.TaskCardId, timestamp);
            if (decision.IsBlocked || string.IsNullOrWhiteSpace(decision.NormalizedPath))
            {
                skipped.Add(path);
                Record("local_access_blocked", $"Blocked document ingest path: {path}. {decision.Reason}", "Blocked", timestamp, request.TaskCardId);
                continue;
            }

            var fullPath = decision.NormalizedPath;
            if (!IsSupportedTextFile(fullPath) || LooksBinary(fullPath))
            {
                skipped.Add(fullPath);
                continue;
            }

            try
            {
                var text = File.ReadAllText(fullPath, Encoding.UTF8);
                var sha256 = ComputeSha256Bytes(File.ReadAllBytes(fullPath));
                var docId = ComputeSha256($"{fullPath}|{sha256}");
                var chunks = BuildChunks(docId, fullPath, sha256, text, request.ChunkSizeTokens, request.ChunkOverlapTokens, timestamp);
                _memoryStore.AddDocumentChunks(request.PetId, docId, fullPath, sha256, chunks, timestamp);
                readPaths.Add(fullPath);
                filesIndexed++;
                chunksWritten += chunks.Count;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or DecoderFallbackException)
            {
                skipped.Add(fullPath);
                Record("local_doc_ingest", $"Skipped unreadable file {fullPath}: {ex.GetType().Name}.", "Skipped", timestamp, request.TaskCardId);
            }
        }

        var manifestPath = Path.Combine(artifactRoot, "manifest.json");
        var manifest = new
        {
            schemaVersion = "1",
            generatedAtUtc = timestamp,
            filesIndexed,
            chunksWritten,
            readPaths,
            skippedPaths = skipped,
            didUseNetwork = false,
            didUseHostedAi = false,
            didMutate = false
        };
        File.WriteAllText(manifestPath, JsonSerializer.Serialize(manifest, JsonDefaults.Options));
        Record("local_doc_ingest", $"Indexed {filesIndexed} file(s), wrote {chunksWritten} chunk(s).", "Completed", timestamp, request.TaskCardId, manifestPath);
        return new LocalDocumentIngestResult(true, filesIndexed, chunksWritten, readPaths, skipped, manifestPath, $"Indexed {filesIndexed} file(s), wrote {chunksWritten} chunk(s).");
    }

    private IReadOnlyList<RetrievalChunk> BuildChunks(string docId, string path, string sha256, string text, int chunkSizeTokens, int overlapTokens, DateTimeOffset timestamp)
    {
        var tokens = TokenizeWithOffsets(text);
        var size = Math.Clamp(chunkSizeTokens, 32, 2048);
        var overlap = Math.Clamp(overlapTokens, 0, Math.Max(0, size - 1));
        var step = Math.Max(1, size - overlap);
        var chunks = new List<RetrievalChunk>();
        for (var start = 0; start < tokens.Count; start += step)
        {
            var window = tokens.Skip(start).Take(size).ToList();
            if (window.Count == 0)
            {
                break;
            }

            var byteStart = window.First().ByteStart;
            var byteEnd = window.Last().ByteEnd;
            var chunkText = text.Substring(window.First().CharStart, window.Last().CharEnd - window.First().CharStart);
            var chunkId = $"{docId}-{chunks.Count:0000}";
            chunks.Add(new RetrievalChunk(chunkId, docId, path, sha256, chunkText, byteStart, byteEnd, _embeddingService.Embed(chunkText), timestamp));
            if (start + size >= tokens.Count)
            {
                break;
            }
        }

        if (chunks.Count == 0 && !string.IsNullOrWhiteSpace(text))
        {
            chunks.Add(new RetrievalChunk($"{docId}-0000", docId, path, sha256, text, 0, Encoding.UTF8.GetByteCount(text), _embeddingService.Embed(text), timestamp));
        }

        return chunks;
    }

    private static IReadOnlyList<string> ResolveFiles(IReadOnlyList<string> paths)
    {
        return (paths ?? [])
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(Path.GetFullPath)
            .SelectMany(path =>
            {
                if (File.Exists(path))
                {
                    return [path];
                }

                return Directory.Exists(path)
                    ? Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories)
                    : [];
            })
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static bool IsSupportedTextFile(string path)
    {
        return TextExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);
    }

    private static bool LooksBinary(string path)
    {
        var bytes = File.ReadAllBytes(path);
        return bytes.Take(Math.Min(bytes.Length, 4096)).Any(value => value == 0);
    }

    private static IReadOnlyList<TokenSpan> TokenizeWithOffsets(string text)
    {
        var tokens = new List<TokenSpan>();
        var start = -1;
        for (var index = 0; index <= text.Length; index++)
        {
            var atEnd = index == text.Length;
            var isToken = !atEnd && char.IsLetterOrDigit(text[index]);
            if (isToken && start < 0)
            {
                start = index;
            }
            else if ((!isToken || atEnd) && start >= 0)
            {
                var end = index;
                tokens.Add(new TokenSpan(
                    start,
                    end,
                    Encoding.UTF8.GetByteCount(text[..start]),
                    Encoding.UTF8.GetByteCount(text[..end])));
                start = -1;
            }
        }

        return tokens;
    }

    private static string ResolveArtifactRoot(string artifactRoot, DateTimeOffset timestamp)
    {
        return string.IsNullOrWhiteSpace(artifactRoot)
            ? Path.GetFullPath(Path.Combine("vnext", "artifacts", "pet-tasks", $"{timestamp:yyyyMMdd-HHmmss}-doc-ingest"))
            : Path.GetFullPath(artifactRoot);
    }

    private void Record(string kind, string summary, string status, DateTimeOffset timestamp, Guid? taskCardId, string artifactPath = "")
    {
        _auditLedgerService?.Record(new EvidencePacket(
            Guid.NewGuid(),
            kind,
            taskCardId,
            timestamp,
            DidUseNetwork: false,
            DidUseHostedAi: false,
            DidUseLocalModel: false,
            DidMutate: false,
            ArtifactPath: artifactPath,
            summary,
            status,
            status.Equals("Blocked", StringComparison.OrdinalIgnoreCase) ? summary : ""));
    }

    private static string ComputeSha256(string value)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    }

    private static string ComputeSha256Bytes(byte[] bytes)
    {
        return Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    }

    private sealed record TokenSpan(int CharStart, int CharEnd, int ByteStart, int ByteEnd);
}
