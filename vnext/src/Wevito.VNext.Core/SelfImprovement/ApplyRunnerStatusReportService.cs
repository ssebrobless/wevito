using System.Globalization;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Wevito.VNext.Contracts;
using Wevito.VNext.Core.Audit;

namespace Wevito.VNext.Core.SelfImprovement;

public sealed class ApplyRunnerStatusReportService
{
    public const string EnabledSetting = "apply_runner_status_report_enabled";
    public const string StatusReportPacketKind = SelfImprovementPacketKinds.ApplyRunnerStatusReport;

    private const string NotImplementedMarker = "apply_runner_not_implemented_in_v0";

    private static readonly IReadOnlyList<string> OutstandingPrerequisites =
    [
        "KillSwitch armed",
        "EvalGateRunner v1 enabled",
        "Heuristic judge enabled",
        "Snapshot signed and verified recently",
        "Held-out store contains >= 1 case",
        "In-distribution store contains >= 1 case",
        "Scope hash matches latest awaiting-approval artifact",
        "Replay run within window",
        "Capability default-off audit",
        "Apply runner declared not implemented",
        "apply_runner_design_approved=false",
        "apply_runner_implementation_phase_approved=false"
    ];

    private readonly string _databasePath;
    private readonly AuditLedgerService _ledger;
    private readonly KillSwitchService? _killSwitch;
    private readonly Func<IReadOnlyDictionary<string, string>> _settingsProvider;

    public ApplyRunnerStatusReportService(
        AuditLedgerService ledger,
        KillSwitchService? killSwitch = null,
        Func<IReadOnlyDictionary<string, string>>? settingsProvider = null)
    {
        _ledger = ledger;
        _databasePath = ledger.DatabasePath;
        _killSwitch = killSwitch;
        _settingsProvider = settingsProvider ?? (() => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
    }

    public ApplyRunnerStatusReport EmitReport(DateTimeOffset nowUtc)
    {
        if (_killSwitch?.IsActive() == true)
        {
            return Refused(nowUtc, "kill_switch=true");
        }

        var settings = _settingsProvider();
        if (!IsTrue(settings, EnabledSetting))
        {
            return Refused(nowUtc, $"{EnabledSetting}=false");
        }

        var report = BuildReport(Guid.NewGuid().ToString("N"), nowUtc);
        _ledger.Record(new EvidencePacket(
            Guid.NewGuid(),
            SelfImprovementPacketKinds.ApplyRunnerStatusReport,
            null,
            nowUtc,
            DidUseNetwork: false,
            DidUseHostedAi: false,
            DidUseLocalModel: false,
            DidMutate: false,
            ArtifactPath: "",
            Summary: JsonSerializer.Serialize(report, JsonDefaults.Options),
            Status: "Completed"));
        return report;
    }

    public ApplyRunnerStatusReport? ReadLatest(string databasePath, DateTimeOffset nowUtc)
    {
        _ = nowUtc;
        if (_killSwitch?.IsActive() == true)
        {
            return null;
        }

        var fullPath = Path.GetFullPath(databasePath);
        if (!File.Exists(fullPath))
        {
            return null;
        }

        using var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = fullPath,
            Mode = SqliteOpenMode.ReadOnly
        }.ToString());
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT summary
            FROM audit_ledger
            WHERE packet_kind = $packet_kind
            ORDER BY created_at_utc DESC, id DESC
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("$packet_kind", SelfImprovementPacketKinds.ApplyRunnerStatusReport);

        var summary = Convert.ToString(command.ExecuteScalar(), CultureInfo.InvariantCulture);
        return string.IsNullOrWhiteSpace(summary)
            ? null
            : JsonSerializer.Deserialize<ApplyRunnerStatusReport>(summary, JsonDefaults.Options);
    }

    public ApplyRunnerStatusReport? ReadLatest(DateTimeOffset nowUtc)
    {
        return ReadLatest(_databasePath, nowUtc);
    }

    private static ApplyRunnerStatusReport BuildReport(string reportId, DateTimeOffset nowUtc)
    {
        var applyRunnerImplemented = !string.Equals(
            SupervisedImprovementLoop.ApplyRunnerNotImplementedReason,
            NotImplementedMarker,
            StringComparison.Ordinal);
        return new ApplyRunnerStatusReport(
            reportId,
            applyRunnerImplemented,
            OutstandingPrerequisites,
            SupervisedImprovementLoop.ApplyRunnerNotImplementedReason,
            nowUtc);
    }

    private static ApplyRunnerStatusReport Refused(DateTimeOffset nowUtc, string reason)
    {
        return new ApplyRunnerStatusReport(
            "",
            false,
            [reason],
            "",
            nowUtc);
    }

    private static bool IsTrue(IReadOnlyDictionary<string, string> settings, string key)
    {
        return settings.TryGetValue(key, out var value) &&
               bool.TryParse(value, out var parsed) &&
               parsed;
    }
}
