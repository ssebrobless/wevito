using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed record AutonomousScopeDescriptor(
    string ScopeId,
    string DisplayName,
    string Description,
    TimeSpan MinimumInterval,
    bool CanMutate);

public sealed record AutonomousScopeRunRequest(
    IReadOnlyDictionary<string, string> Settings,
    RuntimeSupervisorStatus RuntimeStatus,
    string ArtifactRoot,
    DateTimeOffset RequestedAtUtc,
    IReadOnlyList<TaskCard> ExistingTaskCards);

public sealed record AutonomousScopeRunResult(
    string ScopeId,
    bool Ran,
    bool DidMutate,
    IReadOnlyList<TaskCard> TaskCards,
    string Summary,
    string BlockReason = "");

public sealed record AutonomousScopeEvidenceFlags(
    bool DidUseNetwork,
    bool DidUseHostedAi,
    bool DidUseLocalModel,
    bool DidMutate)
{
    public static AutonomousScopeEvidenceFlags PreviewOnly { get; } = new(false, false, false, false);
}

public sealed record AutonomousScopePreviewItem(
    string Label,
    string SourcePath = "",
    string DestinationPath = "",
    string Sha256 = "",
    int? AgeDays = null);

public sealed record AutonomousScopePreview(
    string ScopeId,
    string Summary,
    int ActionCount,
    AutonomousScopeEvidenceFlags EvidenceFlags,
    string BlockReason = "",
    IReadOnlyList<AutonomousScopePreviewItem>? Items = null)
{
    public IReadOnlyList<AutonomousScopePreviewItem> PlannedItems { get; } = Items ?? [];

    public static AutonomousScopePreview Blocked(string scopeId, string reason)
    {
        return new AutonomousScopePreview(scopeId, "Preview blocked before any scope inspection.", 0, AutonomousScopeEvidenceFlags.PreviewOnly, reason);
    }
}

public sealed record AuditLedgerCleanupMovePlan(
    string Source,
    string Destination,
    string Sha256,
    string PostMoveSha256,
    int AgeDays);

public sealed record AuditLedgerCleanupPlan(
    string SchemaVersion,
    string AuditRoot,
    string ArchiveRoot,
    DateTimeOffset RequestedAtUtc,
    int MoveCount,
    IReadOnlyList<AuditLedgerCleanupMovePlan> Moves);

public sealed record AuditLedgerCleanupSummary(
    string SchemaVersion,
    string Mode,
    string AuditRoot,
    string ArchiveRoot,
    DateTimeOffset RequestedAtUtc,
    int MovedCount,
    string SummaryPath,
    IReadOnlyList<AuditLedgerCleanupMovePlan> Moved);

public interface IAutonomousScope
{
    AutonomousScopeDescriptor Descriptor { get; }

    AutonomousScopeRunResult TryRun(AutonomousScopeRunRequest request);

    Task<AutonomousScopePreview> DescribePlannedActionsAsync(CancellationToken cancellationToken);
}

public sealed class AutonomousScopeService
{
    public const string SpriteRepairTriageScopeId = "sprite-repair-triage";
    public const string SpriteRepairBatchProposalScopeId = "sprite-repair-batch-proposal";
    public const string EvalCoverageProposalScopeId = "eval-coverage-proposal";
    public const string AuditLedgerCleanupScopeId = "audit-ledger-cleanup";
    public const string EnabledChangedPacketKind = "autonomous_scope_enabled_changed";
    public const string TickPacketKind = "autonomous_scope_tick";
    public const string PreviewPacketKind = "autonomous_scope_preview";

