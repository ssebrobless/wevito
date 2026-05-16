namespace Wevito.VNext.Core;

public enum ModelProviderMode
{
    Disabled,
    LocalOnly,
    ApprovedCloud
}

public sealed record ModelProviderSettings(
    ModelProviderMode Mode,
    bool HostedProviderApproved,
    bool LocalProviderAvailable,
    string LocalProviderId,
    string HostedProviderId,
    bool InProcessLocalRuntimeEnabled = false,
    string LocalRuntimeEndpoint = LocalRuntimeProbeService.DefaultOllamaEndpoint,
    string LocalRuntimeModel = LocalRuntimeProbeService.DefaultOllamaModel);

public enum ModelProviderRoute
{
    Disabled,
    Ollama,
    OnnxPhi,
    DeterministicLocal,
    HostedCloudBlocked
}

public sealed record ModelProviderRouteDecision(
    ModelProviderRoute Route,
    string ProviderId,
    string Reason,
    bool DidSelectHostedProvider);

public sealed class ModelProviderModeService
{
    public const string LocalOnlyModeValue = "local_only";
    public const string DefaultLocalProviderId = "ollama";
    public const string ProviderModeSetting = "pet_model_provider_mode";
    public const string LocalProviderIdSetting = "pet_model_local_provider_id";
    public const string HostedProviderIdSetting = "pet_model_hosted_provider_id";
    public const string HostedProviderApprovedSetting = "pet_model_hosted_provider_approved";
    public const string LocalProviderAvailableSetting = "pet_model_local_provider_available";
    public const string InProcessLocalRuntimeEnabledSetting = "local_runtime_inproc_enabled";
    public const string LocalRuntimeEndpointSetting = LocalRuntimeProbeService.OllamaEndpointSetting;
    public const string LocalRuntimeModelSetting = LocalRuntimeProbeService.OllamaModelSetting;

    public ModelProviderSettings ReadSettings(IReadOnlyDictionary<string, string>? settings)
    {
        var snapshot = settings ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        return new ModelProviderSettings(
            ParseMode(Read(snapshot, ProviderModeSetting, LocalOnlyModeValue)),
            HostedProviderApproved: ReadBool(snapshot, HostedProviderApprovedSetting, false),
            LocalProviderAvailable: ReadBool(snapshot, LocalProviderAvailableSetting, false),
            LocalProviderId: Read(snapshot, LocalProviderIdSetting, DefaultLocalProviderId),
            HostedProviderId: Read(snapshot, HostedProviderIdSetting, "none"),
            InProcessLocalRuntimeEnabled: ReadBool(snapshot, InProcessLocalRuntimeEnabledSetting, false),
            LocalRuntimeEndpoint: Read(snapshot, LocalRuntimeEndpointSetting, LocalRuntimeProbeService.DefaultOllamaEndpoint),
            LocalRuntimeModel: Read(snapshot, LocalRuntimeModelSetting, LocalRuntimeProbeService.DefaultOllamaModel));
    }

    public bool CanUseHostedProvider(ModelProviderSettings settings, out string reason)
    {
        if (settings.Mode != ModelProviderMode.ApprovedCloud)
        {
            reason = "Hosted provider mode is not selected.";
            return false;
        }

        if (!settings.HostedProviderApproved)
        {
            reason = "Hosted provider use requires explicit approval.";
            return false;
        }

        reason = "";
        return true;
    }

    public bool CanUseLocalProvider(ModelProviderSettings settings, out string reason)
    {
        if (settings.Mode != ModelProviderMode.LocalOnly)
        {
            reason = "Local-only provider mode is not selected.";
            return false;
        }

        if (!settings.LocalProviderAvailable)
        {
            reason = "No local provider runtime is marked available; deterministic local fallback will be used.";
            return false;
        }

        reason = "";
        return true;
    }

    public ModelProviderRouteDecision DecideRoute(
        ModelProviderSettings settings,
        LocalRuntimeProbeResult? probeResult = null,
        bool onnxPhiWeightsPresent = false)
    {
        if (settings.Mode == ModelProviderMode.Disabled)
        {
            return new ModelProviderRouteDecision(ModelProviderRoute.Disabled, "none", "Model provider mode is disabled.", DidSelectHostedProvider: false);
        }

        if (settings.Mode == ModelProviderMode.ApprovedCloud)
        {
            return new ModelProviderRouteDecision(ModelProviderRoute.HostedCloudBlocked, settings.HostedProviderId, "Hosted providers are outside C-PHASE 79 and remain blocked.", DidSelectHostedProvider: false);
        }

        if (probeResult?.IsAvailable == true)
        {
            return new ModelProviderRouteDecision(ModelProviderRoute.Ollama, "ollama", "LocalOnly routed to localhost Ollama.", DidSelectHostedProvider: false);
        }

        if (settings.InProcessLocalRuntimeEnabled && onnxPhiWeightsPresent)
        {
            return new ModelProviderRouteDecision(ModelProviderRoute.OnnxPhi, "onnx-phi", "LocalOnly routed to feature-flagged in-process ONNX Phi fallback.", DidSelectHostedProvider: false);
        }

        return new ModelProviderRouteDecision(ModelProviderRoute.DeterministicLocal, "deterministic-local", "LocalOnly fell back to deterministic local adapter.", DidSelectHostedProvider: false);
    }

    public static bool IsHostedProviderId(string providerId)
    {
        return providerId.Contains("anthropic", StringComparison.OrdinalIgnoreCase) ||
               providerId.Contains("openai", StringComparison.OrdinalIgnoreCase) ||
               providerId.Contains("gemini", StringComparison.OrdinalIgnoreCase) ||
               providerId.Contains("hosted", StringComparison.OrdinalIgnoreCase) ||
               providerId.Contains("cloud", StringComparison.OrdinalIgnoreCase);
    }

    public static ModelProviderMode ParseMode(string? value)
    {
        return value?.Trim().ToLowerInvariant() switch
        {
            "local_only" or "local-only" or "local" => ModelProviderMode.LocalOnly,
            "approved_cloud" or "approved-cloud" or "cloud" => ModelProviderMode.ApprovedCloud,
            _ => ModelProviderMode.Disabled
        };
    }

    private static string Read(IReadOnlyDictionary<string, string> settings, string key, string defaultValue)
    {
        return settings.TryGetValue(key, out var raw) && !string.IsNullOrWhiteSpace(raw)
            ? raw
            : defaultValue;
    }

    private static bool ReadBool(IReadOnlyDictionary<string, string> settings, string key, bool defaultValue)
    {
        return settings.TryGetValue(key, out var raw) && bool.TryParse(raw, out var parsed)
            ? parsed
            : defaultValue;
    }
}
