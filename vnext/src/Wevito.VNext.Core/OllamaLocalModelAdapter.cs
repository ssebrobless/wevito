using System.Diagnostics;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed class OllamaLocalModelAdapter : IModelAdapter
{
    public const string Provider = "ollama";
    private readonly HttpClient _httpClient;
    private readonly LocalRuntimeProbeService _probeService;
    private readonly LocalModelAdapter _fallbackAdapter;
    private readonly Func<IReadOnlyDictionary<string, string>> _settingsProvider;
    private readonly KillSwitchService? _killSwitchService;

    public OllamaLocalModelAdapter(
        HttpClient? httpClient = null,
        LocalRuntimeProbeService? probeService = null,
        LocalModelAdapter? fallbackAdapter = null,
        Func<IReadOnlyDictionary<string, string>>? settingsProvider = null,
        KillSwitchService? killSwitchService = null)
    {
        _httpClient = httpClient ?? new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
        _killSwitchService = killSwitchService;
        _probeService = probeService ?? new LocalRuntimeProbeService(_httpClient, killSwitchService);
        _fallbackAdapter = fallbackAdapter ?? new LocalModelAdapter(killSwitchService);
        _settingsProvider = settingsProvider ?? (() => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
    }

    public async Task<ModelResponse> SuggestAsync(ModelRequest request, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var auditPath = ResolveAuditPath(request);
        var settings = _settingsProvider();
        var endpoint = Read(settings, LocalRuntimeProbeService.OllamaEndpointSetting, LocalRuntimeProbeService.DefaultOllamaEndpoint);
        var model = Read(settings, LocalRuntimeProbeService.OllamaModelSetting, LocalRuntimeProbeService.DefaultOllamaModel);

        if (_killSwitchService?.IsActive() == true)
        {
            stopwatch.Stop();
            await WriteEvidenceAsync(request, auditPath, model, "", stopwatch.Elapsed, didUseLocalModel: false, didFallback: false, "kill_switch=true", cancellationToken).ConfigureAwait(false);
            return new ModelResponse(Provider, model, "", DidCallProvider: false, BlockReason: "kill_switch=true", AuditLogPath: auditPath, Latency: stopwatch.Elapsed);
        }

        if (!LocalRuntimeProbeService.IsLocalhostEndpoint(endpoint, out var baseUri, out var endpointReason))
        {
            return await FallbackAsync(request, auditPath, model, stopwatch, endpointReason, cancellationToken).ConfigureAwait(false);
        }

        var probe = await _probeService.ProbeAsync(settings, null, request.RequestedAtUtc == default ? DateTimeOffset.UtcNow : request.RequestedAtUtc, cancellationToken).ConfigureAwait(false);
        if (!probe.IsAvailable)
        {
            return await FallbackAsync(request, auditPath, model, stopwatch, probe.Reason, cancellationToken).ConfigureAwait(false);
        }

        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, new Uri(baseUri, "/v1/chat/completions"));
            httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpRequest.Content = new StringContent(JsonSerializer.Serialize(BuildPayload(request, model)), Encoding.UTF8, "application/json");

            using var response = await _httpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);
            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            stopwatch.Stop();

            if (!response.IsSuccessStatusCode)
            {
                return await FallbackAsync(request, auditPath, model, stopwatch, $"Ollama chat failed with HTTP {(int)response.StatusCode}.", cancellationToken).ConfigureAwait(false);
            }

            var summary = ExtractOpenAiCompatibleSummary(body);
            await WriteEvidenceAsync(request, auditPath, model, summary, stopwatch.Elapsed, didUseLocalModel: true, didFallback: false, "", cancellationToken).ConfigureAwait(false);
            return new ModelResponse(Provider, model, summary, DidCallProvider: true, AuditLogPath: auditPath, Latency: stopwatch.Elapsed);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException or JsonException)
        {
            return await FallbackAsync(request, auditPath, model, stopwatch, $"Ollama local model call failed: {ex.GetType().Name}.", cancellationToken).ConfigureAwait(false);
        }
    }

    public static string FormatReadableStatus(LocalRuntimeProbeResult? probe)
    {
        if (probe is null)
        {
            return "Ollama status: not probed.";
        }

        var state = probe.WasDormant
            ? "dormant"
            : probe.IsAvailable
                ? "available"
                : "unavailable";
        return $"Ollama status: {state}; endpoint={probe.Endpoint}; model={probe.Model}; reason={probe.Reason}; last={probe.ProbedAtUtc:O}";
    }

    private async Task<ModelResponse> FallbackAsync(
        ModelRequest request,
        string auditPath,
        string model,
        Stopwatch stopwatch,
        string reason,
        CancellationToken cancellationToken)
    {
        var fallback = await _fallbackAdapter.SuggestAsync(request, cancellationToken).ConfigureAwait(false);
        stopwatch.Stop();
        await WriteEvidenceAsync(request, auditPath, model, fallback.Summary, stopwatch.Elapsed, didUseLocalModel: false, didFallback: true, reason, cancellationToken).ConfigureAwait(false);
        return fallback with
        {
            Provider = Provider,
            Model = $"{model}+deterministic-fallback",
            BlockReason = reason,
            AuditLogPath = auditPath,
            Latency = stopwatch.Elapsed
        };
    }

    private static object BuildPayload(ModelRequest request, string model)
    {
        var trusted = request.TrustedContext is { Count: > 0 }
            ? string.Join(Environment.NewLine, request.TrustedContext)
            : "None.";
        var untrusted = request.UntrustedContext is { Count: > 0 }
            ? string.Join(Environment.NewLine, request.UntrustedContext.Select(PetModelSummaryService.WrapUntrusted))
            : "None.";

        return new
        {
            model,
            messages = new[]
            {
                new
                {
                    role = "system",
                    content = "You are Wevito's local-only helper. Use local evidence only. Do not request credentials, hosted AI, network fetches, mutation, or hidden tool execution."
                },
                new
                {
                    role = "user",
                    content = $"""
                    Helper: {request.PetName} ({request.HelperRole})
                    Tool family: {request.ToolFamily}
                    User task: {request.UserTask}

                    Trusted context:
                    {trusted}

                    Untrusted context:
                    {untrusted}

                    Tool summary:
                    {request.ToolSummary}
                    """
                }
            },
            temperature = 0.2,
            stream = false
        };
    }

    private static string ExtractOpenAiCompatibleSummary(string body)
    {
        using var document = JsonDocument.Parse(body);
        if (!document.RootElement.TryGetProperty("choices", out var choices) ||
            choices.ValueKind != JsonValueKind.Array)
        {
            return "";
        }

        foreach (var choice in choices.EnumerateArray())
        {
            if (choice.TryGetProperty("message", out var message) &&
                message.TryGetProperty("content", out var content) &&
                content.ValueKind == JsonValueKind.String)
            {
                return content.GetString() ?? "";
            }
        }

        return "";
    }

    private static string ResolveAuditPath(ModelRequest request)
    {
        var artifactRoot = string.IsNullOrWhiteSpace(request.ArtifactRoot)
            ? Path.Combine("vnext", "artifacts", "pet-tasks", BuildAuditSlug(request))
            : request.ArtifactRoot;
        return Path.GetFullPath(Path.Combine(artifactRoot, "local-model-inference.json"));
    }

    private static string BuildAuditSlug(ModelRequest request)
    {
        var timestamp = (request.RequestedAtUtc == default ? DateTimeOffset.UtcNow : request.RequestedAtUtc).ToString("yyyyMMdd-HHmmss");
        return $"{timestamp}-{Slugify(request.PetName)}-{Slugify(request.ToolFamily)}";
    }

    private static string Slugify(string value)
    {
        var chars = value
            .Where(ch => char.IsLetterOrDigit(ch) || ch is '-' or '_')
            .Select(char.ToLowerInvariant)
            .ToArray();
        return chars.Length == 0 ? "local-model" : new string(chars);
    }

    private static async Task WriteEvidenceAsync(
        ModelRequest request,
        string auditPath,
        string model,
        string response,
        TimeSpan latency,
        bool didUseLocalModel,
        bool didFallback,
        string blockReason,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(auditPath) ?? ".");
        var packet = new ModelInferenceEvidencePacket(
            "1",
            request.PetId,
            request.PetName,
            request.HelperRole.ToString(),
            request.ToolFamily,
            Provider,
            model,
            "ollama",
            ComputeSha256(BuildPromptMaterial(request)),
            ComputeSha256(response),
            (long)latency.TotalMilliseconds,
            didUseLocalModel,
            DidUseNetwork: false,
            DidUseHostedAi: false,
            didFallback,
            blockReason,
            DateTimeOffset.UtcNow);
        await File.WriteAllTextAsync(auditPath, JsonSerializer.Serialize(packet, JsonDefaults.Options), cancellationToken).ConfigureAwait(false);
    }

    private static string BuildPromptMaterial(ModelRequest request)
    {
        return JsonSerializer.Serialize(new
        {
            request.PetId,
            request.PetName,
            helperRole = request.HelperRole.ToString(),
            request.ToolFamily,
            request.UserTask,
            request.ToolSummary,
            request.TrustedContext,
            request.UntrustedContext
        }, JsonDefaults.Options);
    }

    private static string ComputeSha256(string value)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value ?? ""))).ToLowerInvariant();
    }

    private static string Read(IReadOnlyDictionary<string, string> settings, string key, string defaultValue)
    {
        return settings.TryGetValue(key, out var raw) && !string.IsNullOrWhiteSpace(raw)
            ? raw
            : defaultValue;
    }
}
