using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.Sqlite;
using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed record PetMemoryExample(
    long Id,
    string Kind,
    string Content,
    string Label,
    float[] Embedding,
    DateTimeOffset CreatedAtUtc,
    string DatasetVersion = "",
    string SourceTaskCardId = "");

public sealed record PetMemorySearchResult(
    PetMemoryExample Example,
    double Score,
    double Distance);

public sealed record PetMemoryStoreStatus(
    bool DatabaseExists,
    bool VectorExtensionAvailable,
    bool WasRebuilt,
    string DatabasePath,
    string Message);

public interface ITextEmbeddingService
{
    float[] Embed(string text);
}

public sealed class HashingTextEmbeddingService : ITextEmbeddingService
{
    public const int DefaultDimensions = 16;
    private readonly int _dimensions;

    public HashingTextEmbeddingService(int dimensions = DefaultDimensions)
    {
        _dimensions = Math.Max(4, dimensions);
    }

    public float[] Embed(string text)
    {
        var normalized = (text ?? string.Empty).Trim().ToLowerInvariant();
        var vector = new float[_dimensions];
        foreach (var token in normalized.Split([' ', '\t', '\r', '\n', '.', ',', ';', ':', '/', '\\', '-', '_'], StringSplitOptions.RemoveEmptyEntries))
        {
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
            var index = BitConverter.ToUInt16(hash, 0) % _dimensions;
            var sign = (hash[2] & 1) == 0 ? 1f : -1f;
            vector[index] += sign;
        }

        Normalize(vector);
        return vector;
    }

    private static void Normalize(float[] vector)
    {
        var length = Math.Sqrt(vector.Sum(value => value * value));
        if (length <= 0)
        {
            return;
        }

        for (var index = 0; index < vector.Length; index++)
        {
            vector[index] = (float)(vector[index] / length);
        }
    }
}

public sealed class CachingTextEmbeddingService : ITextEmbeddingService
{
    private readonly ITextEmbeddingService _inner;
    private readonly Dictionary<string, float[]> _cache = new(StringComparer.Ordinal);

    public CachingTextEmbeddingService(ITextEmbeddingService? inner = null)
    {
        _inner = inner ?? new HashingTextEmbeddingService();
    }

    public float[] Embed(string text)
    {
        var key = text ?? string.Empty;
        if (!_cache.TryGetValue(key, out var embedding))
        {
            embedding = _inner.Embed(key);
            _cache[key] = embedding;
        }

        return embedding.ToArray();
    }
}

public sealed class PetMemoryStore
{
    public const string SchemaVersion = "1";
    public const int EmbeddingDimensions = HashingTextEmbeddingService.DefaultDimensions;

    private readonly string _memoryRoot;
    private readonly ITextEmbeddingService _embeddingService;

