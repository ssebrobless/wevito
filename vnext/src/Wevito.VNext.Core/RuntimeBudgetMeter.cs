using System.Diagnostics;
using System.Text.Json;

namespace Wevito.VNext.Core;

public sealed record RuntimeResourceSnapshot(
    double CpuPercent,
    int MemoryMb,
    DateTimeOffset CapturedAtUtc,
    double WevitoCpuPercent = 0,
    double GpuUtilizationPercent = 0,
    int AvailableMemoryMb = int.MaxValue);

public sealed record RuntimeBudgetSnapshot(
    int MaxBackgroundTasksPerHour,
    int CpuBudgetPercent,
    int MemoryBudgetMb);

public sealed record RuntimeBudgetReservation(
    bool Allowed,
    int UsedThisHour,
    int MaxThisHour,
    RuntimeResourceSnapshot ResourceSnapshot,
    string Reason);

public sealed record RuntimeUserProtectionFloors(
    double MaxWevitoCpuPercent = 50,
    double MaxGpuUtilizationPercent = 80,
    int MinimumAvailableMemoryMb = 4096);

public sealed record TieredRuntimeBudgetReservation(
    bool Allowed,
    RuntimeBudgetReservation RuntimeReservation,
    WorkloadTierReservation TierReservation,
    string Reason);

public sealed class RuntimeBudgetMeter
{
    private const string SchemaVersion = "1";
    private readonly string _statePath;
    private readonly Func<DateTimeOffset> _clock;
    private readonly Func<RuntimeResourceSnapshot> _resourceReader;
    private DateTimeOffset? _lastFlushAtUtc;

    public RuntimeBudgetMeter(
        string? statePath = null,
        Func<DateTimeOffset>? clock = null,
        Func<RuntimeResourceSnapshot>? resourceReader = null)
    {
        _statePath = string.IsNullOrWhiteSpace(statePath)
            ? ResolveDefaultStatePath()
            : statePath;
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
        _resourceReader = resourceReader ?? ReadCurrentProcessResources;
    }

    public RuntimeBudgetReservation TryReserve(RuntimeBudgetSnapshot budget)
    {
        return TryReserve(budget, isForeground: false);
    }

    public RuntimeBudgetReservation TryReserve(RuntimeBudgetSnapshot budget, bool isForeground)
    {
        var now = _clock();
        var resourceSnapshot = _resourceReader();
        var state = ReadState();
        var currentHour = TruncateToHour(now);
        if (state.HourStartedAtUtc != currentHour)
        {
            state = new BudgetMeterState(SchemaVersion, currentHour, 0);
        }

        var protectionFloorReason = isForeground ? "" : BuildUserProtectionFloorReason(resourceSnapshot);
        if (!string.IsNullOrWhiteSpace(protectionFloorReason))
        {
            WriteState(state);
            return new RuntimeBudgetReservation(false, state.UsedThisHour, budget.MaxBackgroundTasksPerHour, resourceSnapshot, protectionFloorReason);
        }

        if (budget.MaxBackgroundTasksPerHour <= 0)
        {
            WriteState(state);
            return new RuntimeBudgetReservation(false, state.UsedThisHour, budget.MaxBackgroundTasksPerHour, resourceSnapshot, "Background task budget is zero.");
        }

        if (state.UsedThisHour >= budget.MaxBackgroundTasksPerHour)
        {
            WriteState(state);
            return new RuntimeBudgetReservation(false, state.UsedThisHour, budget.MaxBackgroundTasksPerHour, resourceSnapshot, "Hourly background task budget is exhausted.");
        }

        if (resourceSnapshot.CpuPercent > budget.CpuBudgetPercent)
        {
            WriteState(state);
            return new RuntimeBudgetReservation(false, state.UsedThisHour, budget.MaxBackgroundTasksPerHour, resourceSnapshot, "CPU budget is exceeded.");
        }

        if (resourceSnapshot.MemoryMb > budget.MemoryBudgetMb)
        {
            WriteState(state);
            return new RuntimeBudgetReservation(false, state.UsedThisHour, budget.MaxBackgroundTasksPerHour, resourceSnapshot, "Memory budget is exceeded.");
        }

        state = state with { UsedThisHour = state.UsedThisHour + 1 };
        WriteState(state);
        return new RuntimeBudgetReservation(true, state.UsedThisHour, budget.MaxBackgroundTasksPerHour, resourceSnapshot, "");
    }

    public TieredRuntimeBudgetReservation TryReserve(
        RuntimeBudgetSnapshot budget,
        WorkloadTier tier,
        double requestedCpuPercent,
        WorkloadTierSnapshot tierSnapshot,
        WorkloadTierService? tierService = null)
    {
        var service = tierService ?? new WorkloadTierService();
        var tierReservation = service.TryReserve(tier, requestedCpuPercent, tierSnapshot);
        if (!tierReservation.Allowed)
        {
            var current = ReadCurrent(budget);
            return new TieredRuntimeBudgetReservation(false, current, tierReservation, tierReservation.Reason);
        }

        var runtimeReservation = TryReserve(budget, tier == WorkloadTier.UserForeground);
        return new TieredRuntimeBudgetReservation(
            runtimeReservation.Allowed,
            runtimeReservation,
            tierReservation,
            runtimeReservation.Allowed ? "" : runtimeReservation.Reason);
    }

