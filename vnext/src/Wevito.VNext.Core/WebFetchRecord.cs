namespace Wevito.VNext.Core;

public sealed record WebFetchRecord(
    Guid Id,
    string Backend,
    string Query,
    string SanitizedQuery,
    string Url,
    string Title,
    string Snippet,
    DateTimeOffset FetchedAtUtc,
    bool FromCache,
    string CachePath,
    string Citation);

public sealed record WebResearchRequest(
    Guid TaskCardId,
    bool ApprovedTaskCard,
    string Query,
    string Backend,
    string ArtifactRoot,
    string CacheRoot,
    IReadOnlyDictionary<string, string> Settings,
    RuntimeSupervisorStatus RuntimeStatus,
    int FetchesUsedThisHour = 0,
    int FetchesUsedForTask = 0,
    bool ForceRefresh = false,
    DateTimeOffset RequestedAtUtc = default);

public sealed record WebResearchResult(
    bool Succeeded,
    bool Blocked,
    string BlockReason,
    IReadOnlyList<WebFetchRecord> Records,
    string ArtifactFolder,
    string EvidencePath,
    string SummaryPath);
