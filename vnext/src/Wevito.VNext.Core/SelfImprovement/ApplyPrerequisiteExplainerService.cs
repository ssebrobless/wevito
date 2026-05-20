using System.Globalization;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Wevito.VNext.Core.Audit;

namespace Wevito.VNext.Core.SelfImprovement;

public sealed class ApplyPrerequisiteExplainerService
{
    public const string NoPacketReason = "no prerequisite check packet found";
    public const string DetailsUnavailableReason = "per-entry details unavailable; run the prerequisite check service to regenerate";

    private static readonly IReadOnlyDictionary<string, string> KnownPlainLanguage = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["KillSwitch armed"] = "Stop Everything must be off so we can react to a halt.",
        ["EvalGateRunner v1 enabled"] = "The cheap deterministic eval gates must be enabled before any apply.",
        ["Heuristic judge enabled"] = "The deterministic critique must have passed for the current proposal.",
        ["Snapshot signed and verified recently"] = "A signed snapshot of recent self-improvement audit rows must verify.",
        ["Held-out store contains >= 1 case"] = "At least one held-out evaluation case must exist.",
        ["In-distribution store contains >= 1 case"] = "At least one in-distribution evaluation case must exist.",
        ["Scope hash matches latest awaiting-approval artifact"] = "The proposal artifacts' content hashes must still match the live files.",
        ["Replay run within window"] = "A recent deterministic replay must have returned Identical.",
        ["Capability default-off audit"] = "No capability flag is unexpectedly enabled outside the documented allowlist.",
        ["Apply runner declared not implemented"] = "The v0 apply runner must remain explicitly not implemented."
    };

    private readonly string _databasePath;
    private readonly string _artifactRoot;
    private readonly KillSwitchService? _killSwitch;
    private readonly Action<string>? _commandObserver;

    public ApplyPrerequisiteExplainerService(
        string databasePath,
        string artifactRoot,
        KillSwitchService? killSwitch = null,
        Action<string>? commandObserver = null)
    {
        _databasePath = Path.GetFullPath(databasePath);
        _artifactRoot = Path.GetFullPath(artifactRoot);
        _killSwitch = killSwitch;
        _commandObserver = commandObserver;
    }

    public ApplyPrerequisiteExplanation Explain(string operationId, DateTimeOffset nowUtc)
    {
        var normalizedOperationId = string.IsNullOrWhiteSpace(operationId) ? "" : operationId.Trim();
        if (_killSwitch?.IsActive() == true)
        {
            return Empty(normalizedOperationId, nowUtc, "kill_switch=true");
        }

        if (string.IsNullOrWhiteSpace(normalizedOperationId) || !File.Exists(_databasePath))
        {
            return Empty(normalizedOperationId, nowUtc, NoPacketReason);
        }

        var row = ReadLatestPrerequisiteRow(normalizedOperationId);
        if (row is null)
        {
            return Empty(normalizedOperationId, nowUtc, NoPacketReason);
        }

        var allPassed = ReadAllPassed(row.Summary);
        var artifactPath = FindPrerequisiteArtifact(normalizedOperationId);
        if (string.IsNullOrWhiteSpace(artifactPath) ||
            !ReadEntriesFromArtifact(artifactPath, normalizedOperationId, out var entries, out var artifactAllPassed))
        {
            return new ApplyPrerequisiteExplanation(
                normalizedOperationId,
                [],
                allPassed,
                nowUtc,
                DetailsUnavailableReason);
        }

        return new ApplyPrerequisiteExplanation(
            normalizedOperationId,
            entries,
            artifactAllPassed ?? allPassed,
            nowUtc,
            "");
    }

    private AuditLedgerRow? ReadLatestPrerequisiteRow(string operationId)
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
            WHERE packet_kind = $packet_kind
              AND summary LIKE $operation_pattern
            ORDER BY created_at_utc DESC, id DESC
            LIMIT 1;
            """;
        _commandObserver?.Invoke(command.CommandText);
        command.Parameters.AddWithValue("$packet_kind", SelfImprovementPacketKinds.ApplyPrerequisiteCheck);
        command.Parameters.AddWithValue("$operation_pattern", $"%{EscapeLike(operationId)}%");
        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }

        var taskCard = Convert.ToString(reader["task_card_id"], CultureInfo.InvariantCulture);
        return new AuditLedgerRow(
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
            Convert.ToString(reader["error"], CultureInfo.InvariantCulture) ?? "");
    }

    private string? FindPrerequisiteArtifact(string operationId)
    {
        if (!Directory.Exists(_artifactRoot))
        {
            return null;
        }

        return Directory.EnumerateFiles(_artifactRoot, "prerequisite-check.json", SearchOption.AllDirectories)
            .Select(Path.GetFullPath)
            .Where(IsUnderArtifactRoot)
            .Select(path => new { Path = path, Modified = File.GetLastWriteTimeUtc(path), OperationMatches = ArtifactMatchesOperation(path, operationId) })
            .Where(candidate => candidate.OperationMatches)
            .OrderByDescending(candidate => candidate.Modified)
            .Select(candidate => candidate.Path)
            .FirstOrDefault();
    }

    private bool ArtifactMatchesOperation(string path, string operationId)
    {
        if (path.Contains(operationId, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (!TryReadJson(path, out var document))
        {
            return false;
        }

        using (document)
        {
            var artifactOperationId = GetString(document.RootElement, "operationId", "operation_id");
            return artifactOperationId.Equals(operationId, StringComparison.Ordinal);
        }
    }

    private static bool ReadEntriesFromArtifact(
        string path,
        string operationId,
        out IReadOnlyList<ApplyPrerequisiteExplanationEntry> entries,
        out bool? allPassed)
    {
        entries = [];
        allPassed = null;
        if (!TryReadJson(path, out var document))
        {
            return false;
        }

        using (document)
        {
            var root = document.RootElement;
            var artifactOperationId = GetString(root, "operationId", "operation_id");
            if (!string.IsNullOrWhiteSpace(artifactOperationId) &&
                !artifactOperationId.Equals(operationId, StringComparison.Ordinal))
            {
                return false;
            }

            allPassed = GetBool(root, "allPassed", "all_passed");
            if (!TryGetProperty(root, "entries", out var entryArray) &&
                !TryGetProperty(root, "checks", out entryArray))
            {
                return false;
            }

            if (entryArray.ValueKind != JsonValueKind.Array)
            {
                return false;
            }

            entries = entryArray.EnumerateArray()
                .Where(entry => entry.ValueKind == JsonValueKind.Object)
                .Select(entry =>
                {
                    var name = GetString(entry, "name", "Name");
                    var detail = GetString(entry, "detail", "Detail");
                    var passed = GetBool(entry, "passed", "Passed") ?? false;
                    return new ApplyPrerequisiteExplanationEntry(
                        name,
                        passed,
                        detail,
                        KnownPlainLanguage.TryGetValue(name, out var plainLanguage) ? plainLanguage : "");
                })
                .Where(entry => !string.IsNullOrWhiteSpace(entry.Name))
                .ToArray();
            return true;
        }
    }

    private static bool ReadAllPassed(string summary)
    {
        if (string.IsNullOrWhiteSpace(summary))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(summary);
            return GetBool(document.RootElement, "allPassed", "all_passed") ?? false;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private bool IsUnderArtifactRoot(string fullPath)
    {
        return fullPath.Equals(_artifactRoot, StringComparison.OrdinalIgnoreCase) ||
               fullPath.StartsWith(_artifactRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryReadJson(string path, out JsonDocument document)
    {
        document = null!;
        try
        {
            document = JsonDocument.Parse(File.ReadAllText(path));
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static string GetString(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (TryGetProperty(element, name, out var property) && property.ValueKind == JsonValueKind.String)
            {
                return property.GetString() ?? "";
            }
        }

        return "";
    }

    private static bool? GetBool(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (TryGetProperty(element, name, out var property))
            {
                if (property.ValueKind is JsonValueKind.True or JsonValueKind.False)
                {
                    return property.GetBoolean();
                }

                if (property.ValueKind == JsonValueKind.String &&
                    bool.TryParse(property.GetString(), out var parsed))
                {
                    return parsed;
                }
            }
        }

        return null;
    }

    private static bool TryGetProperty(JsonElement element, string name, out JsonElement property)
    {
        if (element.TryGetProperty(name, out property))
        {
            return true;
        }

        foreach (var candidate in element.EnumerateObject())
        {
            if (string.Equals(candidate.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                property = candidate.Value;
                return true;
            }
        }

        property = default;
        return false;
    }

    private static string EscapeLike(string value)
    {
        return value.Replace("[", "[[]", StringComparison.Ordinal)
            .Replace("%", "[%]", StringComparison.Ordinal)
            .Replace("_", "[_]", StringComparison.Ordinal);
    }

    private static ApplyPrerequisiteExplanation Empty(string operationId, DateTimeOffset nowUtc, string reason)
    {
        return new ApplyPrerequisiteExplanation(operationId, [], false, nowUtc, reason);
    }
}
