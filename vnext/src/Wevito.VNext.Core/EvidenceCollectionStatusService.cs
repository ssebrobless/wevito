using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed record EvidenceCollectionDayStatus(
    DateOnly DateUtc,
    int HeartbeatCount,
    int FlaggedRows,
    int FocusStealDelta,
    bool BudgetExceeded,
    DateTimeOffset? LastSelfImprovementReportAtUtc);

public sealed record EvidenceCollectionStatus(
    bool Active,
    bool HasManifest,
    DateTimeOffset? StartedAtUtc,
    int DayN,
    int DayMax,
    int RowsToday,
    int FlaggedRowsToday,
    DateTimeOffset? LastReportAtUtc,
    string LastReadinessLabel,
    int HeartbeatCountToday,
    int FocusStealDeltaToday,
    bool BudgetExceededToday,
    string ManifestPath,
    IReadOnlyList<EvidenceCollectionDayStatus> Days);

public sealed class EvidenceCollectionStatusService
{
    private readonly AuditLedgerService _ledger;
    private readonly string _artifactRoot;
    private readonly Func<DateTimeOffset> _clock;
    private readonly KillSwitchService? _killSwitchService;

    public EvidenceCollectionStatusService(
        AuditLedgerService ledger,
        string artifactRoot,
        Func<DateTimeOffset>? clock = null,
        KillSwitchService? killSwitchService = null)
    {
        _ledger = ledger;
        _artifactRoot = Path.GetFullPath(artifactRoot);
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
        _killSwitchService = killSwitchService;
    }

    public EvidenceCollectionStatus Read()
    {
        var manifestPath = FindLatestManifest(_artifactRoot);
        if (manifestPath is null)
        {
            return Empty("not_started");
        }

        var manifest = ReadManifest(manifestPath);
        if (manifest is null)
        {
            return Empty("blocked_by_manifest");
        }

        var now = _clock();
        var elapsedDays = Math.Max(0, (now.UtcDateTime.Date - manifest.StartedAtUtc.UtcDateTime.Date).Days);
        var dayN = Math.Clamp(elapsedDays + 1, 1, manifest.RequestedDays);
        var today = DateOnly.FromDateTime(now.UtcDateTime);
        var until = now.AddMinutes(1);
        var rows = _ledger.Snapshot(manifest.StartedAtUtc.AddMinutes(-1), until);
        var todayRows = rows.Where(row => DateOnly.FromDateTime(row.CreatedAtUtc.UtcDateTime) == today).ToList();
        var days = BuildDays(rows, manifest.StartedAtUtc, now, manifest.RequestedDays);
        var todayStatus = days.FirstOrDefault(day => day.DateUtc == today);
        var active = now < manifest.StartedAtUtc.AddDays(manifest.RequestedDays) &&
                     _killSwitchService?.IsActive() != true;
        var readiness = active
            ? $"day_{dayN}_of_{manifest.RequestedDays}"
            : now >= manifest.StartedAtUtc.AddDays(manifest.RequestedDays)
                ? "ready_to_eval"
                : "blocked_by_kill_switch";

        return new EvidenceCollectionStatus(
            active,
            HasManifest: true,
            manifest.StartedAtUtc,
            active ? dayN : 0,
            manifest.RequestedDays,
            todayRows.Count,
            todayRows.Count(IsFlagged),
            rows.Where(row => row.PacketKind.Equals(AuditLedgerService.SelfImprovementReportPacketKind, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(row => row.CreatedAtUtc)
                .FirstOrDefault()
                ?.CreatedAtUtc,
            readiness,
            todayStatus?.HeartbeatCount ?? 0,
            todayStatus?.FocusStealDelta ?? 0,
            todayStatus?.BudgetExceeded ?? false,
            manifestPath,
            days);
    }

    public static string ComputeSettingsHash(IReadOnlyDictionary<string, string> settings)
    {
        var canonical = string.Join("\n", settings
            .OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .Select(pair => $"{pair.Key}={pair.Value}"));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();
    }

    private static IReadOnlyList<EvidenceCollectionDayStatus> BuildDays(
        IReadOnlyList<AuditLedgerRow> rows,
        DateTimeOffset startedAtUtc,
        DateTimeOffset nowUtc,
        int requestedDays)
    {
        var days = new List<EvidenceCollectionDayStatus>();
        for (var index = 0; index < requestedDays; index++)
        {
            var date = DateOnly.FromDateTime(startedAtUtc.AddDays(index).UtcDateTime);
            if (date > DateOnly.FromDateTime(nowUtc.UtcDateTime))
            {
                break;
            }

            var dayRows = rows.Where(row => DateOnly.FromDateTime(row.CreatedAtUtc.UtcDateTime) == date).ToList();
            days.Add(new EvidenceCollectionDayStatus(
                date,
                dayRows.Count(row => row.PacketKind.Equals("runtime_session_heartbeat", StringComparison.OrdinalIgnoreCase)),
                dayRows.Count(IsFlagged),
                dayRows.Where(row => row.PacketKind.Equals("focus_steal_snapshot", StringComparison.OrdinalIgnoreCase)).Sum(row => ParseIntToken(row.Summary, "day_delta")),
                dayRows.Any(row => row.PacketKind.Equals("budget_meter_snapshot", StringComparison.OrdinalIgnoreCase) && row.Summary.Contains("budget_exceeded=true", StringComparison.OrdinalIgnoreCase)),
                dayRows.Where(row => row.PacketKind.Equals(AuditLedgerService.SelfImprovementReportPacketKind, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(row => row.CreatedAtUtc)
                    .FirstOrDefault()
                    ?.CreatedAtUtc));
        }

        return days;
    }

    private static int ParseIntToken(string summary, string token)
    {
        var marker = token + "=";
        var start = summary.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (start < 0)
        {
            return 0;
        }

        start += marker.Length;
        var end = start;
        while (end < summary.Length && char.IsDigit(summary[end]))
        {
            end++;
        }

        return int.TryParse(summary[start..end], NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? value
            : 0;
    }

    private EvidenceCollectionStatus Empty(string label)
    {
        return new EvidenceCollectionStatus(
            Active: false,
            HasManifest: false,
            StartedAtUtc: null,
            DayN: 0,
            DayMax: 0,
            RowsToday: 0,
            FlaggedRowsToday: 0,
            LastReportAtUtc: null,
            label,
            HeartbeatCountToday: 0,
            FocusStealDeltaToday: 0,
            BudgetExceededToday: false,
            ManifestPath: "",
            Days: []);
    }

    private static bool IsFlagged(AuditLedgerRow row)
    {
        return row.DidUseNetwork ||
               row.DidUseHostedAi ||
               row.DidUseLocalModel ||
               row.DidMutate ||
               !string.IsNullOrWhiteSpace(row.Error) ||
               row.Status.Equals("Blocked", StringComparison.OrdinalIgnoreCase);
    }

    private static string? FindLatestManifest(string artifactRoot)
    {
        if (!Directory.Exists(artifactRoot))
        {
            return null;
        }

        return Directory.GetFiles(artifactRoot, "manifest.json", SearchOption.AllDirectories)
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .FirstOrDefault();
    }

    private static SoakManifest? ReadManifest(string manifestPath)
    {
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(manifestPath));
            var root = document.RootElement;
            return new SoakManifest(
                root.GetProperty("started_at_utc").GetDateTimeOffset(),
                root.GetProperty("requested_days").GetInt32());
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException or KeyNotFoundException or InvalidOperationException)
        {
            return null;
        }
    }

    private sealed record SoakManifest(DateTimeOffset StartedAtUtc, int RequestedDays);
}
