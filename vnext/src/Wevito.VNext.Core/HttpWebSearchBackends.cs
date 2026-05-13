using System.Net.Http.Headers;
using System.Text.Json;
using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public abstract class HttpWebSearchBackend : IWebSearchBackend
{
    private readonly HttpClient _httpClient;
    private readonly IWebCredentialStore _credentialStore;

    protected HttpWebSearchBackend(HttpClient? httpClient = null, IWebCredentialStore? credentialStore = null)
    {
        _httpClient = httpClient ?? new HttpClient();
        _credentialStore = credentialStore ?? new WindowsCredentialStore();
    }

    public abstract string BackendId { get; }

    public bool UsesNetwork => true;

    protected abstract Uri BuildUri(string query);

    protected virtual void ApplyHeaders(HttpRequestMessage request, string apiKey)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
    }

    public async Task<IReadOnlyList<WebFetchRecord>> SearchAsync(WebSearchBackendRequest request, CancellationToken cancellationToken = default)
    {
        var dayRoot = Path.Combine(request.CacheRoot, request.RequestedAtUtc.ToString("yyyyMMdd"));
        Directory.CreateDirectory(dayRoot);
        var cachePath = Path.Combine(dayRoot, $"{Sha256Text($"{BackendId}|{request.Query}")}.json");
        if (File.Exists(cachePath) && !request.ForceRefresh)
        {
            var cached = JsonSerializer.Deserialize<WebFetchRecord>(await File.ReadAllTextAsync(cachePath, cancellationToken).ConfigureAwait(false), JsonDefaults.Options);
            return cached is null ? [] : [cached with { FromCache = true }];
        }

        var secret = await _credentialStore.ReadSecretAsync(WindowsCredentialStore.BuildWebSearchTargetName(BackendId), cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(secret))
        {
            throw new InvalidOperationException($"Missing API key for {BackendId} in Windows Credential Manager target {WindowsCredentialStore.BuildWebSearchTargetName(BackendId)}.");
        }

        using var httpRequest = new HttpRequestMessage(HttpMethod.Get, BuildUri(request.Query));
        ApplyHeaders(httpRequest, secret);
        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        var record = new WebFetchRecord(
            Guid.NewGuid(),
            BackendId,
            request.Query,
            request.Query,
            BuildUri(request.Query).ToString(),
            $"{BackendId} search result",
            body.Length > 500 ? body[..500] : body,
            request.RequestedAtUtc,
            FromCache: false,
            cachePath,
            BuildUri(request.Query).ToString());
        await File.WriteAllTextAsync(cachePath, JsonSerializer.Serialize(record, JsonDefaults.Options), cancellationToken).ConfigureAwait(false);
        return [record];
    }

    private static string Sha256Text(string value)
    {
        return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    }
}

public sealed class BraveSearchBackend : HttpWebSearchBackend
{
    public BraveSearchBackend(HttpClient? httpClient = null, IWebCredentialStore? credentialStore = null)
        : base(httpClient, credentialStore)
    {
    }

    public override string BackendId => "brave";

    protected override Uri BuildUri(string query)
    {
        return new Uri($"https://api.search.brave.com/res/v1/web/search?q={Uri.EscapeDataString(query)}");
    }

    protected override void ApplyHeaders(HttpRequestMessage request, string apiKey)
    {
        request.Headers.Add("X-Subscription-Token", apiKey);
    }
}

public sealed class TavilySearchBackend : HttpWebSearchBackend
{
    public TavilySearchBackend(HttpClient? httpClient = null, IWebCredentialStore? credentialStore = null)
        : base(httpClient, credentialStore)
    {
    }

    public override string BackendId => "tavily";

    protected override Uri BuildUri(string query)
    {
        return new Uri($"https://api.tavily.com/search?query={Uri.EscapeDataString(query)}");
    }
}

public sealed class FirecrawlSearchBackend : HttpWebSearchBackend
{
    public FirecrawlSearchBackend(HttpClient? httpClient = null, IWebCredentialStore? credentialStore = null)
        : base(httpClient, credentialStore)
    {
    }

    public override string BackendId => "firecrawl";

    protected override Uri BuildUri(string query)
    {
        return new Uri($"https://api.firecrawl.dev/v1/search?q={Uri.EscapeDataString(query)}");
    }
}