    public static IReadOnlyList<AutonomousScopeDescriptor> KnownScopes { get; } =
    [
        new AutonomousScopeDescriptor(
            SpriteRepairTriageScopeId,
            "Sprite repair triage",
            "Drafts review-only task cards for high-priority sprite repair queue rows.",
            TimeSpan.FromMinutes(30),
            CanMutate: false),
        new AutonomousScopeDescriptor(
            SpriteRepairBatchProposalScopeId,
            "Sprite-repair batch proposal",
            "Review only. No sprite mutation. No apply. Drafts a supervised self-improvement repair proposal packet.",
            TimeSpan.FromMinutes(30),
            CanMutate: false),
        new AutonomousScopeDescriptor(
            EvalCoverageProposalScopeId,
            "Eval coverage proposal (review-only)",
            "Review only. No mutation. No apply. Drafts an eval coverage gap proposal packet.",
            TimeSpan.FromMinutes(30),
            CanMutate: false),
        new AutonomousScopeDescriptor(
            AuditLedgerCleanupScopeId,
            "Audit ledger cleanup",
            "Moves old already-archived JSONL audit files into an archive folder without editing or deleting them.",
            TimeSpan.FromHours(24),
            CanMutate: true)
    ];

    private readonly AuditLedgerService _ledger;
    private readonly KillSwitchService? _killSwitchService;

    public AutonomousScopeService(AuditLedgerService ledger, KillSwitchService? killSwitchService = null)
    {
        _ledger = ledger;
        _killSwitchService = killSwitchService;
    }

    public static string BuildEnabledSettingKey(string scopeId)
    {
        return $"autonomous_scope_{scopeId}_enabled";
    }

    public static bool IsEnabled(IReadOnlyDictionary<string, string> settings, string scopeId)
    {
        return settings.TryGetValue(BuildEnabledSettingKey(scopeId), out var raw) &&
               bool.TryParse(raw, out var parsed) &&
               parsed;
    }

    public bool CanRunScope(AutonomousScopeDescriptor scope, AutonomousScopeRunRequest request, out string blockReason)
    {
        if (_killSwitchService?.IsActive() == true || KillSwitchService.IsActive(request.Settings))
        {
            blockReason = "kill_switch=true";
            return false;
        }

        var config = AutonomousOperationsConfig.FromSettings(request.Settings);
        if (!config.Enabled)
        {
            blockReason = "runtime_autonomous_beta_enabled=false";
            return false;
        }

        if (!IsEnabled(request.Settings, scope.ScopeId))
        {
            blockReason = $"{BuildEnabledSettingKey(scope.ScopeId)}=false";
            return false;
        }

        if (request.RuntimeStatus.Mode != RuntimeSupervisorMode.Active)
        {
            blockReason = "Runtime supervisor must be Active.";
            return false;
        }

        if (!request.RuntimeStatus.BackgroundWorkAllowed)
        {
            blockReason = "Runtime supervisor background work must be allowed.";
            return false;
        }

        blockReason = "";
        return true;
    }

    public void RecordEnabledChanged(string scopeId, bool enabled, DateTimeOffset nowUtc)
    {
        if (_killSwitchService?.IsActive() == true)
        {
            return;
        }

        _ledger.Record(new EvidencePacket(
            Guid.NewGuid(),
            EnabledChangedPacketKind,
            null,
            nowUtc,
            DidUseNetwork: false,
            DidUseHostedAi: false,
            DidUseLocalModel: false,
            DidMutate: false,
            ArtifactPath: "",
            Summary: $"Autonomous scope '{scopeId}' enabled={enabled}.",
            Status: "Completed"));
    }

    public void RecordTick(string scopeId, bool didMutate, string summary, DateTimeOffset nowUtc)
    {
        _ledger.Record(new EvidencePacket(
            Guid.NewGuid(),
            TickPacketKind,
            null,
            nowUtc,
            DidUseNetwork: false,
            DidUseHostedAi: false,
            DidUseLocalModel: false,
            DidMutate: didMutate,
            ArtifactPath: "",
            Summary: summary,
            Status: "Completed"));
    }

