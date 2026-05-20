namespace Wevito.VNext.Core.SelfImprovement.Eval;

public sealed class EvalCoverageHealthService
{
    public const int HeldOutMinimum = 5;
    public const int InDistributionMinimum = 10;

    private readonly IHeldOutEvalStore _heldOut;
    private readonly IInDistributionEvalStore _inDistribution;
    private readonly KillSwitchService? _killSwitch;

    public EvalCoverageHealthService(
        IHeldOutEvalStore heldOut,
        IInDistributionEvalStore inDistribution,
        KillSwitchService? killSwitch = null)
    {
        _heldOut = heldOut;
        _inDistribution = inDistribution;
        _killSwitch = killSwitch;
    }

    public EvalCoverageHealthSnapshot Snapshot(DateTimeOffset? capturedAtUtc = null)
    {
        var now = capturedAtUtc ?? DateTimeOffset.UtcNow;
        if (_killSwitch?.IsActive() == true)
        {
            return new EvalCoverageHealthSnapshot(
                0,
                0,
                HeldOutMinimum,
                InDistributionMinimum,
                HeldOutMeetsMinimum: false,
                InDistributionMeetsMinimum: false,
                AllMet: false,
                now,
                "kill_switch=true");
        }

        var heldOutCount = _heldOut.ListCaseIds().Count;
        var inDistributionCount = _inDistribution.ListCaseIds().Count;
        var heldOutMet = heldOutCount >= HeldOutMinimum;
        var inDistributionMet = inDistributionCount >= InDistributionMinimum;
        var allMet = heldOutMet && inDistributionMet;
        return new EvalCoverageHealthSnapshot(
            heldOutCount,
            inDistributionCount,
            HeldOutMinimum,
            InDistributionMinimum,
            heldOutMet,
            inDistributionMet,
            allMet,
            now,
            allMet ? "ok" : "below_minimum");
    }
}
