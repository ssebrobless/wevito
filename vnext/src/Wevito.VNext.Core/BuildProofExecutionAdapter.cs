using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed class BuildProofExecutionAdapter
{
    private const string ToolFamily = "buildProof";
    private readonly ProofExecutionAllowlistEvaluator _allowlistEvaluator;
    private readonly ICommandRunner _commandRunner;

    public BuildProofExecutionAdapter(
        ProofExecutionAllowlistEvaluator? allowlistEvaluator = null,
        ICommandRunner? commandRunner = null)
    {
        _allowlistEvaluator = allowlistEvaluator ?? new ProofExecutionAllowlistEvaluator();
        _commandRunner = commandRunner ?? new ProcessCommandRunner();
    }

    public async Task<TaskAdapterResult> ExecuteAsync(TaskAdapterRequest request, DateTimeOffset? nowUtc = null, CancellationToken cancellationToken = default)
    {
        var timestamp = nowUtc ?? DateTimeOffset.UtcNow;
        if (request.RunMode != TaskAdapterRunMode.Execute)
        {
            return Block(request, "buildProof execution requires Execute mode.", timestamp);
        }

        if (!string.Equals(request.PolicySnapshot.ToolFamily, ToolFamily, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(request.Intent.RequestedToolFamily, ToolFamily, StringComparison.OrdinalIgnoreCase))
        {
            return Block(request, "Task intent and policy must both target buildProof.", timestamp);
        }

        if (request.PolicySnapshot.AccessMode != ToolAccessMode.Write)
        {
            return Block(request, "buildProof execution requires a write-gated execution policy.", timestamp);
        }

        var command = SelectCommand(request.Intent.RawText);
        var allowlist = _allowlistEvaluator.Evaluate(command);
        if (allowlist.Decision != ProofExecutionAllowlistDecision.Allowed || allowlist.MatchedCommand is null)
        {
            return Block(request, allowlist.Reason, timestamp);
        }

        var artifactRoot = ResolveArtifactRoot(request.ArtifactRoot, timestamp, allowlist.MatchedCommand.CommandId);
        var proofRequest = new ProofExecutionRequest(
            request.TaskCardId,
            allowlist.MatchedCommand.CommandId,
            allowlist.MatchedCommand,
            artifactRoot,
            new Dictionary<string, string>(),
            timestamp);
        var result = await _commandRunner.RunAsync(proofRequest, cancellationToken).ConfigureAwait(false);

        return new TaskAdapterResult(
            request.TaskCardId,
            ToolFamily,
            result.Status == ProofExecutionResultStatus.Succeeded ? TaskAdapterResultStatus.Completed : TaskAdapterResultStatus.Failed,
            DidMutate: result.MutationDetected,
            ReadPaths: [result.ManifestPath],
            WrittenPaths: [result.StdoutPath, result.StderrPath, result.MergedLogPath, result.ManifestPath],
            ResultSummary: result.Summary,
            AuditLogPath: result.MergedLogPath,
            BlockReason: result.Status == ProofExecutionResultStatus.Succeeded ? "" : result.Summary,
            CompletedAtUtc: result.FinishedAtUtc);
    }

    private ProofExecutionCommand SelectCommand(string rawText)
    {
        var commands = _allowlistEvaluator.DefaultAllowedCommands;
        if (rawText.Contains("test", StringComparison.OrdinalIgnoreCase))
        {
            return commands.Single(command => command.CommandId == "dotnet-test-vnext-no-build");
        }

        if (rawText.Contains("publish", StringComparison.OrdinalIgnoreCase) ||
            rawText.Contains("build-vnext", StringComparison.OrdinalIgnoreCase))
        {
            return commands.Single(command => command.CommandId == "publish-vnext-debug-skip-asset-prep");
        }

        if (rawText.Contains("probe", StringComparison.OrdinalIgnoreCase) ||
            rawText.Contains("pet state", StringComparison.OrdinalIgnoreCase))
        {
            return commands.Single(command => command.CommandId == "probe-petstate-skip-build");
        }

        return commands.Single(command => command.CommandId == "dotnet-build-vnext");
    }

    private static string ResolveArtifactRoot(string artifactRoot, DateTimeOffset timestamp, string commandId)
    {
        if (!string.IsNullOrWhiteSpace(artifactRoot))
        {
            return Path.GetFullPath(Path.Combine(artifactRoot, commandId));
        }

        var slug = $"{timestamp:yyyyMMdd-HHmmss}-build-proof-execution";
        return Path.GetFullPath(Path.Combine("vnext", "artifacts", "pet-tasks", slug, commandId));
    }

    private static TaskAdapterResult Block(TaskAdapterRequest request, string reason, DateTimeOffset timestamp)
    {
        return new TaskAdapterResult(
            request.TaskCardId,
            ToolFamily,
            TaskAdapterResultStatus.Blocked,
            DidMutate: false,
            ReadPaths: [],
            WrittenPaths: [],
            BlockReason: reason,
            CompletedAtUtc: timestamp);
    }
}
