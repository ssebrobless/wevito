namespace Wevito.VNext.Core;

public interface IWebSearchBackend
{
    string BackendId { get; }

    bool UsesNetwork { get; }

    Task<IReadOnlyList<WebFetchRecord>> SearchAsync(WebSearchBackendRequest request, CancellationToken cancellationToken = default);
}

public sealed record WebSearchBackendRequest(
    string Query,
    string CacheRoot,
    DateTimeOffset RequestedAtUtc,
    bool ForceRefresh = false);
