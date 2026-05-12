using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public enum RuntimeSupervisorMode
{
    Active,
    Quiet,
    PetOnly
}

public sealed record RuntimeSupervisorSettings(
    RuntimeSupervisorMode Mode,
    bool BackgroundWorkAllowed,
    bool NoFocusSteal,
    bool AutoQuietDuringFullscreen,
    int MaxBackgroundTasksPerHour,
    int CpuBudgetPercent,
    int MemoryBudgetMb);

public sealed record RuntimeSupervisorStatus(
    RuntimeSupervisorMode Mode,
    bool BackgroundWorkAllowed,
    bool ToolWindowAllowed,
    bool IsQuietedForFullscreen,
    string UserStatus,
    string BlockReason);

public sealed class RuntimeSupervisorService
{
    public const string QuietModeSetting = "runtime_quiet_mode";
    public const string PetOnlyModeSetting = "runtime_pet_only_mode";
    public const string BackgroundWorkAllowedSetting = "runtime_background_work_allowed";
    public const string NoFocusStealSetting = "runtime_no_focus_steal";
    public const string AutoQuietFullscreenSetting = "runtime_auto_quiet_fullscreen";
    public const string MaxBackgroundTasksPerHourSetting = "runtime_max_background_tasks_per_hour";
    public const string CpuBudgetPercentSetting = "runtime_cpu_budget_percent";
    public const string MemoryBudgetMbSetting = "runtime_memory_budget_mb";

    public RuntimeSupervisorSettings ReadSettings(IReadOnlyDictionary<string, string>? settings)
    {
        var snapshot = settings ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var quiet = ReadBool(snapshot, QuietModeSetting, false);
        var petOnly = ReadBool(snapshot, PetOnlyModeSetting, false);
        var mode = petOnly
            ? RuntimeSupervisorMode.PetOnly
            : quiet
                ? RuntimeSupervisorMode.Quiet
                : RuntimeSupervisorMode.Active;

        return new RuntimeSupervisorSettings(
            mode,
            BackgroundWorkAllowed: ReadBool(snapshot, BackgroundWorkAllowedSetting, false),
            NoFocusSteal: ReadBool(snapshot, NoFocusStealSetting, true),
            AutoQuietDuringFullscreen: ReadBool(snapshot, AutoQuietFullscreenSetting, true),
            MaxBackgroundTasksPerHour: Clamp(ReadInt(snapshot, MaxBackgroundTasksPerHourSetting, 4), 0, 60),
            CpuBudgetPercent: Clamp(ReadInt(snapshot, CpuBudgetPercentSetting, 20), 1, 100),
            MemoryBudgetMb: Clamp(ReadInt(snapshot, MemoryBudgetMbSetting, 512), 64, 8192));
    }

    public RuntimeSupervisorStatus Evaluate(
        IReadOnlyDictionary<string, string>? settings,
        DesktopContext? desktopContext = null,
        bool isUserInitiatedToolOpen = false)
    {
        var parsed = ReadSettings(settings);
        var isFullscreen = desktopContext?.ForegroundWindow.IsFullscreenApp == true;
        var quietedForFullscreen = parsed.AutoQuietDuringFullscreen && isFullscreen;
        var effectiveMode = quietedForFullscreen && parsed.Mode == RuntimeSupervisorMode.Active
            ? RuntimeSupervisorMode.Quiet
            : parsed.Mode;

        var backgroundAllowed = effectiveMode == RuntimeSupervisorMode.Active && parsed.BackgroundWorkAllowed;
        var toolWindowAllowed = effectiveMode != RuntimeSupervisorMode.PetOnly &&
                                (!quietedForFullscreen || isUserInitiatedToolOpen) &&
                                (!parsed.NoFocusSteal || !isFullscreen || isUserInitiatedToolOpen);

        var blockReason = BuildBlockReason(effectiveMode, quietedForFullscreen, parsed.BackgroundWorkAllowed);
        return new RuntimeSupervisorStatus(
            effectiveMode,
            backgroundAllowed,
            toolWindowAllowed,
            quietedForFullscreen,
            BuildUserStatus(effectiveMode, backgroundAllowed, quietedForFullscreen, parsed),
            blockReason);
    }

    public bool CanStartBackgroundWork(RuntimeSupervisorStatus status, out string reason)
    {
        if (status.BackgroundWorkAllowed)
        {
            reason = "";
            return true;
        }

        reason = string.IsNullOrWhiteSpace(status.BlockReason)
            ? "Background PET TASKS are paused by the runtime supervisor."
            : status.BlockReason;
        return false;
    }

    public bool CanStartUserInitiatedWork(RuntimeSupervisorStatus status, out string reason)
    {
        if (status.Mode == RuntimeSupervisorMode.Active)
        {
            reason = "";
            return true;
        }

        reason = string.IsNullOrWhiteSpace(status.BlockReason)
            ? "Helper work is paused by the runtime supervisor."
            : status.BlockReason;
        return false;
    }

    private static string BuildUserStatus(
        RuntimeSupervisorMode mode,
        bool backgroundAllowed,
        bool quietedForFullscreen,
        RuntimeSupervisorSettings settings)
    {
        if (mode == RuntimeSupervisorMode.PetOnly)
        {
            return "Pet-only mode: Wevito will keep the pets visible but will not start helper work.";
        }

        if (quietedForFullscreen)
        {
            return "Quiet mode: fullscreen app detected, helper work is paused.";
        }

        if (mode == RuntimeSupervisorMode.Quiet)
        {
            return "Quiet mode: helper work is paused until you turn it back on.";
        }

        return backgroundAllowed
            ? $"Active: background helper previews may run within budget ({settings.MaxBackgroundTasksPerHour}/hr, CPU {settings.CpuBudgetPercent}%, memory {settings.MemoryBudgetMb} MB)."
            : "Active: background helper work is off; user-started previews are still available.";
    }

    private static string BuildBlockReason(RuntimeSupervisorMode mode, bool quietedForFullscreen, bool configuredBackgroundAllowed)
    {
        if (mode == RuntimeSupervisorMode.PetOnly)
        {
            return "Pet-only mode blocks helper background work.";
        }

        if (quietedForFullscreen)
        {
            return "Fullscreen quiet mode blocks helper background work.";
        }

        if (mode == RuntimeSupervisorMode.Quiet)
        {
            return "Quiet mode blocks helper background work.";
        }

        return configuredBackgroundAllowed
            ? ""
            : "Background helper work is disabled.";
    }

    private static bool ReadBool(IReadOnlyDictionary<string, string> settings, string key, bool defaultValue)
    {
        return settings.TryGetValue(key, out var raw) && bool.TryParse(raw, out var parsed)
            ? parsed
            : defaultValue;
    }

    private static int ReadInt(IReadOnlyDictionary<string, string> settings, string key, int defaultValue)
    {
        return settings.TryGetValue(key, out var raw) && int.TryParse(raw, out var parsed)
            ? parsed
            : defaultValue;
    }

    private static int Clamp(int value, int min, int max)
    {
        return Math.Min(max, Math.Max(min, value));
    }
}
