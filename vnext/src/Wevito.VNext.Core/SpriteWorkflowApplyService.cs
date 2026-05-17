using System.Text.Json;
using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed record SpriteWorkflowApplyRequest(
    SpriteWorkflowDryRunApplyManifest DryRunManifest,
    DateTimeOffset AppliedAtUtc);

public sealed record SpriteWorkflowApplyResult(
    bool Succeeded,
    string ApplyLogPath,
    SpriteWorkflowApplyManifest? Manifest,
    string Message);

public sealed class SpriteWorkflowApplyService
{
    public const int BackupRetentionLimit = 50;

    private readonly Func<string, string, bool> _sameVolume;

    public SpriteWorkflowApplyService(Func<string, string, bool>? sameVolume = null)
    {
        _sameVolume = sameVolume ?? SameVolume;
    }

    public SpriteWorkflowApplyResult Apply(SpriteWorkflowApplyRequest request)
    {
        var dryRun = request.DryRunManifest;
        if (dryRun.Changes.Count == 0)
        {
            return Fail("Dry-run manifest contains no changes.");
        }

        var runtimeRoot = FindSpritesRuntimeRoot(dryRun.RuntimeRowFolder);
        var backupParent = Path.GetDirectoryName(dryRun.PlannedBackupFolder)
            ?? throw new InvalidOperationException("Could not resolve planned backup parent folder.");
        var dryRunArtifactRoot = Path.GetDirectoryName(backupParent)
            ?? throw new InvalidOperationException("Could not resolve dry-run artifact root.");
        var stagingFolder = Path.Combine(
            dryRunArtifactRoot,
            "staging",
            $"{BuildSafeRowId(dryRun.Target)}-{request.AppliedAtUtc:yyyyMMdd-HHmmss}");
        if (!_sameVolume(runtimeRoot, stagingFolder))
        {
            return Fail("Runtime row and staging folder are not on the same volume.");
        }

        var backupFolder = dryRun.PlannedBackupFolder;
        Directory.CreateDirectory(backupFolder);
        Directory.CreateDirectory(stagingFolder);
        Directory.CreateDirectory(dryRun.RuntimeRowFolder);

        foreach (var change in dryRun.Changes)
        {
            if (File.Exists(change.RuntimePath))
            {
                File.Copy(change.RuntimePath, Path.Combine(backupFolder, Path.GetFileName(change.RuntimePath)), overwrite: false);
            }

            File.Copy(change.CandidatePath, Path.Combine(stagingFolder, Path.GetFileName(change.RuntimePath)), overwrite: false);
        }

        foreach (var change in dryRun.Changes)
        {
            var stagingPath = Path.Combine(stagingFolder, Path.GetFileName(change.RuntimePath));
            File.Move(stagingPath, change.RuntimePath, overwrite: true);
        }

        var applyLogPath = Path.Combine(backupFolder, "apply.json");
        var manifest = new SpriteWorkflowApplyManifest(
            "1",
            dryRun.Target,
            dryRun.CandidateFolder,
            dryRun.RuntimeRowFolder,
            stagingFolder,
            backupFolder,
            dryRun.Changes,
            applyLogPath,
            Applied: true,
            request.AppliedAtUtc);
        File.WriteAllText(applyLogPath, JsonSerializer.Serialize(manifest, JsonDefaults.Options));
        PruneBackups(backupParent);

        return new SpriteWorkflowApplyResult(true, applyLogPath, manifest, $"Applied {dryRun.Changes.Count} frame(s) with backup.");
    }

    private static void PruneBackups(string backupParent)
    {
        var backupRoot = backupParent;
        if (!Directory.Exists(backupRoot))
        {
            return;
        }

        var backups = Directory.GetDirectories(backupRoot)
            .Select(path => new DirectoryInfo(path))
            .OrderByDescending(info => info.CreationTimeUtc)
            .ThenByDescending(info => info.Name, StringComparer.OrdinalIgnoreCase)
            .Skip(BackupRetentionLimit)
            .ToList();
        foreach (var backup in backups)
        {
            backup.Delete(recursive: true);
        }
    }

    private static string FindSpritesRuntimeRoot(string runtimeRowFolder)
    {
        var directory = new DirectoryInfo(Path.GetFullPath(runtimeRowFolder));
        while (directory is not null)
        {
            if (string.Equals(directory.Name, "sprites_runtime", StringComparison.OrdinalIgnoreCase))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate sprites_runtime root.");
    }

    private static bool SameVolume(string left, string right)
    {
        return string.Equals(Path.GetPathRoot(Path.GetFullPath(left)), Path.GetPathRoot(Path.GetFullPath(right)), StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildSafeRowId(SpriteRowKey target)
    {
        return $"{target.Species}-{target.AgeStage.ToString().ToLowerInvariant()}-{target.Gender.ToString().ToLowerInvariant()}-{target.ColorVariant}-{target.Family}";
    }

    private static SpriteWorkflowApplyResult Fail(string message)
    {
        return new SpriteWorkflowApplyResult(false, "", null, message);
    }
}
