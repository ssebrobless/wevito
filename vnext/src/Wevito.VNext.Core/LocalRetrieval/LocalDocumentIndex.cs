using System.Globalization;
using Microsoft.Data.Sqlite;
using Wevito.VNext.Core.Settings;

namespace Wevito.VNext.Core.LocalRetrieval;

public sealed class LocalDocumentIndex
{
    public const string CanonicalQuerySql = """
        SELECT path, line_no, content, bm25(docs) AS score
        FROM docs WHERE docs MATCH @q
        ORDER BY bm25(docs) ASC LIMIT @n;
        """;

    private readonly string _indexPath;

    public LocalDocumentIndex(string? indexPath = null)
    {
        _indexPath = Path.GetFullPath(indexPath ?? SettingKeys.DefaultLocalDocumentIndexPath());
    }

    public string IndexPath => _indexPath;

    public string? LastCommandText { get; private set; }

    public string? LastMatchParameter { get; private set; }

    public bool IsIndexed(string relativePath, string sha256)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_indexPath) ?? ".");
        using var connection = OpenConnection();
        Initialize(connection);
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT last_indexed_utc FROM files WHERE path = @p AND sha256 = @s LIMIT 1;";
        command.Parameters.AddWithValue("@p", relativePath);
        command.Parameters.AddWithValue("@s", sha256);
        var value = command.ExecuteScalar();
        return value is string text && !string.IsNullOrWhiteSpace(text);
    }

    public void IndexLines(string relativePath, string sha256, long sizeBytes, IReadOnlyList<string> lines, DateTimeOffset indexedAtUtc)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_indexPath) ?? ".");
        using var connection = OpenConnection();
        Initialize(connection);
        using var transaction = connection.BeginTransaction();
        using (var delete = connection.CreateCommand())
        {
            delete.Transaction = transaction;
            delete.CommandText = "DELETE FROM docs WHERE path = @p;";
            delete.Parameters.AddWithValue("@p", relativePath);
            delete.ExecuteNonQuery();
        }

        for (var i = 0; i < lines.Count; i++)
        {
            using var insert = connection.CreateCommand();
            insert.Transaction = transaction;
            insert.CommandText = "INSERT INTO docs(path, line_no, content) VALUES (@p, @line, @content);";
            insert.Parameters.AddWithValue("@p", relativePath);
            insert.Parameters.AddWithValue("@line", i + 1);
            insert.Parameters.AddWithValue("@content", lines[i] ?? string.Empty);
            insert.ExecuteNonQuery();
        }

        using (var upsert = connection.CreateCommand())
        {
            upsert.Transaction = transaction;
            upsert.CommandText = """
                INSERT INTO files(path, sha256, size_bytes, last_indexed_utc)
                VALUES (@p, @sha, @size, @utc)
                ON CONFLICT(path) DO UPDATE SET
                    sha256 = excluded.sha256,
                    size_bytes = excluded.size_bytes,
                    last_indexed_utc = excluded.last_indexed_utc;
                """;
            upsert.Parameters.AddWithValue("@p", relativePath);
            upsert.Parameters.AddWithValue("@sha", sha256);
            upsert.Parameters.AddWithValue("@size", sizeBytes);
            upsert.Parameters.AddWithValue("@utc", indexedAtUtc.ToString("O", CultureInfo.InvariantCulture));
            upsert.ExecuteNonQuery();
        }

        transaction.Commit();
    }

    public IReadOnlyList<LocalDocumentSnippet> Query(string query, int topN)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_indexPath) ?? ".");
        using var connection = OpenConnection();
        Initialize(connection);
        using var command = connection.CreateCommand();
        command.CommandText = CanonicalQuerySql;
        var matchQuery = QuoteMatchQuery(query);
        command.Parameters.AddWithValue("@q", matchQuery);
        command.Parameters.AddWithValue("@n", Math.Clamp(topN, 1, 50));
        LastCommandText = command.CommandText;
        LastMatchParameter = matchQuery;

        var rows = new List<LocalDocumentSnippet>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var relativePath = reader.GetString(0);
            var lineNumber = reader.GetInt32(1);
            var content = reader.IsDBNull(2) ? string.Empty : reader.GetString(2);
            var score = reader.GetDouble(3);
            rows.Add(new LocalDocumentSnippet(relativePath, lineNumber, TrimSnippet(content), score));
        }

        return rows;
    }

    public static string NormalizeInsideRoot(string root, string candidate)
    {
        var rootFull = Path.GetFullPath(root);
        var candidateFull = Path.GetFullPath(candidate);
        var comparableRoot = EnsureTrailingSeparator(rootFull);
        if (!candidateFull.StartsWith(comparableRoot, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(candidateFull.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), rootFull.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), StringComparison.OrdinalIgnoreCase))
        {
            throw new LocalDocumentSandboxException($"Path is outside the configured local document root: {candidateFull}");
        }

        return Path.GetRelativePath(rootFull, candidateFull);
    }

    public static string QuoteMatchQuery(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            throw new ArgumentException("Local document query cannot be empty.", nameof(query));
        }

        if (query.Count(ch => ch == '"') % 2 != 0)
        {
            throw new ArgumentException("Local document query has unbalanced quotes.", nameof(query));
        }

        var escaped = query.Trim().Replace("\"", "\"\"", StringComparison.Ordinal);
        return $"\"{escaped}\"";
    }

    private SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = _indexPath,
            Mode = SqliteOpenMode.ReadWriteCreate
        }.ToString());
        connection.Open();
        return connection;
    }

    private static void Initialize(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE VIRTUAL TABLE IF NOT EXISTS docs
              USING fts5(path UNINDEXED, line_no UNINDEXED, content,
                         tokenize = 'unicode61 remove_diacritics 1');
            CREATE TABLE IF NOT EXISTS files (
              path TEXT PRIMARY KEY,
              sha256 TEXT NOT NULL,
              size_bytes INTEGER NOT NULL,
              last_indexed_utc TEXT NOT NULL);
            """;
        command.ExecuteNonQuery();
    }

    private static string EnsureTrailingSeparator(string path)
    {
        return path.EndsWith(Path.DirectorySeparatorChar) || path.EndsWith(Path.AltDirectorySeparatorChar)
            ? path
            : path + Path.DirectorySeparatorChar;
    }

    private static string TrimSnippet(string content)
    {
        var normalized = content.Replace('\r', ' ').Replace('\n', ' ').Trim();
        return normalized.Length <= 240 ? normalized : normalized[..240];
    }
}
