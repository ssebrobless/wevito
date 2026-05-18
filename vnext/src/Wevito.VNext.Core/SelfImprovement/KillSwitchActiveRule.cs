namespace Wevito.VNext.Core.SelfImprovement;

public sealed class KillSwitchActiveRule : ConstitutionalRule
{
    public const string Reason = "kill_switch_active";

    public ConstitutionalDecisionOutcome? Evaluate(ConstitutionalDecisionInput input)
    {
        return input.KillSwitchActive
            ? new ConstitutionalDecisionOutcome.Blocked(Reason)
            : null;
    }
}
