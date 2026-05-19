namespace Wevito.VNext.Core.SelfImprovement;

public sealed record OperationTimelineRow(
    DateTimeOffset AtUtc,
    string PacketKind,
    string PlainLanguage,
    string StatusBadge,
    bool DidMutate,
    bool DidUseLocalModel)
{
    public OperationTimelineRow(KillSwitchService killSwitchService)
        : this(DateTimeOffset.MinValue, "blocked", "Blocked: kill_switch=true", "Blocked", false, false)
    {
        ArgumentNullException.ThrowIfNull(killSwitchService);
    }
}