    public async Task<AutonomousScopePreview> PreviewAsync(
        string scopeId,
        IReadOnlyList<IAutonomousScope> scopes,
        CancellationToken cancellationToken = default)
    {
        if (_killSwitchService?.IsActive() == true)
        {
            return AutonomousScopePreview.Blocked(scopeId, "kill_switch=true");
        }

        var scope = scopes.FirstOrDefault(candidate =>
            candidate.Descriptor.ScopeId.Equals(scopeId, StringComparison.OrdinalIgnoreCase));
        if (scope is null)
        {
            var missingPreview = AutonomousScopePreview.Blocked(scopeId, "scope_not_registered");
            RecordPreview(missingPreview, DateTimeOffset.UtcNow, "Blocked");
            return missingPreview;
        }

        var preview = await scope.DescribePlannedActionsAsync(cancellationToken);
        RecordPreview(preview, DateTimeOffset.UtcNow, string.IsNullOrWhiteSpace(preview.BlockReason) ? "PreviewReady" : "Blocked");
        return preview;
    }

    private void RecordPreview(AutonomousScopePreview preview, DateTimeOffset nowUtc, string status)
    {
        if (_killSwitchService?.IsActive() == true)
        {
            return;
        }

        _ledger.Record(new EvidencePacket(
            Guid.NewGuid(),
            PreviewPacketKind,
            null,
            nowUtc,
            preview.EvidenceFlags.DidUseNetwork,
            preview.EvidenceFlags.DidUseHostedAi,
            preview.EvidenceFlags.DidUseLocalModel,
            preview.EvidenceFlags.DidMutate,
            ArtifactPath: "",
            Summary: $"{preview.ScopeId}: {preview.Summary}",
            Status: status,
            Error: preview.BlockReason));
    }
}

public sealed class AutonomousScopeRegistry
{
    private readonly AutonomousScopeService _scopeService;
    private readonly IReadOnlyList<IAutonomousScope> _scopes;

    public AutonomousScopeRegistry(AutonomousScopeService scopeService, IReadOnlyList<IAutonomousScope> scopes)
    {
        _scopeService = scopeService;
        _scopes = scopes;
    }

    public AutonomousScopeRunResult RunEnabledScopes(AutonomousScopeRunRequest request)
    {
        var cards = request.ExistingTaskCards.ToList();
        var didMutate = false;
        var summaries = new List<string>();
        var anyRan = false;

        foreach (var scope in _scopes)
        {
            if (!_scopeService.CanRunScope(scope.Descriptor, request with { ExistingTaskCards = cards }, out var blockReason))
            {
                summaries.Add($"{scope.Descriptor.ScopeId}: blocked ({blockReason})");
                continue;
            }

            var result = scope.TryRun(request with { ExistingTaskCards = cards });
            if (!result.Ran)
            {
                summaries.Add($"{scope.Descriptor.ScopeId}: skipped ({result.BlockReason})");
                continue;
            }

            anyRan = true;
            didMutate |= result.DidMutate;
            cards = result.TaskCards.ToList();
            summaries.Add($"{scope.Descriptor.ScopeId}: {result.Summary}");
            _scopeService.RecordTick(scope.Descriptor.ScopeId, result.DidMutate, result.Summary, request.RequestedAtUtc);
        }

        return new AutonomousScopeRunResult(
            "registry",
            anyRan,
            didMutate,
            cards,
            string.Join(" | ", summaries));
    }
}

public sealed class SpriteRepairTriageScope : IAutonomousScope
{
    public const string PacketKind = "sprite_repair_triage_card_drafted";
    public const string EnrichedPacketKind = "sprite_repair_triage_card_enriched";
    public const string ToolFamily = "sprite-repair-batch-proposal";

    private readonly string _queuePath;
    private readonly AuditLedgerService _ledger;
    private readonly AgentTaskCardQueueService _queueService;

    public SpriteRepairTriageScope(string queuePath, AuditLedgerService ledger, AgentTaskCardQueueService? queueService = null)
    {
        _queuePath = queuePath;
        _ledger = ledger;
        _queueService = queueService ?? new AgentTaskCardQueueService();
    }

