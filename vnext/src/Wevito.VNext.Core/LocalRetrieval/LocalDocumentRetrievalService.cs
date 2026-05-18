using System.Security.Cryptography;
using Wevito.VNext.Core.Settings;

namespace Wevito.VNext.Core.LocalRetrieval;

public sealed class LocalDocumentRetrievalService
{
    public const string IndexBuiltPacketKind = "local_document_index_built";
    public const string QueryCompletedPacketKind = "local_document_query_completed";
    public const string IndexRefusedPacketKind = "local_document_index_refused";

    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".md",
        ".txt",
        ".json"
    };

    private readonly Func<IReadOnlyDictionary<string, string>> _settingsProvider;
    private readonly AuditLedgerService? _auditLedgerService;
    private readonly KillSwitchService? _killSwitchService;
    private readonly LocalDocumentIndex _index;
    private readonly Func<string, IEnumerable<string>> _fileEnumerator;
    private readonly Func<DateTimeOffset> _clock;

    public LocalDocumentRetrievalService(
        Func<IReadOnlyDictionary<string, string>> settingsProvider,
        AuditLedgerService? auditLedgerService = null,
        KillSwitchService? killSwitchService = null,
        LocalDocumentIndex? index = null,
        Func<string, IEnumerable<string>>? fileEnumerator = null,
        Func<DateTimeOffset>? clock = null)
    {
        _settingsProvider = settingsProvider;
        _auditLedgerService = auditLedgerService;
        _killSwitchService = killSwitchService;
        _index = index ?? new LocalDocumentIndex();
        _fileEnumerator = fileEnumerator ?? (root => Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories));
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
    }

    public string IndexPath => _index.IndexPath;

    public async Task<LocalDocumentIndexBuildResult> BuildIndexAsync(CancellationToken ct)
    {
        if (_killSwitchService?.IsActive() == true)
        {
            RecordRefused("killswitch", "Local document indexing refused because Stop Everything is active.");
            return Refused("killswitch");
        }

        var settings = _settingsProvider();
        if (!ReadBool(settings, SettingKeys.LocalDocumentRetrievalEnabled))
        {
            RecordRefused("capability_disabled", "Local document indexing refused because local document retrieval is disabled.");
            return Refused("capability_disabled");
        }

        var root = ResolveRoot(settings);
        var maxBytes = ResolveMaxBytes(settings);
        Directory.CreateDirectory(root);
        var indexedFiles = 0;
        var skippedFiles = 0;
        var indexedLines = 0;
        var skippedPaths = new List<string>();

        foreach (var candidate in _fileEnumerator(root))
        {
            ct.ThrowIfCancellationRequested();
            var extension = Path.GetExtension(candidate);
            string relativePath;
            try
            {
                relativePath = LocalDocumentIndex.NormalizeInsideRoot(root, candidate);
            }
            catch (LocalDocumentSandboxException)
            {
                skippedFiles++;
                skippedPaths.Add(candidate);
                RecordRefused("outside_root", $"Skipped local document outside root: {candidate}");
                continue;
            }

            if (!SupportedExtensions.Contains(extension))
            {
                skippedFiles++;
                skippedPaths.Add(relativePath);
                RecordRefused("unsupported_extension", $"Skipped unsupported local document extension: {relativePath}");
                continue;
            }

            var fileInfo = new FileInfo(Path.GetFullPath(candidate));
            if (fileInfo.Length > maxBytes)
            {
                skippedFiles++;
                skippedPaths.Add(relativePath);
                RecordRefused("size_cap", $"Skipped local document over size cap: {relativePath} ({fileInfo.Length} bytes).");
                continue;
            }

            var sha256 = await ComputeSha256Async(fileInfo.FullName, ct).ConfigureAwait(false);
            if (_index.IsIndexed(relativePath, sha256))
            {
                skippedFiles++;
                continue;
            }

            var lines = await File.ReadAllLinesAsync(fileInfo.FullName, ct).ConfigureAwait(false);
            _index.IndexLines(relativePath, sha256, fileInfo.Length, lines, _clock());
            indexedFiles++;
            indexedLines += lines.Length;
        }

        Record(IndexBuiltPacketKind, true, _index.IndexPath, $"Built local document index. indexed_files={indexedFiles}; skipped_files={skippedFiles}; indexed_lines={indexedLines}.", "Completed");
        return new LocalDocumentIndexBuildResult(true, "Completed", "ok", indexedFiles, skippedFiles, indexedLines, _index.IndexPath, skippedPaths);
    }

    public Task<IReadOnlyList<LocalDocumentSnippet>> QueryAsync(string query, int topN, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (_killSwitchService?.IsActive() == true)
        {
            RecordRefused("killswitch", "Local document query refused because Stop Everything is active.");
            return Task.FromResult<IReadOnlyList<LocalDocumentSnippet>>([]);
        }

        var settings = _settingsProvider();
        if (!ReadBool(settings, SettingKeys.LocalDocumentRetrievalEnabled))
        {
            RecordRefused("capability_disabled", "Local document query refused because local document retrieval is disabled.");
            return Task.FromResult<IReadOnlyList<LocalDocumentSnippet>>([]);
        }

        var results = _index.Query(query, topN);
        Record(QueryCompletedPacketKind, false, _index.IndexPath, $"Completed local document query. results={results.Count}; top_n={Math.Clamp(topN, 1, 50)}.", "Completed");
        return Task.FromResult(results);
    }

    private static async Task<string> ComputeSha256Async(string path, CancellationToken ct)
    {
        await using var stream = File.OpenRead(path);
        var hash = await SHA256.HashDataAsync(stream, ct).ConfigureAwait(false);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private string ResolveRoot(IReadOnlyDictionary<string, string> settings)
    {
        return settings.TryGetValue(SettingKeys.LocalDocumentRetrievalRoot, out var configured) && !string.IsNullOrWhiteSpace(configured)
            ? Path.GetFullPath(Environment.ExpandEnvironmentVariables(configured))
            : SettingKeys.DefaultLocalDocumentRetrievalRoot();
    }

    private static long ResolveMaxBytes(IReadOnlyDictionary<string, string> settings)
    {
        return settings.TryGetValue(SettingKeys.LocalDocumentRetrievalMaxFileBytes, out var raw) &&
            long.TryParse(raw, out var parsed) &&
            parsed > 0
            ? parsed
            : long.Parse(SettingKeys.LocalDocumentRetrievalDefaultMaxFileBytes);
    }

    private static bool ReadBool(IReadOnlyDictionary<string, string> settings, string key)
    {
        return settings.TryGetValue(key, out var raw) && bool.TryParse(raw, out var parsed) && parsed;
    }

    private LocalDocumentIndexBuildResult Refused(string reason)
    {
        return new LocalDocumentIndexBuildResult(false, "Refused", reason, 0, 0, 0, _index.IndexPath, []);
    }

    private void RecordRefused(string reason, string summary)
    {
        Record(IndexRefusedPacketKind, false, _index.IndexPath, $"{summary} reason={reason}", "Refused");
    }

    private void Record(string packetKind, bool didMutate, string artifactPath, string summary, string status)
    {
        _auditLedgerService?.Record(new EvidencePacket(
            Guid.NewGuid(),
            packetKind,
            null,
            _clock(),
            DidUseNetwork: false,
            DidUseHostedAi: false,
            DidUseLocalModel: false,
            DidMutate: didMutate,
            ArtifactPath: artifactPath,
            Summary: summary,
            Status: status));
    }
}
