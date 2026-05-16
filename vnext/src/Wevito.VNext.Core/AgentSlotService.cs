using System.Security.Cryptography;
using System.Text;
using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed record AgentSlotRoster(
    IReadOnlyList<AgentSlot> Slots,
    IReadOnlyList<EvidencePacket> RenamePackets);

public sealed class AgentSlotService
{
    public const string SlotAssignedPacketKind = "agent_slot_assigned";
    public const string SlotRenamedPacketKind = "agent_slot_renamed";
    public const string SlotStatusChangedPacketKind = "agent_slot_status_changed";

    public AgentSlotRoster BuildRoster(
        IReadOnlyList<PetActor> activePets,
        IReadOnlyList<AgentSlot>? previousSlots = null,
        DateTimeOffset? nowUtc = null)
    {
        var timestamp = nowUtc ?? DateTimeOffset.UtcNow;
        var visiblePets = activePets
            .Where(pet => !pet.IsDead && !pet.IsGhost)
            .Take(PetAgentContractLimits.MaxActiveHelpers)
            .ToList();
        var previousByIndex = (previousSlots ?? [])
            .GroupBy(slot => slot.SlotIndex)
            .ToDictionary(group => group.Key, group => group.First());
        var slots = new List<AgentSlot>(PetAgentContractLimits.MaxActiveHelpers);
        var renamePackets = new List<EvidencePacket>();

        for (var index = 0; index < PetAgentContractLimits.MaxActiveHelpers; index++)
        {
            var pet = index < visiblePets.Count ? visiblePets[index] : null;
            var name = string.IsNullOrWhiteSpace(pet?.Name) ? $"Agent {index + 1}" : pet!.Name;
            var previous = previousByIndex.GetValueOrDefault(index);
            var status = previous?.Status ?? AgentSlotStatus.Idle;
            var slot = new AgentSlot(
                BuildSlotId(index),
                index,
                name,
                status,
                previous?.CurrentTaskCardId,
                previous?.LastUsedAtUtc ?? timestamp,
                pet?.Id,
                pet?.SpeciesId ?? "pet",
                previous?.ToolIcon ?? "",
                previous?.ActiveToolFamily ?? "");
            slots.Add(slot);

            if (previous is not null &&
                !string.Equals(previous.Name, slot.Name, StringComparison.Ordinal) &&
                !string.IsNullOrWhiteSpace(previous.Name))
            {
                renamePackets.Add(new EvidencePacket(
                    Guid.NewGuid(),
                    SlotRenamedPacketKind,
                    TaskCardId: null,
                    timestamp,
                    DidUseNetwork: false,
                    DidUseHostedAi: false,
                    DidUseLocalModel: false,
                    DidMutate: false,
                    ArtifactPath: "",
                    Summary: $"slot={index + 1} old={previous.Name} new={slot.Name}",
                    Status: "Completed"));
            }
        }

        return new AgentSlotRoster(slots, renamePackets);
    }

    public static PetHelperProfile ToProfile(AgentSlot slot)
    {
        return new PetHelperProfile(
            slot.Id,
            slot.Name,
            slot.SlotIndex,
            slot.Status,
            ToAvailability(slot.Status),
            slot.CurrentTaskCardId,
            BuildAllowedToolFamilies(slot.SlotIndex),
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["species"] = slot.Species,
                ["display_role"] = $"Agent slot {slot.SlotIndex + 1}",
                ["agent_status"] = slot.Status.ToString(),
                ["active_tool_family"] = slot.ActiveToolFamily,
                ["tool_icon"] = slot.ToolIcon
            });
    }

    public static Guid BuildSlotId(int slotIndex)
    {
        if (slotIndex is < 0 or >= PetAgentContractLimits.MaxActiveHelpers)
        {
            throw new ArgumentOutOfRangeException(nameof(slotIndex), "Agent slot index must be 0, 1, or 2.");
        }

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"wevito-agent-slot::{slotIndex}"));
        return new Guid(bytes[..16]);
    }

    public static IReadOnlyList<string> BuildAllowedToolFamilies(int slotIndex)
    {
        return slotIndex switch
        {
            0 =>
            [
                "spriteAudit",
                "assetInventory",
                "proofCapture",
                "localDocs",
                "petState",
                "petMemory"
            ],
            1 =>
            [
                "codeReview",
                "codePatchPlan",
                "checklist",
                "buildProof",
                "localDocs",
                "basket",
                "petState",
                "petMemory"
            ],
            _ =>
            [
                "localDocs",
                "localResearch",
                "translateText",
                "audioAssist",
                "screenCapture",
                "assetInventory",
                "basket",
                "proofCapture",
                "petState",
                "petMemory"
            ]
        };
    }

    public static IReadOnlyList<string> BuildAllowedToolFamilies()
    {
        return
        [
            "localDocs",
            "localResearch",
            "spriteAudit",
            "assetInventory",
            "petState",
            "codeReview",
            "codePatchPlan",
            "checklist",
            "buildProof",
            "translateText",
            "audioAssist",
            "screenCapture",
            "basket",
            "proofCapture",
            "petMemory"
        ];
    }

    private static PetHelperAvailability ToAvailability(AgentSlotStatus status)
    {
        return status switch
        {
            AgentSlotStatus.Drafting => PetHelperAvailability.Drafting,
            AgentSlotStatus.Waiting => PetHelperAvailability.WaitingForApproval,
            AgentSlotStatus.RunningTool => PetHelperAvailability.Running,
            AgentSlotStatus.Generating => PetHelperAvailability.Running,
            AgentSlotStatus.Reviewing => PetHelperAvailability.Reviewing,
            AgentSlotStatus.Blocked => PetHelperAvailability.Blocked,
            AgentSlotStatus.Failed => PetHelperAvailability.Failed,
            _ => PetHelperAvailability.Available
        };
    }
}
