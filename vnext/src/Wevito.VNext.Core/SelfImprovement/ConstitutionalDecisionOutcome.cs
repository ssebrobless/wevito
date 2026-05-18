namespace Wevito.VNext.Core.SelfImprovement;

public abstract record ConstitutionalDecisionOutcome
{
    private ConstitutionalDecisionOutcome()
    {
    }

    public sealed record Allowed : ConstitutionalDecisionOutcome;

    public sealed record Blocked(string Reason) : ConstitutionalDecisionOutcome;

    public sealed record NeedsHumanApproval(string Reason) : ConstitutionalDecisionOutcome;
}
