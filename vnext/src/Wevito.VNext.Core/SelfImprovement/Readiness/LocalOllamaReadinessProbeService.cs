using System.IO;
using System.Net.Http;
using System.Text.Json;
using Wevito.VNext.Contracts;
using Wevito.VNext.Core.Audit;
using Wevito.VNext.Core.SelfImprovement.Scoring;

namespace Wevito.VNext.Core.SelfImprovement.Readiness;

public sealed class LocalOllamaReadinessProbeService
{
    public const string EnabledSetting = "local_ollama_readiness_probe_enabled";
    public const string EndpointSetting = "local_ollama_readiness_probe_endpoint";
    private const string ScoringEndpointSetting = "local_scoring_provider_loopback_endpoint";
    private const string ScoringModelSetting = "local_scoring_provider_ollama_model";
    private const string DefaultEndpoint = "127.0.0.1:11434";
    private const string DefaultModel = "qwen2.5:7b-instruct-q4_k_m";

    private readonly IScoringHttpClient _http;
    private readonly AuditLedgerService _ledger;
    private readonly KillSwitchService? _killSwitch;
    private readonly Func<IReadOnlyDictionary<string, string>> _settingsProvider;

    public LocalOllamaReadinessProbeService(
        IScoringHttpClient http,
        AuditLedgerService ledger,
        KillSwitchService? killSwitch,
        Func<IReadOnlyDictionary<string, string>> settingsProvider)
    {
        _http = http;
        _ledger = ledger;
        _killSwitch = killSwitch;
        _settingsProvider = settingsProvider;
    }

    public LocalOllamaReadinessSnapshot Probe(DateTimeOffset nowUtc, CancellationToken cancellationToken)
    {
        var settings = _settingsProvider();
        var endpoint = ResolveEndpoint(settings);
        var configuredModel = ResolveModel(settings);

        if (_killSwitch?.IsActive() == true)
        {
            return Refused(endpoint, configuredModel, nowUtc, "kill_switch=true");
        }

        if (!IsTrue(settings, EnabledSetting))
        {
            return Refused(endpoint, configuredModel, nowUtc, "local_ollama_readiness_probe_enabled=false");
        }

        if (!endpoint.StartsWith("127.0.0.1:", StringComparison.Ordinal))
        {
            return Refused(endpoint, configuredModel, nowUtc, "non_loopback_endpoint");
        }

        try
        {
            var uri = new Uri($"http://{endpoint}/api/tags");
            var response = _http.GetAsync(uri, cancellationToken).GetAwaiter().GetResult();
            var modelPresent = ParseModelPresent(response, configuredModel);
            var snapshot = new LocalOllamaReadinessSnapshot(
                endpoint,
                configuredModel,
                ProbeRan: true,
                LoopbackReachable: true,
                ConfiguredModelPresent: modelPresent,
                nowUtc,
                "ok");
            Record(snapshot);
            return snapshot;
        }
        catch (JsonException)
        {
            var snapshot = new LocalOllamaReadinessSnapshot(
                endpoint,
                configuredModel,
                ProbeRan: true,
                LoopbackReachable: true,
                ConfiguredModelPresent: false,
                nowUtc,
                "invalid_tags_response");
            Record(snapshot);
            return snapshot;
        }
        catch (Exception exception) when (exception is HttpRequestException or IOException or OperationCanceledException)
        {
            var snapshot = new LocalOllamaReadinessSnapshot(
                endpoint,
                configuredModel,
                ProbeRan: true,
                LoopbackReachable: false,
                ConfiguredModelPresent: false,
                nowUtc,
                "runtime_unreachable");
            Record(snapshot);
            return snapshot;
        }
    }

    private static LocalOllamaReadinessSnapshot Refused(
        string endpoint,
        string configuredModel,
        DateTimeOffset nowUtc,
        string reason)
    {
        return new LocalOllamaReadinessSnapshot(
            endpoint,
            configuredModel,
            ProbeRan: false,
            LoopbackReachable: false,
            ConfiguredModelPresent: false,
            nowUtc,
            reason);
    }

    private static bool ParseModelPresent(string response, string configuredModel)
    {
        using var document = JsonDocument.Parse(response);
        if (!document.RootElement.TryGetProperty("models", out var modelsElement) ||
            modelsElement.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        foreach (var modelElement in modelsElement.EnumerateArray())
        {
            if (modelElement.TryGetProperty("name", out var nameElement) &&
                string.Equals(nameElement.GetString(), configuredModel, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private void Record(LocalOllamaReadinessSnapshot snapshot)
    {
        _ledger.Record(new EvidencePacket(
            Guid.NewGuid(),
            SelfImprovementPacketKinds.LocalRuntimeProbe,
            TaskCardId: null,
            snapshot.ProbedAtUtc,
            DidUseNetwork: true,
            DidUseHostedAi: false,
            DidUseLocalModel: false,
            DidMutate: false,
            ArtifactPath: "",
            Summary: JsonSerializer.Serialize(new
            {
                endpoint = snapshot.Endpoint,
                configured_model = snapshot.ConfiguredModel,
                loopback_reachable = snapshot.LoopbackReachable,
                configured_model_present = snapshot.ConfiguredModelPresent,
                probed_at_utc = snapshot.ProbedAtUtc,
                reason = snapshot.Reason
            }, JsonDefaults.Options),
            Status: "Completed"));
    }

    private static string ResolveEndpoint(IReadOnlyDictionary<string, string> settings)
    {
        if (settings.TryGetValue(EndpointSetting, out var readinessEndpoint) &&
            !string.IsNullOrWhiteSpace(readinessEndpoint))
        {
            return readinessEndpoint.Trim();
        }

        return settings.TryGetValue(ScoringEndpointSetting, out var scoringEndpoint) &&
               !string.IsNullOrWhiteSpace(scoringEndpoint)
            ? scoringEndpoint.Trim()
            : DefaultEndpoint;
    }

    private static string ResolveModel(IReadOnlyDictionary<string, string> settings)
    {
        return settings.TryGetValue(ScoringModelSetting, out var configuredModel) &&
               !string.IsNullOrWhiteSpace(configuredModel)
            ? configuredModel.Trim()
            : DefaultModel;
    }

    private static bool IsTrue(IReadOnlyDictionary<string, string> settings, string key)
    {
        return settings.TryGetValue(key, out var value) &&
               string.Equals(value, bool.TrueString, StringComparison.OrdinalIgnoreCase);
    }
}