    public AutonomousScopeDescriptor Descriptor { get; } = AutonomousScopeService.KnownScopes.Single(scope =>
        scope.ScopeId.Equals(AutonomousScopeService.SpriteRepairTriageScopeId, StringComparison.OrdinalIgnoreCase));

    public AutonomousScopeRunResult TryRun(AutonomousScopeRunRequest request)
    {
        if (!File.Exists(_queuePath))
        {
            return Skipped(request.ExistingTaskCards, $"queue missing: {_queuePath}");
        }

        var rows = ReadPriorityRows(_queuePath).Take(5).ToList();
        if (rows.Count == 0)
        {
            return new AutonomousScopeRunResult(Descriptor.ScopeId, true, false, request.ExistingTaskCards, "0 P0/P1 repair rows found.");
        }

        var cards = request.ExistingTaskCards;
        var drafted = 0;
        foreach (var row in rows)
        {
            if (cards.Any(card => card.Intent.TargetPathsOrAssets?.Contains(row.RowId, StringComparer.OrdinalIgnoreCase) == true))
            {
                continue;
            }

            var card = BuildCard(row, request.RequestedAtUtc, ResolveRepoRoot(_queuePath));
            cards = _queueService.AppendDraft(cards, card);
            drafted++;
            _ledger.Record(new EvidencePacket(
                Guid.NewGuid(),
                PacketKind,
                card.Id,
                request.RequestedAtUtc,
                DidUseNetwork: false,
                DidUseHostedAi: false,
                DidUseLocalModel: false,
                DidMutate: false,
                ArtifactPath: _queuePath,
                Summary: $"Drafted review-only sprite repair triage card for {row.RowId} priority={row.Priority}.",
                Status: "Drafted"));
            _ledger.Record(new EvidencePacket(
                Guid.NewGuid(),
                EnrichedPacketKind,
                card.Id,
                request.RequestedAtUtc,
                DidUseNetwork: false,
                DidUseHostedAi: false,
                DidUseLocalModel: false,
                DidMutate: false,
                ArtifactPath: _queuePath,
                Summary: $"Enriched sprite repair triage card for {row.RowId} with review-only plan payload.",
                Status: "Drafted"));
        }

        return new AutonomousScopeRunResult(
            Descriptor.ScopeId,
            true,
            false,
            cards,
            $"drafted {drafted} sprite repair triage card(s).");
    }

