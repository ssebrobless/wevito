using System.Net.Http.Headers;

namespace Wevito.VNext.Core;

public sealed class LocalRuntimeProbeService
{
    public const string OllamaEndpointSetting = "local_runtime_ollama_endpoint";
    public const string OllamaModelSetting = "local_runtime_ollama_model";
    public const string DefaultOllamaEndpoint = "http://127.0.0.1:11434";
    public const string DefaultOllamaModel = "qwen2.5:7b-instruct-q4_K_M";

    private readonly HttpClient _httpClient;
    private readonly KillSwitchService? _killSwitchService;
    private readonly Queue<LocalRuntimeProbeResult> _recentProbeHistory = new();

    public LocalRuntimeProbeService(HttpClient? httpClient = null, KillSwitchService? killSwitchService = null)
    {
        _httpClient = httpClient ?? new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
        _killSwitchService = killSwitchService;
    }

    public IReadOnlyList<LocalRuntimeProbeResult> RecentProbeHistory => _recentProbeHistory.ToList();

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
            return Remember(Unavailable(endpoint, model, "kill_switch=true", timestamp));
        }

        if (runtimeStatus?.Mode is RuntimeSupervisorMode.Quiet or RuntimeSupervisorMode.PetOnly)
        {
            return Remember(new LocalRuntimeProbeResult(false, true, "ollama", endpoint, model, "Runtime supervisor is Quiet or PetOnly; local runtime probe is dormant.", timestamp));
        }

        if (!IsLocalhostEndpoint(endpoint, out var baseUri, out var reason))
        {
            return Remember(Unavailable(endpoint, model, reason, timestamp));
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(baseUri, "/api/tags"));
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return Remember(Unavailable(endpoint, model, $"Ollama probe failed with HTTP {(int)response.StatusCode}.", timestamp));
            }

            return Remember(new LocalRuntimeProbeResult(true, false, "ollama", endpoint, model, "Ollama runtime responded to /api/tags.", timestamp));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            return Remember(Unavailable(endpoint, model, $"Ollama probe failed: {ex.GetType().Name}.", timestamp));
        }
    }

    public string FormatRecentProbeStatus()
    {
        var last = _recentProbeHistory.LastOrDefault();
        if (last is null)
        {
            return "Local runtime probe has not run yet.";
        }

        var state = last.WasDormant
            ? "dormant"
            : last.IsAvailable
                ? "available"
                : "unavailable";
        return $"Local runtime probe: {state}; runtime={last.RuntimeId}; endpoint={last.Endpoint}; model={last.Model}; reason={last.Reason}; last={last.ProbedAtUtc:O}";
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

    private LocalRuntimeProbeResult Remember(LocalRuntimeProbeResult result)
    {
        _recentProbeHistory.Enqueue(result);
        while (_recentProbeHistory.Count > 10)
        {
            _recentProbeHistory.Dequeue();
        }

        return result;
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
