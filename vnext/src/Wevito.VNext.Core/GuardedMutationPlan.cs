using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public enum GuardedMutationKind
{
    TextReplace,
    BinaryReplace
}

public enum GuardedMutationStepStatus
{
    Planned,
    Completed,
    Blocked,
    RolledBack,
    Failed
}

public sealed record GuardedMutationEdit(
    string TargetPath,
    string ProposedContent,
    GuardedMutationKind Kind = GuardedMutationKind.TextReplace,
    string SourcePath = "");

public sealed record GuardedMutationFileHash(
    string Path,
    string Sha256,
    bool Exists);

public sealed record GuardedMutationPlan(
    string SchemaVersion,
    Guid PlanId,
    Guid TaskCardId,
    string ScopeId,
    string RepoRoot,
    IReadOnlyList<string> ApprovedRoots,
    IReadOnlyList<GuardedMutationEdit> Edits,
    IReadOnlyList<ProofExecutionCommand> PostProofCommands,
    bool DryRunOnly,
    DateTimeOffset CreatedAtUtc);

public sealed record GuardedMutationManifest(
    string SchemaVersion,
    Guid PlanId,
    Guid TaskCardId,
    string ScopeId,
    string BackupFolder,
    IReadOnlyList<GuardedMutationFileHash> BeforeHashes,
    IReadOnlyList<GuardedMutationFileHash> AfterHashes,
    IReadOnlyList<ProofExecutionCommand> PostProofCommands,
    bool Applied,
    bool RolledBack,
    DateTimeOffset CreatedAtUtc);

public sealed record GuardedMutationResult(
    bool Succeeded,
    bool DidMutate,
    bool RolledBack,
    string ArtifactFolder,
    string ManifestPath,
    IReadOnlyList<ProofExecutionCommand> PostProofCommands,
    string Message);
