using System.Text.Json;
using Wevito.VNext.Contracts;
using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class ProofExecutionAllowlistEvaluatorTests
{
    private readonly ProofExecutionAllowlistEvaluator _evaluator = new();

    [Fact]
    public void Evaluate_AllowsExactExecutableAndOrderedArguments()
    {
        var requested = new ProofExecutionCommand(
            "dotnet-build-vnext",
            "dotnet",
            ["build", @".\vnext\Wevito.VNext.sln"],
            ".",
            TimeSpan.FromMinutes(5),
            MustSkipAssetPrep: false);

        var result = _evaluator.Evaluate(requested);

        Assert.Equal(ProofExecutionAllowlistDecision.Allowed, result.Decision);
        Assert.NotNull(result.MatchedCommand);
    }

    [Fact]
    public void Evaluate_BlocksArgumentOrderDrift()
    {
        var requested = new ProofExecutionCommand(
            "dotnet-build-vnext",
            "dotnet",
            [@".\vnext\Wevito.VNext.sln", "build"],
            ".",
            TimeSpan.FromMinutes(5),
            MustSkipAssetPrep: false);

        var result = _evaluator.Evaluate(requested);

        Assert.Equal(ProofExecutionAllowlistDecision.Blocked, result.Decision);
        Assert.Contains("exactly match", result.Reason);
    }

    [Theory]
    [InlineData("powershell", "-NoProfile; Remove-Item .\\sprites_runtime")]
    [InlineData("powershell", "-File .\\tools\\build-vnext.ps1 > out.txt")]
    [InlineData("powershell", "-File .\\tools\\probe-vnext-pet-tasks.ps1 | more")]
    public void Evaluate_BlocksShellComposition(string executable, string argument)
    {
        var requested = new ProofExecutionCommand(
            "composed",
            executable,
            [argument],
            ".",
            TimeSpan.FromMinutes(1),
            MustSkipAssetPrep: false);

        var result = _evaluator.Evaluate(requested);

        Assert.Equal(ProofExecutionAllowlistDecision.Blocked, result.Decision);
        Assert.Contains("blocked", result.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("Remove-Item", ".\\vnext")]
    [InlineData("git", "reset", "--hard")]
    [InlineData("git", "checkout", "--", ".")]
    [InlineData("npm", "install")]
    [InlineData("playwright", "test", "--headed")]
    public void Evaluate_BlocksDestructiveOrExternalAutomation(string executable, params string[] arguments)
    {
        var requested = new ProofExecutionCommand(
            "blocked",
            executable,
            arguments,
            ".",
            TimeSpan.FromMinutes(1),
            MustSkipAssetPrep: false);

        var result = _evaluator.Evaluate(requested);

        Assert.Equal(ProofExecutionAllowlistDecision.Blocked, result.Decision);
    }

    [Fact]
    public void Evaluate_BlocksBuildVnextWithoutSkipAssetPrep()
    {
        var requested = new ProofExecutionCommand(
            "publish-vnext-debug",
            "powershell",
            ["-NoProfile", "-ExecutionPolicy", "Bypass", "-File", @".\tools\build-vnext.ps1", "-Configuration", "Debug", "-SkipTests"],
            ".",
            TimeSpan.FromMinutes(10),
            MustSkipAssetPrep: false);

        var result = _evaluator.Evaluate(requested);

        Assert.Equal(ProofExecutionAllowlistDecision.Blocked, result.Decision);
        Assert.Contains("-SkipAssetPrep", result.Reason);
    }

    [Fact]
    public void Evaluate_AllowsBuildVnextOnlyWithSkipAssetPrep()
    {
        var requested = new ProofExecutionCommand(
            "publish-vnext-debug-skip-asset-prep",
            "powershell",
            ["-NoProfile", "-ExecutionPolicy", "Bypass", "-File", @".\tools\build-vnext.ps1", "-Configuration", "Debug", "-SkipAssetPrep", "-SkipTests"],
            ".",
            TimeSpan.FromMinutes(10),
            MustSkipAssetPrep: true);

        var result = _evaluator.Evaluate(requested);

        Assert.Equal(ProofExecutionAllowlistDecision.Allowed, result.Decision);
        Assert.True(result.MatchedCommand?.MustSkipAssetPrep);
    }

    [Fact]
    public void ProofExecutionManifest_RoundTrips()
    {
        var command = _evaluator.DefaultAllowedCommands[0];
        var manifest = new ProofExecutionManifest(
            "1",
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            "buildProof",
            command,
            @"C:\repo\vnext\artifacts\pet-tasks\fake",
            "stdout.txt",
            "stderr.txt",
            "merged.log",
            new Dictionary<string, string>
            {
                ["WEVITO_FAKE"] = "1"
            },
            new Dictionary<string, string>
            {
                ["sprites_runtime/goose.png"] = "before"
            },
            new Dictionary<string, string>
            {
                ["sprites_runtime/goose.png"] = "after"
            },
            DidMutateCode: false,
            DidMutateAssets: false,
            DidRunAssetPrep: false,
            MutationDetected: false,
            DateTimeOffset.Parse("2026-05-07T00:00:00Z"),
            DateTimeOffset.Parse("2026-05-07T00:00:01Z"),
            ProofExecutionResultStatus.Succeeded,
            ExitCode: 0);

        var json = JsonSerializer.Serialize(manifest, JsonDefaults.Options);
        var roundTrip = JsonSerializer.Deserialize<ProofExecutionManifest>(json, JsonDefaults.Options);

        Assert.Contains("\"toolFamily\":\"buildProof\"", json);
        Assert.Contains("\"didRunAssetPrep\":false", json);
        Assert.Contains("\"mutationDetected\":false", json);
        Assert.NotNull(roundTrip);
        Assert.Equal("dotnet", roundTrip.Command.Executable);
        Assert.False(roundTrip.DidMutateAssets);
    }
}
