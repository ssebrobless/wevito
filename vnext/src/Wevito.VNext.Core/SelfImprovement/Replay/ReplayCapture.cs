namespace Wevito.VNext.Core.SelfImprovement.Replay;

public sealed record ReplayCapture(
    string ScopeId,
    string OperationId,
    string Seed,
    DateTimeOffset OriginalAtUtc,
    IReadOnlyList<EvidencePacket> Packets)
{
    public ReplayCapture(KillSwitchService killSwitchService)
        : this("", "", "", DateTimeOffset.MinValue, [])
    {
        _ = killSwitchService;
    }
}
