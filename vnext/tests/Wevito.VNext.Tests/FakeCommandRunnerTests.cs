using Wevito.VNext.Contracts;
using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class FakeCommandRunnerTests
{
    [Fact]
    public async Task RunAsync_WritesLogsWithoutStartingProcess()
    {
        var root = Path.Combine(Path.GetTempPath(), "wevito-fake-runner-tests", Guid.NewGuid().ToString("N"), "vnext", "artifacts", "pet-tasks", "fake-run");
        var command = new ProofExecutionCommand(
            "dotnet-build-vnext",
            "dotnet",
            ["build", @".\vnext\Wevito.VNext.sln"],
            ".",
            TimeSpan.FromMinutes(5),
            MustSkipAssetPrep: false);
        var request = new ProofExecutionRequest(
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            command.CommandId,
            command,
            root,
            new Dictionary<string, string>(),
            DateTimeOffset.Parse("2026-05-07T00:00:00Z"));
        var runner = new FakeCommandRunner();

        var result = await runner.RunAsync(request);

        Assert.Equal(ProofExecutionResultStatus.Succeeded, result.Status);
        Assert.Equal(0, result.ExitCode);
        Assert.True(File.Exists(result.StdoutPath));
        Assert.True(File.Exists(result.StderrPath));
        Assert.True(File.Exists(result.MergedLogPath));
        Assert.Contains("FAKE RUN: dotnet build", File.ReadAllText(result.StdoutPath));
        Assert.Contains("did not start a process", result.Summary);
    }
}
