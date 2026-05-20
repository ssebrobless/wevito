using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Wevito.VNext.Contracts;
using Wevito.VNext.Core.Audit;

namespace Wevito.VNext.Core.SelfImprovement.Scoring;

public sealed class SupervisedScoringDryRunService
{
    public const string EnabledSetting = "supervised_scoring_dry_run_enabled";
    public const string RubricId = "supervised_self_improvement_dry_run_v1";

    private readonly string _databasePath;
    private readonly AuditLedgerService _ledger;
    private readonly ILocalScoringProvider _provider;
    private readonly KillSwitchService? _killSwitch;
    private readonly Func<IReadOnlyDictionary<string, string>> _settingsProvider;

    public SupervisedScoringDryRunService(
        string databasePath,
        AuditLedgerService ledger,
        ILocalScoringProvider provider,
        KillSwitchService? killSwitch = null,
        Func<IReadOnlyDictionary<string, string>>? settingsProvider = null)
    {
        _databasePath = Path.GetFullPath(databasePath);
        _ledger = ledger;
        _provider = provider;
        _killSwitch = killSwitch;
        _settingsProvider = settingsProvider ?? (() => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
    }

    public SupervisedScoringDryRunResult Run(string operationId, DateTimeOffset nowUtc, CancellationToken cancellationToken)
    {
        var normalizedOperationId = string.IsNullOrWhiteSpace(operationId) ? "" : operationId.Trim();
        if (_killSwitch?.IsActive() == true)
        {
            return Refused(normalizedOperationId, "kill_switch=true", nowUtc);
        }

        var settings = _settingsProvider();
        if (!IsTrue(settings, EnabledSetting))
        {
            return Refused(normalizedOperationId, "supervised_scoring_dry_run_enabled=false", nowUtc);
        }

        var awaiting = ReadLatestAwaitingApproval(normalizedOperationId);
        if (awaiting is null)
        {
            return Refused(normalizedOperationId, "no_awaiting_approval_packet", nowUtc);
        }

        var scopeHash = ReadScopeHash(awaiting.ArtifactPath);
        var request = new LocalScoringRequest(
            ComputePromptSha256(normalizedOperationId, scopeHash),
            RubricId);
        var score = _provider.Score(request, cancellationToken);
        var result = score switch
        {
            LocalScoringResult.Scored scored => new SupervisedScoringDryRunResult(
                normalizedOperationId,
                "Scored",
                "scored",
                scored.ModelIdentity,
                nowUtc),
            LocalScoringResult.Refused refused => new SupervisedScoringDryRunResult(
                normalizedOperationId,
                "Refused",
                refused.Reason,
                "",
                nowUtc),
            _ => new SupervisedScoringDryRunResult(
                normalizedOperationId,
                "Refused",
                "unknown_scoring_result",
                "",
                nowUtc)
        };

        Record(awaiting.TaskCardId, result, nowUtc);
        return result;
    }

    private AwaitingApprovalRow? ReadLatestAwaitingApproval(string operationId)
    {
        if (string.IsNullOrWhiteSpace(operationId) || !File.Exists(_databasePath))
        {
            return null;
        }

        using var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = _databasePath,
            Mode = SqliteOpenMode.ReadOnly
        }.ToString());
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT task_card_id, artifact_path
            FROM audit_ledger
            WHERE packet_kind = $packet_kind
              AND summary LIKE $operation_id
            ORDER BY created_at_utc DESC, id DESC
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("$packet_kind", SelfImprovementPacketKinds.ApplyAwaitingApproval);
        command.Parameters.AddWithValue("$operation_id", $"%{operationId}%");

        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }

        var taskCardText = Convert.ToString(reader["task_card_id"], CultureInfo.InvariantCulture) ?? "";
        return new AwaitingApprovalRow(
            Guid.TryParse(taskCardText, out var taskCardId) ? taskCardId : null,
            Convert.ToString(reader["artifact_path"], CultureInfo.InvariantCulture) ?? "");
    }

    private static string ReadScopeHash(string artifactPath)
    {
        if (string.IsNullOrWhiteSpace(artifactPath) || !File.Exists(artifactPath))
        {
            return "";
        }

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(artifactPath));
            return GetString(document.RootElement, "scopeHash", "scope_hash");
        }
        catch (JsonException)
        {
            return "";
        }
    }

    private void Record(Guid? taskCardId, SupervisedScoringDryRunResult result, DateTimeOffset nowUtc)
    {
        var usedLocalModel = result.ResultKind.Equals("Scored", StringComparison.Ordinal) &&
                             !string.IsNullOrWhiteSpace(result.ModelIdentity);
        _ledger.Record(new EvidencePacket(
            Guid.NewGuid(),
            SelfImprovementPacketKinds.ScoringDryRun,
            taskCardId,
            nowUtc,
            DidUseNetwork: usedLocalModel,
            DidUseHostedAi: false,
            DidUseLocalModel: usedLocalModel,
            DidMutate: false,
            ArtifactPath: "",
            Summary: JsonSerializer.Serialize(new
            {
                operation_id = result.OperationId,
                result_kind = result.ResultKind,
                reason = result.Reason,
                model_identity = result.ModelIdentity,
                rubric_id = RubricId
            }, JsonDefaults.Options),
            Status: result.ResultKind.Equals("Scored", StringComparison.Ordinal) ? "Scored" : "Refused"));
    }

    private static SupervisedScoringDryRunResult Refused(string operationId, string reason, DateTimeOffset nowUtc)
    {
        return new SupervisedScoringDryRunResult(operationId, "Refused", reason, "", nowUtc);
    }

    private static string ComputePromptSha256(string operationId, string scopeHash)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{operationId}|{scopeHash}"))).ToLowerInvariant();
    }

    private static string GetString(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (element.TryGetProperty(name, out var value))
            {
                return value.ValueKind == JsonValueKind.String ? value.GetString() ?? "" : value.ToString();
            }
        }

        return "";
    }

    private static bool IsTrue(IReadOnlyDictionary<string, string> settings, string key)
    {
        return settings.TryGetValue(key, out var value) &&
               bool.TryParse(value, out var parsed) &&
               parsed;
    }

    private sealed record AwaitingApprovalRow(Guid? TaskCardId, string ArtifactPath);
}
