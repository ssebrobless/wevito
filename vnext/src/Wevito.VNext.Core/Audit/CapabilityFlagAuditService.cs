namespace Wevito.VNext.Core.Audit;

public sealed record CapabilityFlagAuditRow(
    string Name,
    string DefaultValue,
    string CurrentValue,
    bool IsDefault,
    string PlainLanguage);

public sealed class CapabilityFlagAuditService
{
    private const string KillSwitchMaskedValue = "(masked: kill_switch=true)";

    private readonly Func<IReadOnlyDictionary<string, string>> _settingsProvider;
    private readonly KillSwitchService? _killSwitchService;

    public CapabilityFlagAuditService(
        Func<IReadOnlyDictionary<string, string>>? settingsProvider = null,
        KillSwitchService? killSwitchService = null)
    {
        _settingsProvider = settingsProvider ?? (() => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
        _killSwitchService = killSwitchService;
    }

    public IReadOnlyList<CapabilityFlagAuditRow> GetRows()
    {
        var settings = _settingsProvider();
        var killSwitchActive = _killSwitchService?.IsActive() == true || KillSwitchService.IsActive(settings);
        return CapabilityFlagInventory.Entries
            .Select(entry =>
            {
                var current = killSwitchActive
                    ? KillSwitchMaskedValue
                    : settings.TryGetValue(entry.Name, out var raw)
                        ? raw
                        : entry.DefaultValue;
                return new CapabilityFlagAuditRow(
                    entry.Name,
                    entry.DefaultValue,
                    current,
                    !killSwitchActive && string.Equals(current, entry.DefaultValue, StringComparison.OrdinalIgnoreCase),
                    entry.PlainLanguage);
            })
            .ToList();
    }
}
