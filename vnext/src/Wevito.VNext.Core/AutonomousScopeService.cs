using System.Security.Cryptography;
using System.Text.Json;
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

public interface IAutonomousScope
{
    AutonomousScopeDescriptor Descriptor { get; }

    AutonomousScopeRunResult TryRun(AutonomousScopeRunRequest request);
}

public sealed class AutonomousScopeService
{
    public const string SpriteRepairTriageScopeId = "sprite-repair-triage";
    public const string AuditLedgerCleanupScopeId = "audit-ledger-cleanup";
    public const string EnabledChangedPacketKind = "autonomous_scope_enabled_changed";
    public const string TickPacketKind = "autonomous_scope_tick";

    public static IReadOnlyList<AutonomousScopeDescriptor> KnownScopes { get; } =
    [
        new AutonomousScopeDescriptor(
            SpriteRepairTriageScopeId,
            "Sprite repair triage",
            "Drafts review-only task cards for high-priority sprite repair queue rows.",
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

            var card = BuildCard(row, request.RequestedAtUtc);
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
        }

        return new AutonomousScopeRunResult(
            Descriptor.ScopeId,
            true,
            false,
            cards,
            $"drafted {drafted} sprite repair triage card(s).");
    }

    private static TaskCard BuildCard(SpriteRepairQueueRow row, DateTimeOffset nowUtc)
    {
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
            Timeline: [$"{nowUtc:O} autonomous scope drafted review-only repair proposal."],
            ResultSummary: "Draft only. No preview, execution, or sprite mutation has run.",
            AuditLogPath: "",
            CreatedAtUtc: nowUtc,
            UpdatedAtUtc: nowUtc);
    }

    private static IReadOnlyList<SpriteRepairQueueRow> ReadPriorityRows(string queuePath)
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
            if (!priority.Equals("P0", StringComparison.OrdinalIgnoreCase) &&
                !priority.Equals("P1", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            result.Add(new SpriteRepairQueueRow(
                ReadString(row, "rowId"),
                priority,
                ReadString(row, "speciesId"),
                ReadString(row, "lifeStage"),
                ReadString(row, "gender")));
        }

        return result;
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

    private sealed record SpriteRepairQueueRow(string RowId, string Priority, string SpeciesId, string LifeStage, string Gender);
}

public sealed class AuditLedgerCleanupScope : IAutonomousScope
{
    public const string PacketKind = "audit_ledger_cleanup_summary";

    private readonly string _auditRoot;
    private readonly AuditLedgerService _ledger;

    public AuditLedgerCleanupScope(string auditRoot, AuditLedgerService ledger)
    {
        _auditRoot = auditRoot;
        _ledger = ledger;
    }

    public AutonomousScopeDescriptor Descriptor { get; } = AutonomousScopeService.KnownScopes.Single(scope =>
        scope.ScopeId.Equals(AutonomousScopeService.AuditLedgerCleanupScopeId, StringComparison.OrdinalIgnoreCase));

    public AutonomousScopeRunResult TryRun(AutonomousScopeRunRequest request)
    {
        Directory.CreateDirectory(_auditRoot);
        var archiveRoot = Path.Combine(_auditRoot, "archive");
        Directory.CreateDirectory(archiveRoot);
        var cutoff = request.RequestedAtUtc.UtcDateTime.AddDays(-30);
        var moved = new List<object>();

        foreach (var file in new DirectoryInfo(_auditRoot).EnumerateFiles("*.jsonl", SearchOption.TopDirectoryOnly))
        {
            if (!IsArchivedLedgerFile(file) || file.LastWriteTimeUtc > cutoff)
            {
                continue;
            }

            var destination = Path.Combine(archiveRoot, file.Name);
            if (File.Exists(destination))
            {
                destination = Path.Combine(archiveRoot, $"{Path.GetFileNameWithoutExtension(file.Name)}-{request.RequestedAtUtc:yyyyMMddHHmmss}.jsonl");
            }

            var beforeHash = Sha256(file.FullName);
            File.Move(file.FullName, destination);
            var afterHash = Sha256(destination);
            moved.Add(new
            {
                source = file.FullName,
                destination,
                sha256 = beforeHash,
                afterSha256 = afterHash
            });
        }

        var artifactRoot = Path.Combine(request.ArtifactRoot, $"{request.RequestedAtUtc:yyyyMMdd-HHmmss}-audit-ledger-cleanup");
        Directory.CreateDirectory(artifactRoot);
        var summaryPath = Path.Combine(artifactRoot, "cleanup-summary.json");
        File.WriteAllText(summaryPath, JsonSerializer.Serialize(new
        {
            schemaVersion = "1",
            auditRoot = _auditRoot,
            archiveRoot,
            movedCount = moved.Count,
            moved
        }, JsonDefaults.Options));

        var summary = $"Audit ledger cleanup moved {moved.Count} old archived JSONL file(s); delete=false edit=false.";
        _ledger.Record(new EvidencePacket(
            Guid.NewGuid(),
            PacketKind,
            null,
            request.RequestedAtUtc,
            DidUseNetwork: false,
            DidUseHostedAi: false,
            DidUseLocalModel: false,
            DidMutate: moved.Count > 0,
            ArtifactPath: summaryPath,
            Summary: summary,
            Status: "Completed"));
        return new AutonomousScopeRunResult(Descriptor.ScopeId, true, moved.Count > 0, request.ExistingTaskCards, summary);
    }

    internal static bool IsArchivedLedgerFile(FileInfo file)
    {
        return file.Extension.Equals(".jsonl", StringComparison.OrdinalIgnoreCase) &&
               file.Name.Contains("archived", StringComparison.OrdinalIgnoreCase);
    }

    private static string Sha256(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }
}
