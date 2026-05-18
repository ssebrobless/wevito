namespace Wevito.VNext.Core.SelfImprovement.Eval;

public interface IHeldOutEvalStore
{
    IReadOnlyList<string> ListCaseIds();

    string? ReadCase(string caseId);
}
