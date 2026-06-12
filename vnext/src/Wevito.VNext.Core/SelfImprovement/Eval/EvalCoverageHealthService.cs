using Wevito.VNext.Core.SelfImprovement.Invariants;

namespace Wevito.VNext.Core.SelfImprovement.Eval;

public sealed class EvalCoverageHealthService
{
    public const string WatchdogObserverEnabledSetting = "snapshot_v0_invariant_observer_in_eval_coverage_health_enabled";
    public const int HeldOutMinimum = 5;
    public const int InDistributionMinimum = 10;

    private readonly IHeldOutEvalStore _heldOut;
    private readonly IInDistributionEvalStore _inDistribution;
    private readonly KillSwitchService? _killSwitch;
    private readonly InvariantViolationWatchdog? _watchdog;
    private readonly Func<IReadOnlyDictionary<string, string>>? _settingsProvider;

    public EvalCoverageHealthService(
        IHeldOutEvalStore heldOut,
        IInDistributionEvalStore inDistribution,
        KillSwitchService? killSwitch = null,
        InvariantViolationWatchdog? watchdog = null,
        Func<IReadOnlyDictionary<string, string>>? settingsProvider = null)
    {
        _heldOut = heldOut;
        _inDistribution = inDistribution;
        _killSwitch = killSwitch;
        _watchdog = watchdog;
        _settingsProvider = settingsProvider;
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
        if (_watchdog is not null && _settingsProvider is not null)
        {
            var settingsForObserver = _settingsProvider();
            if (settingsForObserver.TryGetValue(WatchdogObserverEnabledSetting, out var observerEnabled) &&
                string.Equals(observerEnabled, bool.TrueString, StringComparison.OrdinalIgnoreCase))
            {
                _watchdog.ScanAndEmit(now);
            }
        }

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