    public Task<AutonomousScopePreview> DescribePlannedActionsAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!File.Exists(_queuePath))
        {
            return Task.FromResult(new AutonomousScopePreview(
                Descriptor.ScopeId,
                $"No cards would be drafted because the queue is missing: {_queuePath}",
                0,
                AutonomousScopeEvidenceFlags.PreviewOnly,
                "queue_missing"));
        }

        var rows = ReadPriorityRows(_queuePath).Take(5).ToList();
        var items = rows
            .Select(row => new AutonomousScopePreviewItem(
                $"{row.Priority} {row.RowId} -> draft review-only sprite repair card",
                SourcePath: _queuePath))
            .ToList();
        var summary = rows.Count == 0
            ? "No P0/P1 sprite repair rows would draft cards."
            : $"Would draft {rows.Count} review-only sprite repair card(s) on the next eligible tick.";
        return Task.FromResult(new AutonomousScopePreview(
            Descriptor.ScopeId,
            summary,
            rows.Count,
            AutonomousScopeEvidenceFlags.PreviewOnly,
            Items: items));
    }

    private static TaskCard BuildCard(SpriteRepairQueueRow row, DateTimeOffset nowUtc, string repoRoot)
    {
        var runner = new SpriteRepairBatchRunner();
        var issues = row.Issues.Count > 0 ? row.Issues : BuildFallbackIssues(row);
        var plans = issues.Select((issue, index) => runner.BuildPlanForReview(new SpriteRepairBatchRequest(
                repoRoot,
                row,
                issue,
                Path.Combine(repoRoot, "vnext", "artifacts", "sprite-repair-triage-review"),
                nowUtc,
                BatchId: $"{row.RowId}-{issue.ColorVariant}-{issue.AnimationFamily}-review-{index + 1:00}")))
            .ToArray();
        var payload = BuildReviewPayload(row, plans);
        var intent = new TaskIntent(
            Guid.NewGuid(),
            $"Review sprite repair queue row {row.RowId} ({row.Priority}) and prepare a guarded repair proposal only.",
            TaskIntentTargetMode.RouteToBestHelper,
            TaskKind: TaskKind.ReviewSprites,
            RequestedToolFamily: ToolFamily,
            TargetPathsOrAssets: [row.RowId, row.SpeciesId, row.LifeStage, row.Gender],
            RiskLevel: ToolRiskLevel.Medium,
            NeedsApproval: true,
            ExpectedOutput: "Draft proposal only; no sprite mutation.");
        var policy = new ToolPolicy(
            "autonomous-sprite-repair-triage",
            ToolFamily,
            ToolAccessMode.ReadOnly,
            ToolRiskLevel.Medium,
            ApprovalRequirement.BeforeExecution,
            IsEnabled: false,
            ApprovedRootPaths: [],
            BlockReason: "Autonomous triage may draft cards only; guarded apply requires explicit user approval.");
        return new TaskCard(
            Guid.NewGuid(),
            intent,
            TaskCardStatus.Draft,
            ToolFamily: ToolFamily,
            PolicySnapshot: policy,
            Timeline:
            [
                $"{nowUtc:O} autonomous scope drafted review-only repair proposal.",
                $"enriched_payload: plans={plans.Length} flagged_frames={plans.SelectMany(plan => plan.FlaggedFrameRelativePaths).Distinct(StringComparer.OrdinalIgnoreCase).Count()} would_write={plans.SelectMany(plan => plan.WouldWriteRelativePaths).Distinct(StringComparer.OrdinalIgnoreCase).Count()}"
            ],
            ResultSummary: BuildResultSummary(row, plans),
            AuditLogPath: "",
            CreatedAtUtc: nowUtc,
            UpdatedAtUtc: nowUtc,
            ReviewPayload: payload);
    }

    private static IReadOnlyList<SpriteRepairQueueRow> ReadPriorityRows(string queuePath)
    {
        var repoRoot = ResolveRepoRoot(queuePath);
        try
        {
            return new SpriteRepairQueueReader().Load(queuePath, repoRoot).Rows
                .Where(IsP0OrP1)
                .ToArray();
        }
        catch (Exception) when (File.Exists(queuePath))
        {
            return ReadPriorityRowsFallback(queuePath);
        }
    }

    private static IReadOnlyList<SpriteRepairQueueRow> ReadPriorityRowsFallback(string queuePath)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(queuePath));
        if (!document.RootElement.TryGetProperty("rows", out var rows) || rows.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var result = new List<SpriteRepairQueueRow>();
        foreach (var row in rows.EnumerateArray())
        {
            var priority = ReadString(row, "priority");
            if (!IsP0OrP1(priority))
            {
                continue;
            }

            var rowId = ReadString(row, "rowId");
            var speciesId = ReadString(row, "speciesId");
            var lifeStage = ReadString(row, "lifeStage");
            var gender = ReadString(row, "gender");
            result.Add(new SpriteRepairQueueRow(
                rowId,
                speciesId,
                lifeStage,
                gender,
                priority,
                ReadString(row, "status"),
                1,
                ["blue"],
                ["idle"],
                [],
                [
                    new SpriteRepairQueueIssue(
                        "blue",
                        "idle",
                        priority,
                        [],
                        [],
                        "",
                        "fallback row parsed from minimal repair queue JSON.",
                        null,
                        null)
                ]));
        }

        return result;
    }

    private static IReadOnlyList<SpriteRepairQueueIssue> BuildFallbackIssues(SpriteRepairQueueRow row)
    {
        var color = row.ColorsAffected.FirstOrDefault() ?? "blue";
        var animation = row.AnimationsAffected.FirstOrDefault() ?? "idle";
        var tool = row.RecommendedTools.FirstOrDefault() ?? "";
        return
        [
            new SpriteRepairQueueIssue(
                color,
                animation,
                row.Priority,
                [],
                [],
                tool,
                "fallback review issue for row with no issue details.",
                null,
                null)
        ];
    }

    private static Dictionary<string, string> BuildReviewPayload(SpriteRepairQueueRow row, IReadOnlyList<SpriteRepairBatchPlan> plans)
    {
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["schema_version"] = "1",
            ["row_id"] = row.RowId,
            ["species"] = row.SpeciesId,
            ["age"] = row.LifeStage,
            ["gender"] = row.Gender,
            ["priority"] = row.Priority,
            ["flagged_frame_relative_paths"] = string.Join("|", plans.SelectMany(plan => plan.FlaggedFrameRelativePaths).Distinct(StringComparer.OrdinalIgnoreCase)),
            ["repair_command_lines"] = string.Join("|", plans.Select(plan => plan.CommandLine)),
            ["candidate_output_directories"] = string.Join("|", plans.Select(plan => plan.CandidateOutputDirectory)),
            ["runtime_target_directories"] = string.Join("|", plans.Select(plan => plan.RuntimeTargetDirectory)),
            ["would_write_relative_paths"] = string.Join("|", plans.SelectMany(plan => plan.WouldWriteRelativePaths).Distinct(StringComparer.OrdinalIgnoreCase)),
            ["plan_summaries"] = string.Join("|", plans.Select(plan => plan.Summary))
        };
    }

    private static string BuildResultSummary(SpriteRepairQueueRow row, IReadOnlyList<SpriteRepairBatchPlan> plans)
    {
        var flaggedCount = plans.SelectMany(plan => plan.FlaggedFrameRelativePaths).Distinct(StringComparer.OrdinalIgnoreCase).Count();
        var wouldWriteCount = plans.SelectMany(plan => plan.WouldWriteRelativePaths).Distinct(StringComparer.OrdinalIgnoreCase).Count();
        return $"Draft only. {row.SpeciesId}/{row.LifeStage}/{row.Gender} has {plans.Count} repair plan(s), {flaggedCount} flagged frame path(s), and {wouldWriteCount} would-write runtime target path(s). No preview, execution, candidate creation, or sprite mutation has run.";
    }

    private static bool IsP0OrP1(SpriteRepairQueueRow row)
    {
        return IsP0OrP1(row.Priority);
    }

    private static bool IsP0OrP1(string priority)
    {
        return priority.Equals("P0", StringComparison.OrdinalIgnoreCase) ||
               priority.Equals("P1", StringComparison.OrdinalIgnoreCase);
    }

    private static string ResolveRepoRoot(string path)
    {
        var directory = new DirectoryInfo(Path.GetDirectoryName(Path.GetFullPath(path)) ?? Directory.GetCurrentDirectory());
        while (directory is not null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, ".git")) ||
                File.Exists(Path.Combine(directory.FullName, "wevito.godot")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return Directory.GetCurrentDirectory();
    }

    private static string ReadString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString() ?? ""
            : "";
    }

    private static AutonomousScopeRunResult Skipped(IReadOnlyList<TaskCard> cards, string reason)
    {
        return new AutonomousScopeRunResult(AutonomousScopeService.SpriteRepairTriageScopeId, false, false, cards, "", reason);
    }

}

