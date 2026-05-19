using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Wevito.VNext.Core.Audit;

namespace Wevito.VNext.Core.SelfImprovement;

internal static class ExpectedPacketKinds
{
    public static IReadOnlyList<string> CanonicalOrder { get; } =
    [
        SelfImprovementPacketKinds.ProposalDrafted,
        SelfImprovementPacketKinds.ConstitutionalReviewed,
        SelfImprovementPacketKinds.DryRunCompleted,
        SelfImprovementPacketKinds.EvalCompleted,
        SelfImprovementPacketKinds.ApplyAwaitingApproval,
        SelfImprovementPacketKinds.ApplyRefused,
        SelfImprovementPacketKinds.ApplyCompleted,
        SelfImprovementPacketKinds.RollbackVerified,
        SelfImprovementPacketKinds.MaturityClockReset
    ];
}

public sealed class ApprovalCardDetailService
{
    private readonly string _databasePath;
    private readonly KillSwitchService? _killSwitchService;
    private readonly Func<string, string> _sha256Resolver;
    private readonly Action<string>? _commandObserver;

    public ApprovalCardDetailService(
        string databasePath,
        KillSwitchService? killSwitchService = null,
        Func<string, string>? sha256Resolver = null,
        Action<string>? commandObserver = null)
    {
        _databasePath = Path.GetFullPath(databasePath);
        _killSwitchService = killSwitchService;
        _sha256Resolver = sha256Resolver ?? ComputeSha256;
        _commandObserver = commandObserver;
    }

    public ApprovalCardDetail BuildFor(Guid taskCardId)
    {
        if (_killSwitchService?.IsActive() == true)
        {
            return ApprovalCardDetail.BlockedDetail("kill_switch=true");
        }

        var row = ReadLatestAwaitingApprovalRow(taskCardId);
        if (row is null)
        {
            return ApprovalCardDetail.BlockedDetail("apply_awaiting_approval_row_not_found");
        }

        var artifactPath = CanonicalizeArtifactPath(row.ArtifactPath);
        if (string.IsNullOrWhiteSpace(artifactPath) || !IsUnderVNextArtifacts(artifactPath))
        {
            return ApprovalCardDetail.BlockedDetail("artifact_path_outside_allowed_root");
        }

        if (!File.Exists(artifactPath))
        {
            return ApprovalCardDetail.BlockedDetail("artifact_json_not_found");
        }

        ApprovalAwaitingArtifact? artifact;
        try
        {
            artifact = JsonSerializer.Deserialize<ApprovalAwaitingArtifact>(
                File.ReadAllText(artifactPath),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException)
        {
            return ApprovalCardDetail.BlockedDetail("artifact_json_invalid");
        }

        if (artifact is null)
        {
            return ApprovalCardDetail.BlockedDetail("artifact_json_invalid");
        }

        var inputFiles = new List<ApprovalCardInputFile>
        {
            BuildInputFile("proposal", artifact.ProposalPath),
            BuildInputFile("dry_run", artifact.DryRunPath),
            BuildInputFile("eval", artifact.EvalPath)
        };

        return new ApprovalCardDetail(
            artifact.OperationId ?? "",
            artifact.ScopeId ?? "",
            artifact.ScopeHash ?? "",
            inputFiles,
            ExpectedPacketKinds.CanonicalOrder,
            ApprovalCardDetail.SafetyCopyText,
            artifactPath,
            Blocked: false,
            BlockedReason: "");
    }

    private ApprovalCardInputFile BuildInputFile(string role, string? path)
    {
        var safePath = path ?? "";
        return new ApprovalCardInputFile(safePath, _sha256Resolver(safePath), role);
    }

    private ApprovalAwaitingRow? ReadLatestAwaitingApprovalRow(Guid taskCardId)
    {
        if (!File.Exists(_databasePath))
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
            SELECT artifact_path
            FROM audit_ledger
            WHERE task_card_id = $task_card_id
              AND packet_kind = $packet_kind
            ORDER BY created_at_utc DESC, id DESC
            LIMIT 1;
            """;
        _commandObserver?.Invoke(command.CommandText);
        command.Parameters.AddWithValue("$task_card_id", taskCardId.ToString());
        command.Parameters.AddWithValue("$packet_kind", SelfImprovementPacketKinds.ApplyAwaitingApproval);
        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }

        return new ApprovalAwaitingRow(Convert.ToString(reader["artifact_path"], CultureInfo.InvariantCulture) ?? "");
    }

    private static string CanonicalizeArtifactPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return "";
        }

        return Path.GetFullPath(path);
    }

    private static bool IsUnderVNextArtifacts(string fullPath)
    {
        var parts = fullPath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        for (var i = 0; i < parts.Length - 1; i++)
        {
            if (string.Equals(parts[i], "vnext", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(parts[i + 1], "artifacts", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static string ComputeSha256(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return "";
        }

        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }

    private sealed record ApprovalAwaitingRow(string ArtifactPath);

    private sealed record ApprovalAwaitingArtifact(
        string? OperationId,
        string? ScopeId,
        string? ScopeHash,
        string? ProposalPath,
        string? DryRunPath,
        string? EvalPath);
}
