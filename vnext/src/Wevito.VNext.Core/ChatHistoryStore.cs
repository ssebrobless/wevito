using System.Globalization;
using Microsoft.Data.Sqlite;

namespace Wevito.VNext.Core;

public sealed class ChatHistoryStore
{
    public const string DefaultRelativePath = "Wevito/chat-history.sqlite";

    private readonly string _databasePath;
    private readonly KillSwitchService? _killSwitchService;

    public ChatHistoryStore(string? databasePath = null, KillSwitchService? killSwitchService = null)
    {
        _databasePath = Path.GetFullPath(databasePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Wevito",
            "chat-history.sqlite"));
        _killSwitchService = killSwitchService;
    }

    public string DatabasePath => _databasePath;

    public Guid CreateSession(string? title = null, DateTimeOffset? nowUtc = null)
    {
        if (_killSwitchService?.IsActive() == true)
        {
            throw new InvalidOperationException("kill_switch=true");
        }

        var timestamp = nowUtc ?? DateTimeOffset.UtcNow;
        var sessionId = Guid.NewGuid();
        Directory.CreateDirectory(Path.GetDirectoryName(_databasePath) ?? ".");
        using var connection = OpenConnection();
        Initialize(connection);
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO chat_sessions(session_id, title, created_at_utc, updated_at_utc, soft_deleted)
            VALUES($session_id, $title, $created_at_utc, $updated_at_utc, 0);
            """;
        command.Parameters.AddWithValue("$session_id", sessionId.ToString());
        command.Parameters.AddWithValue("$title", string.IsNullOrWhiteSpace(title) ? "New chat" : title);
        command.Parameters.AddWithValue("$created_at_utc", timestamp.ToString("O", CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$updated_at_utc", timestamp.ToString("O", CultureInfo.InvariantCulture));
        command.ExecuteNonQuery();
        return sessionId;
    }

    public void AppendTurn(ChatTurn turn)
    {
        if (_killSwitchService?.IsActive() == true)
        {
            throw new InvalidOperationException("kill_switch=true");
        }

        if (turn.SessionId == Guid.Empty || turn.TurnId == Guid.Empty)
        {
            throw new ArgumentException("Chat turn requires session_id and turn_id.");
        }

        Directory.CreateDirectory(Path.GetDirectoryName(_databasePath) ?? ".");
        using var connection = OpenConnection();
        Initialize(connection);
        EnsureSession(connection, turn.SessionId, turn.CreatedAtUtc);
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO chat_turns(
                session_id,
                turn_id,
                role,
                content,
                tool_call_json,
                tool_result_id,
                created_at_utc,
                model_id,
                tokens_used)
            VALUES(
                $session_id,
                $turn_id,
                $role,
                $content,
                $tool_call_json,
                $tool_result_id,
                $created_at_utc,
                $model_id,
                $tokens_used);
            UPDATE chat_sessions SET updated_at_utc=$created_at_utc WHERE session_id=$session_id;
            """;
        AddTurnParameters(command, turn);
        command.ExecuteNonQuery();
    }

    public void SetSessionTitle(Guid sessionId, string title, DateTimeOffset? nowUtc = null)
    {
        if (_killSwitchService?.IsActive() == true)
        {
            throw new InvalidOperationException("kill_switch=true");
        }

        var timestamp = nowUtc ?? DateTimeOffset.UtcNow;
        Directory.CreateDirectory(Path.GetDirectoryName(_databasePath) ?? ".");
        using var connection = OpenConnection();
        Initialize(connection);
        EnsureSession(connection, sessionId, timestamp);
        using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE chat_sessions
            SET title=$title, updated_at_utc=$updated_at_utc
            WHERE session_id=$session_id AND soft_deleted=0;
            """;
        command.Parameters.AddWithValue("$session_id", sessionId.ToString());
        command.Parameters.AddWithValue("$title", string.IsNullOrWhiteSpace(title) ? "New chat" : title.Trim());
        command.Parameters.AddWithValue("$updated_at_utc", timestamp.ToString("O", CultureInfo.InvariantCulture));
        command.ExecuteNonQuery();
    }

    public IReadOnlyList<ChatTurn> GetTurns(Guid sessionId, int limit = 50)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_databasePath) ?? ".");
        using var connection = OpenConnection();
        Initialize(connection);
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT session_id, turn_id, role, content, tool_call_json, tool_result_id, created_at_utc, model_id, tokens_used
            FROM chat_turns
            WHERE session_id=$session_id
            ORDER BY created_at_utc DESC, id DESC
            LIMIT $limit;
            """;
        command.Parameters.AddWithValue("$session_id", sessionId.ToString());
        command.Parameters.AddWithValue("$limit", Math.Max(1, limit));
        var turns = ReadTurns(command);
        turns.Reverse();
        return turns;
    }

    public IReadOnlyList<ChatSessionSummary> ListSessions(int limit = 20)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_databasePath) ?? ".");
        using var connection = OpenConnection();
        Initialize(connection);
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT s.session_id, s.title, s.created_at_utc, s.updated_at_utc, COUNT(t.id) AS turn_count
            FROM chat_sessions s
            LEFT JOIN chat_turns t ON t.session_id = s.session_id
            WHERE s.soft_deleted=0
            GROUP BY s.session_id, s.title, s.created_at_utc, s.updated_at_utc
            ORDER BY s.updated_at_utc DESC
            LIMIT $limit;
            """;
        command.Parameters.AddWithValue("$limit", Math.Max(1, limit));
        return ReadSessionSummaries(command);
    }

    public IReadOnlyList<ChatTurn> SearchTurns(string query, int limit = 50)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        Directory.CreateDirectory(Path.GetDirectoryName(_databasePath) ?? ".");
        using var connection = OpenConnection();
        Initialize(connection);
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT t.session_id, t.turn_id, t.role, t.content, t.tool_call_json, t.tool_result_id, t.created_at_utc, t.model_id, t.tokens_used
            FROM chat_turns_fts f
            JOIN chat_turns t ON t.id = f.rowid
            JOIN chat_sessions s ON s.session_id = t.session_id
            WHERE chat_turns_fts MATCH $query AND s.soft_deleted=0
            ORDER BY rank
            LIMIT $limit;
            """;
        command.Parameters.AddWithValue("$query", query.Trim());
        command.Parameters.AddWithValue("$limit", Math.Max(1, limit));
        return ReadTurns(command);
    }

    public void AssertAppendOnlyGuards()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_databasePath) ?? ".");
        using var connection = OpenConnection();
        Initialize(connection);
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT name FROM sqlite_master
            WHERE type='trigger' AND name IN ('chat_turns_no_update', 'chat_turns_no_delete');
            """;
        using var reader = command.ExecuteReader();
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while (reader.Read())
        {
            names.Add(Convert.ToString(reader["name"], CultureInfo.InvariantCulture) ?? "");
        }

        if (!names.Contains("chat_turns_no_update") || !names.Contains("chat_turns_no_delete"))
        {
            throw new InvalidOperationException("Chat turn append-only guards are missing.");
        }
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
            CREATE TABLE IF NOT EXISTS chat_sessions(
                session_id TEXT PRIMARY KEY,
                title TEXT NOT NULL,
                created_at_utc TEXT NOT NULL,
                updated_at_utc TEXT NOT NULL,
                soft_deleted INTEGER NOT NULL CHECK(soft_deleted IN (0, 1)) DEFAULT 0
            );
            CREATE TABLE IF NOT EXISTS chat_turns(
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                session_id TEXT NOT NULL,
                turn_id TEXT NOT NULL UNIQUE,
                role TEXT NOT NULL CHECK(role IN ('user', 'assistant', 'tool', 'system')),
                content TEXT NOT NULL DEFAULT '',
                tool_call_json TEXT NULL,
                tool_result_id TEXT NULL,
                created_at_utc TEXT NOT NULL,
                model_id TEXT NOT NULL DEFAULT '',
                tokens_used INTEGER NOT NULL DEFAULT 0,
                FOREIGN KEY(session_id) REFERENCES chat_sessions(session_id)
            );
            CREATE VIRTUAL TABLE IF NOT EXISTS chat_turns_fts USING fts5(
                content,
                role UNINDEXED,
                session_id UNINDEXED,
                content='chat_turns',
                content_rowid='id'
            );
            CREATE TRIGGER IF NOT EXISTS chat_turns_ai
            AFTER INSERT ON chat_turns
            BEGIN
                INSERT INTO chat_turns_fts(rowid, content, role, session_id)
                VALUES (new.id, new.content, new.role, new.session_id);
            END;
            CREATE TRIGGER IF NOT EXISTS chat_turns_no_update
            BEFORE UPDATE ON chat_turns
            BEGIN
                SELECT RAISE(ABORT, 'chat turns are append-only');
            END;
            CREATE TRIGGER IF NOT EXISTS chat_turns_no_delete
            BEFORE DELETE ON chat_turns
            BEGIN
                SELECT RAISE(ABORT, 'chat turns are append-only');
            END;
            """;
        command.ExecuteNonQuery();
    }

    private static void EnsureSession(SqliteConnection connection, Guid sessionId, DateTimeOffset timestamp)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT OR IGNORE INTO chat_sessions(session_id, title, created_at_utc, updated_at_utc, soft_deleted)
            VALUES($session_id, 'New chat', $created_at_utc, $updated_at_utc, 0);
            """;
        command.Parameters.AddWithValue("$session_id", sessionId.ToString());
        command.Parameters.AddWithValue("$created_at_utc", timestamp.ToString("O", CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$updated_at_utc", timestamp.ToString("O", CultureInfo.InvariantCulture));
        command.ExecuteNonQuery();
    }

    private static void AddTurnParameters(SqliteCommand command, ChatTurn turn)
    {
        command.Parameters.AddWithValue("$session_id", turn.SessionId.ToString());
        command.Parameters.AddWithValue("$turn_id", turn.TurnId.ToString());
        command.Parameters.AddWithValue("$role", turn.Role);
        command.Parameters.AddWithValue("$content", turn.Content ?? "");
        command.Parameters.AddWithValue("$tool_call_json", string.IsNullOrWhiteSpace(turn.ToolCallJson) ? DBNull.Value : turn.ToolCallJson);
        command.Parameters.AddWithValue("$tool_result_id", string.IsNullOrWhiteSpace(turn.ToolResultId) ? DBNull.Value : turn.ToolResultId);
        command.Parameters.AddWithValue("$created_at_utc", turn.CreatedAtUtc.ToString("O", CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$model_id", turn.ModelId ?? "");
        command.Parameters.AddWithValue("$tokens_used", Math.Max(0, turn.TokensUsed));
    }

    private static List<ChatTurn> ReadTurns(SqliteCommand command)
    {
        var turns = new List<ChatTurn>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            turns.Add(new ChatTurn(
                Guid.Parse(Convert.ToString(reader["session_id"], CultureInfo.InvariantCulture) ?? Guid.Empty.ToString()),
                Guid.Parse(Convert.ToString(reader["turn_id"], CultureInfo.InvariantCulture) ?? Guid.Empty.ToString()),
                Convert.ToString(reader["role"], CultureInfo.InvariantCulture) ?? "",
                Convert.ToString(reader["content"], CultureInfo.InvariantCulture) ?? "",
                reader["tool_call_json"] == DBNull.Value ? null : Convert.ToString(reader["tool_call_json"], CultureInfo.InvariantCulture),
                reader["tool_result_id"] == DBNull.Value ? null : Convert.ToString(reader["tool_result_id"], CultureInfo.InvariantCulture),
                DateTimeOffset.Parse(Convert.ToString(reader["created_at_utc"], CultureInfo.InvariantCulture) ?? DateTimeOffset.MinValue.ToString("O"), CultureInfo.InvariantCulture),
                Convert.ToString(reader["model_id"], CultureInfo.InvariantCulture) ?? "",
                Convert.ToInt32(reader["tokens_used"], CultureInfo.InvariantCulture)));
        }

        return turns;
    }

    private static List<ChatSessionSummary> ReadSessionSummaries(SqliteCommand command)
    {
        var sessions = new List<ChatSessionSummary>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            sessions.Add(new ChatSessionSummary(
                Guid.Parse(Convert.ToString(reader["session_id"], CultureInfo.InvariantCulture) ?? Guid.Empty.ToString()),
                Convert.ToString(reader["title"], CultureInfo.InvariantCulture) ?? "",
                DateTimeOffset.Parse(Convert.ToString(reader["created_at_utc"], CultureInfo.InvariantCulture) ?? DateTimeOffset.MinValue.ToString("O"), CultureInfo.InvariantCulture),
                DateTimeOffset.Parse(Convert.ToString(reader["updated_at_utc"], CultureInfo.InvariantCulture) ?? DateTimeOffset.MinValue.ToString("O"), CultureInfo.InvariantCulture),
                Convert.ToInt32(reader["turn_count"], CultureInfo.InvariantCulture)));
        }

        return sessions;
    }
}