    public PetMemoryStore(string? memoryRoot = null, ITextEmbeddingService? embeddingService = null)
    {
        _memoryRoot = Path.GetFullPath(memoryRoot ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Wevito",
            "memory"));
        _embeddingService = embeddingService ?? new CachingTextEmbeddingService();
    }

    public PetMemoryStoreStatus EnsureReady(Guid petId, DateTimeOffset? nowUtc = null)
    {
        var path = ResolveDatabasePath(petId);
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? _memoryRoot);
        var rebuilt = false;
        SqliteConnection.ClearAllPools();
        if (File.Exists(path) && !IsIntegrityOk(path))
        {
            SqliteConnection.ClearAllPools();
            var stamp = (nowUtc ?? DateTimeOffset.UtcNow).ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
            File.Move(path, Path.Combine(Path.GetDirectoryName(path) ?? _memoryRoot, $"{Path.GetFileNameWithoutExtension(path)}.corrupt-{stamp}.db"), overwrite: true);
            rebuilt = true;
        }

        using var connection = OpenConnection(path);
        var vectorAvailable = TryLoadVector(connection);
        InitializeSchema(connection, vectorAvailable);
        return new PetMemoryStoreStatus(
            DatabaseExists: true,
            vectorAvailable,
            rebuilt,
            path,
            rebuilt ? "Memory database was corrupt and has been rebuilt." : "Memory database is ready.");
    }

    public PetMemoryExample AddExample(
        Guid petId,
        string kind,
        string content,
        string label,
        DateTimeOffset? nowUtc = null,
        string datasetVersion = "",
        string sourceTaskCardId = "")
    {
        var timestamp = nowUtc ?? DateTimeOffset.UtcNow;
        var status = EnsureReady(petId, timestamp);
        var embedding = _embeddingService.Embed($"{kind} {label} {content}");
        using var connection = OpenConnection(status.DatabasePath);
        var vectorAvailable = TryLoadVector(connection);
        using var transaction = connection.BeginTransaction();
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO examples(kind, content, label, embedding, created_at, dataset_version, source_task_card_id)
            VALUES ($kind, $content, $label, $embedding, $created_at, $dataset_version, $source_task_card_id);
            """;
        command.Parameters.AddWithValue("$kind", kind);
        command.Parameters.AddWithValue("$content", content);
        command.Parameters.AddWithValue("$label", label);
        command.Parameters.Add("$embedding", SqliteType.Blob).Value = SerializeEmbedding(embedding);
        command.Parameters.AddWithValue("$created_at", timestamp.ToString("O"));
        command.Parameters.AddWithValue("$dataset_version", datasetVersion ?? "");
        command.Parameters.AddWithValue("$source_task_card_id", sourceTaskCardId ?? "");
        command.ExecuteNonQuery();

        using var idCommand = connection.CreateCommand();
        idCommand.Transaction = transaction;
        idCommand.CommandText = "SELECT last_insert_rowid();";
        var id = (long)(idCommand.ExecuteScalar() ?? 0L);

        if (vectorAvailable)
        {
            using var vectorCommand = connection.CreateCommand();
            vectorCommand.Transaction = transaction;
            vectorCommand.CommandText = "INSERT INTO vec_examples(example_id, embedding) VALUES ($id, $embedding);";
            vectorCommand.Parameters.AddWithValue("$id", id);
            vectorCommand.Parameters.AddWithValue("$embedding", FormatVector(embedding));
            vectorCommand.ExecuteNonQuery();
        }

        transaction.Commit();
        return new PetMemoryExample(id, kind, content, label, embedding, timestamp, datasetVersion ?? "", sourceTaskCardId ?? "");
    }

    public IReadOnlyList<PetMemorySearchResult> Search(Guid petId, string query, string kind = "", int topK = 3)
    {
        var status = EnsureReady(petId);
        var queryEmbedding = _embeddingService.Embed(query);
        using var connection = OpenConnection(status.DatabasePath);
        var vectorAvailable = TryLoadVector(connection);
        if (vectorAvailable)
        {
            var vectorResults = TrySearchWithSqliteVec(connection, queryEmbedding, kind, topK);
            if (vectorResults.Count > 0)
            {
                return vectorResults;
            }
        }

        return SearchInProcess(connection, queryEmbedding, kind, topK);
    }

    public string ResolveDatabasePath(Guid petId)
    {
        return Path.GetFullPath(Path.Combine(_memoryRoot, $"{petId:N}.db"));
    }

    private static SqliteConnection OpenConnection(string path)
    {
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = path,
            Mode = SqliteOpenMode.ReadWriteCreate
        }.ToString());
        connection.Open();
        using var pragma = connection.CreateCommand();
        pragma.CommandText = "PRAGMA journal_mode=WAL; PRAGMA foreign_keys=ON;";
        pragma.ExecuteNonQuery();
        return connection;
    }

    private static bool IsIntegrityOk(string path)
    {
        try
        {
            Span<byte> header = stackalloc byte[16];
            using (var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
            {
                if (stream.Length > 0)
                {
                    var read = stream.Read(header);
                    if (read < 16 || Encoding.ASCII.GetString(header) != "SQLite format 3\0")
                    {
                        return false;
                    }
                }
            }

            using var connection = OpenConnection(path);
            using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA integrity_check;";
            return string.Equals(command.ExecuteScalar()?.ToString(), "ok", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private static bool TryLoadVector(SqliteConnection connection)
    {
        try
        {
            connection.EnableExtensions(true);
            connection.LoadVector();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT vec_version();";
            _ = command.ExecuteScalar();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void InitializeSchema(SqliteConnection connection, bool vectorAvailable)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS schema_info(
                key TEXT PRIMARY KEY,
                value TEXT NOT NULL
            );
            INSERT INTO schema_info(key, value)
            VALUES ('schema_version', '1')
            ON CONFLICT(key) DO UPDATE SET value = excluded.value;
            CREATE TABLE IF NOT EXISTS examples(
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                kind TEXT NOT NULL,
                content TEXT NOT NULL,
                label TEXT NOT NULL,
                embedding BLOB NOT NULL,
                created_at TEXT NOT NULL,
                dataset_version TEXT NOT NULL DEFAULT '',
                source_task_card_id TEXT NOT NULL DEFAULT ''
            );
            CREATE VIRTUAL TABLE IF NOT EXISTS examples_fts USING fts5(kind, content, label, content='examples', content_rowid='id');
            """;
        command.ExecuteNonQuery();
        EnsureColumn(connection, "examples", "dataset_version", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(connection, "examples", "source_task_card_id", "TEXT NOT NULL DEFAULT ''");

        if (!vectorAvailable)
        {
            return;
        }

        using var vectorCommand = connection.CreateCommand();
        vectorCommand.CommandText = $"CREATE VIRTUAL TABLE IF NOT EXISTS vec_examples USING vec0(example_id INTEGER PRIMARY KEY, embedding float[{EmbeddingDimensions}]);";
        vectorCommand.ExecuteNonQuery();
    }

    private static IReadOnlyList<PetMemorySearchResult> TrySearchWithSqliteVec(SqliteConnection connection, float[] queryEmbedding, string kind, int topK)
    {
        try
        {
            using var command = connection.CreateCommand();
            var hasKind = !string.IsNullOrWhiteSpace(kind);
            command.CommandText = hasKind
                ? """
                  SELECT e.id, e.kind, e.content, e.label, e.embedding, e.created_at, e.dataset_version, e.source_task_card_id, v.distance
                  FROM vec_examples v
                  JOIN examples e ON e.id = v.example_id
                  WHERE v.embedding MATCH $embedding AND k = $k AND e.kind = $kind
                  ORDER BY v.distance
                  """
                : """
                  SELECT e.id, e.kind, e.content, e.label, e.embedding, e.created_at, e.dataset_version, e.source_task_card_id, v.distance
                  FROM vec_examples v
                  JOIN examples e ON e.id = v.example_id
                  WHERE v.embedding MATCH $embedding AND k = $k
                  ORDER BY v.distance
                  """;
            command.Parameters.AddWithValue("$embedding", FormatVector(queryEmbedding));
            command.Parameters.AddWithValue("$k", Math.Max(1, topK));
            if (hasKind)
            {
                command.Parameters.AddWithValue("$kind", kind);
            }

            var results = new List<PetMemorySearchResult>();
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var embedding = DeserializeEmbedding((byte[])reader["embedding"]);
                var distance = Convert.ToDouble(reader["distance"], CultureInfo.InvariantCulture);
                results.Add(new PetMemorySearchResult(
                    ReadExample(reader, embedding),
                    Score: 1d / (1d + distance),
                    distance));
            }

            return results;
        }
        catch
        {
            return [];
        }
    }

    private static IReadOnlyList<PetMemorySearchResult> SearchInProcess(SqliteConnection connection, float[] queryEmbedding, string kind, int topK)
    {
        using var command = connection.CreateCommand();
        var hasKind = !string.IsNullOrWhiteSpace(kind);
        command.CommandText = hasKind
            ? "SELECT id, kind, content, label, embedding, created_at, dataset_version, source_task_card_id FROM examples WHERE kind = $kind"
            : "SELECT id, kind, content, label, embedding, created_at, dataset_version, source_task_card_id FROM examples";
        if (hasKind)
        {
            command.Parameters.AddWithValue("$kind", kind);
        }

        var results = new List<PetMemorySearchResult>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var embedding = DeserializeEmbedding((byte[])reader["embedding"]);
            var score = Cosine(queryEmbedding, embedding);
            results.Add(new PetMemorySearchResult(ReadExample(reader, embedding), score, Distance: 1d - score));
        }

        return results
            .OrderByDescending(result => result.Score)
            .Take(Math.Max(1, topK))
            .ToList();
    }

    private static PetMemoryExample ReadExample(SqliteDataReader reader, float[] embedding)
    {
        return new PetMemoryExample(
            Convert.ToInt64(reader["id"], CultureInfo.InvariantCulture),
            Convert.ToString(reader["kind"], CultureInfo.InvariantCulture) ?? "",
            Convert.ToString(reader["content"], CultureInfo.InvariantCulture) ?? "",
            Convert.ToString(reader["label"], CultureInfo.InvariantCulture) ?? "",
            embedding,
            DateTimeOffset.Parse(Convert.ToString(reader["created_at"], CultureInfo.InvariantCulture) ?? DateTimeOffset.MinValue.ToString("O"), CultureInfo.InvariantCulture),
            Convert.ToString(reader["dataset_version"], CultureInfo.InvariantCulture) ?? "",
            Convert.ToString(reader["source_task_card_id"], CultureInfo.InvariantCulture) ?? "");
    }

    private static void EnsureColumn(SqliteConnection connection, string tableName, string columnName, string definition)
    {
        using var read = connection.CreateCommand();
        read.CommandText = $"PRAGMA table_info({tableName});";
        using var reader = read.ExecuteReader();
        while (reader.Read())
        {
            if (string.Equals(reader["name"]?.ToString(), columnName, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
        }

        using var alter = connection.CreateCommand();
        alter.CommandText = $"ALTER TABLE {tableName} ADD COLUMN {columnName} {definition};";
        alter.ExecuteNonQuery();
    }

    private static byte[] SerializeEmbedding(float[] embedding)
    {
        var bytes = new byte[embedding.Length * sizeof(float)];
        Buffer.BlockCopy(embedding, 0, bytes, 0, bytes.Length);
        return bytes;
    }

    private static float[] DeserializeEmbedding(byte[] bytes)
    {
        var embedding = new float[bytes.Length / sizeof(float)];
        Buffer.BlockCopy(bytes, 0, embedding, 0, bytes.Length);
        return embedding;
    }

    private static string FormatVector(float[] embedding)
    {
        return "[" + string.Join(",", embedding.Select(value => value.ToString("R", CultureInfo.InvariantCulture))) + "]";
    }

    private static double Cosine(float[] left, float[] right)
    {
        var count = Math.Min(left.Length, right.Length);
        if (count == 0)
        {
            return 0;
        }

        double dot = 0;
        double leftLength = 0;
        double rightLength = 0;
        for (var index = 0; index < count; index++)
        {
            dot += left[index] * right[index];
            leftLength += left[index] * left[index];
            rightLength += right[index] * right[index];
        }

        if (leftLength <= 0 || rightLength <= 0)
        {
            return 0;
        }

        return dot / (Math.Sqrt(leftLength) * Math.Sqrt(rightLength));
    }
}
