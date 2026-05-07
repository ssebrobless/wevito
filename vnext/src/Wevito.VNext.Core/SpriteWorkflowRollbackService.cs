using System.Text.Json;
using Blake3;
using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed record SpriteWorkflowRollbackRequest(
    SpriteWorkflowApplyManifest ApplyManifest,
    DateTimeOffset RolledBackAtUtc);

public sealed record SpriteWorkflowRollbackResult(
    bool Succeeded,
    string RollbackLogPath,
    SpriteWorkflowRollbackManifest? Manifest,
    string Message);

public sealed class SpriteWorkflowRollbackService
{
    public SpriteWorkflowRollbackResult Rollback(SpriteWorkflowRollbackRequest request)
    {
        var apply = request.ApplyManifest;
        if (!Directory.Exists(apply.BackupFolder))
        {
            return Fail("Backup folder does not exist.");
        }

        foreach (var change in apply.Changes)
        {
            if (string.IsNullOrWhiteSpace(change.CurrentRuntimeBlake3))
            {
                continue;
            }

            var backupPath = Path.Combine(apply.BackupFolder, Path.GetFileName(change.RuntimePath));
            if (!File.Exists(backupPath))
            {
                return Fail($"Missing backup frame: {backupPath}");
            }

            var backupHash = ComputeBlake3(backupPath);
            if (!string.Equals(backupHash, change.CurrentRuntimeBlake3, StringComparison.OrdinalIgnoreCase))
            {
                return Fail($"Backup hash mismatch for {Path.GetFileName(backupPath)}.");
            }
        }

        foreach (var change in apply.Changes)
        {
            if (string.IsNullOrWhiteSpace(change.CurrentRuntimeBlake3))
            {
                if (File.Exists(change.RuntimePath))
                {
                    File.Delete(change.RuntimePath);
                }
                continue;
            }

            var backupPath = Path.Combine(apply.BackupFolder, Path.GetFileName(change.RuntimePath));
            File.Copy(backupPath, change.RuntimePath, overwrite: true);
        }

        var manifest = new SpriteWorkflowRollbackManifest(
            "1",
            apply.Target,
            apply.RuntimeRowFolder,
            apply.BackupFolder,
            apply.Changes,
            RolledBack: true,
            request.RolledBackAtUtc);
        var rollbackLogPath = Path.Combine(apply.BackupFolder, "rollback.json");
        File.WriteAllText(rollbackLogPath, JsonSerializer.Serialize(manifest, JsonDefaults.Options));
        return new SpriteWorkflowRollbackResult(true, rollbackLogPath, manifest, "Rollback restored runtime row from backup.");
    }

    private static string ComputeBlake3(string absolutePath)
    {
        var hash = Hasher.Hash(File.ReadAllBytes(absolutePath));
        return Convert.ToHexString(hash.AsSpan()).ToLowerInvariant();
    }

    private static SpriteWorkflowRollbackResult Fail(string message)
    {
        return new SpriteWorkflowRollbackResult(false, "", null, message);
    }
}
