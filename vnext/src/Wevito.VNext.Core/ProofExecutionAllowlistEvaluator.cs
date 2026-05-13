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
    private readonly UnifiedPolicyService _unifiedPolicyService;

    public ProofExecutionAllowlistEvaluator(UnifiedPolicyService? unifiedPolicyService = null)
    {
        _unifiedPolicyService = unifiedPolicyService ?? new UnifiedPolicyService();
    }

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
        return _unifiedPolicyService.EvaluateProofExecution(
            requestedCommand,
            allowedCommands ?? DefaultAllowedCommands,
            BlockedExecutables,
            BlockedTokens);
    }
}
