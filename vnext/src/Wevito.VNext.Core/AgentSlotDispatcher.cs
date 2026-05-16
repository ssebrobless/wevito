using System.Text.RegularExpressions;
using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed class AgentSlotDispatcher
{
    private readonly IModelAdapter _modelAdapter;

    public AgentSlotDispatcher(IModelAdapter modelAdapter)
    {
        _modelAdapter = modelAdapter;
    }

    public AgentSlot? PickAgentForTask(TaskKind kind, TaskOrigin origin, IReadOnlyList<AgentSlot> slots)
    {
        return origin == TaskOrigin.Background
            ? PickFirstIdle(slots)
            : throw new InvalidOperationException("User-visible task routing must call PickAgentForTaskAsync so the local model can be consulted.");
    }

    public async Task<AgentSlot?> PickAgentForTaskAsync(
        TaskKind kind,
        TaskOrigin origin,
        IReadOnlyList<AgentSlot> slots,
        string userTask,
        CancellationToken cancellationToken = default)
    {
        if (slots.Count == 0)
        {
            return null;
        }

        if (origin == TaskOrigin.Background)
        {
            return PickFirstIdle(slots);
        }

        var response = await _modelAdapter.SuggestAsync(
            new ModelRequest(
                AgentSlotService.BuildSlotId(0),
                "Agent dispatcher",
                "AgentDispatcher",
                kind.ToString(),
                userTask,
                $"Choose one visible agent slot from 1-{slots.Count}. Return the slot number only.",
                TrustedContext: slots.Select(slot => $"{slot.SlotIndex + 1}: {slot.Name} status={slot.Status}").ToList(),
                ApprovedForModelCall: false),
            cancellationToken);
        var selectedIndex = TryParseSlotIndex(response.Summary, slots.Count);
        if (selectedIndex is not null)
        {
            var selected = slots.FirstOrDefault(slot => slot.SlotIndex == selectedIndex.Value);
            if (selected is not null && IsIdle(selected))
            {
                return selected;
            }
        }

        return PickFirstIdle(slots) ?? slots.OrderBy(slot => IsForegroundPriority(kind) ? 0 : 1).ThenBy(slot => slot.LastUsedAtUtc).FirstOrDefault();
    }

    private static AgentSlot? PickFirstIdle(IReadOnlyList<AgentSlot> slots)
    {
        return slots.OrderBy(slot => slot.SlotIndex).FirstOrDefault(IsIdle);
    }

    private static bool IsIdle(AgentSlot slot)
    {
        return slot.Status == AgentSlotStatus.Idle && slot.CurrentTaskCardId is null;
    }

    private static int? TryParseSlotIndex(string text, int slotCount)
    {
        var match = Regex.Match(text ?? "", @"\b([1-3])\b");
        if (!match.Success || !int.TryParse(match.Groups[1].Value, out var oneBased))
        {
            return null;
        }

        var zeroBased = oneBased - 1;
        return zeroBased >= 0 && zeroBased < slotCount ? zeroBased : null;
    }

    private static bool IsForegroundPriority(TaskKind kind)
    {
        return kind is TaskKind.ScreenCapture or TaskKind.TranslateText or TaskKind.ReviewSprites or TaskKind.ReviewCode;
    }
}
