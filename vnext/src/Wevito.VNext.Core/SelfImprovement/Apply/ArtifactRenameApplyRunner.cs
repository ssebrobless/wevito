using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;
using Wevito.VNext.Contracts;
using Wevito.VNext.Core.Audit;

namespace Wevito.VNext.Core.SelfImprovement.Apply;

public sealed partial class ArtifactRenameApplyRunner
{
    public const string DesignApprovedSetting = "apply_runner_design_approved";
    public const string ImplementationPhaseApprovedSetting = "apply_runner_implementation_phase_approved";
    public const string EnabledSetting = "apply_runner_v0_enabled";
    public const string DryRunRequiredSetting = "apply_runner_v0_dry_run_required";
    public const string BackupRequiredSetting = "apply_runner_v0_backup_required";
    public const string PostProofRequiredSetting = "apply_runner_v0_post_proof_required";
    public const string RollbackRequiredSetting = "apply_runner_v0_rollback_required";

    private static readonly string[] RequiredFlags =
    [
        DesignApprovedSetting,
        ImplementationPhaseApprovedSetting,
        EnabledSetting,
        DryRunRequiredSetting,
        BackupRequiredSetting,
        PostProofRequiredSetting,
        RollbackRequiredSetting
    ];

    private readonly AuditLedgerService _ledger;
    private readonly Func<string, string?> _settings;
    private readonly KillSwitchService _killSwitch;
    private readonly ApplyRunnerPrerequisiteCheckService? _prereqCheck;
    private readonly string _artifactRoot;
    private readonly Func<string, string> _sha256;
    private readonly Func<string, DateTimeOffset> _clock;
    private readonly Func<string, ApplyRunnerPrerequisiteCheckResult>? _prereqOverride;

    public ArtifactRenameApplyRunner(
        AuditLedgerService ledger,
        Func<string, string?> settings,
        KillSwitchService killSwitch,
        ApplyRunnerPrerequisiteCheckService prereqCheck,
        string artifactRoot)
        : this(ledger, settings, killSwitch, prereqCheck, artifactRoot, ComputeFileSha256, _ => DateTimeOffset.UtcNow, null)
    {
    }

    public ArtifactRenameApplyRunner(
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

    private ArtifactRenameApplyRunner(
        AuditLedgerService ledger,
        Func<string, string?> settings,
        KillSwitchService killSwitch,
        ApplyRunnerPrerequisiteCheckService? prereqCheck,
        string artifactRoot,
        Func<string, string> sha256,
        Func<string, DateTimeOffset>? clock = null,
        Func<string, ApplyRunnerPrerequisiteCheckResult>? prereqOverride = null)
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

    public ApplyResult Apply(ApplyRequest request, CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
        }
        catch (OperationCanceledException)
        {
            return new ApplyResult.Refused("cancelled");
        }

        var preflight = Preflight(request);
        if (preflight is ApplyResult.Refused refused)
        {
            return refused;
        }

        var sourcePath = ResolveUnderRoot(request.DraftRelativePath);
        var approvedRelativePath = ToApprovedRelativePath(request.DraftRelativePath);
        var destinationPath = ResolveUnderRoot(approvedRelativePath);
        var dryRunRelativePath = $"{request.OperationId}/{request.ScopeId}/apply-v0-dry-run.json";
        var dryRunPath = ResolveUnderRoot(dryRunRelativePath);
        var backupPath = "";
        var backupRelativePath = "";
        var preHash = _sha256(sourcePath);

        Record(SelfImprovementPacketKinds.ApplyV0DryRunStarted, request, "", $"operation_id={request.OperationId} scope_id={request.ScopeId} scope_hash={request.ScopeHash} source={request.DraftRelativePath} pre_sha256={preHash}", "Started", true);

        Directory.CreateDirectory(Path.GetDirectoryName(dryRunPath) ?? _artifactRoot);
        File.WriteAllText(dryRunPath, JsonSerializer.Serialize(new
        {
            operationId = request.OperationId,
            scopeId = request.ScopeId,
            scopeHash = request.ScopeHash,
            source = request.DraftRelativePath,
            destination = approvedRelativePath,
            expectedPreSha256 = preHash,
            plannedAt = _clock(dryRunPath)
        }, JsonDefaults.Options), Encoding.UTF8);
        var dryRunHash = _sha256(dryRunPath);
        Record(SelfImprovementPacketKinds.ApplyV0DryRunCompleted, request, dryRunPath, $"dry_run_path={dryRunPath} dry_run_sha256={dryRunHash}", "Completed", true);

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_killSwitch.IsActive())
            {
                return RollBackAfterBackup(request, sourcePath, destinationPath, backupPath, backupRelativePath, preHash, "kill_switch_activated");
            }

