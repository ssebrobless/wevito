using System.Net.Http.Headers;

namespace Wevito.VNext.Core;

public sealed class LocalRuntimeProbeService
{
    public const string OllamaEndpointSetting = "local_runtime_ollama_endpoint";
    public const string OllamaModelSetting = "local_runtime_ollama_model";
    public const string DefaultOllamaEndpoint = "http://127.0.0.1:11434";
    public const string DefaultOllamaModel = "llama3.2:3b";

    private readonly HttpClient _httpClient;
    private readonly KillSwitchService? _killSwitchService;

    public LocalRuntimeProbeService(HttpClient? httpClient = null, KillSwitchService? killSwitchService = null)
    {
        _httpClient = httpClient ?? new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
        _killSwitchService = killSwitchService;
    }

    public async Task<LocalRuntimeProbeResult> ProbeAsync(
        IReadOnlyDictionary<string, string>? settings = null,
        RuntimeSupervisorStatus? runtimeStatus = null,
        DateTimeOffset? nowUtc = null,
        CancellationToken cancellationToken = default)
    {
        var timestamp = nowUtc ?? DateTimeOffset.UtcNow;
        var endpoint = Read(settings, OllamaEndpointSetting, DefaultOllamaEndpoint);
        var model = Read(settings, OllamaModelSetting, DefaultOllamaModel);

        if (_killSwitchService?.IsActive() == true)
        {
            return Unavailable(endpoint, model, "kill_switch=true", timestamp);
        }

        if (runtimeStatus?.Mode is RuntimeSupervisorMode.Quiet or RuntimeSupervisorMode.PetOnly)
        {
            return new LocalRuntimeProbeResult(false, true, "ollama", endpoint, model, "Runtime supervisor is Quiet or PetOnly; local runtime probe is dormant.", timestamp);
        }

        if (!IsLocalhostEndpoint(endpoint, out var baseUri, out var reason))
        {
            return Unavailable(endpoint, model, reason, timestamp);
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(baseUri, "/api/tags"));
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return Unavailable(endpoint, model, $"Ollama probe failed with HTTP {(int)response.StatusCode}.", timestamp);
            }

            return new LocalRuntimeProbeResult(true, false, "ollama", endpoint, model, "Ollama runtime responded to /api/tags.", timestamp);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            return Unavailable(endpoint, model, $"Ollama probe failed: {ex.GetType().Name}.", timestamp);
        }
    }

    public static bool IsLocalhostEndpoint(string endpoint, out Uri baseUri, out string reason)
    {
        baseUri = default!;
        reason = "";
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var parsed))
        {
            reason = "Local runtime endpoint is not a valid absolute URI.";
            return false;
        }

        if (parsed.Scheme is not ("http" or "https"))
        {
            reason = "Local runtime endpoint must use http or https.";
            return false;
        }

        if (!parsed.IsLoopback)
        {
            reason = "Local runtime endpoint must be localhost, 127.0.0.1, or ::1.";
            return false;
        }

        baseUri = parsed;
        return true;
    }

    private static LocalRuntimeProbeResult Unavailable(string endpoint, string model, string reason, DateTimeOffset timestamp)
    {
        return new LocalRuntimeProbeResult(false, false, "ollama", endpoint, model, reason, timestamp);
    }

    private static string Read(IReadOnlyDictionary<string, string>? settings, string key, string defaultValue)
    {
        return settings is not null &&
               settings.TryGetValue(key, out var raw) &&
               !string.IsNullOrWhiteSpace(raw)
            ? raw
            : defaultValue;
    }
}
