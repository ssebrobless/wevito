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
    string Message,
    int EmbeddingDimensions = HashingTextEmbeddingService.DefaultDimensions);

public sealed record RerankHeadApplicationResult(
    bool Succeeded,
    IReadOnlyList<PetMemorySearchResult> Results,
    string Message);

public sealed record DocumentChunkSearchResult(
    RetrievalChunk Chunk,
    double Score,
    string Method);

public interface ITextEmbeddingService
{
    int Dimensions { get; }

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

    public int Dimensions => _dimensions;

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

    public int Dimensions => _inner.Dimensions;

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
    public const int HashingFallbackEmbeddingDimensions = HashingTextEmbeddingService.DefaultDimensions;

    private readonly string _memoryRoot;
    private readonly ITextEmbeddingService _embeddingService;
    private readonly int _embeddingDimensions;

    public PetMemoryStore(string? memoryRoot = null, ITextEmbeddingService? embeddingService = null)
    {
        _memoryRoot = Path.GetFullPath(memoryRoot ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Wevito",
            "memory"));
        _embeddingService = embeddingService ?? new CachingTextEmbeddingService();
        _embeddingDimensions = Math.Max(4, _embeddingService.Dimensions);
    }

    public int EmbeddingDimensions => _embeddingDimensions;

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
            MoveDatabaseAside(path, $"corrupt-{stamp}");
            rebuilt = true;
        }

        if (File.Exists(path) && ShouldRebuildForEmbeddingDimension(path, _embeddingDimensions))
        {
            SqliteConnection.ClearAllPools();
            var stamp = (nowUtc ?? DateTimeOffset.UtcNow).ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
            MoveDatabaseAside(path, $"legacy-{stamp}");
            rebuilt = true;
        }

        using var connection = OpenConnection(path);
        var vectorAvailable = TryLoadVector(connection);
        InitializeSchema(connection, vectorAvailable, _embeddingDimensions);
        return new PetMemoryStoreStatus(
            DatabaseExists: true,
            vectorAvailable,
            rebuilt,
            path,
            rebuilt ? "Memory database was rebuilt for integrity or embedding-dimension compatibility." : "Memory database is ready.",
            _embeddingDimensions);
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

    public RerankHeadApplicationResult ApplyRerankHead(Guid petId, string query, RerankHead rerankHead, string requestedToolFamily, int topK = 3)
    {
        var results = Search(petId, query, kind: "", topK: Math.Max(1, topK) * 2)
            .OrderByDescending(result => rerankHead.Score(result, requestedToolFamily))
            .ThenBy(result => result.Example.Id)
            .Take(Math.Max(1, topK))
            .ToList();
        return new RerankHeadApplicationResult(true, results, $"Applied rerank head {rerankHead.HeadId} in-process.");
    }

    public IReadOnlyList<RetrievalChunk> AddDocumentChunks(
        Guid petId,
        string docId,
        string path,
        string sha256,
        IReadOnlyList<RetrievalChunk> chunks,
        DateTimeOffset? nowUtc = null)
    {
        var timestamp = nowUtc ?? DateTimeOffset.UtcNow;
        var status = EnsureReady(petId, timestamp);
        using var connection = OpenConnection(status.DatabasePath);
        var vectorAvailable = TryLoadVector(connection);
        using var transaction = connection.BeginTransaction();
        ArchivePriorChunks(connection, transaction, path, sha256, timestamp);
        foreach (var chunk in chunks)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                INSERT OR REPLACE INTO pet_doc_chunks(
                    chunk_id, doc_id, path, sha256, text, byte_start, byte_end, embedding, chunked_at_utc, archived_at_utc, is_tombstone)
                VALUES(
                    $chunk_id, $doc_id, $path, $sha256, $text, $byte_start, $byte_end, $embedding, $chunked_at_utc, '', 0);
                """;
            command.Parameters.AddWithValue("$chunk_id", chunk.ChunkId);
            command.Parameters.AddWithValue("$doc_id", docId);
            command.Parameters.AddWithValue("$path", path);
            command.Parameters.AddWithValue("$sha256", sha256);
            command.Parameters.AddWithValue("$text", chunk.Text);
            command.Parameters.AddWithValue("$byte_start", chunk.ByteStart);
            command.Parameters.AddWithValue("$byte_end", chunk.ByteEnd);
            command.Parameters.Add("$embedding", SqliteType.Blob).Value = SerializeEmbedding(chunk.Embedding);
            command.Parameters.AddWithValue("$chunked_at_utc", chunk.ChunkedAtUtc.ToString("O"));
            command.ExecuteNonQuery();

            using var deleteFts = connection.CreateCommand();
            deleteFts.Transaction = transaction;
            deleteFts.CommandText = "DELETE FROM pet_doc_chunks_fts WHERE chunk_id = $chunk_id;";
            deleteFts.Parameters.AddWithValue("$chunk_id", chunk.ChunkId);
            deleteFts.ExecuteNonQuery();

            using var fts = connection.CreateCommand();
            fts.Transaction = transaction;
            fts.CommandText = "INSERT INTO pet_doc_chunks_fts(chunk_id, path, text) VALUES($chunk_id, $path, $text);";
            fts.Parameters.AddWithValue("$chunk_id", chunk.ChunkId);
            fts.Parameters.AddWithValue("$path", path);
            fts.Parameters.AddWithValue("$text", chunk.Text);
            fts.ExecuteNonQuery();

            if (vectorAvailable)
            {
                using var vectorDelete = connection.CreateCommand();
                vectorDelete.Transaction = transaction;
                vectorDelete.CommandText = "DELETE FROM vec_doc_chunks WHERE chunk_id = $chunk_id;";
                vectorDelete.Parameters.AddWithValue("$chunk_id", chunk.ChunkId);
                vectorDelete.ExecuteNonQuery();

                using var vector = connection.CreateCommand();
                vector.Transaction = transaction;
                vector.CommandText = "INSERT INTO vec_doc_chunks(chunk_id, embedding) VALUES($chunk_id, $embedding);";
                vector.Parameters.AddWithValue("$chunk_id", chunk.ChunkId);
                vector.Parameters.AddWithValue("$embedding", FormatVector(chunk.Embedding));
                vector.ExecuteNonQuery();
            }
        }

        transaction.Commit();
        return chunks;
    }

    public IReadOnlyList<DocumentChunkSearchResult> SearchDocumentChunksDense(Guid petId, string query, int topK = 20)
    {
        var status = EnsureReady(petId);
        var queryEmbedding = _embeddingService.Embed(query);
        using var connection = OpenConnection(status.DatabasePath);
        var vectorAvailable = TryLoadVector(connection);
        if (vectorAvailable)
        {
            var vectorResults = TrySearchDocumentChunksWithSqliteVec(connection, queryEmbedding, topK);
            if (vectorResults.Count > 0)
            {
                return vectorResults;
            }
        }

        return SearchDocumentChunksDenseInProcess(connection, queryEmbedding, topK);
    }

    public IReadOnlyList<DocumentChunkSearchResult> SearchDocumentChunksKeyword(Guid petId, string query, int topK = 20)
    {
        var status = EnsureReady(petId);
        using var connection = OpenConnection(status.DatabasePath);
        var ftsResults = TrySearchDocumentChunksWithFts(connection, query, topK);
        return ftsResults.Count > 0
            ? ftsResults
            : SearchDocumentChunksKeywordInProcess(connection, query, topK);
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

    private static void InitializeSchema(SqliteConnection connection, bool vectorAvailable, int embeddingDimensions)
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
            INSERT INTO schema_info(key, value)
            VALUES ('embedding_dimensions', $embedding_dimensions)
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
            CREATE TABLE IF NOT EXISTS pet_doc_chunks(
                chunk_id TEXT PRIMARY KEY,
                doc_id TEXT NOT NULL,
                path TEXT NOT NULL,
                sha256 TEXT NOT NULL,
                text TEXT NOT NULL,
                byte_start INTEGER NOT NULL,
                byte_end INTEGER NOT NULL,
                embedding BLOB NOT NULL,
                chunked_at_utc TEXT NOT NULL,
                archived_at_utc TEXT NOT NULL DEFAULT '',
                is_tombstone INTEGER NOT NULL DEFAULT 0
            );
            CREATE INDEX IF NOT EXISTS ix_pet_doc_chunks_path ON pet_doc_chunks(path);
            CREATE INDEX IF NOT EXISTS ix_pet_doc_chunks_doc_id ON pet_doc_chunks(doc_id);
            CREATE VIRTUAL TABLE IF NOT EXISTS pet_doc_chunks_fts USING fts5(chunk_id UNINDEXED, path UNINDEXED, text);
            """;
        command.Parameters.AddWithValue("$embedding_dimensions", embeddingDimensions.ToString(CultureInfo.InvariantCulture));
        command.ExecuteNonQuery();
        EnsureColumn(connection, "examples", "dataset_version", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(connection, "examples", "source_task_card_id", "TEXT NOT NULL DEFAULT ''");

        if (!vectorAvailable)
        {
            return;
        }

        using var vectorCommand = connection.CreateCommand();
        vectorCommand.CommandText = $"""
            CREATE VIRTUAL TABLE IF NOT EXISTS vec_examples USING vec0(example_id INTEGER PRIMARY KEY, embedding float[{embeddingDimensions}]);
            CREATE VIRTUAL TABLE IF NOT EXISTS vec_doc_chunks USING vec0(chunk_id TEXT PRIMARY KEY, embedding float[{embeddingDimensions}]);
            """;
        vectorCommand.ExecuteNonQuery();
    }

    private static void ArchivePriorChunks(SqliteConnection connection, SqliteTransaction transaction, string path, string sha256, DateTimeOffset timestamp)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            UPDATE pet_doc_chunks
            SET archived_at_utc = $archived_at_utc, is_tombstone = 1
            WHERE path = $path AND sha256 <> $sha256 AND archived_at_utc = '';
            """;
        command.Parameters.AddWithValue("$path", path);
        command.Parameters.AddWithValue("$sha256", sha256);
        command.Parameters.AddWithValue("$archived_at_utc", timestamp.ToString("O"));
        command.ExecuteNonQuery();
    }

    private static IReadOnlyList<DocumentChunkSearchResult> TrySearchDocumentChunksWithSqliteVec(SqliteConnection connection, float[] queryEmbedding, int topK)
    {
        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT c.chunk_id, c.doc_id, c.path, c.sha256, c.text, c.byte_start, c.byte_end, c.embedding, c.chunked_at_utc, v.distance
                FROM vec_doc_chunks v
                JOIN pet_doc_chunks c ON c.chunk_id = v.chunk_id
                WHERE v.embedding MATCH $embedding AND k = $k AND c.archived_at_utc = '' AND c.is_tombstone = 0
                ORDER BY v.distance
                """;
            command.Parameters.AddWithValue("$embedding", FormatVector(queryEmbedding));
            command.Parameters.AddWithValue("$k", Math.Max(1, topK));
            var results = new List<DocumentChunkSearchResult>();
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var distance = Convert.ToDouble(reader["distance"], CultureInfo.InvariantCulture);
                results.Add(new DocumentChunkSearchResult(
                    ReadChunk(reader),
                    Score: 1d / (1d + distance),
                    Method: "dense:sqlite-vec"));
            }

            return results;
        }
        catch
        {
            return [];
        }
    }

    private static IReadOnlyList<DocumentChunkSearchResult> SearchDocumentChunksDenseInProcess(SqliteConnection connection, float[] queryEmbedding, int topK)
    {
        return ReadActiveChunks(connection)
            .Select(chunk => new DocumentChunkSearchResult(chunk, Cosine(queryEmbedding, chunk.Embedding), "dense:in-process"))
            .OrderByDescending(result => result.Score)
            .ThenBy(result => result.Chunk.ChunkId, StringComparer.OrdinalIgnoreCase)
            .Take(Math.Max(1, topK))
            .ToList();
    }

    private static IReadOnlyList<DocumentChunkSearchResult> TrySearchDocumentChunksWithFts(SqliteConnection connection, string query, int topK)
    {
        var ftsQuery = BuildFtsQuery(query);
        if (string.IsNullOrWhiteSpace(ftsQuery))
        {
            return [];
        }

        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT c.chunk_id, c.doc_id, c.path, c.sha256, c.text, c.byte_start, c.byte_end, c.embedding, c.chunked_at_utc, bm25(f) AS rank
                FROM pet_doc_chunks_fts f
                JOIN pet_doc_chunks c ON c.chunk_id = f.chunk_id
                WHERE pet_doc_chunks_fts MATCH $query AND c.archived_at_utc = '' AND c.is_tombstone = 0
                ORDER BY rank
                LIMIT $k;
                """;
            command.Parameters.AddWithValue("$query", ftsQuery);
            command.Parameters.AddWithValue("$k", Math.Max(1, topK));
            var results = new List<DocumentChunkSearchResult>();
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var rank = Math.Abs(Convert.ToDouble(reader["rank"], CultureInfo.InvariantCulture));
                results.Add(new DocumentChunkSearchResult(ReadChunk(reader), 1d / (1d + rank), "keyword:fts5"));
            }

            return results;
        }
        catch
        {
            return [];
        }
    }

    private static IReadOnlyList<DocumentChunkSearchResult> SearchDocumentChunksKeywordInProcess(SqliteConnection connection, string query, int topK)
    {
        var queryTokens = Tokenize(query);
        if (queryTokens.Count == 0)
        {
            return [];
        }

        return ReadActiveChunks(connection)
            .Select(chunk =>
            {
                var tokens = Tokenize(chunk.Text);
                var score = queryTokens.Count(token => tokens.Contains(token)) / (double)queryTokens.Count;
                return new DocumentChunkSearchResult(chunk, score, "keyword:in-process");
            })
            .Where(result => result.Score > 0)
            .OrderByDescending(result => result.Score)
            .ThenBy(result => result.Chunk.ChunkId, StringComparer.OrdinalIgnoreCase)
            .Take(Math.Max(1, topK))
            .ToList();
    }

    private static IReadOnlyList<RetrievalChunk> ReadActiveChunks(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT chunk_id, doc_id, path, sha256, text, byte_start, byte_end, embedding, chunked_at_utc
            FROM pet_doc_chunks
            WHERE archived_at_utc = '' AND is_tombstone = 0;
            """;
        var chunks = new List<RetrievalChunk>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            chunks.Add(ReadChunk(reader));
        }

        return chunks;
    }

    private static RetrievalChunk ReadChunk(SqliteDataReader reader)
    {
        return new RetrievalChunk(
            Convert.ToString(reader["chunk_id"], CultureInfo.InvariantCulture) ?? "",
            Convert.ToString(reader["doc_id"], CultureInfo.InvariantCulture) ?? "",
            Convert.ToString(reader["path"], CultureInfo.InvariantCulture) ?? "",
            Convert.ToString(reader["sha256"], CultureInfo.InvariantCulture) ?? "",
            Convert.ToString(reader["text"], CultureInfo.InvariantCulture) ?? "",
            Convert.ToInt32(reader["byte_start"], CultureInfo.InvariantCulture),
            Convert.ToInt32(reader["byte_end"], CultureInfo.InvariantCulture),
            DeserializeEmbedding((byte[])reader["embedding"]),
            DateTimeOffset.Parse(Convert.ToString(reader["chunked_at_utc"], CultureInfo.InvariantCulture) ?? DateTimeOffset.MinValue.ToString("O"), CultureInfo.InvariantCulture));
    }

    private static string BuildFtsQuery(string query)
    {
        var tokens = Tokenize(query)
            .Take(12)
            .Select(token => token.Replace("\"", "\"\"", StringComparison.Ordinal))
            .ToList();
        return tokens.Count == 0 ? "" : string.Join(" OR ", tokens.Select(token => $"\"{token}\""));
    }

    private static HashSet<string> Tokenize(string value)
    {
        return (value ?? "")
            .ToLowerInvariant()
            .Split([' ', '\t', '\r', '\n', '.', ',', ';', ':', '/', '\\', '-', '_', '(', ')', '[', ']', '{', '}'], StringSplitOptions.RemoveEmptyEntries)
            .Where(token => token.Length > 1)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static bool ShouldRebuildForEmbeddingDimension(string path, int expectedDimensions)
    {
        try
        {
            using var connection = OpenConnection(path);
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT value FROM schema_info WHERE key = 'embedding_dimensions';";
            var value = command.ExecuteScalar()?.ToString();
            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var storedDimensions))
            {
                return storedDimensions != expectedDimensions;
            }

            return expectedDimensions != HashingFallbackEmbeddingDimensions;
        }
        catch
        {
            return expectedDimensions != HashingFallbackEmbeddingDimensions;
        }
    }

    private static void MoveDatabaseAside(string path, string suffix)
    {
        var directory = Path.GetDirectoryName(path) ?? ".";
        var stem = Path.GetFileNameWithoutExtension(path);
        File.Move(path, Path.Combine(directory, $"{stem}.{suffix}.db"), overwrite: true);
        MoveSidecar(path + "-wal", Path.Combine(directory, $"{stem}.{suffix}.db-wal"));
        MoveSidecar(path + "-shm", Path.Combine(directory, $"{stem}.{suffix}.db-shm"));
    }

    private static void MoveSidecar(string source, string destination)
    {
        if (File.Exists(source))
        {
            File.Move(source, destination, overwrite: true);
        }
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
