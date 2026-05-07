using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed class ProofExecutionAllowlistEvaluator
{
    private static readonly string[] BlockedExecutables =
    [
        "Remove-Item",
        "rm",
        "del",
        "git",
        "npm",
        "pnpm",
        "yarn",
        "pip",
        "winget",
        "choco",
        "playwright"
    ];

    private static readonly string[] BlockedTokens =
    [
        ";",
        "&&",
        "||",
        ">",
        ">>",
        "<",
        "|",
        "`",
        "$(",
        "Start-Process",
        "runas",
        "--headed",
        "browser"
    ];

    public IReadOnlyList<ProofExecutionCommand> DefaultAllowedCommands { get; } =
    [
        new ProofExecutionCommand(
            "dotnet-build-vnext",
            "dotnet",
            ["build", @".\vnext\Wevito.VNext.sln"],
            ".",
            TimeSpan.FromMinutes(5),
            MustSkipAssetPrep: false),
        new ProofExecutionCommand(
            "dotnet-test-vnext-no-build",
            "dotnet",
            ["test", @".\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj", "--no-build"],
            ".",
            TimeSpan.FromMinutes(5),
            MustSkipAssetPrep: false),
        new ProofExecutionCommand(
            "publish-vnext-debug-skip-asset-prep",
            "powershell",
            ["-NoProfile", "-ExecutionPolicy", "Bypass", "-File", @".\tools\build-vnext.ps1", "-Configuration", "Debug", "-SkipAssetPrep", "-SkipTests"],
            ".",
            TimeSpan.FromMinutes(10),
            MustSkipAssetPrep: true),
        new ProofExecutionCommand(
            "probe-petstate-skip-build",
            "powershell",
            ["-NoProfile", "-ExecutionPolicy", "Bypass", "-File", @".\tools\probe-vnext-pet-tasks.ps1", "-TaskText", "review pet state", "-ExpectedToolFamily", "petState", "-SkipBuild"],
            ".",
            TimeSpan.FromMinutes(3),
            MustSkipAssetPrep: false)
    ];

    public ProofExecutionAllowlistResult Evaluate(
        ProofExecutionCommand requestedCommand,
        IReadOnlyList<ProofExecutionCommand>? allowedCommands = null)
    {
        var hardBlock = FindHardBlockReason(requestedCommand);
        if (!string.IsNullOrWhiteSpace(hardBlock))
        {
            return new ProofExecutionAllowlistResult(
                ProofExecutionAllowlistDecision.Blocked,
                hardBlock,
                MatchedCommand: null);
        }

        var allowed = allowedCommands ?? DefaultAllowedCommands;
        var matched = allowed.FirstOrDefault(command => IsExactMatch(command, requestedCommand));
        if (matched is null)
        {
            return new ProofExecutionAllowlistResult(
                ProofExecutionAllowlistDecision.Blocked,
                "Command did not exactly match an allowlisted executable and ordered argument list.",
                MatchedCommand: null);
        }

        return new ProofExecutionAllowlistResult(
            ProofExecutionAllowlistDecision.Allowed,
            "Command exactly matched allowlist.",
            matched);
    }

    private static string FindHardBlockReason(ProofExecutionCommand command)
    {
        var executableName = Path.GetFileNameWithoutExtension(command.Executable);
        if (BlockedExecutables.Any(blocked => string.Equals(blocked, executableName, StringComparison.OrdinalIgnoreCase)))
        {
            if (!string.Equals(executableName, "git", StringComparison.OrdinalIgnoreCase))
            {
                return $"Executable '{command.Executable}' is hard-blocked.";
            }

            var gitSubcommand = command.Arguments.FirstOrDefault() ?? "";
            if (gitSubcommand.Equals("reset", StringComparison.OrdinalIgnoreCase) ||
                gitSubcommand.Equals("checkout", StringComparison.OrdinalIgnoreCase))
            {
                return $"git {gitSubcommand} is hard-blocked.";
            }
        }

        var parts = new[] { command.Executable }.Concat(command.Arguments);
        foreach (var part in parts)
        {
            if (BlockedTokens.Any(token => part.Contains(token, StringComparison.OrdinalIgnoreCase)))
            {
                return $"Command contains blocked shell composition or automation token '{part}'.";
            }
        }

        if (command.Arguments.Any(argument => argument.Contains("build-vnext.ps1", StringComparison.OrdinalIgnoreCase)) &&
            !command.Arguments.Any(argument => argument.Equals("-SkipAssetPrep", StringComparison.OrdinalIgnoreCase)))
        {
            return "build-vnext.ps1 is blocked unless -SkipAssetPrep is present.";
        }

        if (command.Arguments.Any(argument => argument.Contains("prep", StringComparison.OrdinalIgnoreCase) || argument.Contains("asset-prep", StringComparison.OrdinalIgnoreCase)) &&
            !command.Arguments.Any(argument => argument.Equals("-SkipAssetPrep", StringComparison.OrdinalIgnoreCase)))
        {
            return "Asset preparation is blocked in buildProof execution.";
        }

        return "";
    }

    private static bool IsExactMatch(ProofExecutionCommand allowed, ProofExecutionCommand requested)
    {
        return string.Equals(allowed.Executable, requested.Executable, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(NormalizeWorkingDirectory(allowed.WorkingDirectory), NormalizeWorkingDirectory(requested.WorkingDirectory), StringComparison.OrdinalIgnoreCase) &&
               allowed.Arguments.SequenceEqual(requested.Arguments, StringComparer.Ordinal) &&
               allowed.MustSkipAssetPrep == requested.MustSkipAssetPrep;
    }

    private static string NormalizeWorkingDirectory(string workingDirectory)
    {
        return string.IsNullOrWhiteSpace(workingDirectory) ? "." : workingDirectory.Trim();
    }
}
