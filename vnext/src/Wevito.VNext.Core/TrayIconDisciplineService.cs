namespace Wevito.VNext.Core;

public sealed record TrayIconDecision(
    bool Animate,
    string Reason);

public sealed class TrayIconDisciplineService
{
    public const string AnimationEnabledSetting = "tray_icon_animation_enabled";

    public TrayIconDecision DecideAnimation(IReadOnlyDictionary<string, string>? settings)
    {
        return GetBool(settings, AnimationEnabledSetting, false)
            ? new TrayIconDecision(true, "user_enabled_tray_animation")
            : new TrayIconDecision(false, "tray_animation_disabled_by_default");
    }

    private static bool GetBool(IReadOnlyDictionary<string, string>? settings, string key, bool defaultValue)
    {
        return settings is not null &&
               settings.TryGetValue(key, out var raw) &&
               bool.TryParse(raw, out var parsed)
            ? parsed
            : defaultValue;
    }
}
