using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;
using Wevito.VNext.Contracts;
using Wevito.VNext.Core.Audit;

namespace Wevito.VNext.Core.SelfImprovement.Apply;

public sealed partial class ArtifactRenameRollbackRunner
{
    public const string ExplicitRollbackEnabledSetting = "apply_runner_v0_explicit_rollback_enabled";
    public const string ExplicitRollbackDesignApprovedSetting = "apply_runner_v0_explicit_rollback_design_approved";

    private static readonly string[] RequiredFlags =
    [
        ExplicitRollbackEnabledSetting,
        ExplicitRollbackDesignApprovedSetting,
        ArtifactRenameApplyRunner.DesignApprovedSetting,
        ArtifactRenameApplyRunner.ImplementationPhaseApprovedSetting
    ];

    private readonly AuditLedgerService _ledger;
    private readonly Func<string, string?> _settings;
    private readonly KillSwitchService _killSwitch;
    private readonly ApplyRunnerPrerequisiteCheckService? _prereqCheck;
    private readonly string _artifactRoot;
    private readonly Func<string, string> _sha256;
    private readonly Func<string, DateTimeOffset> _clock;
    private readonly Func<string, ApplyRunnerPrerequisiteCheckResult>? _prereqOverride;

    public ArtifactRenameRollbackRunner(
        AuditLedgerService ledger,
        Func<string, string?> settings,
        KillSwitchService killSwitch,
        ApplyRunnerPrerequisiteCheckService prereqCheck,
        string artifactRoot)
        : this(ledger, settings, killSwitch, prereqCheck, artifactRoot, ComputeFileSha256, _ => DateTimeOffset.UtcNow, null)
    {
    }

    public ArtifactRenameRollbackRunner(
        AuditLedgerService ledger,
        Func<string, string?> settings,
        KillSwitchService killSwitch,
        string artifactRoot,
        Func<string, ApplyRunnerPrerequisiteCheckResult> prereqOverride,
        Func<string, string>? sha256 = null,
        Func<string, DateTimeOffset>? clock = null)
        : this(ledger, settings, killSwitch, null, artifactRoot, sha256 ?? ComputeFileSha256, clock ?? (_ => DateTimeOffset.UtcNow), prereqOverride)
    {
    }

    private ArtifactRenameRollbackRunner(
        AuditLedgerService ledger,
        Func<string, string?> settings,
        KillSwitchService killSwitch,
        ApplyRunnerPrerequisiteCheckService? prereqCheck,
        string artifactRoot,
        Func<string, string> sha256,
        Func<string, DateTimeOffset>? clock,
        Func<string, ApplyRunnerPrerequisiteCheckResult>? prereqOverride)
    {
        _ledger = ledger;
        _settings = settings;
        _killSwitch = killSwitch;
        _prereqCheck = prereqCheck;
        _artifactRoot = Path.GetFullPath(artifactRoot);
        _sha256 = sha256;
        _clock = clock ?? (_ => DateTimeOffset.UtcNow);
        _prereqOverride = prereqOverride;
    }

    public RollbackResult ExplicitRollback(RollbackRequest request, CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
        }
        catch (OperationCanceledException)
        {
            return Refuse(request, "cancelled");
        }

        var preflight = Preflight(request);
        if (preflight is RollbackResult.Refused refused)
        {
            return refused;
        }

