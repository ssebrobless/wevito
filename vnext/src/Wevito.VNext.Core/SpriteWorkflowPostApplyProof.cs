using System.Diagnostics;
using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed record SpriteWorkflowPostApplyProofResult(
    bool Succeeded,
    bool RolledBack,
    string Message);

public sealed class SpriteWorkflowPostApplyProof
{
    private readonly Func<SpriteWorkflowApplyManifest, bool> _proofRunner;
    private readonly SpriteWorkflowRollbackService _rollbackService;

    public SpriteWorkflowPostApplyProof(
        Func<SpriteWorkflowApplyManifest, bool>? proofRunner = null,
        SpriteWorkflowRollbackService? rollbackService = null)
    {
        _proofRunner = proofRunner ?? RunDefaultProof;
        _rollbackService = rollbackService ?? new SpriteWorkflowRollbackService();
    }

    public SpriteWorkflowPostApplyProofResult VerifyOrRollback(SpriteWorkflowApplyManifest applyManifest, DateTimeOffset timestamp)
    {
        if (_proofRunner(applyManifest))
        {
            return new SpriteWorkflowPostApplyProofResult(true, RolledBack: false, "Post-apply proof passed.");
        }

        var rollback = _rollbackService.Rollback(new SpriteWorkflowRollbackRequest(applyManifest, timestamp));
        return new SpriteWorkflowPostApplyProofResult(
            false,
            rollback.Succeeded,
            rollback.Succeeded
                ? "Post-apply proof failed; rollback completed."
                : $"Post-apply proof failed and rollback failed: {rollback.Message}");
    }

    private static bool RunDefaultProof(SpriteWorkflowApplyManifest applyManifest)
    {
        var repoRoot = FindRepoRoot(applyManifest.RuntimeRowFolder);
        return RunPythonScript(repoRoot, Path.Combine(repoRoot, "tools", "audit_sprite_contract.py")) &&
               RunPythonScript(repoRoot, Path.Combine(repoRoot, "tools", "report_runtime_canvas_mismatches.py"));
    }

    private static string FindRepoRoot(string startPath)
    {
        var directory = new DirectoryInfo(Path.GetFullPath(startPath));
        while (directory is not null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "tools")) &&
                Directory.Exists(Path.Combine(directory.FullName, "sprites_runtime")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate Wevito repository root for post-apply proof.");
    }

    private static bool RunPythonScript(string workingDirectory, string scriptPath)
    {
        if (!File.Exists(scriptPath))
        {
            return false;
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = "python",
            WorkingDirectory = workingDirectory,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false
        };
        startInfo.ArgumentList.Add(scriptPath);

        using var process = Process.Start(startInfo);
        if (process is null)
        {
            return false;
        }

        process.WaitForExit();
        return process.ExitCode == 0;
    }
}
