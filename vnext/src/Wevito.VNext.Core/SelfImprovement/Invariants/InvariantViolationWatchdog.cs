using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;
using Wevito.VNext.Core.Audit;
using Wevito.VNext.Core.SelfImprovement.Maturity;

namespace Wevito.VNext.Core.SelfImprovement.Invariants;

public sealed partial class InvariantViolationWatchdog
{
    public const string EnabledSetting = "invariant_violation_watchdog_enabled";

    private static readonly InvariantCheck SilentMutationInReviewOnlyScope = new(
        "rule_silent_mutation_in_review_only_scope",
        "Review-only sprite repair proposal rows must never report mutation.",
        MaturityClockResetReason.SilentMutationDetected);

    private static readonly InvariantCheck SilentNetwork = new(
        "rule_silent_network",
        "Self-improvement rows must not report network use unless an approved network surface exists.",
        MaturityClockResetReason.SilentNetworkDetected);

    private static readonly InvariantCheck SilentHostedAi = new(
        "rule_silent_hosted_ai",
        "Self-improvement rows must never report hosted AI use.",
        MaturityClockResetReason.SilentHostedAiDetected);

    private static readonly InvariantCheck ApplyCompletedWithoutAwaitingApproval = new(
        "rule_apply_completed_without_awaiting_approval",
        "Apply-completed rows require a matching prior awaiting-approval row.",
        MaturityClockResetReason.InvariantViolation);

    private static readonly InvariantCheck AwaitingApprovalWithoutPrecedingChain = new(
        "rule_awaiting_approval_without_preceding_chain",
        "Awaiting-approval rows require the proposal, constitutional review, dry-run, and eval packet chain.",
        MaturityClockResetReason.InvariantViolation);

    private static readonly InvariantCheck DuplicateAwaitingApproval = new(
        "rule_duplicate_awaiting_approval",
        "Only one awaiting-approval row is allowed per operation id.",
        MaturityClockResetReason.InvariantViolation);

    private static readonly IReadOnlyList<InvariantCheck> Checks =
    [
        SilentMutationInReviewOnlyScope,
        SilentNetwork,
        SilentHostedAi,
        ApplyCompletedWithoutAwaitingApproval,
        AwaitingApprovalWithoutPrecedingChain,
        DuplicateAwaitingApproval
    ];

    private readonly string _databasePath;
    private readonly AuditLedgerService? _auditLedgerService;
    private readonly KillSwitchService? _killSwitchService;
    private readonly Func<IReadOnlyDictionary<string, string>>? _settingsProvider;
    private readonly Action<string>? _commandObserver;

    public InvariantViolationWatchdog(
        string databasePath,
        AuditLedgerService? auditLedgerService = null,
        KillSwitchService? killSwitchService = null,
        Func<IReadOnlyDictionary<string, string>>? settingsProvider = null,
        Action<string>? commandObserver = null)
    {
        _databasePath = Path.GetFullPath(databasePath);
        _auditLedgerService = auditLedgerService;
        _killSwitchService = killSwitchService;
        _settingsProvider = settingsProvider;
        _commandObserver = commandObserver;
    }

