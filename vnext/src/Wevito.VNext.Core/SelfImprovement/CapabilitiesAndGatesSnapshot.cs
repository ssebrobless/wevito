namespace Wevito.VNext.Core.SelfImprovement;

public sealed record CapabilitiesAndGatesEntry(
    string Name,
    string Default,
    string Current,
    string PlainLanguage,
    string Effect,
    string State)
{
    public CapabilitiesAndGatesEntry(
        string name,
        string @default,
        string current,
        string plainLanguage,
        string effect,
        string state,
        KillSwitchService killSwitchService)
        : this(name, @default, current, plainLanguage, effect, state)
    {
        _ = killSwitchService;
    }
}

public sealed record CapabilitiesAndGatesSnapshot(
    IReadOnlyList<CapabilitiesAndGatesEntry> Entries,
    int OnCount,
    int OffCount,
    int UnsetCount,
    DateTimeOffset CapturedAtUtc,
    bool KillSwitchActive)
{
    public CapabilitiesAndGatesSnapshot(
        IReadOnlyList<CapabilitiesAndGatesEntry> entries,
        int onCount,
        int offCount,
        int unsetCount,
        DateTimeOffset capturedAtUtc,
        bool killSwitchActive,
        KillSwitchService killSwitchService)
        : this(entries, onCount, offCount, unsetCount, capturedAtUtc, killSwitchActive)
    {
        _ = killSwitchService;
    }
}
