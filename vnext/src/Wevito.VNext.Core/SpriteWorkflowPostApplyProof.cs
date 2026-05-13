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
        var commands = new MutationVerifier().BuildPostProofCommands(repoRoot, applyManifest.Changes.Select(change => change.RuntimePath).ToList());
        return commands.Count > 0 && commands.All(command => RunProofCommand(command));
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

    private static bool RunProofCommand(ProofExecutionCommand command)
    {
        if (string.Equals(command.Executable, "python", StringComparison.OrdinalIgnoreCase) &&
            command.Arguments.Count > 0 &&
            !File.Exists(Path.GetFullPath(command.Arguments[0], command.WorkingDirectory)))
        {
            return false;
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = command.Executable,
            WorkingDirectory = command.WorkingDirectory,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false
        };
        foreach (var argument in command.Arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo);
        if (process is null)
        {
            return false;
        }

        process.WaitForExit();
        return process.ExitCode == 0;
    }
}
