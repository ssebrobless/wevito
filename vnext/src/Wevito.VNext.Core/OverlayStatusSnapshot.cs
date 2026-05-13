namespace Wevito.VNext.Core;

public sealed record OverlayStatusCounts(
    int Previews,
    int Approvals,
    int Mutations,
    int NetworkUses,
    int HostedAiUses,
    int LocalModelUses);

public sealed record OverlayStatusSnapshot(
    DateTimeOffset SinceUtc,
    DateTimeOffset UntilUtc,
    string LastPacketKind,
    DateTimeOffset? LastAtUtc,
    OverlayStatusCounts TodayCounts,
    IReadOnlyList<string> FlaggedRows,
    bool IsEmpty)
{
    public static OverlayStatusSnapshot Empty(DateTimeOffset sinceUtc, DateTimeOffset untilUtc)
    {
        return new OverlayStatusSnapshot(
            sinceUtc,
            untilUtc,
            "",
            null,
            new OverlayStatusCounts(0, 0, 0, 0, 0, 0),
            [],
            true);
    }
}
