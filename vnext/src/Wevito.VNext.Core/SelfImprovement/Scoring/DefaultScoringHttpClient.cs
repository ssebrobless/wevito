using System.Net.Http;
using System.Text;

namespace Wevito.VNext.Core.SelfImprovement.Scoring;

public sealed record DefaultScoringHttpClient : IScoringHttpClient
{
    private readonly KillSwitchService? _killSwitchService;
    private readonly HttpClient _httpClient;

    public DefaultScoringHttpClient(KillSwitchService? killSwitchService = null, HttpClient? httpClient = null)
    {
        _killSwitchService = killSwitchService;
        _httpClient = httpClient ?? new HttpClient();
    }

    public override async Task<string> PostAsync(Uri uri, string body, CancellationToken cancellationToken)
    {
        if (_killSwitchService?.IsActive() == true)
        {
            throw new InvalidOperationException("kill_switch=true");
        }

        if (!string.Equals(uri.Host, "127.0.0.1", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("non_loopback_endpoint");
        }

        using var content = new StringContent(body, Encoding.UTF8, "application/json");
        using var response = await _httpClient.PostAsync(uri, content, cancellationToken).ConfigureAwait(false);
        return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
    }
}
