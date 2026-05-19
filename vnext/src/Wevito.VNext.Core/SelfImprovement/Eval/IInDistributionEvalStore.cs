namespace Wevito.VNext.Core.SelfImprovement.Eval;

public abstract class IInDistributionEvalStore
{
    protected IInDistributionEvalStore()
    {
    }

    public IInDistributionEvalStore(KillSwitchService killSwitchService)
    {
        _ = killSwitchService;
    }

    public abstract IReadOnlyList<string> ListCaseIds();

    public abstract string? ReadCase(string caseId);
}
