using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed class PetCommandBarService
{
    public static readonly Guid ScoutHelperId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid InspectorHelperId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid BuilderHelperId = Guid.Parse("33333333-3333-3333-3333-333333333333");

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

    public IReadOnlyList<HelperPet> BuildDefaultRoster()
    {
        return
        [
            new HelperPet(ScoutHelperId, "Scout", "frog", PetHelperRole.ResearchHelper),
            new HelperPet(InspectorHelperId, "Inspector", "pigeon", PetHelperRole.SpriteReviewHelper),
            new HelperPet(BuilderHelperId, "Builder", "rat", PetHelperRole.ChecklistHelper)
        ];
    }

    public IReadOnlyList<HelperPet> AddHelper(IReadOnlyList<HelperPet> activeHelpers, HelperPet helper)
    {
        if (activeHelpers.Count >= PetAgentContractLimits.MaxActiveHelpers)
        {
            throw new InvalidOperationException($"Only {PetAgentContractLimits.MaxActiveHelpers} active helper pets are allowed.");
        }

        return activeHelpers.Concat([helper]).ToList();
    }

    public IReadOnlyList<PetHelperProfile> BuildDefaultHelperProfiles()
    {
        return BuildDefaultRoster()
            .Select(ToProfile)
            .ToList();
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

    private static PetHelperProfile ToProfile(HelperPet helper)
    {
        return new PetHelperProfile(
            helper.Id,
            helper.Name,
            helper.Role,
            Availability: helper.State switch
            {
                HelperPetState.Drafting => PetHelperAvailability.Drafting,
                HelperPetState.Reviewing => PetHelperAvailability.Reviewing,
                HelperPetState.Blocked => PetHelperAvailability.Blocked,
                _ => PetHelperAvailability.Available
            },
            CurrentTaskCardId: helper.CurrentTaskId,
            AllowedToolFamilies: BuildAllowedToolFamilies(helper.Role),
            PreferenceSnapshot: new Dictionary<string, string>
            {
                ["species"] = helper.Species,
                ["display_role"] = helper.Role switch
                {
                    PetHelperRole.ResearchHelper => "docs/research",
                    PetHelperRole.SpriteReviewHelper => "sprite QA",
                    PetHelperRole.ChecklistHelper => "code/proofs",
                    _ => "helper"
                }
            });
    }

    private static IReadOnlyList<string> BuildAllowedToolFamilies(PetHelperRole role)
    {
        return role switch
        {
            PetHelperRole.SpriteReviewHelper => ["spriteAudit", "assetInventory", "proofCapture", "localDocs", "petState"],
            PetHelperRole.ChecklistHelper => ["codeReview", "codePatchPlan", "checklist", "localDocs", "basket", "petState", "buildProof"],
            PetHelperRole.ResearchHelper => ["localDocs", "translateText", "audioAssist", "screenCapture", "assetInventory", "basket", "proofCapture", "petState"],
            _ => ["localDocs"]
        };
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
