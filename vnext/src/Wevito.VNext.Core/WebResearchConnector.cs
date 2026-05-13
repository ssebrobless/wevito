using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed class WebResearchConnector
{
    public const string WebSearchEnabledSetting = "web_search_enabled";
    public const string WebBackendSetting = "web_search_backend";
    public const string MaxFetchesPerHourSetting = "web_max_fetches_per_hour";
    public const string MaxFetchesPerTaskSetting = "web_max_fetches_per_task";

    private readonly IReadOnlyDictionary<string, IWebSearchBackend> _backends;
    private readonly WebQueryPrivacyFilter _privacyFilter;
    private readonly RuntimeSupervisorService _supervisorService;
    private readonly AuditLedgerService? _ledger;
    private readonly KillSwitchService? _killSwitch;

    public WebResearchConnector(
        IEnumerable<IWebSearchBackend>? backends = null,
        WebQueryPrivacyFilter? privacyFilter = null,
        RuntimeSupervisorService? supervisorService = null,
        AuditLedgerService? ledger = null,
        KillSwitchService? killSwitch = null)
    {
        _backends = (backends ?? [new OfflineWebSearchBackend()])
            .GroupBy(backend => backend.BackendId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        _privacyFilter = privacyFilter ?? new WebQueryPrivacyFilter();
        _supervisorService = supervisorService ?? new RuntimeSupervisorService();
        _ledger = ledger;
        _killSwitch = killSwitch;
    }

    public async Task<WebResearchResult> FetchAsync(WebResearchRequest request, CancellationToken cancellationToken = default)
    {
        var timestamp = request.RequestedAtUtc == default ? DateTimeOffset.UtcNow : request.RequestedAtUtc;
        if (KillSwitchService.IsActive(request.Settings) || _killSwitch?.IsActive() == true)
        {
            return Block(request, timestamp, "kill_switch=true");
        }

        if (!request.ApprovedTaskCard)
        {
            return Block(request, timestamp, "Approved task card is required before web fetch.");
        }

        if (!_supervisorService.CanStartUserInitiatedWork(request.RuntimeStatus, out var supervisorReason))
        {
            return Block(request, timestamp, supervisorReason);
        }

        var backendId = ResolveBackendId(request);
        var backend = _backends.TryGetValue(backendId, out var found) ? found : _backends["offline"];
        var enabled = ReadBool(request.Settings, WebSearchEnabledSetting, false);
        if (backend.UsesNetwork && !enabled)
        {
            return Block(request, timestamp, "Web search is disabled.");
        }

        var maxPerHour = ReadInt(request.Settings, MaxFetchesPerHourSetting, 30);
        var maxPerTask = ReadInt(request.Settings, MaxFetchesPerTaskSetting, 5);
        if (backend.UsesNetwork && request.FetchesUsedThisHour >= maxPerHour)
        {
            return Block(request, timestamp, "Hourly web fetch limit reached.");
        }

        if (backend.UsesNetwork && request.FetchesUsedForTask >= maxPerTask)
        {
            return Block(request, timestamp, "Per-task web fetch limit reached.");
        }

        var privacy = _privacyFilter.Sanitize(request.Query);
        if (!privacy.Allowed)
        {
            return Block(request, timestamp, privacy.Reason);
        }

        var cacheRoot = ResolveCacheRoot(request.CacheRoot);
        var connectorCachePath = ResolveConnectorCachePath(cacheRoot, timestamp, backend.BackendId, privacy.SanitizedQuery);
        IReadOnlyList<WebFetchRecord> records;
        if (File.Exists(connectorCachePath) && !request.ForceRefresh)
        {
            records = JsonSerializer.Deserialize<IReadOnlyList<WebFetchRecord>>(
                await File.ReadAllTextAsync(connectorCachePath, cancellationToken).ConfigureAwait(false),
                JsonDefaults.Options) ?? [];
            records = records.Select(record => record with { FromCache = true }).ToList();
        }
        else
        {
            records = await backend.SearchAsync(new WebSearchBackendRequest(
                privacy.SanitizedQuery,
                cacheRoot,
                timestamp,
                request.ForceRefresh), cancellationToken).ConfigureAwait(false);
            Directory.CreateDirectory(Path.GetDirectoryName(connectorCachePath) ?? cacheRoot);
            await File.WriteAllTextAsync(connectorCachePath, JsonSerializer.Serialize(records, JsonDefaults.Options), cancellationToken).ConfigureAwait(false);
        }
        var folder = ResolveArtifactFolder(request.ArtifactRoot, timestamp);
        Directory.CreateDirectory(folder);
        var evidencePath = Path.Combine(folder, "web-fetch-records.json");
        var summaryPath = Path.Combine(folder, "run-summary.md");
        await File.WriteAllTextAsync(evidencePath, JsonSerializer.Serialize(records, JsonDefaults.Options), cancellationToken).ConfigureAwait(false);
        await File.WriteAllTextAsync(summaryPath, BuildSummary(request, backend, records, privacy), cancellationToken).ConfigureAwait(false);
        _ledger?.Record(new EvidencePacket(
            Guid.NewGuid(),
            "web_fetch",
            request.TaskCardId,
            timestamp,
            backend.UsesNetwork && records.Count > 0,
            DidUseHostedAi: false,
            DidUseLocalModel: false,
            DidMutate: false,
            folder,
            $"Fetched {records.Count} web research record(s) with backend {backend.BackendId}.",
            "Completed"));

        return new WebResearchResult(true, false, "", records, folder, evidencePath, summaryPath);
    }

    private WebResearchResult Block(WebResearchRequest request, DateTimeOffset timestamp, string reason)
    {
        _ledger?.Record(new EvidencePacket(
            Guid.NewGuid(),
            "web_fetch",
            request.TaskCardId,
            timestamp,
            DidUseNetwork: false,
            DidUseHostedAi: false,
            DidUseLocalModel: false,
            DidMutate: false,
            ArtifactPath: "",
            $"Blocked web fetch: {reason}",
            "Blocked",
            reason));
        return new WebResearchResult(false, true, reason, [], "", "", "");
    }

    private string ResolveBackendId(WebResearchRequest request)
    {
        var requested = string.IsNullOrWhiteSpace(request.Backend)
            ? request.Settings.TryGetValue(WebBackendSetting, out var configured) ? configured : "offline"
            : request.Backend;
        return _backends.ContainsKey(requested) ? requested : "offline";
    }

    private static string ResolveArtifactFolder(string artifactRoot, DateTimeOffset timestamp)
    {
        var root = string.IsNullOrWhiteSpace(artifactRoot)
            ? Path.Combine("vnext", "artifacts", "pet-tasks")
            : artifactRoot;
        return Path.GetFullPath(Path.Combine(root, $"{timestamp:yyyyMMdd-HHmmss}-web-research"));
    }

    private static string ResolveCacheRoot(string cacheRoot)
    {
        return string.IsNullOrWhiteSpace(cacheRoot)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Wevito", "web-cache")
            : cacheRoot;
    }

    private static string ResolveConnectorCachePath(string cacheRoot, DateTimeOffset timestamp, string backend, string query)
    {
        return Path.Combine(cacheRoot, timestamp.ToString("yyyyMMdd"), $"{HashText($"{backend}|{query}")}.json");
    }

    private static string BuildSummary(WebResearchRequest request, IWebSearchBackend backend, IReadOnlyList<WebFetchRecord> records, WebQueryPrivacyFilterResult privacy)
    {
        return string.Join(Environment.NewLine, [
            "# Web Research Fetch",
            "",
            $"- Backend: {backend.BackendId}",
            $"- Network used: {(backend.UsesNetwork && records.Count > 0).ToString().ToLowerInvariant()}",
            "- Hosted AI used: false",
            "- Mutated files: false",
            $"- Original query hash: {HashText(request.Query)}",
            $"- Sanitized query: {privacy.SanitizedQuery}",
            $"- Redactions: {(privacy.Redactions.Count == 0 ? "none" : string.Join(", ", privacy.Redactions))}",
            $"- Records: {records.Count}",
            "",
            "## Citations",
            "",
            records.Count == 0 ? "No records fetched." : string.Join(Environment.NewLine, records.Select(record => $"- {record.Title}: {record.Citation}"))
        ]);
    }

    private static bool ReadBool(IReadOnlyDictionary<string, string> settings, string key, bool defaultValue)
    {
        return settings.TryGetValue(key, out var raw) && bool.TryParse(raw, out var parsed) ? parsed : defaultValue;
    }

    private static int ReadInt(IReadOnlyDictionary<string, string> settings, string key, int defaultValue)
    {
        return settings.TryGetValue(key, out var raw) && int.TryParse(raw, out var parsed) ? parsed : defaultValue;
    }

    private static string HashText(string text)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text ?? ""))).ToLowerInvariant();
    }
}
