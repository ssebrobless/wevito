namespace Wevito.VNext.Core;

public sealed class LiveStatusFeed
{
    public const string PollSecondsSetting = "live_status_poll_seconds";

    private readonly AuditLedgerService _ledger;
    private readonly PlainLanguageExplainer _explainer;

    public LiveStatusFeed(AuditLedgerService ledger, PlainLanguageExplainer? explainer = null)
    {
        _ledger = ledger;
        _explainer = explainer ?? new PlainLanguageExplainer();
    }

    public TimeSpan ReadPollInterval(IReadOnlyDictionary<string, string>? settings)
    {
        if (settings is not null &&
            settings.TryGetValue(PollSecondsSetting, out var raw) &&
            int.TryParse(raw, out var seconds))
        {
            return TimeSpan.FromSeconds(Math.Clamp(seconds, 5, 300));
        }

        return TimeSpan.FromSeconds(10);
    }

    public OverlayStatusSnapshot BuildDaily(DateTimeOffset nowUtc, IReadOnlyDictionary<string, string>? settings = null)
    {
        if (KillSwitchService.IsActive(settings))
        {
            return OverlayStatusSnapshot.Empty(new DateTimeOffset(nowUtc.UtcDateTime.Date, TimeSpan.Zero), nowUtc);
        }

        var since = new DateTimeOffset(nowUtc.UtcDateTime.Date, TimeSpan.Zero);
        return Build(since, nowUtc);
    }

    public OverlayStatusSnapshot Build(DateTimeOffset sinceUtc, DateTimeOffset untilUtc)
    {
        var rows = _ledger.Snapshot(sinceUtc, untilUtc);
        if (rows.Count == 0)
        {
            return OverlayStatusSnapshot.Empty(sinceUtc, untilUtc);
        }

        var last = rows.OrderByDescending(row => row.CreatedAtUtc).First();
        var counts = new OverlayStatusCounts(
            rows.Count(IsPreview),
            rows.Count(IsApproval),
            rows.Count(row => row.DidMutate),
            rows.Count(row => row.DidUseNetwork),
            rows.Count(row => row.DidUseHostedAi),
            rows.Count(row => row.DidUseLocalModel));
        var flaggedRows = rows
            .Where(row => row.DidUseNetwork || row.DidUseHostedAi || row.DidMutate || row.Status.Equals("Blocked", StringComparison.OrdinalIgnoreCase))
            .Take(5)
            .Select(row => $"{row.CreatedAtUtc:HH:mm} {_explainer.Explain(row)}")
            .ToList();
        return new OverlayStatusSnapshot(sinceUtc, untilUtc, last.PacketKind, last.CreatedAtUtc, counts, flaggedRows, false);
    }

    public string FormatBanner(OverlayStatusSnapshot snapshot)
    {
        if (snapshot.IsEmpty || snapshot.LastAtUtc is null)
        {
            return "Last action: none yet · today: 0 previews, 0 approvals, 0 mutations";
        }

        var lastKind = _explainer.ExplainPacketKind(snapshot.LastPacketKind).TrimEnd('.');
        return $"Last action: {lastKind} at {snapshot.LastAtUtc:HH:mm} · today: {snapshot.TodayCounts.Previews} previews, {snapshot.TodayCounts.Approvals} approvals, {snapshot.TodayCounts.Mutations} mutations";
    }

    private static bool IsPreview(AuditLedgerRow row)
    {
        return row.Status.Equals("PreviewReady", StringComparison.OrdinalIgnoreCase) ||
            row.Status.Equals("Draft", StringComparison.OrdinalIgnoreCase) ||
            row.PacketKind.Contains("preview", StringComparison.OrdinalIgnoreCase) ||
            row.PacketKind.Equals("scheduler_proposal", StringComparison.OrdinalIgnoreCase) ||
            row.PacketKind.Equals("mutation_proposal", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsApproval(AuditLedgerRow row)
    {
        return row.Status.Equals("Approved", StringComparison.OrdinalIgnoreCase) ||
            row.PacketKind.Contains("approval", StringComparison.OrdinalIgnoreCase) ||
            row.Summary.Contains("approved", StringComparison.OrdinalIgnoreCase);
    }
}
