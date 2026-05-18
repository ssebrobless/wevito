namespace Wevito.VNext.Core.SelfImprovement;

public interface ConstitutionalRule
{
    ConstitutionalDecisionOutcome? Evaluate(ConstitutionalDecisionInput input);
}
