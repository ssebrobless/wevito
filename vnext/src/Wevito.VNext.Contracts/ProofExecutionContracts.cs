namespace Wevito.VNext.Contracts;

public enum ProofExecutionAllowlistDecision
{
    Allowed,
    Blocked
}

public enum ProofExecutionResultStatus
{
    Succeeded,
    Failed,
    Blocked,
    Cancelled,
    TimedOut,
    MutationDetected
}

public sealed record ProofExecutionCommand(
    string CommandId,
    string Executable,
    IReadOnlyList<string> Arguments,
    string WorkingDirectory,
    TimeSpan Timeout,
    bool MustSkipAssetPrep);

public sealed record ProofExecutionRequest(
    Guid TaskCardId,
    string CommandId,
    ProofExecutionCommand Command,
    string ArtifactRoot,
    IReadOnlyDictionary<string, string> EnvironmentOverrides,
    DateTimeOffset RequestedAtUtc);

public sealed record ProofExecutionManifest(
    string SchemaVersion,
    Guid TaskCardId,
    string ToolFamily,
    ProofExecutionCommand Command,
    string ArtifactRoot,
    string StdoutPath,
    string StderrPath,
    string MergedLogPath,
    IReadOnlyDictionary<string, string> EnvironmentOverrides,
    IReadOnlyDictionary<string, string> PreRunProtectedHashes,
    IReadOnlyDictionary<string, string> PostRunProtectedHashes,
    bool DidMutateCode,
    bool DidMutateAssets,
    bool DidRunAssetPrep,
    bool MutationDetected,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? FinishedAtUtc,
    ProofExecutionResultStatus Status,
    int? ExitCode);

public sealed record ProofExecutionResult(
    Guid TaskCardId,
    string CommandId,
    ProofExecutionResultStatus Status,
    int? ExitCode,
    string StdoutPath,
    string StderrPath,
    string MergedLogPath,
    string ManifestPath,
    bool MutationDetected,
    string Summary,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset FinishedAtUtc);

public sealed record ProofExecutionAllowlistResult(
    ProofExecutionAllowlistDecision Decision,
    string Reason,
    ProofExecutionCommand? MatchedCommand);
