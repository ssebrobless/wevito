using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed record PetMemoryWriteRequest(
    Guid PetId,
    string PetName,
    string Kind,
    string Content,
    string Label,
    bool ContainsUntrustedContent = false,
    bool Approved = false);

public sealed record PetMemoryWriteDecision(
    ToolPolicyDecisionStatus Status,
    ApprovalRequirement ApprovalRequirement,
    string Reason);

public sealed class PetMemoryWriteGate
{
    public PetMemoryWriteDecision Evaluate(PetMemoryWriteRequest request)
    {
        if (request.PetId == Guid.Empty || string.IsNullOrWhiteSpace(request.PetName))
        {
            return new PetMemoryWriteDecision(ToolPolicyDecisionStatus.Blocked, ApprovalRequirement.HandOffRequired, "Memory writes require a concrete helper pet.");
        }

        if (string.IsNullOrWhiteSpace(request.Content) || string.IsNullOrWhiteSpace(request.Kind))
        {
            return new PetMemoryWriteDecision(ToolPolicyDecisionStatus.Blocked, ApprovalRequirement.HandOffRequired, "Memory writes require a kind and content.");
        }

        if (!request.Approved)
        {
            return new PetMemoryWriteDecision(
                ToolPolicyDecisionStatus.ApprovalRequired,
                ApprovalRequirement.BeforeExecution,
                request.ContainsUntrustedContent
                    ? "Memory write contains untrusted content and requires explicit approval."
                    : "Memory write requires explicit approval.");
        }

        return new PetMemoryWriteDecision(ToolPolicyDecisionStatus.Allowed, ApprovalRequirement.None, "Approved memory write.");
    }
}
