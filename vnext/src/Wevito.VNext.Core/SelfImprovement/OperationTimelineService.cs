using System.Globalization;
using Microsoft.Data.Sqlite;
using Wevito.VNext.Core.Audit;

namespace Wevito.VNext.Core.SelfImprovement;

public sealed class OperationTimelineService
{
    private readonly string _databasePath;
    private readonly PlainLanguageExplainer _explainer;
    private readonly KillSwitchService? _killSwitch;
    private readonly Action<string>? _commandObserver;

    public OperationTimelineService(
        string databasePath,
        PlainLanguageExplainer explainer,
        KillSwitchService? killSwitch = null,
        Action<string>? commandObserver = null)
    {
        _databasePath = Path.GetFullPath(databasePath);
        _explainer = explainer;
        _killSwitch = killSwitch;
        _commandObserver = commandObserver;
    }

    public IReadOnlyList<OperationTimelineRow> BuildFor(string operationId)
    {
        if (_killSwitch?.IsActive() == true)
        {
            return [new OperationTimelineRow(DateTimeOffset.UtcNow, "blocked", "Blocked: kill_switch=true", "Blocked", false, false)];
        }

        if (string.IsNullOrWhiteSpace(operationId) || !File.Exists(_databasePath))
        {
            return [];
        }

        var rows = ReadRows(operationId.Trim());
        var knownKinds = PlainLanguageExplainer.KnownPacketKinds.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return rows
            .OrderBy(row => row.CreatedAtUtc)
            .ThenBy(row => row.Id)
            .Select(row => new OperationTimelineRow(
                row.CreatedAtUtc,
                row.PacketKind,
                knownKinds.Contains(row.PacketKind) ? _explainer.Explain(row) : "(unrecognized packet kind)",
                string.IsNullOrWhiteSpace(row.Status) ? "Unknown" : row.Status,
                row.DidMutate,
                row.DidUseLocalModel))
            .ToList();
    }

    private IReadOnlyList<AuditLedgerRow> ReadRows(string operationId)
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
            WHERE packet_kind LIKE 'self_improvement_%'
              AND (
                  summary LIKE $operation_pattern
                  OR task_card_id IN (
                      SELECT task_card_id
                      FROM audit_ledger
                      WHERE packet_kind LIKE 'self_improvement_%'
                        AND summary LIKE $operation_pattern
                        AND task_card_id <> ''
                  )
              )
            ORDER BY created_at_utc ASC, id ASC;
            """;
        _commandObserver?.Invoke(command.CommandText);
        command.Parameters.AddWithValue("$operation_pattern", $"%{EscapeLike(operationId)}%");

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

    private static string EscapeLike(string value)
    {
        return value.Replace("[", "[[]", StringComparison.Ordinal)
            .Replace("%", "[%]", StringComparison.Ordinal)
            .Replace("_", "[_]", StringComparison.Ordinal);
    }
}
