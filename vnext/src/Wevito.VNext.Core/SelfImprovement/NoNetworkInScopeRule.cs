namespace Wevito.VNext.Core.SelfImprovement;

public sealed class NoNetworkInScopeRule : ConstitutionalRule
{
    public const string Reason = "network_not_allowed_for_scope";

    public ConstitutionalDecisionOutcome? Evaluate(ConstitutionalDecisionInput input)
    {
        return input.RequestsNetwork && !input.ScopeAllowsNetwork
            ? new ConstitutionalDecisionOutcome.Blocked(Reason)
            : null;
    }
}
