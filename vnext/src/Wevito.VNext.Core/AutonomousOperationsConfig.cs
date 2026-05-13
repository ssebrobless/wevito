namespace Wevito.VNext.Core;

public sealed record AutonomousOperationsConfig(
    bool Enabled,
    TimeSpan TickInterval,
    int DailyIterationCap,
    bool ApprovedWebFetchEnabled,
    bool ApprovedFileReadEnabled,
    bool LocalModelReasoningEnabled)
{
    public const string EnabledSetting = "runtime_autonomous_beta_enabled";
    public const string DailyCapSetting = "runtime_autonomous_beta_daily_cap";
    public const string IntervalMinutesSetting = "runtime_autonomous_beta_interval_minutes";

    public static AutonomousOperationsConfig FromSettings(IReadOnlyDictionary<string, string>? settings)
    {
        var snapshot = settings ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var interval = TimeSpan.FromMinutes(Math.Clamp(ReadInt(snapshot, IntervalMinutesSetting, 10), 1, 120));
        return new AutonomousOperationsConfig(
            Enabled: ReadBool(snapshot, EnabledSetting, false),
            interval,
            DailyIterationCap: Math.Clamp(ReadInt(snapshot, DailyCapSetting, 3), 0, 24),
            ApprovedWebFetchEnabled: ReadBool(snapshot, WebResearchConnector.WebSearchEnabledSetting, false),
            ApprovedFileReadEnabled: ReadBool(snapshot, "local_access_file_read_enabled", false),
            LocalModelReasoningEnabled: ReadBool(snapshot, ModelProviderModeService.LocalProviderAvailableSetting, false));
    }

    private static bool ReadBool(IReadOnlyDictionary<string, string> settings, string key, bool defaultValue)
    {
        return settings.TryGetValue(key, out var raw) && bool.TryParse(raw, out var parsed)
            ? parsed
            : defaultValue;
    }

    private static int ReadInt(IReadOnlyDictionary<string, string> settings, string key, int defaultValue)
    {
        return settings.TryGetValue(key, out var raw) && int.TryParse(raw, out var parsed)
            ? parsed
            : defaultValue;
    }
}
