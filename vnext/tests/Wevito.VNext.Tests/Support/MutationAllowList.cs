namespace Wevito.VNext.Tests.Support;

public static class MutationAllowList
{
    public static IReadOnlySet<string> TypeNames { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        "ArtifactRenameApplyRunner",
        "ArtifactRenameRollbackRunner",
        "AuditLedgerService",
        "SnapshotExportService",
        "ReplayResultStore",
        "HeldOutEvalSeedTool",
        "InDistributionEvalSeedTool",
        "SnapshotVerifyTool"
    };
}
