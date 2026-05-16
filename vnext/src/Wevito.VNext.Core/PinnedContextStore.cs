using System.Globalization;
using Microsoft.Data.Sqlite;
using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed record PinnedContextRow(
    Guid PinId,
    string Content,
    DateTimeOffset PinnedAtUtc,
    DateTimeOffset? UnpinnedAtUtc,
    int Tokens);

public sealed class PinnedContextStore
{
    public const string PinnedMessageAddedPacketKind = "pinned_message_added";
    public const string PinnedMessageRemovedPacketKind = "pinned_message_removed";
    public const int DefaultTokenBudget = 4_000;

    private readonly string _databasePath;
    private readonly int _tokenBudget;
    private readonly AuditLedgerService? _auditLedgerService;
    private readonly KillSwitchService? _killSwitchService;

    public PinnedContextStore(string? databasePath = null, int tokenBudget = DefaultTokenBudget, AuditLedgerService? auditLedgerService = null, KillSwitchService? killSwitchService = null)
    {
        _databasePath = Path.GetFullPath(databasePath ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Wevito", "pinned-context.sqlite"));
        _tokenBudget = Math.Max(1, tokenBudget);
        _auditLedgerService = auditLedgerService;
        _killSwitchService = killSwitchService;
    }

    public string DatabasePath => _databasePath;

    public PinnedContextRow Pin(string content, DateTimeOffset? nowUtc = null)
    {
        if (_killSwitchService?.IsActive() == true)
        {
            throw new InvalidOperationException("kill_switch=true");
        }

        var timestamp = nowUtc ?? DateTimeOffset.UtcNow;
        var row = new PinnedContextRow(Guid.NewGuid(), content ?? "", timestamp, null, EstimateTokens(content));
        Directory.CreateDirectory(Path.GetDirectoryName(_databasePath) ?? ".");
        using var connection = OpenConnection();
        Initialize(connection);
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO pinned_context(pin_id, content, pinned_at_utc, unpinned_at_utc, tokens)
            VALUES($pin_id, $content, $pinned_at_utc, '', $tokens);
            """;
        command.Parameters.AddWithValue("$pin_id", row.PinId.ToString());
        command.Parameters.AddWithValue("$content", row.Content);
        command.Parameters.AddWithValue("$pinned_at_utc", row.PinnedAtUtc.ToString("O", CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$tokens", row.Tokens);
        command.ExecuteNonQuery();
        EnforceBudget(connection, timestamp);
        Record(PinnedMessageAddedPacketKind, timestamp, $"Pinned context row {row.PinId}.");
        return row;
    }

    public void Unpin(Guid pinId, DateTimeOffset? nowUtc = null)
    {
        if (_killSwitchService?.IsActive() == true)
        {
            throw new InvalidOperationException("kill_switch=true");
        }

        var timestamp = nowUtc ?? DateTimeOffset.UtcNow;
        using var connection = OpenConnection();
        Initialize(connection);
        SoftUnpin(connection, pinId, timestamp);
        Record(PinnedMessageRemovedPacketKind, timestamp, $"Soft-unpinned context row {pinId}.");
    }

    public IReadOnlyList<PinnedContextRow> GetActivePins()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_databasePath) ?? ".");
        using var connection = OpenConnection();
        Initialize(connection);
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT pin_id, content, pinned_at_utc, unpinned_at_utc, tokens
            FROM pinned_context
            WHERE unpinned_at_utc=''
            ORDER BY pinned_at_utc ASC, id ASC;
            """;
        return ReadRows(command);
    }

    private SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = _databasePath, Mode = SqliteOpenMode.ReadWriteCreate }.ToString());
        connection.Open();
        return connection;
    }

    private static void Initialize(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS pinned_context(
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                pin_id TEXT NOT NULL UNIQUE,
                content TEXT NOT NULL,
                pinned_at_utc TEXT NOT NULL,
                unpinned_at_utc TEXT NOT NULL DEFAULT '',
                tokens INTEGER NOT NULL DEFAULT 0
            );
            """;
        command.ExecuteNonQuery();
    }

    private void EnforceBudget(SqliteConnection connection, DateTimeOffset timestamp)
    {
        while (GetActiveTokenTotal(connection) > _tokenBudget)
        {
            using var oldest = connection.CreateCommand();
            oldest.CommandText = "SELECT pin_id FROM pinned_context WHERE unpinned_at_utc='' ORDER BY pinned_at_utc ASC, id ASC LIMIT 1;";
            var raw = Convert.ToString(oldest.ExecuteScalar(), CultureInfo.InvariantCulture);
            if (!Guid.TryParse(raw, out var pinId))
            {
                break;
            }

            SoftUnpin(connection, pinId, timestamp);
        }
    }

    private static int GetActiveTokenTotal(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COALESCE(SUM(tokens), 0) FROM pinned_context WHERE unpinned_at_utc='';";
        return Convert.ToInt32(command.ExecuteScalar() ?? 0, CultureInfo.InvariantCulture);
    }

    private static void SoftUnpin(SqliteConnection connection, Guid pinId, DateTimeOffset timestamp)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "UPDATE pinned_context SET unpinned_at_utc=$unpinned_at_utc WHERE pin_id=$pin_id AND unpinned_at_utc='';";
        command.Parameters.AddWithValue("$pin_id", pinId.ToString());
        command.Parameters.AddWithValue("$unpinned_at_utc", timestamp.ToString("O", CultureInfo.InvariantCulture));
        command.ExecuteNonQuery();
    }

    private static IReadOnlyList<PinnedContextRow> ReadRows(SqliteCommand command)
    {
        var rows = new List<PinnedContextRow>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var unpinned = Convert.ToString(reader["unpinned_at_utc"], CultureInfo.InvariantCulture) ?? "";
            rows.Add(new PinnedContextRow(
                Guid.Parse(Convert.ToString(reader["pin_id"], CultureInfo.InvariantCulture) ?? Guid.Empty.ToString()),
                Convert.ToString(reader["content"], CultureInfo.InvariantCulture) ?? "",
                DateTimeOffset.Parse(Convert.ToString(reader["pinned_at_utc"], CultureInfo.InvariantCulture) ?? DateTimeOffset.MinValue.ToString("O"), CultureInfo.InvariantCulture),
                string.IsNullOrWhiteSpace(unpinned) ? null : DateTimeOffset.Parse(unpinned, CultureInfo.InvariantCulture),
                Convert.ToInt32(reader["tokens"], CultureInfo.InvariantCulture)));
        }

        return rows;
    }

    private void Record(string kind, DateTimeOffset timestamp, string summary)
    {
        _auditLedgerService?.Record(new EvidencePacket(Guid.NewGuid(), kind, null, timestamp, false, false, false, true, _databasePath, summary, "Completed"));
    }

    private static int EstimateTokens(string? value)
    {
        return Math.Max(1, (value ?? "").Split([' ', '\r', '\n', '\t'], StringSplitOptions.RemoveEmptyEntries).Length);
    }
}
