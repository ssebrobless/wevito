namespace Wevito.VNext.Core;

public sealed record CodexPhaseQueueEntry(
    string PhaseId,
    string PromptPath,
    string BranchName,
    bool AutoContinue,
    DateTimeOffset AddedAtUtc,
    string Status = "pending",
    int AttemptCount = 0);

public sealed record CodexPhaseHistoryRow(
    string PhaseId,
    string EventKind,
    DateTimeOffset CreatedAtUtc,
    string Summary,
    string Status);
