namespace Wevito.VNext.Core.SelfImprovement;

public sealed class DefaultDenyRule : ConstitutionalRule
{
    public const string DefaultDenyReason = "default_deny_no_explicit_allow";
    public const string EmptyRegistryReason = "no_experiment_kind_registered";

    public ConstitutionalDecisionOutcome Evaluate(ConstitutionalDecisionInput input)
    {
        return new ConstitutionalDecisionOutcome.Blocked(
            input.ExperimentRegistryIsEmpty ? EmptyRegistryReason : DefaultDenyReason);
    }
}
