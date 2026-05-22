using Wevito.VNext.Core.Audit;
using Wevito.VNext.Core.SelfImprovement.Invariants;

namespace Wevito.VNext.Core.SelfImprovement;

public sealed class CapabilitiesAndGatesService
{
    public const string WatchdogObserverEnabledSetting = "snapshot_v0_invariant_observer_in_capabilities_and_gates_enabled";

    private readonly Func<IReadOnlyDictionary<string, string>> _settingsProvider;
    private readonly KillSwitchService? _killSwitch;
    private readonly InvariantViolationWatchdog? _watchdog;

    public CapabilitiesAndGatesService(
        Func<IReadOnlyDictionary<string, string>> settingsProvider,
        KillSwitchService? killSwitch = null,
        InvariantViolationWatchdog? watchdog = null)
    {
        _settingsProvider = settingsProvider;
        _killSwitch = killSwitch;
        _watchdog = watchdog;
    }

    public CapabilitiesAndGatesSnapshot Snapshot(DateTimeOffset? capturedAtUtc = null)
    {
        var settings = _settingsProvider();
        var timestamp = capturedAtUtc ?? DateTimeOffset.UtcNow;
        var killSwitchActive = _killSwitch?.IsActive() == true;
        var entries = CapabilityFlagInventory.Entries
            .Select(entry => BuildEntry(entry, settings, killSwitchActive))
            .ToList();
        if (_watchdog is not null &&
            settings.TryGetValue(WatchdogObserverEnabledSetting, out var watcherEnabled) &&
            string.Equals(watcherEnabled, bool.TrueString, StringComparison.OrdinalIgnoreCase))
        {
            _watchdog.ScanAndEmit(timestamp);
        }

        return new CapabilitiesAndGatesSnapshot(
            entries,
            entries.Count(entry => entry.State.StartsWith("on", StringComparison.Ordinal)),
            entries.Count(entry => entry.State.StartsWith("off", StringComparison.Ordinal)),
            entries.Count(entry => entry.State.StartsWith("unset", StringComparison.Ordinal)),
            timestamp,
            killSwitchActive);
    }

    private static CapabilitiesAndGatesEntry BuildEntry(
        CapabilityFlagInventoryEntry entry,
        IReadOnlyDictionary<string, string> settings,
        bool killSwitchActive)
    {
        var current = settings.TryGetValue(entry.Name, out var value) ? value : "";
        var state = ResolveState(current);
        if (killSwitchActive)
        {
            state += "; kill_switch=true";
        }

        return new CapabilitiesAndGatesEntry(
            entry.Name,
            entry.DefaultValue,
            current,
            entry.PlainLanguage,
            entry.PlainLanguage,
            state);
    }

    private static string ResolveState(string current)
    {
        if (string.Equals(current, bool.TrueString, StringComparison.OrdinalIgnoreCase))
        {
            return "on";
        }

        if (string.Equals(current, bool.FalseString, StringComparison.OrdinalIgnoreCase))
        {
            return "off";
        }

        return "unset";
    }
}