    public IReadOnlyList<InvariantCheckResult> Scan(DateTimeOffset nowUtc)
    {
        if (_killSwitchService?.IsActive() == true || !IsEnabled(_settingsProvider?.Invoke()))
        {
            return [];
        }

        if (!File.Exists(_databasePath))
        {
            return Checks
                .Select(check => new InvariantCheckResult(check, false, "audit ledger not found"))
                .ToArray();
        }

        var rows = ReadRows();
        var detectionRows = rows
            .Where(row => !row.PacketKind.Equals(SelfImprovementPacketKinds.MaturityClockReset, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        var results = BuildResults(detectionRows);
        RecordNewResetPackets(results, rows, nowUtc);
        return results;
    }

    private static bool IsEnabled(IReadOnlyDictionary<string, string>? settings)
    {
        return settings is not null &&
               settings.TryGetValue(EnabledSetting, out var raw) &&
               bool.TryParse(raw, out var enabled) &&
               enabled;
    }

    private IReadOnlyList<AuditLedgerRow> ReadRows()
    {
        using var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = _databasePath,
            Mode = SqliteOpenMode.ReadOnly
        }.ToString());
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id, packet_id, packet_kind, task_card_id, created_at_utc,
                   did_use_network, did_use_hosted_ai, did_use_local_model,
                   did_mutate, artifact_path, summary, status, error
            FROM audit_ledger
            ORDER BY created_at_utc DESC, id DESC;
            """;
        _commandObserver?.Invoke(command.CommandText);

        var rows = new List<AuditLedgerRow>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var taskCard = Convert.ToString(reader["task_card_id"], CultureInfo.InvariantCulture);
            rows.Add(new AuditLedgerRow(
                Convert.ToInt64(reader["id"], CultureInfo.InvariantCulture),
                Guid.Parse(Convert.ToString(reader["packet_id"], CultureInfo.InvariantCulture) ?? Guid.Empty.ToString()),
                Convert.ToString(reader["packet_kind"], CultureInfo.InvariantCulture) ?? "",
                Guid.TryParse(taskCard, out var taskCardId) ? taskCardId : null,
                DateTimeOffset.Parse(Convert.ToString(reader["created_at_utc"], CultureInfo.InvariantCulture) ?? DateTimeOffset.MinValue.ToString("O"), CultureInfo.InvariantCulture),
                Convert.ToInt32(reader["did_use_network"], CultureInfo.InvariantCulture) != 0,
                Convert.ToInt32(reader["did_use_hosted_ai"], CultureInfo.InvariantCulture) != 0,
                Convert.ToInt32(reader["did_use_local_model"], CultureInfo.InvariantCulture) != 0,
                Convert.ToInt32(reader["did_mutate"], CultureInfo.InvariantCulture) != 0,
                Convert.ToString(reader["artifact_path"], CultureInfo.InvariantCulture) ?? "",
                Convert.ToString(reader["summary"], CultureInfo.InvariantCulture) ?? "",
                Convert.ToString(reader["status"], CultureInfo.InvariantCulture) ?? "",
                Convert.ToString(reader["error"], CultureInfo.InvariantCulture) ?? ""));
        }

        return rows;
    }

    private static IReadOnlyList<InvariantCheckResult> BuildResults(IReadOnlyList<AuditLedgerRow> rows)
    {
        return
        [
            BuildResult(
                SilentMutationInReviewOnlyScope,
                rows.Where(row => row.DidMutate && IsReviewOnlySpriteRepairScopeRow(row)).ToArray(),
                row => $"row_id={row.Id} packet_kind={row.PacketKind}"),
            BuildResult(
                SilentNetwork,
                rows.Where(row => IsSelfImprovementRow(row) && row.DidUseNetwork).ToArray(),
                row => $"row_id={row.Id} packet_kind={row.PacketKind}"),
            BuildResult(
                SilentHostedAi,
                rows.Where(row => IsSelfImprovementRow(row) && row.DidUseHostedAi).ToArray(),
                row => $"row_id={row.Id} packet_kind={row.PacketKind}"),
            BuildApplyCompletedWithoutAwaitingApproval(rows),
            BuildAwaitingApprovalWithoutPrecedingChain(rows),
            BuildDuplicateAwaitingApproval(rows)
        ];
    }

    private static InvariantCheckResult BuildApplyCompletedWithoutAwaitingApproval(IReadOnlyList<AuditLedgerRow> rows)
    {
        var awaitingOperationIds = rows
            .Where(row => row.PacketKind.Equals(SelfImprovementPacketKinds.ApplyAwaitingApproval, StringComparison.OrdinalIgnoreCase))
            .Select(ExtractOperationId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var failures = rows
            .Where(row => row.PacketKind.Equals(SelfImprovementPacketKinds.ApplyCompleted, StringComparison.OrdinalIgnoreCase))
            .Select(row => new { Row = row, OperationId = ExtractOperationId(row) })
            .Where(item => string.IsNullOrWhiteSpace(item.OperationId) || !awaitingOperationIds.Contains(item.OperationId))
            .Select(item => $"row_id={item.Row.Id} operation_id={item.OperationId}")
            .ToArray();
        return BuildResult(ApplyCompletedWithoutAwaitingApproval, failures);
    }

    private static InvariantCheckResult BuildAwaitingApprovalWithoutPrecedingChain(IReadOnlyList<AuditLedgerRow> rows)
    {
        var failures = new List<string>();
        foreach (var row in rows.Where(row => row.PacketKind.Equals(SelfImprovementPacketKinds.ApplyAwaitingApproval, StringComparison.OrdinalIgnoreCase)))
        {
            var operationId = ExtractOperationId(row);
            if (!TryResolveProposalTaskCardId(operationId, out var proposalTaskCardId))
            {
                failures.Add($"row_id={row.Id} operation_id={operationId} missing_proposal_task_card_id");
                continue;
            }

            var proposalRows = rows.Where(candidate => candidate.TaskCardId == proposalTaskCardId).ToArray();
            var missing = new[]
                {
                    SelfImprovementPacketKinds.ProposalDrafted,
                    SelfImprovementPacketKinds.ConstitutionalReviewed,
                    SelfImprovementPacketKinds.DryRunCompleted,
                    SelfImprovementPacketKinds.EvalCompleted
                }
                .Where(kind => !proposalRows.Any(candidate => candidate.PacketKind.Equals(kind, StringComparison.OrdinalIgnoreCase)))
                .ToArray();
            if (missing.Length > 0)
            {
                failures.Add($"row_id={row.Id} operation_id={operationId} missing={string.Join(",", missing)}");
            }
        }

        return BuildResult(AwaitingApprovalWithoutPrecedingChain, failures);
    }

    private static InvariantCheckResult BuildDuplicateAwaitingApproval(IReadOnlyList<AuditLedgerRow> rows)
    {
        var failures = rows
            .Where(row => row.PacketKind.Equals(SelfImprovementPacketKinds.ApplyAwaitingApproval, StringComparison.OrdinalIgnoreCase))
            .Select(row => new { Row = row, OperationId = ExtractOperationId(row) })
            .Where(item => !string.IsNullOrWhiteSpace(item.OperationId))
            .GroupBy(item => item.OperationId, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => $"operation_id={group.Key} count={group.Count()}")
            .ToArray();
        return BuildResult(DuplicateAwaitingApproval, failures);
    }

    private static InvariantCheckResult BuildResult(
        InvariantCheck check,
        IReadOnlyList<AuditLedgerRow> triggeringRows,
        Func<AuditLedgerRow, string> describe)
    {
        return BuildResult(check, triggeringRows.Select(describe).ToArray());
    }

    private static InvariantCheckResult BuildResult(InvariantCheck check, IReadOnlyList<string> evidenceItems)
    {
        return new InvariantCheckResult(
            check,
            evidenceItems.Count > 0,
            evidenceItems.Count == 0
                ? "no violation detected"
                : string.Join("; ", evidenceItems));
    }

    private void RecordNewResetPackets(IReadOnlyList<InvariantCheckResult> results, IReadOnlyList<AuditLedgerRow> rows, DateTimeOffset nowUtc)
    {
        if (_auditLedgerService is null)
        {
            return;
        }

        var alreadyRecorded = rows
            .Where(row => row.PacketKind.Equals(SelfImprovementPacketKinds.MaturityClockReset, StringComparison.OrdinalIgnoreCase))
            .SelectMany(row => results
                .Where(result => result.Triggered && RowMentionsReason(row, result.Check.Reason))
                .Select(result => result.Check.Reason))
            .ToHashSet();

        foreach (var result in results.Where(result => result.Triggered && !alreadyRecorded.Contains(result.Check.Reason)))
        {
            _auditLedgerService.Record(new EvidencePacket(
                Guid.NewGuid(),
                SelfImprovementPacketKinds.MaturityClockReset,
                TaskCardId: null,
                nowUtc,
                DidUseNetwork: false,
                DidUseHostedAi: false,
                DidUseLocalModel: false,
                DidMutate: false,
                ArtifactPath: "",
                Summary: JsonSerializer.Serialize(new { reason = result.Check.Reason.ToString(), source = "invariant_violation_watchdog", check_id = result.Check.Id }),
                Status: "Completed"));
            alreadyRecorded.Add(result.Check.Reason);
        }
    }

    private static bool RowMentionsReason(AuditLedgerRow row, MaturityClockResetReason reason)
    {
        var text = $"{row.Summary} {row.Error}";
        return text.Contains(reason.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsReviewOnlySpriteRepairScopeRow(AuditLedgerRow row)
    {
        var text = $"{row.PacketKind} {row.ArtifactPath} {row.Summary} {row.Error}";
        return text.Contains(AutonomousScopeService.SpriteRepairBatchProposalScopeId, StringComparison.OrdinalIgnoreCase) ||
               row.PacketKind.Equals(SelfImprovementPacketKinds.ProposalDrafted, StringComparison.OrdinalIgnoreCase) ||
               row.PacketKind.Equals(SelfImprovementPacketKinds.ConstitutionalReviewed, StringComparison.OrdinalIgnoreCase) ||
               row.PacketKind.Equals(SelfImprovementPacketKinds.DryRunCompleted, StringComparison.OrdinalIgnoreCase) ||
               row.PacketKind.Equals(SelfImprovementPacketKinds.EvalCompleted, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSelfImprovementRow(AuditLedgerRow row)
    {
        return row.PacketKind.StartsWith("self_improvement_", StringComparison.OrdinalIgnoreCase);
    }

    private static string ExtractOperationId(AuditLedgerRow row)
    {
        var text = $"{row.Summary} {row.Error} {row.ArtifactPath}";
        return OperationIdPattern().Match(text) is { Success: true } match ? match.Value : "";
    }

    private static bool TryResolveProposalTaskCardId(string operationId, out Guid proposalTaskCardId)
    {
        proposalTaskCardId = Guid.Empty;
        const string prefix = "apply-";
        return operationId.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) &&
               Guid.TryParseExact(operationId[prefix.Length..], "N", out proposalTaskCardId);
    }

    [GeneratedRegex("apply-[0-9a-fA-F]{32}", RegexOptions.CultureInvariant)]
    private static partial Regex OperationIdPattern();
}
