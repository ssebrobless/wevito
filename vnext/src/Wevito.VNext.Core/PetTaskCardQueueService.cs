using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed class PetTaskCardQueueService
{
    public const int DefaultMaxCards = 25;

    private readonly int _maxCards;

    public PetTaskCardQueueService(int maxCards = DefaultMaxCards)
    {
        _maxCards = Math.Max(1, maxCards);
    }

    public IReadOnlyList<TaskCard> AppendDraft(IReadOnlyList<TaskCard>? existingCards, TaskCard card)
    {
        var withoutDuplicate = (existingCards ?? [])
            .Where(existing => existing.Id != card.Id)
            .ToList();

        return withoutDuplicate
            .Prepend(card)
            .OrderByDescending(existing => existing.UpdatedAtUtc == default ? existing.CreatedAtUtc : existing.UpdatedAtUtc)
            .ThenByDescending(existing => existing.CreatedAtUtc)
            .Take(_maxCards)
            .ToList();
    }

    public bool TryTransitionStatus(
        IReadOnlyList<TaskCard>? existingCards,
        Guid cardId,
        TaskCardStatus nextStatus,
        DateTimeOffset timestamp,
        out IReadOnlyList<TaskCard> updatedCards,
        out TaskCard? updatedCard,
        out string reason)
    {
        var cards = (existingCards ?? []).ToList();
        var index = cards.FindIndex(card => card.Id == cardId);
        if (index < 0)
        {
            updatedCards = cards;
            updatedCard = null;
            reason = "Task card was not found.";
            return false;
        }

        var current = cards[index];
        if (!CanTransition(current.Status, nextStatus))
        {
            updatedCards = cards;
            updatedCard = current;
            reason = $"Cannot move task card from {current.Status} to {nextStatus}.";
            return false;
        }

        var timeline = (current.Timeline ?? []).ToList();
        timeline.Add(nextStatus switch
        {
            TaskCardStatus.Approved => "user_approved: execution adapter is still disabled",
            TaskCardStatus.Cancelled => "user_cancelled: no execution was started",
            _ => $"status_changed: {current.Status} to {nextStatus}"
        });

        updatedCard = current with
        {
            Status = nextStatus,
            Timeline = timeline,
            ResultSummary = nextStatus switch
            {
                TaskCardStatus.Approved => "Approved by user. Waiting for a future execution adapter.",
                TaskCardStatus.Cancelled => "Cancelled by user before execution.",
                _ => current.ResultSummary
            },
            UpdatedAtUtc = timestamp
        };

        cards[index] = updatedCard;
        updatedCards = cards
            .OrderByDescending(card => card.UpdatedAtUtc == default ? card.CreatedAtUtc : card.UpdatedAtUtc)
            .ThenByDescending(card => card.CreatedAtUtc)
            .Take(_maxCards)
            .ToList();
        reason = "";
        return true;
    }

    public bool TryApplyAdapterResult(
        IReadOnlyList<TaskCard>? existingCards,
        TaskAdapterResult result,
        DateTimeOffset timestamp,
        out IReadOnlyList<TaskCard> updatedCards,
        out TaskCard? updatedCard,
        out string reason)
    {
        var cards = (existingCards ?? []).ToList();
        var index = cards.FindIndex(card => card.Id == result.TaskCardId);
        if (index < 0)
        {
            updatedCards = cards;
            updatedCard = null;
            reason = "Task card was not found.";
            return false;
        }

        var current = cards[index];
        if (current.Status is not (TaskCardStatus.Draft or TaskCardStatus.Approved or TaskCardStatus.Reviewing))
        {
            updatedCards = cards;
            updatedCard = current;
            reason = $"Cannot preview task card while it is {current.Status}.";
            return false;
        }

        var timeline = (current.Timeline ?? []).ToList();
        timeline.Add(result.Status switch
        {
            TaskAdapterResultStatus.PreviewReady => $"preview_ready: {result.PreviewSummary}",
            TaskAdapterResultStatus.Completed => $"adapter_completed: {result.ResultSummary}",
            TaskAdapterResultStatus.Blocked => $"preview_blocked: {result.BlockReason}",
            TaskAdapterResultStatus.Failed => $"preview_failed: {result.BlockReason}",
            _ => $"preview_completed: {result.ResultSummary}"
        });

        updatedCard = current with
        {
            Status = result.Status switch
            {
                TaskAdapterResultStatus.PreviewReady => TaskCardStatus.Reviewing,
                TaskAdapterResultStatus.Blocked => TaskCardStatus.Blocked,
                TaskAdapterResultStatus.Failed => TaskCardStatus.Failed,
                _ => TaskCardStatus.Done
            },
            Timeline = timeline,
            ResultSummary = BuildAdapterResultSummary(result),
            AuditLogPath = result.AuditLogPath,
            UpdatedAtUtc = timestamp
        };

        cards[index] = updatedCard;
        updatedCards = cards
            .OrderByDescending(card => card.UpdatedAtUtc == default ? card.CreatedAtUtc : card.UpdatedAtUtc)
            .ThenByDescending(card => card.CreatedAtUtc)
            .Take(_maxCards)
            .ToList();
        reason = "";
        return true;
    }

    private static bool CanTransition(TaskCardStatus current, TaskCardStatus next)
    {
        if (current == next)
        {
            return true;
        }

        return next switch
        {
            TaskCardStatus.Approved => current == TaskCardStatus.WaitingForApproval,
            TaskCardStatus.Cancelled => current is TaskCardStatus.Draft or TaskCardStatus.WaitingForApproval or TaskCardStatus.Approved,
            _ => false
        };
    }

    private static string BuildAdapterResultSummary(TaskAdapterResult result)
    {
        if (!string.IsNullOrWhiteSpace(result.ResultSummary))
        {
            return result.ResultSummary;
        }

        if (!string.IsNullOrWhiteSpace(result.PreviewSummary))
        {
            return result.PreviewSummary;
        }

        return result.BlockReason;
    }
}
