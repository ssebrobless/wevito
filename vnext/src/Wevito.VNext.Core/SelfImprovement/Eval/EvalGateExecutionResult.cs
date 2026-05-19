namespace Wevito.VNext.Core.SelfImprovement.Eval;

public sealed record EvalGateExecutionResult(
    IReadOnlyDictionary<string, EvalGateResult> Results,
    DateTimeOffset RanAtUtc,
    string BuildCommand,
    string TestCommand)
{
    public EvalGateExecutionResult(KillSwitchService killSwitchService)
        : this(new Dictionary<string, EvalGateResult>(StringComparer.OrdinalIgnoreCase), DateTimeOffset.MinValue, "", "")
    {
        _ = killSwitchService;
    }
}