public sealed class AuditLedgerCleanupScope : IAutonomousScope
{
    public const string PacketKind = "audit_ledger_cleanup_summary";
    public const string DryRunPacketKind = "audit_ledger_cleanup_dry_run";
    public const string RolledBackPacketKind = "audit_ledger_cleanup_rolled_back";

    private readonly string _auditRoot;
    private readonly AuditLedgerService _ledger;
    private readonly KillSwitchService? _killSwitchService;

    public AuditLedgerCleanupScope(string auditRoot, AuditLedgerService ledger, KillSwitchService? killSwitchService = null)
    {
        _auditRoot = auditRoot;
        _ledger = ledger;
        _killSwitchService = killSwitchService;
    }

    public AutonomousScopeDescriptor Descriptor { get; } = AutonomousScopeService.KnownScopes.Single(scope =>
        scope.ScopeId.Equals(AutonomousScopeService.AuditLedgerCleanupScopeId, StringComparison.OrdinalIgnoreCase));

    public AutonomousScopeRunResult TryRun(AutonomousScopeRunRequest request)
    {
        if (_killSwitchService?.IsActive() == true || KillSwitchService.IsActive(request.Settings))
        {
            return new AutonomousScopeRunResult(Descriptor.ScopeId, false, false, request.ExistingTaskCards, "", "kill_switch=true");
        }

        var result = ApplyCleanup(request.RequestedAtUtc);

        var summary = $"Audit ledger cleanup moved {result.MovedCount} old archived JSONL file(s); delete=false edit=false.";
        _ledger.Record(new EvidencePacket(
            Guid.NewGuid(),
            PacketKind,
            null,
            request.RequestedAtUtc,
            DidUseNetwork: false,
            DidUseHostedAi: false,
            DidUseLocalModel: false,
            DidMutate: result.MovedCount > 0,
            ArtifactPath: result.SummaryPath,
            Summary: summary,
            Status: "Completed"));
        return new AutonomousScopeRunResult(Descriptor.ScopeId, true, result.MovedCount > 0, request.ExistingTaskCards, summary);
    }

