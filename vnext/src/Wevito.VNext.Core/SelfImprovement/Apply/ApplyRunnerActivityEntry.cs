namespace Wevito.VNext.Core.SelfImprovement.Apply;

public sealed record ApplyRunnerActivityEntry(
    string OperationId,
    string ScopeId,
    string ScopeHash,
    IReadOnlyList<ApplyRunnerActivityPacket> Packets,
    ApplyRunnerActivityDisposition Disposition);

public sealed record ApplyRunnerActivityPacket(
    string PacketKind,
    DateTimeOffset Timestamp,
    IReadOnlyDictionary<string, string> Summary,
    bool DidMutate);

public enum ApplyRunnerActivityDisposition
{
    Succeeded,
    RolledBack,
    InProgress
}
