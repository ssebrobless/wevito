namespace Wevito.VNext.Core.SelfImprovement;

public sealed record ConstitutionalDecisionInput(
    string ScopeId,
    string ExperimentKind,
    bool ScopeEnabled,
    bool RequestsNetwork,
    bool ScopeAllowsNetwork,
    bool RequestsHostedAi,
    bool ExperimentRegistryIsEmpty,
    bool KillSwitchActive = false);