        var sourcePath = ResolveUnderRoot(request.ApprovedRelativePath);
        var draftRelativePath = ToDraftRelativePath(request.ApprovedRelativePath);
        var destinationPath = ResolveUnderRoot(draftRelativePath);
        var dryRunPath = ResolveUnderRoot($"{request.OperationId}/{request.ScopeId}/rollback-v0-dry-run.json");
        var preHash = _sha256(sourcePath);

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_killSwitch.IsActive())
            {
                return Refuse(request, "kill_switch_active");
            }

            Record(
                SelfImprovementPacketKinds.ApplyV0ExplicitRollbackStarted,
                request,
                "",
                $"operation_id={request.OperationId} scope_id={request.ScopeId} scope_hash={request.ScopeHash} source={request.ApprovedRelativePath} destination={draftRelativePath} pre_sha256={preHash}",
                "Started",
                didMutate: false);

            Directory.CreateDirectory(Path.GetDirectoryName(dryRunPath) ?? _artifactRoot);
            File.WriteAllText(dryRunPath, JsonSerializer.Serialize(new
            {
                operationId = request.OperationId,
                scopeId = request.ScopeId,
                scopeHash = request.ScopeHash,
                source = request.ApprovedRelativePath,
                destination = draftRelativePath,
                expectedPreSha256 = preHash,
                plannedAt = _clock(dryRunPath)
            }, JsonDefaults.Options), Encoding.UTF8);

            cancellationToken.ThrowIfCancellationRequested();
            if (_killSwitch.IsActive())
            {
                return Refuse(request, "kill_switch_active");
            }

            try
            {
                File.Move(sourcePath, destinationPath, overwrite: false);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                return Refuse(request, $"rename_threw:{ex.GetType().Name}");
            }

            var postHash = File.Exists(destinationPath) ? _sha256(destinationPath) : "";
            if (!string.Equals(postHash, preHash, StringComparison.Ordinal) || File.Exists(sourcePath))
            {
                return Refuse(request, "post_proof_mismatch", didMutate: true);
            }

            Record(
                SelfImprovementPacketKinds.ApplyV0ExplicitRollbackCompleted,
                request,
                destinationPath,
                $"operation_id={request.OperationId} scope_id={request.ScopeId} scope_hash={request.ScopeHash} source={request.ApprovedRelativePath} destination={draftRelativePath} pre_sha256={preHash} post_sha256={postHash}",
                "Completed",
                didMutate: true);
            return new RollbackResult.Succeeded(draftRelativePath, postHash);
        }
        catch (OperationCanceledException)
        {
            return Refuse(request, "cancelled");
        }
    }

    private RollbackResult? Preflight(RollbackRequest request)
    {
        if (_killSwitch.IsActive())
        {
            return new RollbackResult.Refused("kill_switch_active");
        }

        foreach (var flag in RequiredFlags)
        {
            if (!string.Equals(_settings(flag), bool.TrueString, StringComparison.OrdinalIgnoreCase))
            {
                return new RollbackResult.Refused($"flag_{flag}_not_true");
            }
        }

        var prereq = _prereqOverride?.Invoke(request.OperationId) ?? _prereqCheck!.Check(request.OperationId, DateTimeOffset.UtcNow);
        var failed = prereq.Entries.FirstOrDefault(entry => !entry.Passed);
        if (failed is not null || !prereq.AllPassed)
        {
            return new RollbackResult.Refused($"prerequisite_check_failed:{failed?.Name ?? "unknown"}");
        }

        if (!ValidRelativePath().IsMatch(request.ApprovedRelativePath) ||
            request.ApprovedRelativePath.Contains("..", StringComparison.Ordinal) ||
            request.ApprovedRelativePath.Contains('\\', StringComparison.Ordinal) ||
            request.ApprovedRelativePath.StartsWith("/", StringComparison.Ordinal) ||
            request.ApprovedRelativePath.Contains(':', StringComparison.Ordinal))
        {
            return new RollbackResult.Refused("invalid_relative_path");
        }

        if (request.ScopeHash.Length != 64 || request.ScopeHash.Any(character => character is not (>= '0' and <= '9' or >= 'a' and <= 'f')))
        {
            return new RollbackResult.Refused("scope_hash_mismatch");
        }

        var parts = request.ApprovedRelativePath.Split('/');
        if (!string.Equals(parts[1], request.ScopeId, StringComparison.Ordinal))
        {
            return new RollbackResult.Refused("scope_id_mismatch");
        }

        if (!string.Equals(parts[0], request.OperationId, StringComparison.Ordinal))
        {
            return new RollbackResult.Refused("operation_id_mismatch");
        }

        var sourcePath = ResolveUnderRoot(request.ApprovedRelativePath);
        if (!IsUnderRoot(sourcePath))
        {
            return new RollbackResult.Refused("source_outside_artifact_root");
        }

        var destinationPath = ResolveUnderRoot(ToDraftRelativePath(request.ApprovedRelativePath));
        if (!IsUnderRoot(destinationPath))
        {
            return new RollbackResult.Refused("destination_outside_artifact_root");
        }

        if (!File.Exists(sourcePath))
        {
            return new RollbackResult.Refused("source_missing");
        }

        if ((File.GetAttributes(sourcePath) & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint)
        {
            return new RollbackResult.Refused("source_reparse_point");
        }

        if (File.Exists(destinationPath))
        {
            return new RollbackResult.Refused("destination_already_exists");
        }

        var approval = ReadLatestAwaitingApproval(request.OperationId);
        if (approval is null || !string.Equals(approval.ApprovalToken, request.ApprovalToken, StringComparison.Ordinal))
        {
            return new RollbackResult.Refused("approval_token_mismatch");
        }

        if (!string.Equals(approval.ScopeHash, request.ScopeHash, StringComparison.Ordinal))
        {
            return new RollbackResult.Refused("scope_hash_mismatch");
        }

        return null;
    }

    private RollbackResult.Refused Refuse(RollbackRequest request, string reason, bool didMutate = false)
    {
        Record(
            SelfImprovementPacketKinds.ApplyV0ExplicitRollbackRefused,
            request,
            "",
            $"operation_id={request.OperationId} scope_id={request.ScopeId} scope_hash={request.ScopeHash} reason={reason}",
            "Refused",
            didMutate,
            reason,
            bypassKillSwitch: true);
        return new RollbackResult.Refused(reason);
    }

    private AwaitingApproval? ReadLatestAwaitingApproval(string operationId)
    {
        if (!File.Exists(_ledger.DatabasePath))
        {
            return null;
        }

        using var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = _ledger.DatabasePath,
            Mode = SqliteOpenMode.ReadOnly
        }.ToString());
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT artifact_path, summary
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

        var artifactPath = Convert.ToString(reader["artifact_path"], CultureInfo.InvariantCulture) ?? "";
        var summary = Convert.ToString(reader["summary"], CultureInfo.InvariantCulture) ?? "";
        return ReadAwaitingApprovalFields(artifactPath, summary);
    }

    private static AwaitingApproval? ReadAwaitingApprovalFields(string artifactPath, string summary)
    {
        var fromArtifact = TryReadJson(File.Exists(artifactPath) ? File.ReadAllText(artifactPath, Encoding.UTF8) : "");
        var fromSummary = TryReadJson(summary);
        var token = FirstNonEmpty(fromArtifact.Token, fromSummary.Token);
        var scopeHash = FirstNonEmpty(fromArtifact.ScopeHash, fromSummary.ScopeHash);
        return string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(scopeHash)
            ? null
            : new AwaitingApproval(token, scopeHash);
    }

    private static (string Token, string ScopeHash) TryReadJson(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return ("", "");
        }

        try
        {
            using var document = JsonDocument.Parse(text);
            var root = document.RootElement;
            return (GetString(root, "approvalToken", "approval_token", "approvedOperationId", "approved_operation_id"), GetString(root, "scopeHash", "scope_hash"));
        }
        catch (JsonException)
        {
            return ("", "");
        }
    }

    private void Record(string packetKind, RollbackRequest request, string artifactPath, string summary, string status, bool didMutate, string error = "", bool bypassKillSwitch = false)
    {
        if (!bypassKillSwitch && _killSwitch.IsActive())
        {
            return;
        }

        _ledger.Record(new EvidencePacket(
            Guid.NewGuid(),
            packetKind,
            null,
            DateTimeOffset.UtcNow,
            DidUseNetwork: false,
            DidUseHostedAi: false,
            DidUseLocalModel: false,
            DidMutate: didMutate,
            ArtifactPath: artifactPath,
            Summary: summary,
            Status: status,
            Error: error));
    }

    private string ResolveUnderRoot(string relativePath)
    {
        return Path.GetFullPath(Path.Combine(_artifactRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));
    }

    private bool IsUnderRoot(string path)
    {
        var fullRoot = _artifactRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        return Path.GetFullPath(path).StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase);
    }

    private static string ToDraftRelativePath(string approvedRelativePath)
    {
        return approvedRelativePath[..^".approved.json".Length] + ".draft.json";
    }

    private static string FirstNonEmpty(params string[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? "";
    }

    private static string GetString(JsonElement root, params string[] names)
    {
        foreach (var name in names)
        {
            if (root.TryGetProperty(name, out var property) && property.ValueKind == JsonValueKind.String)
            {
                return property.GetString() ?? "";
            }
        }

        return "";
    }

    private static string ComputeFileSha256(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }

    [GeneratedRegex("^[a-zA-Z0-9_-]+/[a-zA-Z0-9_-]+/[a-zA-Z0-9._-]+\\.approved\\.json$", RegexOptions.CultureInvariant)]
    private static partial Regex ValidRelativePath();

    private sealed record AwaitingApproval(string ApprovalToken, string ScopeHash);
}
