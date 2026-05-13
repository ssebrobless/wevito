using System.Text.Json;

namespace Wevito.VNext.Core;

public sealed record DailyEvidenceSnapshotResult(
    bool Emitted,
    string Summary);

public sealed class DailyEvidenceSnapshotService
{
    private const string SchemaVersion = "1";
    private readonly string _statePath;
    private readonly AuditLedgerService _ledger;
    private readonly RuntimeBudgetMeter _budgetMeter;
    private readonly FocusStealCounter _focusStealCounter;
    private readonly KillSwitchService? _killSwitchService;

    public DailyEvidenceSnapshotService(
        AuditLedgerService ledger,
        RuntimeBudgetMeter budgetMeter,
        FocusStealCounter focusStealCounter,
        string? statePath = null,
        KillSwitchService? killSwitchService = null)
    {
        _ledger = ledger;
        _budgetMeter = budgetMeter;
        _focusStealCounter = focusStealCounter;
        _statePath = string.IsNullOrWhiteSpace(statePath) ? ResolveDefaultPath() : Path.GetFullPath(statePath);
        _killSwitchService = killSwitchService;
    }

    public DailyEvidenceSnapshotResult Tick(DateTimeOffset nowUtc, RuntimeBudgetSnapshot budget, IReadOnlyDictionary<string, string>? settings = null)
    {
        try
        {
            if (_killSwitchService?.IsActive() == true || KillSwitchService.IsActive(settings))
            {
                return new DailyEvidenceSnapshotResult(false, "kill_switch=true");
            }

            var state = ReadState();
            var today = DateOnly.FromDateTime(nowUtc.UtcDateTime);
            if (state.LastSnapshotDateUtc == today)
            {
                return new DailyEvidenceSnapshotResult(false, "daily snapshots already emitted");
            }

            var focus = _focusStealCounter.Read();
            var dayDelta = Math.Max(0, focus.Count - state.LastFocusTotal);
            var budgetReservation = _budgetMeter.ReadCurrent(budget);
            var budgetExceeded = !budgetReservation.Allowed &&
                (budgetReservation.Reason.StartsWith("Background task budget", StringComparison.OrdinalIgnoreCase) ||
                 budgetReservation.Reason.StartsWith("CPU budget", StringComparison.OrdinalIgnoreCase) ||
                 budgetReservation.Reason.StartsWith("Memory budget", StringComparison.OrdinalIgnoreCase) ||
                 budgetReservation.Reason.StartsWith("Hourly background", StringComparison.OrdinalIgnoreCase));
            var focusSummary = $"focus_steal={(dayDelta > 0).ToString().ToLowerInvariant()} day_delta={dayDelta} total={focus.Count}";
            var budgetSummary = $"budget_exceeded={budgetExceeded.ToString().ToLowerInvariant()} used_this_hour={budgetReservation.UsedThisHour} max_this_hour={budgetReservation.MaxThisHour} cpu={budgetReservation.ResourceSnapshot.CpuPercent:0.##} memory_mb={budgetReservation.ResourceSnapshot.MemoryMb}";

            _ledger.Record(Packet("focus_steal_snapshot", nowUtc, focusSummary));
            _ledger.Record(Packet("budget_meter_snapshot", nowUtc, budgetSummary));
            WriteState(new DailySnapshotState(SchemaVersion, today, focus.Count));
            return new DailyEvidenceSnapshotResult(true, $"{focusSummary}; {budgetSummary}");
        }
        catch (Exception)
        {
            return new DailyEvidenceSnapshotResult(false, "");
        }
    }

    private static EvidencePacket Packet(string packetKind, DateTimeOffset nowUtc, string summary)
    {
        return new EvidencePacket(
            Guid.NewGuid(),
            packetKind,
            TaskCardId: null,
            nowUtc,
            DidUseNetwork: false,
            DidUseHostedAi: false,
            DidUseLocalModel: false,
            DidMutate: false,
            ArtifactPath: "",
            Summary: summary,
            Status: "Completed");
    }

    private DailySnapshotState ReadState()
    {
        try
        {
            if (!File.Exists(_statePath))
            {
                return new DailySnapshotState(SchemaVersion, null, 0);
            }

            var state = JsonSerializer.Deserialize<DailySnapshotState>(File.ReadAllText(_statePath));
            return state is null || state.SchemaVersion != SchemaVersion
                ? new DailySnapshotState(SchemaVersion, null, 0)
                : state;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            return new DailySnapshotState(SchemaVersion, null, 0);
        }
    }

    private void WriteState(DailySnapshotState state)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_statePath) ?? ".");
        File.WriteAllText(_statePath, JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true }));
    }

    private static string ResolveDefaultPath()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Wevito",
            "audit",
            "daily-snapshot.json");
    }

    private sealed record DailySnapshotState(
        string SchemaVersion,
        DateOnly? LastSnapshotDateUtc,
        int LastFocusTotal);
}
