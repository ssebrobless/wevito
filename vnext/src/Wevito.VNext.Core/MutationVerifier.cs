using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed class MutationVerifier
{
    public IReadOnlyList<ProofExecutionCommand> BuildPostProofCommands(string repoRoot, IReadOnlyList<string> targetPaths)
    {
        var root = Path.GetFullPath(repoRoot);
        var commands = new List<ProofExecutionCommand>();
        var extensions = targetPaths.Select(path => Path.GetExtension(path).ToLowerInvariant()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var normalized = targetPaths.Select(Path.GetFullPath).ToList();

        if (extensions.Overlaps([".cs", ".csproj", ".sln", ".xaml"]))
        {
            commands.Add(new ProofExecutionCommand(
                "dotnet-build",
                "dotnet",
                ["build", ".\\vnext\\Wevito.VNext.sln"],
                root,
                TimeSpan.FromMinutes(3),
                MustSkipAssetPrep: false));
        }

        if (normalized.Any(path => path.Contains(Path.Combine("vnext", "tests"), StringComparison.OrdinalIgnoreCase)))
        {
            commands.Add(new ProofExecutionCommand(
                "dotnet-test",
                "dotnet",
                ["test", ".\\vnext\\tests\\Wevito.VNext.Tests\\Wevito.VNext.Tests.csproj", "--no-build"],
                root,
                TimeSpan.FromMinutes(3),
                MustSkipAssetPrep: false));
        }

        if (normalized.Any(path => path.Contains($"{Path.DirectorySeparatorChar}sprites", StringComparison.OrdinalIgnoreCase)))
        {
            commands.Add(new ProofExecutionCommand(
                "sprite-contract",
                "python",
                [".\\tools\\audit_sprite_contract.py"],
                root,
                TimeSpan.FromMinutes(3),
                MustSkipAssetPrep: true));
        }

        if (normalized.Any(path => path.Contains($"{Path.DirectorySeparatorChar}sprites_runtime{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) &&
                                   Path.GetExtension(path).Equals(".png", StringComparison.OrdinalIgnoreCase)))
        {
            commands.Add(new ProofExecutionCommand(
                "runtime-canvas",
                "python",
                [".\\tools\\report_runtime_canvas_mismatches.py", "--fail-on-mismatch"],
                root,
                TimeSpan.FromMinutes(3),
                MustSkipAssetPrep: true));
        }

        return commands
            .GroupBy(command => command.CommandId, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();
    }

    public bool VerifyHashes(IReadOnlyList<GuardedMutationFileHash> expected)
    {
        foreach (var item in expected)
        {
            if (!item.Exists)
            {
                if (File.Exists(item.Path))
                {
                    return false;
                }

                continue;
            }

            if (!File.Exists(item.Path) || !string.Equals(GuardedMutationService.ComputeSha256(item.Path), item.Sha256, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }
}
