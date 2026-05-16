using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public enum PolicyDecisionScope
{
    ToolPolicy,
    HelperCapability,
    Capture,
    ProofExecution,
    FileRead,
    LocalToolExecution,
    PetStateRead
}

public sealed record PolicyDecision(
    PolicyDecisionScope Scope,
    string Subject,
    ToolPolicyDecisionStatus Status,
    ToolRiskLevel RiskLevel,
    ApprovalRequirement ApprovalRequirement,
    string Reason,
    string? NormalizedPath = null)
{
    public bool IsAllowed => Status == ToolPolicyDecisionStatus.Allowed;
    public bool IsBlocked => Status == ToolPolicyDecisionStatus.Blocked;
}
