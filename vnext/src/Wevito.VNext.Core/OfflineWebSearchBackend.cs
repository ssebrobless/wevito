namespace Wevito.VNext.Core;

public sealed class OfflineWebSearchBackend : IWebSearchBackend
{
    public string BackendId => "offline";

    public bool UsesNetwork => false;

    public Task<IReadOnlyList<WebFetchRecord>> SearchAsync(WebSearchBackendRequest request, CancellationToken cancellationToken = default)
    {
        var cachePath = Path.Combine(request.CacheRoot, request.RequestedAtUtc.ToString("yyyyMMdd"), "offline-placeholder.json");
        var record = new WebFetchRecord(
            Guid.NewGuid(),
            BackendId,
            request.Query,
            request.Query,
            "about:wevito-offline-web-research",
            "Offline web research placeholder",
            "Web research is disabled by default. Approve a network backend before fetching live sources.",
            request.RequestedAtUtc,
            FromCache: false,
            cachePath,
            "offline-placeholder");
        return Task.FromResult<IReadOnlyList<WebFetchRecord>>([record]);
    }
}
