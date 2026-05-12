using Microsoft.Data.Sqlite;

namespace Wevito.VNext.Core;

public sealed record LearningLabLabelRecord(
    long Id,
    string SourcePath,
    string Label,
    string Reviewer,
    string Notes,
    DateTimeOffset CreatedAtUtc,
    int SchemaVersion);

public sealed record LearningLabLabelInput(
    string SourcePath,
    string Label,
    string Reviewer,
    string Notes,
    DateTimeOffset CreatedAtUtc,
    int SchemaVersion = LearningLabLabelStore.CurrentSchemaVersion);

public sealed class LearningLabLabelStore
{
    public const int CurrentSchemaVersion = 1;

    public static readonly IReadOnlyList<string> AllowedLabels =
    [
        "accept",
        "reject",
        "revise",
        "defer",
        "blocked"
    ];

    private readonly string _databasePath;
    private static bool _sqliteInitialized;

    public LearningLabLabelStore(string? databasePath = null)
    {
        _databasePath = databasePath ?? ResolveDefaultDatabasePath();
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_databasePath)!);
        await using var connection = OpenConnection();
        await connection.OpenAsync(cancellationToken);

        var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS examples (
                id INTEGER PRIMARY KEY,
                source_path TEXT NOT NULL,
                label TEXT NOT NULL,
                reviewer TEXT NOT NULL,
                notes TEXT NOT NULL,
                created_at TEXT NOT NULL,
                schema_version INTEGER NOT NULL
            );

            CREATE INDEX IF NOT EXISTS idx_examples_source_path_created_at
            ON examples (source_path, created_at DESC, id DESC);
            """;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<LearningLabLabelRecord> SaveAsync(LearningLabLabelInput input, CancellationToken cancellationToken = default)
    {
        Validate(input);
        await InitializeAsync(cancellationToken);
        await using var connection = OpenConnection();
        await connection.OpenAsync(cancellationToken);

        var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO examples (source_path, label, reviewer, notes, created_at, schema_version)
            VALUES ($sourcePath, $label, $reviewer, $notes, $createdAt, $schemaVersion);
            SELECT last_insert_rowid();
            """;
        command.Parameters.AddWithValue("$sourcePath", Path.GetFullPath(input.SourcePath));
        command.Parameters.AddWithValue("$label", input.Label);
        command.Parameters.AddWithValue("$reviewer", input.Reviewer);
        command.Parameters.AddWithValue("$notes", input.Notes);
        command.Parameters.AddWithValue("$createdAt", input.CreatedAtUtc.UtcDateTime.ToString("O"));
        command.Parameters.AddWithValue("$schemaVersion", input.SchemaVersion);

        var id = (long)(await command.ExecuteScalarAsync(cancellationToken) ?? 0L);
        return (await GetByIdAsync(id, cancellationToken))!;
    }

    public async Task<LearningLabLabelRecord?> GetLatestForSourceAsync(string sourcePath, CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken);
        await using var connection = OpenConnection();
        await connection.OpenAsync(cancellationToken);

        var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id, source_path, label, reviewer, notes, created_at, schema_version
            FROM examples
            WHERE source_path = $sourcePath
            ORDER BY created_at DESC, id DESC
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("$sourcePath", Path.GetFullPath(sourcePath));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? ReadRecord(reader) : null;
    }

    public async Task<IReadOnlyDictionary<string, LearningLabLabelRecord>> ListLatestAsync(CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken);
        await using var connection = OpenConnection();
        await connection.OpenAsync(cancellationToken);

        var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id, source_path, label, reviewer, notes, created_at, schema_version
            FROM examples
            ORDER BY source_path ASC, created_at DESC, id DESC;
            """;

        var labels = new Dictionary<string, LearningLabLabelRecord>(StringComparer.OrdinalIgnoreCase);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var record = ReadRecord(reader);
            labels.TryAdd(record.SourcePath, record);
        }

        return labels;
    }

    public async Task<int> DeleteForSourceAsync(string sourcePath, CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken);
        await using var connection = OpenConnection();
        await connection.OpenAsync(cancellationToken);

        var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM examples WHERE source_path = $sourcePath;";
        command.Parameters.AddWithValue("$sourcePath", Path.GetFullPath(sourcePath));
        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task<LearningLabLabelRecord?> GetByIdAsync(long id, CancellationToken cancellationToken)
    {
        await using var connection = OpenConnection();
        await connection.OpenAsync(cancellationToken);

        var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id, source_path, label, reviewer, notes, created_at, schema_version
            FROM examples
            WHERE id = $id;
            """;
        command.Parameters.AddWithValue("$id", id);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? ReadRecord(reader) : null;
    }

    private static void Validate(LearningLabLabelInput input)
    {
        if (string.IsNullOrWhiteSpace(input.SourcePath))
        {
            throw new ArgumentException("Source path is required.", nameof(input));
        }

        if (!AllowedLabels.Contains(input.Label, StringComparer.OrdinalIgnoreCase))
        {
            throw new ArgumentOutOfRangeException(nameof(input), $"Unsupported label: {input.Label}");
        }

        if (string.IsNullOrWhiteSpace(input.Reviewer))
        {
            throw new ArgumentException("Reviewer is required.", nameof(input));
        }
    }

    private static LearningLabLabelRecord ReadRecord(SqliteDataReader reader)
    {
        return new LearningLabLabelRecord(
            reader.GetInt64(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetString(3),
            reader.GetString(4),
            DateTimeOffset.Parse(reader.GetString(5)),
            reader.GetInt32(6));
    }

    private SqliteConnection OpenConnection()
    {
        EnsureSqliteInitialized();
        return new SqliteConnection($"Data Source={_databasePath}");
    }

    private static void EnsureSqliteInitialized()
    {
        if (_sqliteInitialized)
        {
            return;
        }

        SQLitePCL.Batteries_V2.Init();
        _sqliteInitialized = true;
    }

    private static string ResolveDefaultDatabasePath()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Wevito",
            "learning_lab.db");
    }
}
