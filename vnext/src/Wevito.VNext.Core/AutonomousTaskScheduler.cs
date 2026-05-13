using System.Text.Json;
using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed record AutonomousSchedulerRequest(
    IReadOnlyDictionary<string, string> Settings,
    RuntimeSupervisorStatus RuntimeStatus,
    RuntimeBudgetSnapshot Budget,
    IReadOnlyList<SchedulerTrigger> Triggers,
    IReadOnlyList<TaskCard> ExistingCards,
    string ArtifactRoot,
    DateTimeOffset RequestedAtUtc);

public sealed class AutonomousTaskScheduler
{
    public const string SchedulerEnabledSetting = "scheduler_enabled";
    public const string SchedulerPreviewDispatchApprovedSetting = "scheduler_background_preview_dispatch_approved";
    public const string ToolFamily = "scheduler";

    private static readonly HashSet<SchedulerTriggerKind> AllowedTriggers =
    [
        SchedulerTriggerKind.StaleDashboardReport,
        SchedulerTriggerKind.PendingReviewedBundle,
        SchedulerTriggerKind.FailedPriorProofPacket,
        SchedulerTriggerKind.RecurringUserRequest
    ];

    private readonly RuntimeSupervisorService _runtimeSupervisorService;
    private readonly RuntimeBudgetMeter _budgetMeter;

    public AutonomousTaskScheduler(RuntimeSupervisorService runtimeSupervisorService, RuntimeBudgetMeter budgetMeter)
    {
        _runtimeSupervisorService = runtimeSupervisorService;
        _budgetMeter = budgetMeter;
    }

    public SchedulerProposalResult TryCreateProposal(AutonomousSchedulerRequest request)
    {
        if (!ReadBool(request.Settings, SchedulerEnabledSetting, false))
        {
            return Block("Autonomous scheduler is disabled.");
        }

        if (!_runtimeSupervisorService.CanStartBackgroundWork(request.RuntimeStatus, out var supervisorReason))
        {
            return Block(supervisorReason);
        }

        var trigger = request.Triggers.FirstOrDefault(trigger => AllowedTriggers.Contains(trigger.Kind));
        if (trigger is null)
        {
            return Block("No allowed scheduler trigger is available.");
        }

        if (HasExistingDraftForTrigger(request.ExistingCards, trigger))
        {
            return Block("A draft scheduler proposal already exists for this trigger.");
        }

        var reservation = _budgetMeter.TryReserve(request.Budget);
        if (!reservation.Allowed)
        {
            return Block(reservation.Reason);
        }

        var timestamp = request.RequestedAtUtc == default ? DateTimeOffset.UtcNow : request.RequestedAtUtc;
        var cardId = Guid.NewGuid();
        var intent = new TaskIntent(
            cardId,
            trigger.SuggestedTaskText,
            TaskIntentTargetMode.RouteToBestHelper,
            TaskKind: ResolveTaskKind(trigger.ToolFamily),
            RequestedToolFamily: trigger.ToolFamily,
            TargetPathsOrAssets: trigger.SourcePaths ?? [],
            ExpectedOutput: "Scheduler proposal only. Preview manually before any adapter runs.",
            CreatedAtUtc: timestamp);

        var card = new TaskCard(
            cardId,
            intent,
            TaskCardStatus.Draft,
            ToolFamily: trigger.ToolFamily,
            Timeline:
            [
                $"scheduler_proposed: {FormatTriggerKind(trigger.Kind)}",
                "scheduler_policy: draft only; no preview, execution, network, training, or mutation started"
            ],
            ResultSummary: $"Scheduler proposed a {trigger.ToolFamily} preview from {FormatTriggerKind(trigger.Kind)}.",
            CreatedAtUtc: timestamp,
            UpdatedAtUtc: timestamp);

        var packet = new SchedulerEvidencePacket(
            "1",
            Guid.NewGuid(),
            "scheduler_proposal",
            cardId,
            FormatTriggerKind(trigger.Kind),
            trigger.Detail,
            trigger.ToolFamily,
            trigger.SuggestedTaskText,
            DidDispatchAdapter: false,
            DidMutate: false,
            DidUseNetwork: false,
            DidUseHostedAi: false,
            SourcesInspected: trigger.SourcePaths ?? [],
            NextRecommendedAction: "Review the draft task card and choose Preview manually if this proposal is still useful.",
            CreatedAtUtc: timestamp);

        var artifactFolder = WriteEvidencePacket(request.ArtifactRoot, packet, reservation);
        return new SchedulerProposalResult(
            true,
            card with { AuditLogPath = Path.Combine(artifactFolder, "run-summary.md") },
            packet,
            artifactFolder,
            Path.Combine(artifactFolder, "run-summary.md"),
            "");
    }