    public AuditLedgerCleanupPlan DescribeCleanupPlan()
    {
        return BuildPlan(DateTimeOffset.UtcNow);
    }

    public AuditLedgerCleanupSummary ApplyDryRun()
    {
        if (_killSwitchService?.IsActive() == true)
        {
            throw new InvalidOperationException("kill_switch=true");
        }

        var plan = BuildPlan(DateTimeOffset.UtcNow);
        var summary = WriteSummary(plan, "dry-run", plan.Moves);
        _ledger.Record(new EvidencePacket(
            Guid.NewGuid(),
            DryRunPacketKind,
            null,
            plan.RequestedAtUtc,
            DidUseNetwork: false,
            DidUseHostedAi: false,
            DidUseLocalModel: false,
            DidMutate: false,
            ArtifactPath: summary.SummaryPath,
            Summary: $"Audit ledger cleanup dry-run planned {summary.MovedCount} old archived JSONL move(s); moved=0.",
            Status: "PreviewReady"));
        return summary;
    }

    public AuditLedgerCleanupSummary ApplyCleanup()
    {
        return ApplyCleanup(DateTimeOffset.UtcNow);
    }

    public AuditLedgerCleanupSummary ApplyCleanup(DateTimeOffset requestedAtUtc)
    {
        if (_killSwitchService?.IsActive() == true)
        {
            throw new InvalidOperationException("kill_switch=true");
        }

        var plan = BuildPlan(requestedAtUtc);
        Directory.CreateDirectory(plan.ArchiveRoot);
        var moved = new List<AuditLedgerCleanupMovePlan>();
        foreach (var item in plan.Moves)
        {
            File.Move(item.Source, item.Destination);
            moved.Add(item with { PostMoveSha256 = Sha256(item.Destination) });
        }

        return WriteSummary(plan, "apply", moved);
    }

