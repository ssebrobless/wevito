namespace Wevito.VNext.Core.SelfImprovement;

public sealed record RefusedApprovalAggregate(
    int Total,
    IReadOnlyDictionary<string, int> ByReason,
    IReadOnlyDictionary<string, int> OtherBucketByHashPrefix,
    bool IsBlocked = false,
    string BlockedReason = "")
{
    public RefusedApprovalAggregate(KillSwitchService killSwitchService)
        : this(0, new Dictionary<string, int>(StringComparer.Ordinal), new Dictionary<string, int>(StringComparer.Ordinal), true, "kill_switch=true")
    {
        ArgumentNullException.ThrowIfNull(killSwitchService);
    }

    public static RefusedApprovalAggregate Blocked(string reason)
    {
        return new RefusedApprovalAggregate(
            0,
            new Dictionary<string, int>(StringComparer.Ordinal),
            new Dictionary<string, int>(StringComparer.Ordinal),
            true,
            reason);
    }
}
