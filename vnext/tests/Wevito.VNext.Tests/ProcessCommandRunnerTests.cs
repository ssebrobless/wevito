using Wevito.VNext.Contracts;
using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class ProcessCommandRunnerTests
{
    [Fact]
    public async Task RunAsync_CompletesAllowedStyleCommandWithoutMutation()
    {
        var root = CreateRepoLikeRoot();
        var artifactRoot = Path.Combine(root, "vnext", "artifacts", "pet-tasks", "process-runner-success");
        var runner = new ProcessCommandRunner();

        var result = await runner.RunAsync(BuildRequest(
            root,
            artifactRoot,
            new ProofExecutionCommand(
                "dotnet-version",
                "dotnet",
                ["--version"],
                root,
                TimeSpan.FromSeconds(15),
                MustSkipAssetPrep: false)));

        Assert.Equal(ProofExecutionResultStatus.Succeeded, result.Status);
        Assert.False(result.MutationDetected);
        Assert.True(File.Exists(result.ManifestPath));
        Assert.Contains("no protected file mutation", result.Summary);
    }

    [Fact]
    public async Task RunAsync_TimesOutAndKillsProcessTree()
    {
        var root = CreateRepoLikeRoot();
        var artifactRoot = Path.Combine(root, "vnext", "artifacts", "pet-tasks", "process-runner-timeout");
        var runner = new ProcessCommandRunner();

        var result = await runner.RunAsync(BuildRequest(
            root,
            artifactRoot,
            new ProofExecutionCommand(
                "sleep-timeout",
                "powershell",
                ["-NoProfile", "-Command", "Start-Sleep -Seconds 10"],
                root,
                TimeSpan.FromMilliseconds(100),
                MustSkipAssetPrep: false)));

        Assert.Equal(ProofExecutionResultStatus.TimedOut, result.Status);
        Assert.False(result.MutationDetected);
        Assert.Contains("timed out", File.ReadAllText(result.StderrPath), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RunAsync_CancelsAndKillsProcessTree()
    {
        var root = CreateRepoLikeRoot();
        var artifactRoot = Path.Combine(root, "vnext", "artifacts", "pet-tasks", "process-runner-cancel");
        var runner = new ProcessCommandRunner();
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        var result = await runner.RunAsync(BuildRequest(
            root,
            artifactRoot,
            new ProofExecutionCommand(
                "sleep-cancel",
                "powershell",
                ["-NoProfile", "-Command", "Start-Sleep -Seconds 10"],
                root,
                TimeSpan.FromSeconds(10),
                MustSkipAssetPrep: false)),
            cts.Token);

        Assert.Equal(ProofExecutionResultStatus.Cancelled, result.Status);
        Assert.False(result.MutationDetected);
        Assert.Contains("cancel", File.ReadAllText(result.StderrPath), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RunAsync_MutationDetectedWhenProtectedFileChanges()
    {
        var root = CreateRepoLikeRoot();
        var artifactRoot = Path.Combine(root, "vnext", "artifacts", "pet-tasks", "process-runner-mutation");
        var runner = new ProcessCommandRunner();

        var result = await runner.RunAsync(BuildRequest(
            root,
            artifactRoot,
            new ProofExecutionCommand(
                "mutate-source",
                "powershell",
                ["-NoProfile", "-Command", "Set-Content -Path .\\vnext\\src\\Protected.cs -Value 'changed'"],
                root,
                TimeSpan.FromSeconds(10),
                MustSkipAssetPrep: false)));

        Assert.Equal(ProofExecutionResultStatus.MutationDetected, result.Status);
        Assert.True(result.MutationDetected);
        Assert.Contains("vnext/src/Protected.cs", File.ReadAllText(result.MergedLogPath));
    }

    private static ProofExecutionRequest BuildRequest(string root, string artifactRoot, ProofExecutionCommand command)
    {
        return new ProofExecutionRequest(
            Guid.NewGuid(),
            command.CommandId,
            command,
            artifactRoot,
            new Dictionary<string, string>(),
            DateTimeOffset.UtcNow);
    }

    private static string CreateRepoLikeRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "wevito-process-runner-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "vnext", "src"));
        Directory.CreateDirectory(Path.Combine(root, "vnext", "tests"));
        Directory.CreateDirectory(Path.Combine(root, "tools"));
        Directory.CreateDirectory(Path.Combine(root, "sprites_runtime"));
        File.WriteAllText(Path.Combine(root, "vnext", "src", "Protected.cs"), "original");
        File.WriteAllText(Path.Combine(root, "vnext", "tests", "ProtectedTests.cs"), "original");
        File.WriteAllText(Path.Combine(root, "tools", "tool.ps1"), "# original");
        File.WriteAllText(Path.Combine(root, "sprites_runtime", "sprite.png"), "original");
        return root;
    }
}