    public Task<AutonomousScopePreview> DescribePlannedActionsAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!Directory.Exists(_auditRoot))
        {
            return Task.FromResult(new AutonomousScopePreview(
                Descriptor.ScopeId,
                $"No audit files would move because the audit root is missing: {_auditRoot}",
                0,
                AutonomousScopeEvidenceFlags.PreviewOnly,
                "audit_root_missing"));
        }

        var plan = BuildPlan(DateTimeOffset.UtcNow);
        var items = plan.Moves
            .Select(item => new AutonomousScopePreviewItem(
                $"archive old audit JSONL {Path.GetFileName(item.Source)}",
                SourcePath: item.Source,
                DestinationPath: item.Destination,
                Sha256: item.Sha256,
                AgeDays: item.AgeDays))
            .ToList();

        var summary = items.Count == 0
            ? "No old archived audit JSONL files would move."
            : $"Would move {items.Count} old archived audit JSONL file(s) into archive/.";
        return Task.FromResult(new AutonomousScopePreview(
            Descriptor.ScopeId,
            summary,
            items.Count,
            AutonomousScopeEvidenceFlags.PreviewOnly,
            Items: items));
    }

    internal static bool IsArchivedLedgerFile(FileInfo file)
    {
        return file.Extension.Equals(".jsonl", StringComparison.OrdinalIgnoreCase) &&
               file.Name.Contains("archived", StringComparison.OrdinalIgnoreCase);
    }

    private AuditLedgerCleanupPlan BuildPlan(DateTimeOffset requestedAtUtc)
    {
        Directory.CreateDirectory(_auditRoot);
        var archiveRoot = Path.Combine(_auditRoot, "archive");
        var cutoff = requestedAtUtc.UtcDateTime.AddDays(-30);
        var moves = new List<AuditLedgerCleanupMovePlan>();
        foreach (var file in new DirectoryInfo(_auditRoot).EnumerateFiles("*.jsonl", SearchOption.TopDirectoryOnly))
        {
            if (!IsArchivedLedgerFile(file) || file.LastWriteTimeUtc > cutoff)
            {
                continue;
            }

            var destination = Path.Combine(archiveRoot, file.Name);
            if (File.Exists(destination))
            {
                destination = Path.Combine(archiveRoot, $"{Path.GetFileNameWithoutExtension(file.Name)}-{requestedAtUtc:yyyyMMddHHmmss}.jsonl");
            }

            moves.Add(new AuditLedgerCleanupMovePlan(
                file.FullName,
                destination,
                Sha256(file.FullName),
                "",
                Math.Max(0, (int)Math.Floor((requestedAtUtc.UtcDateTime - file.LastWriteTimeUtc).TotalDays))));
        }

        return new AuditLedgerCleanupPlan("1", _auditRoot, archiveRoot, requestedAtUtc, moves.Count, moves);
    }

    private AuditLedgerCleanupSummary WriteSummary(
        AuditLedgerCleanupPlan plan,
        string mode,
        IReadOnlyList<AuditLedgerCleanupMovePlan> moved)
    {
        var summaryRoot = Path.Combine(_auditRoot, "cleanup-summaries");
        Directory.CreateDirectory(summaryRoot);
        var summaryPath = Path.Combine(summaryRoot, $"{plan.RequestedAtUtc:yyyyMMdd-HHmmss-fffffff}-audit-ledger-cleanup-{mode}.json");
        var summary = new AuditLedgerCleanupSummary("1", mode, plan.AuditRoot, plan.ArchiveRoot, plan.RequestedAtUtc, moved.Count, summaryPath, moved);
        File.WriteAllText(summaryPath, JsonSerializer.Serialize(new
        {
            schemaVersion = summary.SchemaVersion,
            mode = summary.Mode,
            auditRoot = summary.AuditRoot,
            archiveRoot = summary.ArchiveRoot,
            requestedAtUtc = summary.RequestedAtUtc,
            movedCount = summary.MovedCount,
            summaryPath = summary.SummaryPath,
            moved = summary.Moved.Select(item => new
            {
                source = item.Source,
                destination = item.Destination,
                sha256 = item.Sha256,
                afterSha256 = item.PostMoveSha256,
                preMoveSha256 = item.Sha256,
                postMoveSha256 = item.PostMoveSha256,
                ageDays = item.AgeDays
            }).ToArray()
        }, JsonDefaults.Options));
        return summary;
    }

    private static string Sha256(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }
}
