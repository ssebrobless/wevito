using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed class ToolPolicyEvaluator
{
    public ToolPolicyDecision Evaluate(TaskIntent intent, IReadOnlyList<ToolPolicy> policies)
    {
        if (intent.RiskLevel == ToolRiskLevel.Blocked ||
            !string.IsNullOrWhiteSpace(intent.RefusalOrClarificationReason))
        {
            return new ToolPolicyDecision(
                intent.RequestedToolFamily,
                ToolPolicyDecisionStatus.Blocked,
                ToolRiskLevel.Blocked,
                ApprovalRequirement.HandOffRequired,
                Reason: string.IsNullOrWhiteSpace(intent.RefusalOrClarificationReason)
                    ? "Task intent is blocked before policy evaluation."
                    : intent.RefusalOrClarificationReason);
        }

        if (string.IsNullOrWhiteSpace(intent.RequestedToolFamily))
        {
            return new ToolPolicyDecision(
                intent.RequestedToolFamily,
                ToolPolicyDecisionStatus.Blocked,
                ToolRiskLevel.Blocked,
                ApprovalRequirement.HandOffRequired,
                Reason: "Task intent does not declare a requested tool family.");
        }

        var policy = policies.FirstOrDefault(candidate =>
            string.Equals(candidate.ToolFamily, intent.RequestedToolFamily, StringComparison.OrdinalIgnoreCase));

        if (policy is null)
        {
            return new ToolPolicyDecision(
                intent.RequestedToolFamily,
                ToolPolicyDecisionStatus.Blocked,
                ToolRiskLevel.Blocked,
                ApprovalRequirement.HandOffRequired,
                Reason: $"No tool policy is registered for \"{intent.RequestedToolFamily}\".");
        }

        if (!policy.IsEnabled || policy.RiskLevel == ToolRiskLevel.Blocked)
        {
            return new ToolPolicyDecision(
                policy.ToolFamily,
                ToolPolicyDecisionStatus.Blocked,
                ToolRiskLevel.Blocked,
                ApprovalRequirement.HandOffRequired,
                policy,
                string.IsNullOrWhiteSpace(policy.BlockReason)
                    ? $"Tool family \"{policy.ToolFamily}\" is disabled."
                    : policy.BlockReason);
        }

        var effectiveRisk = MaxRisk(intent.RiskLevel, policy.RiskLevel);
        if (intent.NeedsApproval || policy.ApprovalRequirement != ApprovalRequirement.None)
        {
            return new ToolPolicyDecision(
                policy.ToolFamily,
                ToolPolicyDecisionStatus.ApprovalRequired,
                effectiveRisk,
                policy.ApprovalRequirement == ApprovalRequirement.None
                    ? ApprovalRequirement.BeforeExecution
                    : policy.ApprovalRequirement,
                policy,
                "Tool policy requires approval before execution.");
        }

        return new ToolPolicyDecision(
            policy.ToolFamily,
            ToolPolicyDecisionStatus.Allowed,
            effectiveRisk,
            ApprovalRequirement.None,
            policy,
            "Read-only or otherwise pre-approved tool family.");
    }

    private static ToolRiskLevel MaxRisk(ToolRiskLevel left, ToolRiskLevel right)
    {
        return (ToolRiskLevel)Math.Max((int)left, (int)right);
    }
}
