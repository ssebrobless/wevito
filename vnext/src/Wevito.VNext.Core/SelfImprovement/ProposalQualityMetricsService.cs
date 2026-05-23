using System.Globalization;
using System.Reflection;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Wevito.VNext.Core.Audit;
using Wevito.VNext.Core.SelfImprovement.Eval;
using Wevito.VNext.Core.SelfImprovement.Invariants;

namespace Wevito.VNext.Core.SelfImprovement;

public sealed class ProposalQualityMetricsService
{
    public const string WatchdogObserverEnabledSetting = "snapshot_v0_invariant_observer_in_proposal_quality_metrics_enabled";

    private readonly string _databasePath;
    private readonly string _artifactRoot;
    private readonly KillSwitchService? _killSwitch;
    private readonly InvariantViolationWatchdog? _watchdog;
    private readonly Func<IReadOnlyDictionary<string, string>>? _settingsProvider;

    public ProposalQualityMetricsService(
        string databasePath,
        string artifactRoot,
        KillSwitchService? killSwitch = null,
        InvariantViolationWatchdog? watchdog = null,
        Func<IReadOnlyDictionary<string, string>>? settingsProvider = null)
    {
        _databasePath = Path.GetFullPath(databasePath);
        _artifactRoot = Path.GetFullPath(artifactRoot);
        _killSwitch = killSwitch;
        _watchdog = watchdog;
        _settingsProvider = settingsProvider;
    }

