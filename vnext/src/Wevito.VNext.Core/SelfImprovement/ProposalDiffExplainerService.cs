using System.Globalization;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Wevito.VNext.Core.Audit;

namespace Wevito.VNext.Core.SelfImprovement;

public sealed class ProposalDiffExplainerService
{
    private static readonly ProposalDiffExplanation Empty = new(
        "",
        [],
        [],
        0,
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
        "",
        "",
        IsBlocked: true,
        BlockReason: "missing_input");

    private readonly string _databasePath;
    private readonly KillSwitchService? _killSwitch;
    private readonly Action<string>? _commandObserver;

    public ProposalDiffExplainerService(
        string databasePath,
        KillSwitchService? killSwitch = null,
        Action<string>? commandObserver = null)
    {
        _databasePath = Path.GetFullPath(databasePath);
        _killSwitch = killSwitch;
        _commandObserver = commandObserver;
    }

    public ProposalDiffExplanation Explain(string operationId)
    {
        if (_killSwitch?.IsActive() == true)
        {
            return Blocked("kill_switch=true");
        }

        if (string.IsNullOrWhiteSpace(operationId) || !File.Exists(_databasePath))
        {
            return Empty;
        }

        var row = ReadLatestAwaitingApprovalRow(operationId.Trim());
        if (row is null)
        {
            return Blocked("apply_awaiting_approval_row_not_found");
        }

        var awaitingPath = Canonicalize(row.ArtifactPath);
        if (string.IsNullOrWhiteSpace(awaitingPath) || !IsUnderVNextArtifacts(awaitingPath))
        {
            return Blocked("artifact_path_outside_allowed_root");
        }

        if (!TryReadJson(awaitingPath, out var awaiting))
        {
            return Blocked("artifact_json_not_found_or_invalid");
        }

        using (awaiting)
        {
            var root = awaiting.RootElement;
            var proposalPath = GetString(root, "proposalPath", "proposal_path");
            var dryRunPath = GetString(root, "dryRunPath", "dry_run_path");
            var evalPath = GetString(root, "evalPath", "eval_path");
            var scopeHash = GetString(root, "scopeHash", "scope_hash");
            var manifestHash = GetString(root, "experimentManifestHash", "experiment_manifest_hash");
            var operation = GetString(root, "operationId", "operation_id");

            return new ProposalDiffExplanation(
                string.IsNullOrWhiteSpace(operation) ? operationId.Trim() : operation,
                ReadStringArrayArtifact(proposalPath, "sourcePaths", "source_paths"),
                ReadStringArrayArtifact(proposalPath, "tools", "recommendedTools", "RecommendedTools"),
                ReadMutationCount(dryRunPath),
                ReadEvalGateStatuses(evalPath),
                scopeHash,
                manifestHash,
                IsBlocked: false,
                BlockReason: "");
        }
    }

    private AwaitingApprovalRow? ReadLatestAwaitingApprovalRow(string operationId)
    {
        using var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = _databasePath,
            Mode = SqliteOpenMode.ReadOnly
        }.ToString());
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT artifact_path
            FROM audit_ledger
            WHERE packet_kind = $packet_kind
              AND summary LIKE $operation_pattern
            ORDER BY created_at_utc DESC, id DESC
            LIMIT 1;
            """;
        _commandObserver?.Invoke(command.CommandText);
        command.Parameters.AddWithValue("$packet_kind", SelfImprovementPacketKinds.ApplyAwaitingApproval);
        command.Parameters.AddWithValue("$operation_pattern", $"%{EscapeLike(operationId)}%");
        using var reader = command.ExecuteReader();
        return reader.Read()
            ? new AwaitingApprovalRow(Convert.ToString(reader["artifact_path"], CultureInfo.InvariantCulture) ?? "")
            : null;
    }

    private static IReadOnlyList<string> ReadStringArrayArtifact(string path, params string[] propertyNames)
    {
        var safePath = Canonicalize(path);
        if (string.IsNullOrWhiteSpace(safePath) || !IsUnderVNextArtifacts(safePath) || !TryReadJson(safePath, out var document))
        {
            return [];
        }

        using (document)
        {
            foreach (var propertyName in propertyNames)
            {
                if (!TryGetProperty(document.RootElement, propertyName, out var property) ||
                    property.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                return property.EnumerateArray()
                    .Where(item => item.ValueKind == JsonValueKind.String)
                    .Select(item => item.GetString() ?? "")
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .ToArray();
            }
        }

        return [];
    }

    private static int ReadMutationCount(string path)
    {
        var safePath = Canonicalize(path);
        if (string.IsNullOrWhiteSpace(safePath) || !IsUnderVNextArtifacts(safePath) || !TryReadJson(safePath, out var document))
        {
            return 0;
        }

        using (document)
        {
            if (TryGetProperty(document.RootElement, "mutations", out var mutations) &&
                mutations.ValueKind == JsonValueKind.Number &&
                mutations.TryGetInt32(out var count))
            {
                return count;
            }
        }

        return 0;
    }

    private static IReadOnlyDictionary<string, string> ReadEvalGateStatuses(string path)
    {
        var safePath = Canonicalize(path);
        if (string.IsNullOrWhiteSpace(safePath) || !IsUnderVNextArtifacts(safePath) || !TryReadJson(safePath, out var document))
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        using (document)
        {
            if (!TryGetProperty(document.RootElement, "results", out var results) ||
                results.ValueKind != JsonValueKind.Object)
            {
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }

            return results.EnumerateObject()
                .Where(gate => gate.Value.ValueKind == JsonValueKind.Object)
                .Select(gate => new
                {
                    gate.Name,
                    Status = TryGetProperty(gate.Value, "status", out var status) && status.ValueKind == JsonValueKind.String
                        ? status.GetString() ?? ""
                        : ""
                })
                .Where(gate => !string.IsNullOrWhiteSpace(gate.Status))
                .ToDictionary(gate => gate.Name, gate => gate.Status, StringComparer.OrdinalIgnoreCase);
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

    private static bool TryReadJson(string path, out JsonDocument document)
    {
        document = null!;
        try
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                return false;
            }

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
    }

    private static string Canonicalize(string path)
    {
        return string.IsNullOrWhiteSpace(path) ? "" : Path.GetFullPath(path);
    }

    private static bool IsUnderVNextArtifacts(string fullPath)
    {
        var parts = fullPath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        for (var index = 0; index < parts.Length - 1; index++)
        {
            if (string.Equals(parts[index], "vnext", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(parts[index + 1], "artifacts", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static string EscapeLike(string value)
    {
        return value.Replace("[", "[[]", StringComparison.Ordinal)
            .Replace("%", "[%]", StringComparison.Ordinal)
            .Replace("_", "[_]", StringComparison.Ordinal);
    }

    private static ProposalDiffExplanation Blocked(string reason)
    {
        return Empty with { BlockReason = reason };
    }

    private sealed record AwaitingApprovalRow(string ArtifactPath);
}