            var now = _clock(sourcePath).UtcDateTime.ToString("yyyyMMddHHmmssffff", CultureInfo.InvariantCulture);
            backupPath = $"{sourcePath}.backup-{now}";
            backupRelativePath = ToRelative(backupPath);
            File.Copy(sourcePath, backupPath, overwrite: false);
            var backupHash = _sha256(backupPath);
            if (!string.Equals(backupHash, preHash, StringComparison.Ordinal))
            {
                TryDelete(backupPath);
                Record(SelfImprovementPacketKinds.ApplyV0RolledBack, request, "", "reason=backup_sha256_mismatch", "RolledBack", true, "backup_sha256_mismatch");
                return new ApplyResult.RolledBack("backup_sha256_mismatch", backupRelativePath);
            }

            Record(SelfImprovementPacketKinds.ApplyV0BackupWritten, request, backupPath, $"backup_path={backupRelativePath} backup_sha256={backupHash}", "Completed", true, bypassKillSwitch: true);

            cancellationToken.ThrowIfCancellationRequested();
            if (_killSwitch.IsActive())
            {
                return RollBackAfterBackup(request, sourcePath, destinationPath, backupPath, backupRelativePath, preHash, "kill_switch_activated");
            }

            try
            {
                File.Move(sourcePath, destinationPath, overwrite: false);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                var reason = $"rename_threw:{ex.GetType().Name}";
                RestoreBackup(sourcePath, destinationPath, backupPath);
                VerifySourceHash(sourcePath, preHash);
                Record(SelfImprovementPacketKinds.ApplyV0RolledBack, request, backupPath, $"reason={reason}", "RolledBack", true, reason, bypassKillSwitch: true);
                return new ApplyResult.RolledBack(reason, backupRelativePath);
            }

            Record(SelfImprovementPacketKinds.ApplyV0Applied, request, destinationPath, $"source={request.DraftRelativePath} destination={approvedRelativePath}", "Completed", true);

            cancellationToken.ThrowIfCancellationRequested();
            if (_killSwitch.IsActive())
            {
                return RollBackAfterBackup(request, sourcePath, destinationPath, backupPath, backupRelativePath, preHash, "kill_switch_activated");
            }

            var postHash = File.Exists(destinationPath) ? _sha256(destinationPath) : "";
            if (!string.Equals(postHash, preHash, StringComparison.Ordinal) || File.Exists(sourcePath))
            {
                RestoreBackup(sourcePath, destinationPath, backupPath);
                VerifySourceHash(sourcePath, preHash);
                Record(SelfImprovementPacketKinds.ApplyV0RolledBack, request, backupPath, "reason=post_proof_mismatch", "RolledBack", true, "post_proof_mismatch", bypassKillSwitch: true);
                return new ApplyResult.RolledBack("post_proof_mismatch", backupRelativePath);
            }

