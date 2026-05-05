using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed class PetCommandBarService
{
    private readonly PetCommandParser _parser;
    private readonly ToolPolicyEvaluator _policyEvaluator;

    public PetCommandBarService()
        : this(new PetCommandParser(), new ToolPolicyEvaluator())
    {
    }

    public PetCommandBarService(PetCommandParser parser, ToolPolicyEvaluator policyEvaluator)
    {
        _parser = parser;
        _policyEvaluator = policyEvaluator;
    }

    public PetCommandBarState BuildInitialState(
        IReadOnlyList<PetHelperProfile> helpers,
        DateTimeOffset? nowUtc = null)
    {
        var activeHelpers = NormalizeActiveHelpers(helpers);
        return new PetCommandBarState(
            activeHelpers,
            StatusMessage: activeHelpers.Count == 0
                ? "No helper pets are active yet."
                : $"Ready for {activeHelpers.Count} helper pet task slots.",
            UpdatedAtUtc: nowUtc ?? DateTimeOffset.UtcNow);
    }

    public PetCommandBarState SubmitDraft(
        string inputText,
        IReadOnlyList<PetHelperProfile> helpers,
        IReadOnlyList<ToolPolicy> policies,
        Guid? selectedPetId = null,
        DateTimeOffset? nowUtc = null)
    {
        var timestamp = nowUtc ?? DateTimeOffset.UtcNow;
        var activeHelpers = NormalizeActiveHelpers(helpers);
        var intent = _parser.Parse(inputText, activeHelpers, selectedPetId, timestamp);
        var initialCard = _parser.CreateDraftTaskCard(intent, activeHelpers, timestamp);
        var decision = _policyEvaluator.Evaluate(intent, policies);
        var card = ApplyPolicyDecision(initialCard, decision, timestamp);

        return new PetCommandBarState(
            activeHelpers,
            inputText.Trim(),
            intent,
            card,
            decision,
            BuildStatusMessage(card, decision),
            timestamp);
    }

    private static IReadOnlyList<PetHelperProfile> NormalizeActiveHelpers(IReadOnlyList<PetHelperProfile> helpers)
    {
        return helpers
            .Take(PetAgentContractLimits.MaxActiveHelpers)
            .ToList();
    }

    private static TaskCard ApplyPolicyDecision(
        TaskCard card,
        ToolPolicyDecision decision,
        DateTimeOffset timestamp)
    {
        var status = decision.Status switch
        {
            ToolPolicyDecisionStatus.Blocked => TaskCardStatus.Blocked,
            ToolPolicyDecisionStatus.ApprovalRequired => TaskCardStatus.WaitingForApproval,
            _ => card.Status
        };

        var timeline = (card.Timeline ?? []).ToList();
        timeline.Add(decision.Status switch
        {
            ToolPolicyDecisionStatus.Blocked => $"policy_blocked: {decision.Reason}",
            ToolPolicyDecisionStatus.ApprovalRequired => $"policy_waiting_for_approval: {decision.Reason}",
            _ => $"policy_allowed: {decision.Reason}"
        });

        return card with
        {
            Status = status,
            PolicySnapshot = decision.PolicySnapshot,
            Timeline = timeline,
            UpdatedAtUtc = timestamp
        };
    }

    private static string BuildStatusMessage(TaskCard card, ToolPolicyDecision decision)
    {
        return decision.Status switch
        {
            ToolPolicyDecisionStatus.Blocked => $"Blocked: {decision.Reason}",
            ToolPolicyDecisionStatus.ApprovalRequired => $"Waiting for approval: {card.AssignedPetNameSnapshot} prepared a draft task card.",
            _ => $"Draft ready: {card.AssignedPetNameSnapshot} prepared a safe task card."
        };
    }
}
