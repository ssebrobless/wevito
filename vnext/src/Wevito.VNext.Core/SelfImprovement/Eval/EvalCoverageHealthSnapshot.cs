namespace Wevito.VNext.Core.SelfImprovement.Eval;

public sealed record EvalCoverageHealthSnapshot(
    int HeldOutCount,
    int InDistributionCount,
    int HeldOutMinimum,
    int InDistributionMinimum,
    bool HeldOutMeetsMinimum,
    bool InDistributionMeetsMinimum,
    bool AllMet,
    DateTimeOffset GeneratedAtUtc,
    string Reason)
{
    public EvalCoverageHealthSnapshot(KillSwitchService killSwitchService)
        : this(0, 0, 5, 10, false, false, false, DateTimeOffset.MinValue, "")
    {
        _ = killSwitchService;
    }
}