            Record(SelfImprovementPacketKinds.ApplyV0PostProofCompleted, request, destinationPath, $"post_sha256={postHash}", "Completed", false);
            Record(SelfImprovementPacketKinds.ApplyV0Completed, request, destinationPath, $"approved_path={approvedRelativePath} post_sha256={postHash}", "Completed", true);
            return new ApplyResult.Succeeded(approvedRelativePath, postHash);
        }
        catch (OperationCanceledException)
        {
            return RollBackAfterBackup(request, sourcePath, destinationPath, backupPath, backupRelativePath, preHash, "cancelled");
        }
    }

    private ApplyResult? Preflight(ApplyRequest request)
    {
        if (_killSwitch.IsActive())
        {
            return new ApplyResult.Refused("kill_switch_active");
        }

        foreach (var flag in RequiredFlags)
        {
            if (!string.Equals(_settings(flag), bool.TrueString, StringComparison.OrdinalIgnoreCase))
            {
                return new ApplyResult.Refused($"flag_{flag}_not_true");
            }
        }

        var prereq = _prereqOverride?.Invoke(request.OperationId) ?? _prereqCheck!.Check(request.OperationId, DateTimeOffset.UtcNow);
        var failed = prereq.Entries.FirstOrDefault(entry => !entry.Passed);
        if (failed is not null || !prereq.AllPassed)
        {
            return new ApplyResult.Refused($"prerequisite_check_failed:{failed?.Name ?? "unknown"}");
        }

        if (!ValidRelativePath().IsMatch(request.DraftRelativePath) ||
            request.DraftRelativePath.Contains("..", StringComparison.Ordinal) ||
            request.DraftRelativePath.Contains('\\', StringComparison.Ordinal) ||
            request.DraftRelativePath.StartsWith("/", StringComparison.Ordinal) ||
            request.DraftRelativePath.Contains(':', StringComparison.Ordinal))
        {
            return new ApplyResult.Refused("invalid_relative_path");
        }

        if (request.ScopeHash.Length != 64 || request.ScopeHash.Any(character => character is not (>= '0' and <= '9' or >= 'a' and <= 'f')))
        {
            return new ApplyResult.Refused("scope_hash_mismatch");
        }

        var parts = request.DraftRelativePath.Split('/');
        if (!string.Equals(parts[1], request.ScopeId, StringComparison.Ordinal))
        {
            return new ApplyResult.Refused("scope_id_mismatch");
        }

        if (!string.Equals(parts[0], request.OperationId, StringComparison.Ordinal))
        {
            return new ApplyResult.Refused("operation_id_mismatch");
        }

        var sourcePath = ResolveUnderRoot(request.DraftRelativePath);
        if (!IsUnderRoot(sourcePath))
        {
            return new ApplyResult.Refused("source_outside_artifact_root");
        }

        var destinationPath = ResolveUnderRoot(ToApprovedRelativePath(request.DraftRelativePath));
        if (!IsUnderRoot(destinationPath))
        {
            return new ApplyResult.Refused("destination_outside_artifact_root");
        }

        if (!File.Exists(sourcePath))
        {
            return new ApplyResult.Refused("source_missing");
        }

        if ((File.GetAttributes(sourcePath) & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint)
        {
            return new ApplyResult.Refused("source_reparse_point");
        }

        if (File.Exists(destinationPath))
        {
            return new ApplyResult.Refused("destination_already_exists");
        }

        var approval = ReadLatestAwaitingApproval(request.OperationId);
        if (approval is null || !string.Equals(approval.ApprovalToken, request.ApprovalToken, StringComparison.Ordinal))
        {
            return new ApplyResult.Refused("approval_token_mismatch");
        }

        if (!string.Equals(approval.ScopeHash, request.ScopeHash, StringComparison.Ordinal))
        {
            return new ApplyResult.Refused("scope_hash_mismatch");
        }

        return null;
    }

    private ApplyResult.RolledBack RollBackAfterBackup(
        ApplyRequest request,
        string sourcePath,
        string destinationPath,
        string backupPath,
        string backupRelativePath,
        string preHash,
        string reason)
    {
        if (!string.IsNullOrWhiteSpace(backupPath) && File.Exists(backupPath))
        {
            RestoreBackup(sourcePath, destinationPath, backupPath);
            VerifySourceHash(sourcePath, preHash);
        }

        Record(SelfImprovementPacketKinds.ApplyV0RolledBack, request, backupPath, $"reason={reason}", "RolledBack", true, reason, bypassKillSwitch: true);
        return new ApplyResult.RolledBack(reason, backupRelativePath);
    }

    private void RestoreBackup(string sourcePath, string destinationPath, string backupPath)
    {
        if (File.Exists(destinationPath))
        {
            File.Move(destinationPath, sourcePath, overwrite: true);
            return;
        }

        if (!File.Exists(sourcePath) && File.Exists(backupPath))
        {
            File.Copy(backupPath, sourcePath, overwrite: true);
        }
    }

    private void VerifySourceHash(string sourcePath, string preHash)
    {
        if (File.Exists(sourcePath) && string.Equals(_sha256(sourcePath), preHash, StringComparison.Ordinal))
        {
            return;
        }

        throw new IOException("rollback_sha256_mismatch");
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

    private void Record(string packetKind, ApplyRequest request, string artifactPath, string summary, string status, bool didMutate, string error = "", bool bypassKillSwitch = false)
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

    private string ToRelative(string path)
    {
        return Path.GetRelativePath(_artifactRoot, path).Replace(Path.DirectorySeparatorChar, '/');
    }

    private static string ToApprovedRelativePath(string draftRelativePath)
    {
        return draftRelativePath[..^".draft.json".Length] + ".approved.json";
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

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static string ComputeFileSha256(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }

    [GeneratedRegex("^[a-zA-Z0-9_-]+/[a-zA-Z0-9_-]+/[a-zA-Z0-9._-]+\\.draft\\.json$", RegexOptions.CultureInvariant)]
    private static partial Regex ValidRelativePath();

    private sealed record AwaitingApproval(string ApprovalToken, string ScopeHash);
}
