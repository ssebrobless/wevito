using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed class FakeCommandRunner : ICommandRunner
{
    public async Task<ProofExecutionResult> RunAsync(ProofExecutionRequest request, CancellationToken cancellationToken = default)
    {
        var started = request.RequestedAtUtc;
        var finished = started.AddMilliseconds(1);
        Directory.CreateDirectory(request.ArtifactRoot);

        var stdoutPath = Path.Combine(request.ArtifactRoot, "stdout.txt");
        var stderrPath = Path.Combine(request.ArtifactRoot, "stderr.txt");
        var mergedLogPath = Path.Combine(request.ArtifactRoot, "merged.log");
        var commandLine = request.Command.Executable + " " + string.Join(" ", request.Command.Arguments);

        await File.WriteAllTextAsync(stdoutPath, $"FAKE RUN: {commandLine}{Environment.NewLine}", cancellationToken).ConfigureAwait(false);
        await File.WriteAllTextAsync(stderrPath, string.Empty, cancellationToken).ConfigureAwait(false);
        await File.WriteAllTextAsync(mergedLogPath, $"[fake-command-runner] {commandLine}{Environment.NewLine}", cancellationToken).ConfigureAwait(false);

        return new ProofExecutionResult(
            request.TaskCardId,
            request.CommandId,
            ProofExecutionResultStatus.Succeeded,
            ExitCode: 0,
            stdoutPath,
            stderrPath,
            mergedLogPath,
            "Fake runner recorded the command but did not start a process.",
            started,
            finished);
    }
}
