using System.Globalization;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public enum BenchmarkCaseReviewState
{
    PendingReview,
    Approved,
    Rejected,
    Revised
}

public sealed record BenchmarkCaseReviewRecord(
    long Id,
    string CaseId,
    string Axis,
    string DraftPath,
    string ApprovedPath,
    BenchmarkCaseReviewState State,
    string Reviewer,
    string Notes,
    DateTimeOffset CreatedAtUtc);

public sealed class BenchmarkCaseCurationStore
{
    public const string CaseApprovedPacketKind = "benchmark_case_approved";
    public const string CaseRejectedPacketKind = "benchmark_case_rejected";
    public const string CaseRevisedPacketKind = "benchmark_case_revised";

    private readonly string _databasePath;
    private readonly AuditLedgerService? _auditLedgerService;
    private readonly KillSwitchService? _killSwitchService;

    public BenchmarkCaseCurationStore(string? databasePath = null, AuditLedgerService? auditLedgerService = null, KillSwitchService? killSwitchService = null)
    {
        _databasePath = Path.GetFullPath(databasePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Wevito",
            "audit",
            "benchmark-curation.sqlite"));
        _auditLedgerService = auditLedgerService;
        _killSwitchService = killSwitchService;
    }

    public BenchmarkCaseReviewRecord AppendPending(string draftPath, string reviewer = "codex", string notes = "", DateTimeOffset? nowUtc = null)
    {
        if (_killSwitchService?.IsActive() == true)
        {
            throw new InvalidOperationException("kill_switch=true");
        }

        var testCase = ReadCase(draftPath);
        return Append(testCase.Id, testCase.Axis, draftPath, "", BenchmarkCaseReviewState.PendingReview, reviewer, notes, nowUtc);
    }

    public BenchmarkCaseReviewRecord Approve(string draftPath, string approvedRoot, string reviewer, string notes = "", DateTimeOffset? nowUtc = null)
    {
        if (_killSwitchService?.IsActive() == true)
        {
            throw new InvalidOperationException("kill_switch=true");
        }

        var testCase = ReadCase(draftPath);
        var approvedAxisRoot = Path.Combine(Path.GetFullPath(approvedRoot), testCase.Axis);
        Directory.CreateDirectory(approvedAxisRoot);
        var approvedPath = Path.Combine(approvedAxisRoot, Path.GetFileName(draftPath));
        if (File.Exists(approvedPath))
        {
            throw new InvalidOperationException("Approved benchmark cases are immutable; refusing to overwrite.");
        }

        File.Move(draftPath, approvedPath);
        File.SetAttributes(approvedPath, File.GetAttributes(approvedPath) | FileAttributes.ReadOnly);
        var record = Append(testCase.Id, testCase.Axis, draftPath, approvedPath, BenchmarkCaseReviewState.Approved, reviewer, notes, nowUtc);
        RecordPacket(CaseApprovedPacketKind, approvedPath, $"Approved benchmark case {testCase.Id}.", record.CreatedAtUtc);
        return record;
    }

    public BenchmarkCaseReviewRecord RecordApproved(string approvedPath, string reviewer, string notes = "", DateTimeOffset? nowUtc = null)
    {
        if (_killSwitchService?.IsActive() == true)
        {
            throw new InvalidOperationException("kill_switch=true");
        }

        var testCase = ReadCase(approvedPath);
        var record = Append(testCase.Id, testCase.Axis, "", approvedPath, BenchmarkCaseReviewState.Approved, reviewer, notes, nowUtc);
        RecordPacket(CaseApprovedPacketKind, approvedPath, $"Approved benchmark case {testCase.Id}.", record.CreatedAtUtc);
        return record;
    }

    public BenchmarkCaseReviewRecord Reject(string draftPath, string reviewer, string notes = "", DateTimeOffset? nowUtc = null)
    {
        if (_killSwitchService?.IsActive() == true)
        {
            throw new InvalidOperationException("kill_switch=true");
        }

        var testCase = ReadCase(draftPath);
        File.Delete(draftPath);
        var record = Append(testCase.Id, testCase.Axis, draftPath, "", BenchmarkCaseReviewState.Rejected, reviewer, notes, nowUtc);
        RecordPacket(CaseRejectedPacketKind, draftPath, $"Rejected benchmark case {testCase.Id}.", record.CreatedAtUtc);
        return record;
    }

    public BenchmarkCaseReviewRecord Revise(string draftPath, BenchmarkCase revisedCase, string reviewer, string notes = "", DateTimeOffset? nowUtc = null)
    {
        if (_killSwitchService?.IsActive() == true)
        {
            throw new InvalidOperationException("kill_switch=true");
        }

        var existing = ReadCase(draftPath);
        if (!existing.Id.Equals(revisedCase.Id, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Revision cannot change benchmark case id.");
        }

        File.WriteAllText(draftPath, JsonSerializer.Serialize(revisedCase, JsonDefaults.Options));
        var record = Append(revisedCase.Id, revisedCase.Axis, draftPath, "", BenchmarkCaseReviewState.Revised, reviewer, notes, nowUtc);
        RecordPacket(CaseRevisedPacketKind, draftPath, $"Revised benchmark case {revisedCase.Id}.", record.CreatedAtUtc);
        return record;
    }

    public IReadOnlyList<BenchmarkCaseReviewRecord> ListLatest()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_databasePath) ?? ".");
        using var connection = OpenConnection();
        Initialize(connection);
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id, case_id, axis, draft_path, approved_path, state, reviewer, notes, created_at_utc
            FROM benchmark_case_reviews
            ORDER BY created_at_utc DESC, id DESC;
            """;
        var rows = new List<BenchmarkCaseReviewRecord>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            rows.Add(ReadRecord(reader));
        }

        return rows;
    }

    public void AssertAppendOnlyGuards()
    {
        using var connection = OpenConnection();
        Initialize(connection);
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT name FROM sqlite_master
            WHERE type='trigger' AND name IN ('benchmark_case_reviews_no_update', 'benchmark_case_reviews_no_delete');
            """;
        using var reader = command.ExecuteReader();
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while (reader.Read())
        {
            names.Add(Convert.ToString(reader["name"], CultureInfo.InvariantCulture) ?? "");
        }

        if (!names.Contains("benchmark_case_reviews_no_update") || !names.Contains("benchmark_case_reviews_no_delete"))
        {
            throw new InvalidOperationException("Benchmark curation append-only guards are missing.");
        }
    }

    private BenchmarkCaseReviewRecord Append(string caseId, string axis, string draftPath, string approvedPath, BenchmarkCaseReviewState state, string reviewer, string notes, DateTimeOffset? nowUtc)
    {
        var createdAt = nowUtc ?? DateTimeOffset.UtcNow;
        Directory.CreateDirectory(Path.GetDirectoryName(_databasePath) ?? ".");
        using var connection = OpenConnection();
        Initialize(connection);
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO benchmark_case_reviews(case_id, axis, draft_path, approved_path, state, reviewer, notes, created_at_utc)
            VALUES($case_id, $axis, $draft_path, $approved_path, $state, $reviewer, $notes, $created_at_utc);
            SELECT last_insert_rowid();
            """;
        var normalizedDraftPath = string.IsNullOrWhiteSpace(draftPath) ? "" : Path.GetFullPath(draftPath);
        var normalizedApprovedPath = string.IsNullOrWhiteSpace(approvedPath) ? "" : Path.GetFullPath(approvedPath);
        command.Parameters.AddWithValue("$case_id", caseId);
        command.Parameters.AddWithValue("$axis", axis);
        command.Parameters.AddWithValue("$draft_path", normalizedDraftPath);
        command.Parameters.AddWithValue("$approved_path", normalizedApprovedPath);
        command.Parameters.AddWithValue("$state", state.ToString());
        command.Parameters.AddWithValue("$reviewer", string.IsNullOrWhiteSpace(reviewer) ? "user" : reviewer.Trim());
        command.Parameters.AddWithValue("$notes", notes ?? "");
        command.Parameters.AddWithValue("$created_at_utc", createdAt.ToString("O", CultureInfo.InvariantCulture));
        var id = Convert.ToInt64(command.ExecuteScalar(), CultureInfo.InvariantCulture);
        return new BenchmarkCaseReviewRecord(id, caseId, axis, normalizedDraftPath, normalizedApprovedPath, state, string.IsNullOrWhiteSpace(reviewer) ? "user" : reviewer.Trim(), notes ?? "", createdAt);
    }

    private void RecordPacket(string kind, string artifactPath, string summary, DateTimeOffset createdAt)
    {
        _auditLedgerService?.Record(new EvidencePacket(
            Guid.NewGuid(),
            kind,
            null,
            createdAt,
            DidUseNetwork: false,
            DidUseHostedAi: false,
            DidUseLocalModel: false,
            DidMutate: true,
            artifactPath,
            summary,
            "Completed"));
    }

    private static BenchmarkCase ReadCase(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Benchmark draft case was not found.", path);
        }

        return JsonSerializer.Deserialize<BenchmarkCase>(File.ReadAllText(path), JsonDefaults.Options)
            ?? throw new InvalidOperationException("Benchmark case could not be parsed.");
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
            CREATE TABLE IF NOT EXISTS benchmark_case_reviews(
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                case_id TEXT NOT NULL,
                axis TEXT NOT NULL,
                draft_path TEXT NOT NULL,
                approved_path TEXT NOT NULL DEFAULT '',
                state TEXT NOT NULL,
                reviewer TEXT NOT NULL,
                notes TEXT NOT NULL DEFAULT '',
                created_at_utc TEXT NOT NULL
            );
            CREATE TRIGGER IF NOT EXISTS benchmark_case_reviews_no_update
            BEFORE UPDATE ON benchmark_case_reviews
            BEGIN
                SELECT RAISE(ABORT, 'benchmark curation review log is append-only');
            END;
            CREATE TRIGGER IF NOT EXISTS benchmark_case_reviews_no_delete
            BEFORE DELETE ON benchmark_case_reviews
            BEGIN
                SELECT RAISE(ABORT, 'benchmark curation review log is append-only');
            END;
            """;
        command.ExecuteNonQuery();
    }

    private static BenchmarkCaseReviewRecord ReadRecord(SqliteDataReader reader)
    {
        return new BenchmarkCaseReviewRecord(
            reader.GetInt64(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetString(3),
            reader.GetString(4),
            Enum.Parse<BenchmarkCaseReviewState>(reader.GetString(5)),
            reader.GetString(6),
            reader.GetString(7),
            DateTimeOffset.Parse(reader.GetString(8), CultureInfo.InvariantCulture));
    }
}
