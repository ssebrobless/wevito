namespace Wevito.VNext.Core.SelfImprovement.Eval;

public abstract record EvalGateResult
{
    private EvalGateResult()
    {
    }

    public sealed record Passed() : EvalGateResult;

    public sealed record Failed(string Reason) : EvalGateResult;

    public sealed record NotApplicable(string Reason) : EvalGateResult;
}
