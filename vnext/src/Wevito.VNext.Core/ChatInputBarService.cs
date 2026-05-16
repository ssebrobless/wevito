using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed class ChatInputBarService
{
    private readonly ChatPromptParser _parser;
    private readonly ToolPolicyEvaluator _policyEvaluator;
    private readonly PetMemoryRouter? _memoryRouter;

    public ChatInputBarService()
        : this(new ChatPromptParser(), new ToolPolicyEvaluator())
    {
    }

    public ChatInputBarService(ChatPromptParser parser, ToolPolicyEvaluator policyEvaluator, PetMemoryRouter? memoryRouter = null)
    {
        _parser = parser;
        _policyEvaluator = policyEvaluator;
        _memoryRouter = memoryRouter;
    }

    public ChatInputBarState BuildInitialState(
        IReadOnlyList<AgentSlotProfile> helpers,
        DateTimeOffset? nowUtc = null)
    {
        var activeHelpers = NormalizeActiveHelpers(helpers);
        return new ChatInputBarState(
            activeHelpers,
            StatusMessage: activeHelpers.Count == 0
                ? "No helper pets are active yet."
                : $"Ready for {activeHelpers.Count} agent task slots.",
            UpdatedAtUtc: nowUtc ?? DateTimeOffset.UtcNow);
    }

    public ChatInputBarState SubmitDraft(
        string inputText,
        IReadOnlyList<AgentSlotProfile> helpers,
        IReadOnlyList<ToolPolicy> policies,
        Guid? selectedPetId = null,
        DateTimeOffset? nowUtc = null)
    {
        var timestamp = nowUtc ?? DateTimeOffset.UtcNow;
        var activeHelpers = NormalizeActiveHelpers(helpers);
        var intent = _parser.Parse(inputText, activeHelpers, selectedPetId, timestamp);
        var routingDecision = _memoryRouter?.Route(intent, activeHelpers, FindHelper(intent, activeHelpers));
        if (routingDecision?.Helper is not null &&
            intent.TargetMode == TaskIntentTargetMode.RouteToBestHelper &&
            intent.RiskLevel != ToolRiskLevel.Blocked)
        {
            intent = intent with
            {
                TargetPetId = routingDecision.Helper.PetId,
                TargetPetNameSnapshot = routingDecision.Helper.PetNameSnapshot
            };
        }

        var initialCard = _parser.CreateDraftTaskCard(intent, activeHelpers, timestamp);
        var decision = _policyEvaluator.Evaluate(intent, policies);
        var card = ApplyPolicyDecision(initialCard, decision, timestamp, routingDecision);

        return new ChatInputBarState(
            activeHelpers,
            inputText.Trim(),
            intent,
            card,
            decision,
            BuildStatusMessage(card, decision),
            timestamp);
    }

    private static IReadOnlyList<AgentSlotProfile> NormalizeActiveHelpers(IReadOnlyList<AgentSlotProfile> helpers)
    {
        return helpers
            .Take(PetAgentContractLimits.MaxActiveHelpers)
            .ToList();
    }

    private static AgentSlotProfile? FindHelper(TaskIntent intent, IReadOnlyList<AgentSlotProfile> helpers)
    {
        if (intent.TargetPetId is not null)
        {
            var byId = helpers.FirstOrDefault(helper => helper.PetId == intent.TargetPetId.Value);
            if (byId is not null)
            {
                return byId;
            }
        }

        return string.IsNullOrWhiteSpace(intent.TargetPetNameSnapshot)
            ? null
            : helpers.FirstOrDefault(helper =>
                string.Equals(helper.PetNameSnapshot, intent.TargetPetNameSnapshot, StringComparison.OrdinalIgnoreCase));
    }

    private static TaskCard ApplyPolicyDecision(
        TaskCard card,
        ToolPolicyDecision decision,
        DateTimeOffset timestamp,
        PetMemoryRoutingDecision? routingDecision = null)
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
        if (routingDecision is not null)
        {
            timeline.Add(routingDecision.UsedMemory
                ? $"memory_routed: {routingDecision.Reason} score={routingDecision.Score:0.000}"
                : $"memory_fallback: {routingDecision.Reason}");
        }

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
