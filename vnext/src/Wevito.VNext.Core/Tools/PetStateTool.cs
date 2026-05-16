using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core.Tools;

public sealed record PetStateToolRequest(int? PetSlot = null, Guid? TaskCardId = null);

public sealed record PetStateToolResult(
    int PetSlot,
    Guid PetId,
    string Name,
    string Species,
    string Age,
    string Gender,
    string Color,
    double Hunger,
    double Thirst,
    double Energy,
    double Cleanliness,
    double Affection,
    double Comfort,
    double Health,
    double Fitness,
    string CurrentAnimation,
    string CurrentGoal,
    IReadOnlyList<string> ActiveConditions,
    IReadOnlyList<string> ActiveStatuses,
    IReadOnlyList<PetInteractionLogEntry> RecentInteractions,
    DateTimeOffset GeneratedAtUtc);

public sealed class PetStateTool
{
    public const string PacketKind = "pet_state_tool_invoked";
    private readonly UnifiedPolicyService _policyService;
    private readonly PetInteractionLogger _interactionLogger;
    private readonly AuditLedgerService? _auditLedgerService;

    public PetStateTool(
        UnifiedPolicyService? policyService = null,
        PetInteractionLogger? interactionLogger = null,
        AuditLedgerService? auditLedgerService = null)
    {
        _auditLedgerService = auditLedgerService;
        _policyService = policyService ?? new UnifiedPolicyService(auditLedgerService: auditLedgerService);
        _interactionLogger = interactionLogger ?? new PetInteractionLogger(auditLedgerService: auditLedgerService);
    }

    public PetStateToolResult? GetState(
        PetStateToolRequest request,
        IReadOnlyList<PetActor> activePets,
        DateTimeOffset? nowUtc = null)
    {
        var timestamp = nowUtc ?? DateTimeOffset.UtcNow;
        var requestedSlot = Math.Max(0, request.PetSlot ?? 0);
        var decision = _policyService.EvaluatePetStateRead(requestedSlot, request.TaskCardId, timestamp);
        if (decision.IsBlocked || activePets.Count == 0)
        {
            Record(request.TaskCardId, timestamp, decision.IsBlocked ? "Blocked" : "Empty", decision.Reason);
            return null;
        }

        var slot = Math.Min(requestedSlot, activePets.Count - 1);
        var pet = activePets[slot];
        var recent = _interactionLogger.RecentInteractions(pet.Id, timestamp.AddHours(-1), timestamp);
        var result = new PetStateToolResult(
            slot,
            pet.Id,
            pet.Name,
            pet.SpeciesId,
            pet.AgeStage.ToString(),
            pet.Gender.ToString(),
            pet.ColorVariant,
            pet.Hunger,
            pet.Thirst,
            pet.Energy,
            pet.Cleanliness,
            pet.Affection,
            pet.Comfort,
            pet.Health,
            pet.Fitness,
            pet.CurrentAnimationState.ToString(),
            DeriveGoal(pet),
            pet.ActiveConditions?.Select(condition => condition.Id).ToList() ?? [],
            pet.ActiveStatuses?.Select(status => status.ToString()).ToList() ?? [],
            recent,
            timestamp);
        Record(request.TaskCardId, timestamp, "Completed", $"{pet.Name} state returned for slot {slot}.");
        return result;
    }

    private void Record(Guid? taskCardId, DateTimeOffset timestamp, string status, string summary)
    {
        _auditLedgerService?.Record(new EvidencePacket(
            Guid.NewGuid(),
            PacketKind,
            taskCardId,
            timestamp,
            DidUseNetwork: false,
            DidUseHostedAi: false,
            DidUseLocalModel: false,
            DidMutate: false,
            ArtifactPath: "",
            Summary: summary,
            Status: status));
    }

    private static string DeriveGoal(PetActor pet)
    {
        if (pet.IsDead)
        {
            return "ghost_roam";
        }

        if (pet.Thirst < 35)
        {
            return "seek_water_zone";
        }

        if (pet.Hunger < 35)
        {
            return "seek_food_zone";
        }

        if (pet.Energy < 30)
        {
            return "rest_zone";
        }

        return pet.BehaviorState == PetBehaviorState.Home ? "home_idle" : "wander";
    }
}
