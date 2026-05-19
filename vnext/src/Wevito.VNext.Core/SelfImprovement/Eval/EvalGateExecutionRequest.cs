namespace Wevito.VNext.Core.SelfImprovement.Eval;

public sealed record EvalGateExecutionRequest(
    string ScopeId,
    string OperationId,
    string RepoRoot,
    IReadOnlyDictionary<string, string> Settings)
{
    public EvalGateExecutionRequest(KillSwitchService killSwitchService)
        : this("", "", "", new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase))
    {
        _ = killSwitchService;
    }
}