    public bool EnsureStateFile()
    {
        var existed = File.Exists(_statePath);
        WriteState(ReadState());
        return existed;
    }

    public bool FlushIfDue(TimeSpan? minimumInterval = null)
    {
        var now = _clock();
        var interval = minimumInterval ?? TimeSpan.FromMinutes(5);
        if (_lastFlushAtUtc is not null && now - _lastFlushAtUtc < interval)
        {
            return false;
        }

        WriteState(ReadState());
        _lastFlushAtUtc = now;
        return true;
    }

    public RuntimeBudgetReservation ReadCurrent(RuntimeBudgetSnapshot budget)
    {
        var now = _clock();
        var resourceSnapshot = _resourceReader();
        var state = ReadState();
        var currentHour = TruncateToHour(now);
        if (state.HourStartedAtUtc != currentHour)
        {
            state = new BudgetMeterState(SchemaVersion, currentHour, 0);
            WriteState(state);
        }

        var reason = BuildBlockReason(state, budget, resourceSnapshot, isForeground: false);
        return new RuntimeBudgetReservation(
            string.IsNullOrWhiteSpace(reason),
            state.UsedThisHour,
            budget.MaxBackgroundTasksPerHour,
            resourceSnapshot,
            reason);
    }

    private static string BuildBlockReason(
        BudgetMeterState state,
        RuntimeBudgetSnapshot budget,
        RuntimeResourceSnapshot resourceSnapshot,
        bool isForeground)
    {
        if (!isForeground)
        {
            var floorReason = BuildUserProtectionFloorReason(resourceSnapshot);
            if (!string.IsNullOrWhiteSpace(floorReason))
            {
                return floorReason;
            }
        }

        if (budget.MaxBackgroundTasksPerHour <= 0)
        {
            return "Background task budget is zero.";
        }

        if (state.UsedThisHour >= budget.MaxBackgroundTasksPerHour)
        {
            return "Hourly background task budget is exhausted.";
        }

        if (resourceSnapshot.CpuPercent > budget.CpuBudgetPercent)
        {
            return "CPU budget is exceeded.";
        }

        return resourceSnapshot.MemoryMb > budget.MemoryBudgetMb
            ? "Memory budget is exceeded."
            : "";
    }

    public static string BuildUserProtectionFloorReason(
        RuntimeResourceSnapshot resourceSnapshot,
        RuntimeUserProtectionFloors? floors = null)
    {
        var parsed = floors ?? new RuntimeUserProtectionFloors();
        if (resourceSnapshot.WevitoCpuPercent > parsed.MaxWevitoCpuPercent)
        {
            return "User protection floor blocked reservation: Wevito CPU would exceed 50%.";
        }

        if (resourceSnapshot.GpuUtilizationPercent > parsed.MaxGpuUtilizationPercent)
        {
            return "User protection floor blocked reservation: GPU utilization is above 80%.";
        }

        return resourceSnapshot.AvailableMemoryMb < parsed.MinimumAvailableMemoryMb
            ? "User protection floor blocked reservation: less than 4 GB RAM is available."
            : "";
    }

    private BudgetMeterState ReadState()
    {
        try
        {
            if (!File.Exists(_statePath))
            {
                return new BudgetMeterState(SchemaVersion, TruncateToHour(_clock()), 0);
            }

            var state = JsonSerializer.Deserialize<BudgetMeterState>(File.ReadAllText(_statePath));
            return state is null || state.SchemaVersion != SchemaVersion
                ? new BudgetMeterState(SchemaVersion, TruncateToHour(_clock()), 0)
                : state;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            return new BudgetMeterState(SchemaVersion, TruncateToHour(_clock()), 0);
        }
    }

    private void WriteState(BudgetMeterState state)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_statePath)!);
        var json = JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_statePath, json);
    }

    private static DateTimeOffset TruncateToHour(DateTimeOffset value)
    {
        return new DateTimeOffset(value.Year, value.Month, value.Day, value.Hour, 0, 0, value.Offset).ToUniversalTime();
    }

    private static RuntimeResourceSnapshot ReadCurrentProcessResources()
    {
        using var process = Process.GetCurrentProcess();
        var memoryMb = (int)Math.Ceiling(process.WorkingSet64 / 1024d / 1024d);
        return new RuntimeResourceSnapshot(0, memoryMb, DateTimeOffset.UtcNow);
    }

    private static string ResolveDefaultStatePath()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Wevito",
            "audit",
            "budget-meter.json");
    }

    private sealed record BudgetMeterState(
        string SchemaVersion,
        DateTimeOffset HourStartedAtUtc,
        int UsedThisHour);
}
