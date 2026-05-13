using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public enum SchedulerTriggerKind
{
    StaleDashboardReport,
    PendingReviewedBundle,
    FailedPriorProofPacket,
    RecurringUserRequest
}

public sealed record SchedulerTrigger(
    SchedulerTriggerKind Kind,
    string Detail,
    string SuggestedTaskText,
    string ToolFamily,
    DateTimeOffset ObservedAtUtc,
    IReadOnlyList<string>? SourcePaths = null);

public sealed record SchedulerEvidencePacket(
    string SchemaVersion,
    Guid PacketId,
    string PacketKind,
    Guid TaskCardId,
    string TriggerKind,
    string TriggerDetail,
    string ToolFamily,
    string SuggestedTaskText,
    bool DidDispatchAdapter,
    bool DidMutate,
    bool DidUseNetwork,
    bool DidUseHostedAi,
    IReadOnlyList<string> SourcesInspected,
    string NextRecommendedAction,
    DateTimeOffset CreatedAtUtc);

public sealed record SchedulerProposalResult(
    bool Created,
    TaskCard? TaskCard,
    SchedulerEvidencePacket? EvidencePacket,
    string ArtifactFolder,
    string SummaryPath,
    string BlockReason);