    public ProposalQualityMetricsSnapshot Snapshot(string operationId, DateTimeOffset nowUtc)
    {
        var normalizedOperationId = string.IsNullOrWhiteSpace(operationId) ? "" : operationId.Trim();
        if (_killSwitch?.IsActive() == true)
        {
            return Empty(normalizedOperationId, nowUtc, "kill_switch=true");
        }

        if (string.IsNullOrWhiteSpace(normalizedOperationId) || !File.Exists(_databasePath))
        {
            return Empty(normalizedOperationId, nowUtc, "no_rows");
        }

        using var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = _databasePath,
            Mode = SqliteOpenMode.ReadOnly
        }.ToString());
        connection.Open();

        var packetKinds = SelfImprovementPacketKindValues();
        var counts = packetKinds.ToDictionary(kind => kind, kind => CountRows(connection, kind, normalizedOperationId), StringComparer.Ordinal);
        if (counts.Values.All(count => count == 0))
        {
            return Empty(normalizedOperationId, nowUtc, "no_rows", counts);
        }

        var latestAwaiting = ReadLatestRow(connection, SelfImprovementPacketKinds.ApplyAwaitingApproval, normalizedOperationId);
        var latestJudge = ReadLatestRow(connection, SelfImprovementPacketKinds.JudgeCritique, normalizedOperationId);
        var latestEval = ReadLatestRow(connection, SelfImprovementPacketKinds.EvalCompleted, normalizedOperationId);
        var scopeHash = ReadScopeHash(latestAwaiting);
        var judge = ReadJudgeCounts(latestJudge);
        var evalGates = ReadEvalGates(latestEval);
        var snapshotAge = ReadLatestSnapshotAgeDays(normalizedOperationId, nowUtc);
        var replayResult = ReadLatestReplayResultKind(normalizedOperationId);
        var refusedCount = CountApplyRefusedNotImplemented(connection, normalizedOperationId);
        var reason = ComputeReason(latestEval, evalGates.ArtifactMissing);

        if (_watchdog is not null && _settingsProvider is not null)
        {
            var settingsForObserver = _settingsProvider();
            if (settingsForObserver.TryGetValue(WatchdogObserverEnabledSetting, out var observerEnabled) &&
                string.Equals(observerEnabled, bool.TrueString, StringComparison.OrdinalIgnoreCase))
            {
                _watchdog.ScanAndEmit(nowUtc);
            }
        }

        return new ProposalQualityMetricsSnapshot(
            normalizedOperationId,
            counts,
            IsLowerHex64(scopeHash),
            judge.RulesEvaluated,
            judge.RulesPassed,
            snapshotAge,
            replayResult,
            evalGates.Present,
            evalGates.Missing,
            latestAwaiting?.Status ?? "",
            refusedCount,
            nowUtc,
            reason);
    }

    private static ProposalQualityMetricsSnapshot Empty(
        string operationId,
        DateTimeOffset nowUtc,
        string reason,
        IReadOnlyDictionary<string, int>? counts = null)
    {
        return new ProposalQualityMetricsSnapshot(
            operationId,
            counts ?? new Dictionary<string, int>(StringComparer.Ordinal),
            false,
            null,
            null,
            null,
            "none",
            [],
            EvalGateManifest.Default().Gates,
            "",
            0,
            nowUtc,
            reason);
    }

    private static IReadOnlyList<string> SelfImprovementPacketKindValues()
    {
        return typeof(SelfImprovementPacketKinds)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.IsLiteral && field.FieldType == typeof(string))
            .Select(field => (string)field.GetRawConstantValue()!)
            .Order(StringComparer.Ordinal)
            .ToArray();
    }

    private static int CountRows(SqliteConnection connection, string packetKind, string operationId)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(*)
            FROM audit_ledger
            WHERE packet_kind = $packet_kind
              AND summary LIKE $operation_id;
            """;
        command.Parameters.AddWithValue("$packet_kind", packetKind);
        command.Parameters.AddWithValue("$operation_id", $"%{operationId}%");
        return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture);
    }

    private static int CountApplyRefusedNotImplemented(SqliteConnection connection, string operationId)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(*)
            FROM audit_ledger
            WHERE packet_kind = $packet_kind
              AND error = $error
              AND summary LIKE $operation_id;
            """;
        command.Parameters.AddWithValue("$packet_kind", SelfImprovementPacketKinds.ApplyRefused);
        command.Parameters.AddWithValue("$error", SupervisedImprovementLoop.ApplyRunnerNotImplementedReason);
        command.Parameters.AddWithValue("$operation_id", $"%{operationId}%");
        return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture);
    }

    private static MetricsRow? ReadLatestRow(SqliteConnection connection, string packetKind, string operationId)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT artifact_path, summary, status, error, created_at_utc
            FROM audit_ledger
            WHERE packet_kind = $packet_kind
              AND summary LIKE $operation_id
            ORDER BY created_at_utc DESC, id DESC
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("$packet_kind", packetKind);
        command.Parameters.AddWithValue("$operation_id", $"%{operationId}%");
        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }

        return new MetricsRow(
            Convert.ToString(reader["artifact_path"], CultureInfo.InvariantCulture) ?? "",
            Convert.ToString(reader["summary"], CultureInfo.InvariantCulture) ?? "",
            Convert.ToString(reader["status"], CultureInfo.InvariantCulture) ?? "",
            Convert.ToString(reader["error"], CultureInfo.InvariantCulture) ?? "",
            DateTimeOffset.TryParse(Convert.ToString(reader["created_at_utc"], CultureInfo.InvariantCulture), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed)
                ? parsed
                : DateTimeOffset.MinValue);
    }

    private static string ReadScopeHash(MetricsRow? row)
    {
        if (row is null)
        {
            return "";
        }

        var fromSummary = TryReadStringFromJson(row.Summary, "scopeHash", "scope_hash");
        if (!string.IsNullOrWhiteSpace(fromSummary))
        {
            return fromSummary;
        }

        return File.Exists(row.ArtifactPath)
            ? TryReadStringFromJson(File.ReadAllText(row.ArtifactPath), "scopeHash", "scope_hash")
            : "";
    }

    private static (int? RulesEvaluated, int? RulesPassed) ReadJudgeCounts(MetricsRow? row)
    {
        if (row is null || string.IsNullOrWhiteSpace(row.Summary))
        {
            return (null, null);
        }

        try
        {
            using var document = JsonDocument.Parse(row.Summary);
            return (
                TryReadInt(document.RootElement, "rules_evaluated"),
                TryReadInt(document.RootElement, "rules_passed"));
        }
        catch (JsonException)
        {
            return (null, null);
        }
    }

    private static EvalGateReadResult ReadEvalGates(MetricsRow? row)
    {
        var expected = EvalGateManifest.Default().Gates;
        if (row is null || string.IsNullOrWhiteSpace(row.ArtifactPath) || !File.Exists(row.ArtifactPath))
        {
            return new EvalGateReadResult([], expected, row is not null);
        }

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(row.ArtifactPath));
            if (!document.RootElement.TryGetProperty("results", out var results) ||
                results.ValueKind != JsonValueKind.Object)
            {
                return new EvalGateReadResult([], expected, false);
            }

            var present = results.EnumerateObject()
                .Select(property => property.Name)
                .Where(name => expected.Contains(name, StringComparer.Ordinal))
                .Order(StringComparer.Ordinal)
                .ToArray();
            var missing = expected
                .Where(gate => !present.Contains(gate, StringComparer.Ordinal))
                .ToArray();
            return new EvalGateReadResult(present, missing, false);
        }
        catch (JsonException)
        {
            return new EvalGateReadResult([], expected, false);
        }
    }

    private double? ReadLatestSnapshotAgeDays(string operationId, DateTimeOffset nowUtc)
    {
        var snapshot = EnumerateArtifactFiles("snapshot*.json")
            .Where(path => File.ReadAllText(path).Contains(operationId, StringComparison.Ordinal))
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .FirstOrDefault();
        return snapshot is null ? null : Math.Max(0, (nowUtc.UtcDateTime - File.GetLastWriteTimeUtc(snapshot)).TotalDays);
    }

    private string ReadLatestReplayResultKind(string operationId)
    {
        var replay = EnumerateArtifactFiles("replay-result.json")
            .Where(path => Path.GetFullPath(path).Contains(operationId, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .FirstOrDefault();
        if (replay is null)
        {
            return "none";
        }

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(replay));
            return TryReadString(document.RootElement, "ResultKind", "resultKind", "result_kind") switch
            {
                "Identical" => "Identical",
                "Diverged" => "Diverged",
                "NotApplicable" => "NotApplicable",
                _ => "none"
            };
        }
        catch (JsonException)
        {
            return "none";
        }
    }

    private IEnumerable<string> EnumerateArtifactFiles(string pattern)
    {
        if (!Directory.Exists(_artifactRoot))
        {
            return [];
        }

        return Directory.EnumerateFiles(_artifactRoot, pattern, SearchOption.AllDirectories)
            .Where(path => Path.GetFullPath(path).StartsWith(_artifactRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase));
    }

    private static string ComputeReason(MetricsRow? latestEval, bool evalArtifactMissing)
    {
        if (evalArtifactMissing)
        {
            return "artifact missing";
        }

        return latestEval is null ? "packet missing" : "ok";
    }

    private static string TryReadStringFromJson(string json, params string[] names)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return "";
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            return TryReadString(document.RootElement, names);
        }
        catch (JsonException)
        {
            return "";
        }
    }

    private static string TryReadString(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (element.TryGetProperty(name, out var property))
            {
                return property.ValueKind == JsonValueKind.String ? property.GetString() ?? "" : property.ToString();
            }
        }

        return "";
    }

    private static int? TryReadInt(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var property))
        {
            return null;
        }

        return property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out var value)
            ? value
            : null;
    }

    private static bool IsLowerHex64(string value)
    {
        return value.Length == 64 && value.All(character => character is >= '0' and <= '9' or >= 'a' and <= 'f');
    }

    private sealed record MetricsRow(string ArtifactPath, string Summary, string Status, string Error, DateTimeOffset CreatedAtUtc);

    private sealed record EvalGateReadResult(
        IReadOnlyList<string> Present,
        IReadOnlyList<string> Missing,
        bool ArtifactMissing);
}
