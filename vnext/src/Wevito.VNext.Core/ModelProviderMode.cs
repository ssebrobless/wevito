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
    string HostedProviderId);

public sealed class ModelProviderModeService
{
    public const string ProviderModeSetting = "pet_model_provider_mode";
    public const string LocalProviderIdSetting = "pet_model_local_provider_id";
    public const string HostedProviderIdSetting = "pet_model_hosted_provider_id";
    public const string HostedProviderApprovedSetting = "pet_model_hosted_provider_approved";
    public const string LocalProviderAvailableSetting = "pet_model_local_provider_available";

    public ModelProviderSettings ReadSettings(IReadOnlyDictionary<string, string>? settings)
    {
        var snapshot = settings ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        return new ModelProviderSettings(
            ParseMode(Read(snapshot, ProviderModeSetting, "disabled")),
            HostedProviderApproved: ReadBool(snapshot, HostedProviderApprovedSetting, false),
            LocalProviderAvailable: ReadBool(snapshot, LocalProviderAvailableSetting, false),
            LocalProviderId: Read(snapshot, LocalProviderIdSetting, "deterministic-local"),
            HostedProviderId: Read(snapshot, HostedProviderIdSetting, "none"));
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