    public static IReadOnlyList<SchedulerTrigger> BuildShellTriggers(
        IReadOnlyList<TaskCard>? existingCards,
        IReadOnlyDictionary<string, string>? settings,
        DateTimeOffset now)
    {
        var triggers = new List<SchedulerTrigger>();
        var cards = existingCards ?? [];
        if (cards.Count == 0 || cards.Max(card => card.UpdatedAtUtc == default ? card.CreatedAtUtc : card.UpdatedAtUtc) < now.AddHours(-24))
        {
            triggers.Add(new SchedulerTrigger(
                SchedulerTriggerKind.StaleDashboardReport,
                "PET TASKS dashboard has no fresh local report card in the last 24 hours.",
                "summarize the local docs",
                "localDocs",
                now,
                ["docs"]));
        }

        if ((settings ?? new Dictionary<string, string>()).TryGetValue("scheduler_recurring_user_request", out var recurring) &&
            !string.IsNullOrWhiteSpace(recurring))
        {
            triggers.Add(new SchedulerTrigger(
                SchedulerTriggerKind.RecurringUserRequest,
                "User configured a recurring scheduler request.",
                recurring,
                "localDocs",
                now));
        }

        return triggers;
    }

    private static string WriteEvidencePacket(string artifactRoot, SchedulerEvidencePacket packet, RuntimeBudgetReservation reservation)
    {
        var safeRoot = string.IsNullOrWhiteSpace(artifactRoot)
            ? Path.Combine(Environment.CurrentDirectory, "vnext", "artifacts", "pet-tasks")
            : artifactRoot;
        var folder = Path.Combine(safeRoot, $"{packet.CreatedAtUtc:yyyyMMdd-HHmmss}-scheduler-proposal");
        Directory.CreateDirectory(folder);

        File.WriteAllText(
            Path.Combine(folder, "scheduler-proposal.json"),
            JsonSerializer.Serialize(new
            {
                packet,
                budget = reservation
            }, new JsonSerializerOptions { WriteIndented = true }));

        File.WriteAllText(
            Path.Combine(folder, "run-summary.md"),
            string.Join(Environment.NewLine, [
                "# Scheduler Proposal",
                "",
                $"- Trigger: {packet.TriggerKind}",
                $"- Tool family: {packet.ToolFamily}",
                $"- Draft card: {packet.TaskCardId}",
                "- Adapter dispatched: false",
                "- Mutated files: false",
                "- Network used: false",
                "- Hosted AI used: false",
                "",
                packet.NextRecommendedAction
            ]));

        return folder;
    }

    private static bool HasExistingDraftForTrigger(IReadOnlyList<TaskCard> cards, SchedulerTrigger trigger)
    {
        var marker = FormatTriggerKind(trigger.Kind);
        return cards.Any(card =>
            card.Status == TaskCardStatus.Draft &&
            string.Equals(card.ToolFamily, trigger.ToolFamily, StringComparison.OrdinalIgnoreCase) &&
            (card.Timeline ?? []).Any(item => item.Contains(marker, StringComparison.OrdinalIgnoreCase)));
    }

    private static TaskKind ResolveTaskKind(string toolFamily)
    {
        return toolFamily switch
        {
            "localDocs" => TaskKind.SummarizeDocs,
            "localResearch" => TaskKind.Research,
            "spriteAudit" => TaskKind.ReviewSprites,
            "assetInventory" => TaskKind.InventoryAssets,
            "codeReview" => TaskKind.ReviewCode,
            "codePatchPlan" => TaskKind.PlanCodePatch,
            "buildProof" => TaskKind.BuildProof,
            _ => TaskKind.Unknown
        };
    }

    private static bool ReadBool(IReadOnlyDictionary<string, string> settings, string key, bool defaultValue)
    {
        return settings.TryGetValue(key, out var raw) && bool.TryParse(raw, out var parsed)
            ? parsed
            : defaultValue;
    }

    private static string FormatTriggerKind(SchedulerTriggerKind kind)
    {
        return kind switch
        {
            SchedulerTriggerKind.StaleDashboardReport => "stale_dashboard_report",
            SchedulerTriggerKind.PendingReviewedBundle => "pending_reviewed_bundle",
            SchedulerTriggerKind.FailedPriorProofPacket => "failed_prior_proof_packet",
            SchedulerTriggerKind.RecurringUserRequest => "recurring_user_request",
            _ => kind.ToString()
        };
    }

    private static SchedulerProposalResult Block(string reason)
    {
        return new SchedulerProposalResult(false, null, null, "", "", reason);
    }
}
