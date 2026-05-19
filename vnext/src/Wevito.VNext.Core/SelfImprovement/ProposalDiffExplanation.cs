namespace Wevito.VNext.Core.SelfImprovement;

public sealed record ProposalDiffExplanation(
    string OperationId,
    IReadOnlyList<string> SourcePaths,
    IReadOnlyList<string> Tools,
    int DryRunMutationCount,
    IReadOnlyDictionary<string, string> EvalGateStatuses,
    string ScopeHash,
    string ManifestHash,
    bool IsBlocked,
    string BlockReason)
{
    public ProposalDiffExplanation(KillSwitchService killSwitchService)
        : this("", [], [], 0, new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase), "", "", true, "kill_switch_constructor_marker")
    {
        ArgumentNullException.ThrowIfNull(killSwitchService);
    }
}
