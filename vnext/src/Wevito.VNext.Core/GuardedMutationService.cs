using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed class GuardedMutationService
{
    public const string MutationProposalPacketKind = "mutation_proposal";
    public const string MutationApplyPacketKind = "mutation_apply";
    public const string MutationRollbackPacketKind = "mutation_rollback";

    private readonly UnifiedPolicyService _policyService;
    private readonly MutationVerifier _verifier;
    private readonly AuditLedgerService? _auditLedgerService;
    private readonly KillSwitchService? _killSwitchService;
    private readonly Func<GuardedMutationPlan, bool> _postProofRunner;

    public GuardedMutationService(
        UnifiedPolicyService? policyService = null,
        MutationVerifier? verifier = null,
        AuditLedgerService? auditLedgerService = null,
        KillSwitchService? killSwitchService = null,
        Func<GuardedMutationPlan, bool>? postProofRunner = null)
    {
        _policyService = policyService ?? new UnifiedPolicyService();
        _verifier = verifier ?? new MutationVerifier();
        _auditLedgerService = auditLedgerService;
        _killSwitchService = killSwitchService;
        _postProofRunner = postProofRunner ?? (_ => true);
    }

    public GuardedMutationResult DryRun(GuardedMutationPlan plan, TaskCard taskCard, RuntimeSupervisorStatus runtimeStatus)
    {
        var guard = ValidateGuards(plan, taskCard, runtimeStatus, requireApproved: false);
        if (!string.IsNullOrWhiteSpace(guard))
        {
            return Block(plan, guard);
        }

        var scope = ValidateScope(plan);
        if (!string.IsNullOrWhiteSpace(scope))
        {
            return Block(plan, scope);
        }

        var artifactFolder = ResolveArtifactFolder(plan, "dry-run");
        Directory.CreateDirectory(artifactFolder);
        var commands = _verifier.BuildPostProofCommands(plan.RepoRoot, plan.Edits.Select(edit => edit.TargetPath).ToList());
        var manifest = BuildManifest(plan, artifactFolder, before: Snapshot(plan.Edits), after: [], commands, applied: false, rolledBack: false);
        var manifestPath = Path.Combine(artifactFolder, "mutation-dry-run.json");
        File.WriteAllText(manifestPath, JsonSerializer.Serialize(manifest, JsonDefaults.Options));
        Record(plan, MutationProposalPacketKind, artifactFolder, "Guarded mutation dry-run packet created.", didMutate: false);
        return new GuardedMutationResult(true, false, false, artifactFolder, manifestPath, commands, "Dry-run completed without mutation.");
    }

    public GuardedMutationResult Apply(GuardedMutationPlan plan, TaskCard taskCard, RuntimeSupervisorStatus runtimeStatus)
    {
        var guard = ValidateGuards(plan, taskCard, runtimeStatus, requireApproved: true);
        if (!string.IsNullOrWhiteSpace(guard))
        {
            return Block(plan, guard);
        }

        var scope = ValidateScope(plan);
        if (!string.IsNullOrWhiteSpace(scope))
        {
            return Block(plan, scope);
        }

        var artifactFolder = ResolveArtifactFolder(plan, "apply");
        var backupFolder = Path.Combine(Path.GetFullPath(plan.RepoRoot), "vnext", "artifacts", "mutation-backups", $"{plan.CreatedAtUtc:yyyyMMdd-HHmmss}-{BuildSafeScope(plan.ScopeId)}");
        Directory.CreateDirectory(artifactFolder);
        Directory.CreateDirectory(backupFolder);
        var before = Snapshot(plan.Edits);
        Backup(plan.Edits, backupFolder);
        ApplyEdits(plan.Edits);
        var after = Snapshot(plan.Edits);
        var commands = plan.PostProofCommands.Count > 0
            ? plan.PostProofCommands
            : _verifier.BuildPostProofCommands(plan.RepoRoot, plan.Edits.Select(edit => edit.TargetPath).ToList());
        var proofPassed = _postProofRunner(plan);
        var rolledBack = false;
        if (!proofPassed)
        {
            Restore(before, backupFolder);
            rolledBack = true;
            if (!_verifier.VerifyHashes(before))
            {
                throw new InvalidOperationException("Guarded mutation rollback failed hash verification.");
            }
        }

        var manifest = BuildManifest(plan, backupFolder, before, after, commands, applied: true, rolledBack);
        var manifestPath = Path.Combine(artifactFolder, rolledBack ? "mutation-rollback.json" : "mutation-apply.json");
        File.WriteAllText(manifestPath, JsonSerializer.Serialize(manifest, JsonDefaults.Options));
        Record(plan, rolledBack ? MutationRollbackPacketKind : MutationApplyPacketKind, artifactFolder, rolledBack ? "Guarded mutation rolled back after post-proof failure." : "Guarded mutation applied after post-proof.", didMutate: true);
        return new GuardedMutationResult(
            true,
            DidMutate: true,
            rolledBack,
            artifactFolder,
            manifestPath,
            commands,
            rolledBack ? "Post-proof failed; rollback restored target bytes." : "Apply completed and post-proof passed.");
    }

    public static string ComputeSha256(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }

    private string ValidateGuards(GuardedMutationPlan plan, TaskCard taskCard, RuntimeSupervisorStatus runtimeStatus, bool requireApproved)
    {
        if (_killSwitchService?.IsActive() == true)
        {
            return "kill_switch=true";
        }

        if (runtimeStatus.Mode != RuntimeSupervisorMode.Active)
        {
            return "Runtime supervisor must be Active before mutation work.";
        }

        if (requireApproved && taskCard.Status != TaskCardStatus.Approved)
        {
            return "Guarded mutation apply requires an approved task card.";
        }

        if (taskCard.Id != plan.TaskCardId)
        {
            return "Task card id does not match mutation plan.";
        }

        return "";
    }

    private string ValidateScope(GuardedMutationPlan plan)
    {
        if (plan.Edits.Count == 0)
        {
            return "Mutation plan contains no edits.";
        }

        foreach (var edit in plan.Edits)
        {
            var read = _policyService.EvaluateRead(edit.TargetPath, plan.ApprovedRoots, plan.TaskCardId, plan.CreatedAtUtc);
            if (read.IsBlocked)
            {
                return read.Reason;
            }

            if (edit.Kind == GuardedMutationKind.BinaryReplace)
            {
                if (string.IsNullOrWhiteSpace(edit.SourcePath) || !File.Exists(edit.SourcePath))
                {
                    return $"Binary replacement source does not exist: {edit.SourcePath}";
                }

                var source = _policyService.EvaluateRead(edit.SourcePath, plan.ApprovedRoots, plan.TaskCardId, plan.CreatedAtUtc);
                if (source.IsBlocked)
                {
                    return source.Reason;
                }
            }
        }

        return "";
    }

    private static IReadOnlyList<GuardedMutationFileHash> Snapshot(IReadOnlyList<GuardedMutationEdit> edits)
    {
        return edits
            .Select(edit => File.Exists(edit.TargetPath)
                ? new GuardedMutationFileHash(Path.GetFullPath(edit.TargetPath), ComputeSha256(edit.TargetPath), Exists: true)
                : new GuardedMutationFileHash(Path.GetFullPath(edit.TargetPath), "", Exists: false))
            .ToList();
    }

    private static void Backup(IReadOnlyList<GuardedMutationEdit> edits, string backupFolder)
    {
        foreach (var edit in edits)
        {
            if (!File.Exists(edit.TargetPath))
            {
                continue;
            }

            var backupPath = ResolveBackupPath(backupFolder, edit.TargetPath);
            Directory.CreateDirectory(Path.GetDirectoryName(backupPath) ?? backupFolder);
            File.Copy(edit.TargetPath, backupPath, overwrite: true);
        }
    }

    private static void ApplyEdits(IReadOnlyList<GuardedMutationEdit> edits)
    {
        foreach (var edit in edits)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(edit.TargetPath) ?? ".");
            if (edit.Kind == GuardedMutationKind.BinaryReplace)
            {
                File.Copy(edit.SourcePath, edit.TargetPath, overwrite: true);
            }
            else
            {
                File.WriteAllText(edit.TargetPath, edit.ProposedContent, Encoding.UTF8);
            }
        }
    }

    private static void Restore(IReadOnlyList<GuardedMutationFileHash> before, string backupFolder)
    {
        foreach (var item in before)
        {
            if (!item.Exists)
            {
                if (File.Exists(item.Path))
                {
                    File.Delete(item.Path);
                }

                continue;
            }

            var backupPath = ResolveBackupPath(backupFolder, item.Path);
            File.Copy(backupPath, item.Path, overwrite: true);
        }
    }

    private static GuardedMutationManifest BuildManifest(
        GuardedMutationPlan plan,
        string backupFolder,
        IReadOnlyList<GuardedMutationFileHash> before,
        IReadOnlyList<GuardedMutationFileHash> after,
        IReadOnlyList<ProofExecutionCommand> commands,
        bool applied,
        bool rolledBack)
    {
        return new GuardedMutationManifest("1", plan.PlanId, plan.TaskCardId, plan.ScopeId, backupFolder, before, after, commands, applied, rolledBack, plan.CreatedAtUtc);
    }

    private static string ResolveArtifactFolder(GuardedMutationPlan plan, string suffix)
    {
        return Path.Combine(Path.GetFullPath(plan.RepoRoot), "vnext", "artifacts", "mutations", $"{plan.CreatedAtUtc:yyyyMMdd-HHmmss}-{BuildSafeScope(plan.ScopeId)}-{suffix}");
    }

    private static string ResolveBackupPath(string backupFolder, string targetPath)
    {
        var fileName = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Path.GetFullPath(targetPath)))).ToLowerInvariant() + "-" + Path.GetFileName(targetPath);
        return Path.Combine(backupFolder, fileName);
    }

    private static string BuildSafeScope(string scopeId)
    {
        var safe = new string((scopeId ?? "scope").Select(ch => char.IsLetterOrDigit(ch) || ch is '-' or '_' ? ch : '-').ToArray());
        return string.IsNullOrWhiteSpace(safe) ? "scope" : safe;
    }

    private void Record(GuardedMutationPlan plan, string packetKind, string artifactFolder, string summary, bool didMutate)
    {
        _auditLedgerService?.Record(new EvidencePacket(
            Guid.NewGuid(),
            packetKind,
            plan.TaskCardId,
            plan.CreatedAtUtc,
            DidUseNetwork: false,
            DidUseHostedAi: false,
            DidUseLocalModel: false,
            didMutate,
            artifactFolder,
            summary,
            didMutate ? "Completed" : "PreviewReady"));
    }

    private static GuardedMutationResult Block(GuardedMutationPlan plan, string reason)
    {
        return new GuardedMutationResult(false, false, false, "", "", [], reason);
    }
}
