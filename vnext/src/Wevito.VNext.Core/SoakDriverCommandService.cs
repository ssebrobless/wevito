using System.Text.Json;
using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed record SoakDriverCommandResult(
    bool Succeeded,
    string Message,
    string ArtifactPath = "",
    EvidenceCollectionStatus? Status = null);

public sealed class SoakDriverCommandService
{
    public const string SoakWindowEndPacketKind = "soak_window_end";

    public static readonly IReadOnlyList<string> DefaultOffSettings =
    [
        "runtime_autonomous_beta_enabled",
        "pet_model_adapter_enabled",
        "web_search_enabled",
        "local_tool_exec_enabled",
        "tuning_lora_enabled",
        KillSwitchService.KillSwitchSetting
    ];

    private readonly AuditLedgerService _ledger;
    private readonly string _artifactRoot;
    private readonly RuntimeSessionTracker _runtimeSessionTracker;
    private readonly SelfImprovementReportService _selfImprovementReportService;
    private readonly EvidenceCollectionStatusService _statusService;
    private readonly Func<DateTimeOffset> _clock;
    private readonly KillSwitchService _killSwitchService;

    public SoakDriverCommandService(
        AuditLedgerService ledger,
        string artifactRoot,
        Func<DateTimeOffset>? clock = null,
        KillSwitchService? killSwitchService = null)
    {
        _ledger = ledger;
        _artifactRoot = Path.GetFullPath(artifactRoot);
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
        _killSwitchService = killSwitchService ?? new KillSwitchService();
        _runtimeSessionTracker = new RuntimeSessionTracker(_ledger, killSwitchService: _killSwitchService);
        _selfImprovementReportService = new SelfImprovementReportService(_ledger, _killSwitchService);
        _statusService = new EvidenceCollectionStatusService(_ledger, _artifactRoot, _clock, _killSwitchService);
    }

    public static IReadOnlyDictionary<string, string> BuildDefaultSettingsSnapshot()
    {
        return DefaultOffSettings.ToDictionary(key => key, _ => bool.FalseString, StringComparer.OrdinalIgnoreCase);
    }

    public SoakDriverCommandResult Heartbeat(string reason, IReadOnlyDictionary<string, string> settings)
    {
        if (KillSwitchService.IsActive(settings) || _killSwitchService.IsActive())
        {
            return new SoakDriverCommandResult(false, "kill_switch=true");
        }

        var now = _clock();
        var result = _runtimeSessionTracker.ForceHeartbeat(now, string.IsNullOrWhiteSpace(reason) ? "scheduled" : reason);
        return result.Emitted
            ? new SoakDriverCommandResult(true, result.Summary)
            : new SoakDriverCommandResult(false, "heartbeat_not_emitted");
    }

    public SoakDriverCommandResult DayEnd()
    {
        var now = _clock();
        var since = new DateTimeOffset(now.UtcDateTime.Date, TimeSpan.Zero);
        var folder = Path.Combine(_artifactRoot, "self-improvement");
        var result = _selfImprovementReportService.Run(new SelfImprovementReportRequest(since, now, folder, now));
        return new SoakDriverCommandResult(result.Succeeded, result.Message, result.ArtifactFolder);
    }

    public SoakDriverCommandResult WindowEnd(string reason)
    {
        var now = _clock();
        Directory.CreateDirectory(_artifactRoot);
        var status = _statusService.Read();
        var summaryPath = Path.Combine(_artifactRoot, "soak-summary.md");
        File.WriteAllText(summaryPath, BuildSummary(status, now, reason));
        _ledger.Record(new EvidencePacket(
            Guid.NewGuid(),
            SoakWindowEndPacketKind,
            TaskCardId: null,
            now,
            DidUseNetwork: false,
            DidUseHostedAi: false,
            DidUseLocalModel: false,
            DidMutate: false,
            ArtifactPath: summaryPath,
            Summary: $"soak_window_end reason={SafeToken(reason)} day={status.DayN}_of_{status.DayMax}",
            Status: "Completed"));
        return new SoakDriverCommandResult(true, "soak window ended", summaryPath, status);
    }

    public SoakDriverCommandResult Status(IReadOnlyDictionary<string, string> settings)
    {
        return new SoakDriverCommandResult(true, "status", Status: _statusService.Read());
    }

    public static bool HasEnabledDefaultOffCapability(IReadOnlyDictionary<string, string> settings, out string key)
    {
        foreach (var setting in DefaultOffSettings)
        {
            if (settings.TryGetValue(setting, out var raw) &&
                bool.TryParse(raw, out var enabled) &&
                enabled)
            {
                key = setting;
                return true;
            }
        }

        key = "";
        return false;
    }

    public static void WriteManifest(
        string folder,
        DateTimeOffset startedAtUtc,
        int days,
        int heartbeatMinutes,
        string artifactRoot,
        IReadOnlyDictionary<string, string> settings)
    {
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, "manifest.json"), JsonSerializer.Serialize(new
        {
            schema_version = "1",
            started_at_utc = startedAtUtc,
            requested_days = days,
            heartbeat_minutes = heartbeatMinutes,
            artifact_root = Path.GetFullPath(artifactRoot),
            initial_settings_snapshot_sha256 = EvidenceCollectionStatusService.ComputeSettingsHash(settings)
        }, JsonDefaults.Options));
    }

    private static string BuildSummary(EvidenceCollectionStatus status, DateTimeOffset nowUtc, string reason)
    {
        var lines = new List<string>
        {
            "# Soak Window Summary",
            "",
            $"- Ended: {nowUtc:O}",
            $"- Reason: {reason}",
            $"- Readiness: {status.LastReadinessLabel}",
            $"- Day: {status.DayN} of {status.DayMax}",
            $"- Rows today: {status.RowsToday}",
            $"- Flagged rows today: {status.FlaggedRowsToday}",
            ""
        };
        lines.Add("## Days");
        if (status.Days.Count == 0)
        {
            lines.Add("- No per-day evidence rows were available.");
        }
        else
        {
            lines.AddRange(status.Days.Select(day => $"- {day.DateUtc}: heartbeats={day.HeartbeatCount}, flagged={day.FlaggedRows}, focus_delta={day.FocusStealDelta}, budget_exceeded={day.BudgetExceeded}"));
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static string SafeToken(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? "unspecified"
            : value.Replace(' ', '_').Replace(';', '_');
    }
}
