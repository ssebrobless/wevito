using System.Globalization;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Wevito.VNext.Core.Audit;

namespace Wevito.VNext.Core.SelfImprovement.Maturity;

public sealed class MaturityScoreboardService
{
    private readonly string _databasePath;
    private readonly AuditLedgerService? _auditLedgerService;
    private readonly KillSwitchService? _killSwitchService;
    private readonly Action<string>? _commandObserver;

    public MaturityScoreboardService(
        string databasePath,
        AuditLedgerService? auditLedgerService = null,
        KillSwitchService? killSwitchService = null,
        Action<string>? commandObserver = null)
    {
        _databasePath = Path.GetFullPath(databasePath);
        _auditLedgerService = auditLedgerService;
        _killSwitchService = killSwitchService;
        _commandObserver = commandObserver;
    }

    public MaturityClock BuildScoreboard(DateTimeOffset nowUtc)
    {
        if (_killSwitchService?.IsActive() == true)
        {
            return new MaturityClock(0, 0, 0, 0, 0, [], IsBlocked: true, "kill_switch=true");
        }

        if (!File.Exists(_databasePath))
        {
            return new MaturityClock(0, 0, 0, 0, 0, [], IsBlocked: false, "audit ledger not found");
        }

        var rows = ReadRows();
        var dryRuns = rows.Count(row => row.PacketKind.Equals(SelfImprovementPacketKinds.DryRunCompleted, StringComparison.OrdinalIgnoreCase));
        var applyCompleted = rows.Count(row => row.PacketKind.Equals(SelfImprovementPacketKinds.ApplyCompleted, StringComparison.OrdinalIgnoreCase));
        var rollbackVerified = rows.Count(row => row.PacketKind.Equals(SelfImprovementPacketKinds.RollbackVerified, StringComparison.OrdinalIgnoreCase));
        var evalPassed = rows.Count(IsPassedEvalCompleted);
        var resetReasons = DetectResetReasons(rows);
        RecordNewResetPackets(resetReasons, rows, nowUtc);

        return new MaturityClock(
            ProgressIncrements: dryRuns + applyCompleted + rollbackVerified + evalPassed,
            DryRunCompletedCount: dryRuns,
            ApplyCompletedCount: applyCompleted,
            RollbackVerifiedCount: rollbackVerified,
            EvalCompletedPassedCount: evalPassed,
            ResetReasons: resetReasons,
            IsBlocked: false,
            StatusMessage: resetReasons.Count == 0 ? "No reset conditions detected." : $"{resetReasons.Count} reset condition(s) detected.");
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

    private static bool IsPassedEvalCompleted(AuditLedgerRow row)
    {
        if (!row.PacketKind.Equals(SelfImprovementPacketKinds.EvalCompleted, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var text = $"{row.Summary} {row.Status} {row.Error}";
        return text.Contains("Passed", StringComparison.OrdinalIgnoreCase)
            && !text.Contains("NotApplicable", StringComparison.OrdinalIgnoreCase)
            && !text.Contains("Failed", StringComparison.OrdinalIgnoreCase);
    }

    private static IReadOnlyList<MaturityClockResetReason> DetectResetReasons(IReadOnlyList<AuditLedgerRow> rows)
    {
        var reasons = new HashSet<MaturityClockResetReason>();
        foreach (var row in rows)
        {
            var text = $"{row.PacketKind} {row.Summary} {row.Status} {row.Error}";
            if (text.Contains("invariant", StringComparison.OrdinalIgnoreCase))
            {
                reasons.Add(MaturityClockResetReason.InvariantViolation);
            }

            if (row.DidUseHostedAi)
            {
                reasons.Add(MaturityClockResetReason.SilentHostedAiDetected);
            }

            if (row.DidUseNetwork)
            {
                reasons.Add(MaturityClockResetReason.SilentNetworkDetected);
            }

            if (row.DidMutate && !IsExpectedSelfImprovementMutation(row.PacketKind))
            {
                reasons.Add(MaturityClockResetReason.SilentMutationDetected);
            }

            if (row.PacketKind.Equals(SelfImprovementPacketKinds.RollbackVerified, StringComparison.OrdinalIgnoreCase) &&
                (!row.Status.Equals("Completed", StringComparison.OrdinalIgnoreCase) || !string.IsNullOrWhiteSpace(row.Error)))
            {
                reasons.Add(MaturityClockResetReason.FailedRollback);
            }

            if (row.PacketKind.Equals(SelfImprovementPacketKinds.ApplyRefused, StringComparison.OrdinalIgnoreCase) &&
                text.Contains("user_rejection", StringComparison.OrdinalIgnoreCase))
            {
                reasons.Add(MaturityClockResetReason.UserRejectedProposal);
            }
        }

        return reasons
            .OrderBy(reason => reason.ToString(), StringComparer.Ordinal)
            .ToList();
    }

    private static bool IsExpectedSelfImprovementMutation(string packetKind)
    {
        return packetKind.Equals(SelfImprovementPacketKinds.ApplyCompleted, StringComparison.OrdinalIgnoreCase)
            || packetKind.Equals(SelfImprovementPacketKinds.RollbackVerified, StringComparison.OrdinalIgnoreCase)
            || packetKind.Equals(SelfImprovementPacketKinds.MaturityClockReset, StringComparison.OrdinalIgnoreCase);
    }

    private void RecordNewResetPackets(IReadOnlyList<MaturityClockResetReason> resetReasons, IReadOnlyList<AuditLedgerRow> rows, DateTimeOffset nowUtc)
    {
        if (_auditLedgerService is null || resetReasons.Count == 0)
        {
            return;
        }

        var alreadyRecorded = rows
            .Where(row => row.PacketKind.Equals(SelfImprovementPacketKinds.MaturityClockReset, StringComparison.OrdinalIgnoreCase))
            .SelectMany(row => resetReasons.Where(reason => RowMentionsReason(row, reason)))
            .ToHashSet();

        foreach (var reason in resetReasons.Where(reason => !alreadyRecorded.Contains(reason)))
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
                Summary: JsonSerializer.Serialize(new { reason = reason.ToString() }),
                Status: "Completed"));
        }
    }

    private static bool RowMentionsReason(AuditLedgerRow row, MaturityClockResetReason reason)
    {
        var text = $"{row.Summary} {row.Error}";
        return text.Contains(reason.ToString(), StringComparison.OrdinalIgnoreCase);
    }
}
