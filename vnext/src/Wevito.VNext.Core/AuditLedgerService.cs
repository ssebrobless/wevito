using System.Globalization;
using Microsoft.Data.Sqlite;

namespace Wevito.VNext.Core;

public sealed class AuditLedgerService
{
    public const string DefaultRelativePath = "Wevito/audit/ledger.sqlite";
    public const string TrainPlanPacketKind = "train_plan";
    public const string TuningApplyPacketKind = "tuning_apply";
    public const string TuningRollbackPacketKind = "tuning_rollback";

    private readonly string _databasePath;

    public AuditLedgerService(string? databasePath = null)
    {
        _databasePath = Path.GetFullPath(databasePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Wevito",
            "audit",
            "ledger.sqlite"));
    }

    public string DatabasePath => _databasePath;

    public long Record(EvidencePacket packet)
    {
        if (packet.PacketId == Guid.Empty)
        {
            throw new ArgumentException("Packet id is required.", nameof(packet));
        }

        if (string.IsNullOrWhiteSpace(packet.PacketKind))
        {
            throw new ArgumentException("Packet kind is required.", nameof(packet));
        }

        Directory.CreateDirectory(Path.GetDirectoryName(_databasePath) ?? ".");
        using var connection = OpenConnection();
        Initialize(connection);
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO audit_ledger(
                packet_id,
                packet_kind,
                task_card_id,
                created_at_utc,
                did_use_network,
                did_use_hosted_ai,
                did_use_local_model,
                did_mutate,
                artifact_path,
                summary,
                status,
                error)
            VALUES (
                $packet_id,
                $packet_kind,
                $task_card_id,
                $created_at_utc,
                $did_use_network,
                $did_use_hosted_ai,
                $did_use_local_model,
                $did_mutate,
                $artifact_path,
                $summary,
                $status,
                $error);
            SELECT last_insert_rowid();
            """;
        command.Parameters.AddWithValue("$packet_id", packet.PacketId.ToString());
        command.Parameters.AddWithValue("$packet_kind", packet.PacketKind);
        command.Parameters.AddWithValue("$task_card_id", packet.TaskCardId?.ToString() ?? "");
        command.Parameters.AddWithValue("$created_at_utc", packet.CreatedAtUtc.ToString("O", CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$did_use_network", packet.DidUseNetwork ? 1 : 0);
        command.Parameters.AddWithValue("$did_use_hosted_ai", packet.DidUseHostedAi ? 1 : 0);
        command.Parameters.AddWithValue("$did_use_local_model", packet.DidUseLocalModel ? 1 : 0);
        command.Parameters.AddWithValue("$did_mutate", packet.DidMutate ? 1 : 0);
        command.Parameters.AddWithValue("$artifact_path", packet.ArtifactPath ?? "");
        command.Parameters.AddWithValue("$summary", packet.Summary ?? "");
        command.Parameters.AddWithValue("$status", packet.Status ?? "");
        command.Parameters.AddWithValue("$error", packet.Error ?? "");
        return Convert.ToInt64(command.ExecuteScalar(), CultureInfo.InvariantCulture);
    }

    public IReadOnlyList<AuditLedgerRow> Snapshot(DateTimeOffset sinceUtc, DateTimeOffset untilUtc)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_databasePath) ?? ".");
        using var connection = OpenConnection();
        Initialize(connection);
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id, packet_id, packet_kind, task_card_id, created_at_utc,
                   did_use_network, did_use_hosted_ai, did_use_local_model,
                   did_mutate, artifact_path, summary, status, error
            FROM audit_ledger
            WHERE created_at_utc >= $since AND created_at_utc <= $until
            ORDER BY created_at_utc DESC, id DESC;
            """;
        command.Parameters.AddWithValue("$since", sinceUtc.ToString("O", CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$until", untilUtc.ToString("O", CultureInfo.InvariantCulture));
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

    private SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = _databasePath,
            Mode = SqliteOpenMode.ReadWriteCreate
        }.ToString());
        connection.Open();
        return connection;
    }

    private static void Initialize(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS audit_ledger(
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                packet_id TEXT NOT NULL UNIQUE,
                packet_kind TEXT NOT NULL,
                task_card_id TEXT NOT NULL DEFAULT '',
                created_at_utc TEXT NOT NULL,
                did_use_network INTEGER NOT NULL CHECK(did_use_network IN (0, 1)),
                did_use_hosted_ai INTEGER NOT NULL CHECK(did_use_hosted_ai IN (0, 1)),
                did_use_local_model INTEGER NOT NULL CHECK(did_use_local_model IN (0, 1)),
                did_mutate INTEGER NOT NULL CHECK(did_mutate IN (0, 1)),
                artifact_path TEXT NOT NULL DEFAULT '',
                summary TEXT NOT NULL DEFAULT '',
                status TEXT NOT NULL DEFAULT '',
                error TEXT NOT NULL DEFAULT ''
            );
            CREATE TRIGGER IF NOT EXISTS audit_ledger_no_update
            BEFORE UPDATE ON audit_ledger
            BEGIN
                SELECT RAISE(ABORT, 'audit ledger is append-only');
            END;
            CREATE TRIGGER IF NOT EXISTS audit_ledger_no_delete
            BEFORE DELETE ON audit_ledger
            BEGIN
                SELECT RAISE(ABORT, 'audit ledger is append-only');
            END;
            """;
        command.ExecuteNonQuery();
    }
}
