namespace Wevito.VNext.Core.SelfImprovement;

public sealed class ScopeMustBeEnabledRule : ConstitutionalRule
{
    public const string Reason = "scope_not_enabled";

    public ConstitutionalDecisionOutcome? Evaluate(ConstitutionalDecisionInput input)
    {
        return input.ScopeEnabled
            ? null
            : new ConstitutionalDecisionOutcome.Blocked(Reason);
    }
}
