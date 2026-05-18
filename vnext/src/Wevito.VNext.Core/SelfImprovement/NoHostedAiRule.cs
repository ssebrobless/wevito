namespace Wevito.VNext.Core.SelfImprovement;

public sealed class NoHostedAiRule : ConstitutionalRule
{
    public const string Reason = "hosted_ai_forbidden";

    public ConstitutionalDecisionOutcome? Evaluate(ConstitutionalDecisionInput input)
    {
        return input.RequestsHostedAi
            ? new ConstitutionalDecisionOutcome.Blocked(Reason)
            : null;
    }
}
