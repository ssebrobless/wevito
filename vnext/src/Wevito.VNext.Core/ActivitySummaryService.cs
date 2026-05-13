namespace Wevito.VNext.Core;

public sealed record ActivitySummaryBucket(
    string PacketKind,
    int Count,
    int NetworkCount,
    int HostedAiCount,
    int LocalModelCount,
    int MutationCount);

public sealed record ActivitySummary(
    DateTimeOffset SinceUtc,
    DateTimeOffset UntilUtc,
    int TotalRows,
    IReadOnlyList<ActivitySummaryBucket> Buckets,
    IReadOnlyList<AuditLedgerRow> RecentRows);

public sealed class ActivitySummaryService
{
    private readonly AuditLedgerService _ledger;

    public ActivitySummaryService(AuditLedgerService ledger)
    {
        _ledger = ledger;
    }

    public ActivitySummary BuildDaily(DateTimeOffset nowUtc)
    {
        var since = new DateTimeOffset(nowUtc.UtcDateTime.Date, TimeSpan.Zero);
        return Build(since, nowUtc);
    }

    public ActivitySummary BuildWeekly(DateTimeOffset nowUtc)
    {
        var date = nowUtc.UtcDateTime.Date;
        var sinceDate = date.AddDays(-6);
        return Build(new DateTimeOffset(sinceDate, TimeSpan.Zero), nowUtc);
    }

    public ActivitySummary Build(DateTimeOffset sinceUtc, DateTimeOffset untilUtc)
    {
        var rows = _ledger.Snapshot(sinceUtc, untilUtc);
        var buckets = rows
            .GroupBy(row => row.PacketKind, StringComparer.OrdinalIgnoreCase)
            .Select(group => new ActivitySummaryBucket(
                group.Key,
                group.Count(),
                group.Count(row => row.DidUseNetwork),
                group.Count(row => row.DidUseHostedAi),
                group.Count(row => row.DidUseLocalModel),
                group.Count(row => row.DidMutate)))
            .OrderByDescending(bucket => bucket.Count)
            .ThenBy(bucket => bucket.PacketKind, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new ActivitySummary(sinceUtc, untilUtc, rows.Count, buckets, rows.Take(10).ToList());
    }

    public static string FormatOneLine(ActivitySummary summary)
    {
        if (summary.TotalRows == 0)
        {
            return "No audited helper activity yet today.";
        }

        var sensitive = summary.Buckets.Sum(bucket => bucket.NetworkCount + bucket.HostedAiCount + bucket.MutationCount);
        var headline = string.Join(", ", summary.Buckets.Take(3).Select(bucket => $"{bucket.PacketKind}: {bucket.Count}"));
        return $"Audited helper activity: {summary.TotalRows} row(s). {headline}. Sensitive flags: {sensitive}.";
    }
}
