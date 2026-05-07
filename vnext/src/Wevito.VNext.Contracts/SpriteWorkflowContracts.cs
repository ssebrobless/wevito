namespace Wevito.VNext.Contracts;

public enum SpriteProofSurface
{
    Godot,
    VNext,
    ContactSheet,
    PreviewGif,
    StaticReport
}

public enum AnimationCandidateState
{
    Draft,
    Generated,
    Validated,
    AwaitingReview,
    AcceptedForApplyProbe,
    Rejected,
    Paused
}

public enum ApplyProofState
{
    Planned,
    DryRunPassed,
    Applied,
    ProofPassed,
    RolledBack,
    Failed
}

public sealed record SpriteRowKey(
    string Species,
    PetAgeStage AgeStage,
    PetGender Gender,
    string ColorVariant,
    string Family);

public sealed record SpriteFrameGeometry(
    int Width,
    int Height);

public sealed record SpriteFrameManifest(
    string FrameId,
    string RelativePath,
    string Sha256,
    SpriteFrameGeometry Geometry);

public sealed record SpriteRowManifest(
    SpriteRowKey Key,
    int ExpectedFrameCount,
    IReadOnlyList<SpriteFrameManifest> Frames,
    bool IsOptionalFamily = false,
    string SourceManifestPath = "",
    string Notes = "");

public sealed record AnimationCandidateManifest(
    Guid Id,
    SpriteRowKey Target,
    AnimationCandidateState State,
    IReadOnlyList<string> CandidateFramePaths,
    SpriteFrameGeometry RuntimeFrameGeometry,
    string Provider = "",
    string SourceManifestPath = "",
    string ValidationReportPath = "",
    string VisualProofPath = "",
    bool UsesRuntimePropOverlay = false,
    DateTimeOffset CreatedAtUtc = default);

public sealed record ApplyProofManifest(
    Guid Id,
    SpriteRowKey Target,
    ApplyProofState State,
    IReadOnlyDictionary<string, string> BackupBeforeApplyHashes,
    IReadOnlyDictionary<string, string> AfterApplyHashes,
    IReadOnlyList<SpriteProofSurface> ProofSurfaces,
    string DryRunReportPath = "",
    string RollbackProcedurePath = "",
    string ProofReportPath = "",
    DateTimeOffset CreatedAtUtc = default,
    DateTimeOffset UpdatedAtUtc = default);

public enum SpriteWorkflowRootKind
{
    Runtime,
    Authored,
    AuthoredVerified,
    Candidate,
    Proof
}

public sealed record SpriteWorkflowFrameEntry(
    SpriteWorkflowRootKind RootKind,
    string FrameId,
    string RelativePath,
    string AbsolutePath,
    string Blake3,
    SpriteFrameGeometry Geometry);

public sealed record SpriteWorkflowRowEvidence(
    SpriteWorkflowRootKind RootKind,
    string RootPath,
    IReadOnlyList<SpriteWorkflowFrameEntry> Frames);

public sealed record SpriteWorkflowQueueRow(
    SpriteRowKey Key,
    string RowId,
    IReadOnlyList<SpriteWorkflowRowEvidence> Evidence,
    IReadOnlyList<string> Findings);

public sealed record SpriteWorkflowManifestSnapshot(
    string SchemaVersion,
    string RepoRoot,
    IReadOnlyList<SpriteWorkflowQueueRow> Rows,
    DateTimeOffset GeneratedAtUtc);

public sealed record SpriteWorkflowCandidateImportManifest(
    string SchemaVersion,
    SpriteRowKey Target,
    string SourceFolder,
    string CandidateFolder,
    IReadOnlyList<SpriteWorkflowFrameEntry> ImportedFrames,
    DateTimeOffset ImportedAtUtc);

public sealed record SpriteWorkflowDryRunChange(
    string FrameId,
    string RuntimePath,
    string CandidatePath,
    string BackupPath,
    string CurrentRuntimeBlake3,
    string CandidateBlake3,
    bool WouldOverwriteRuntime);

public sealed record SpriteWorkflowDryRunApplyManifest(
    string SchemaVersion,
    SpriteRowKey Target,
    string CandidateFolder,
    string RuntimeRowFolder,
    string PlannedBackupFolder,
    IReadOnlyList<SpriteWorkflowDryRunChange> Changes,
    bool WouldMutateRuntime,
    DateTimeOffset GeneratedAtUtc);

public sealed record SpriteWorkflowApplyManifest(
    string SchemaVersion,
    SpriteRowKey Target,
    string CandidateFolder,
    string RuntimeRowFolder,
    string StagingFolder,
    string BackupFolder,
    IReadOnlyList<SpriteWorkflowDryRunChange> Changes,
    string ApplyLogPath,
    bool Applied,
    DateTimeOffset AppliedAtUtc);

public sealed record SpriteWorkflowRollbackManifest(
    string SchemaVersion,
    SpriteRowKey Target,
    string RuntimeRowFolder,
    string BackupFolder,
    IReadOnlyList<SpriteWorkflowDryRunChange> RestoredChanges,
    bool RolledBack,
    DateTimeOffset RolledBackAtUtc);
