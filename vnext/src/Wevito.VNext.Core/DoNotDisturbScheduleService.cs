using System.Text.Json;

namespace Wevito.VNext.Core;

public sealed record DoNotDisturbWindow(
    string Start,
    string End,
    IReadOnlyList<DayOfWeek>? Days = null);

public sealed record DoNotDisturbState(
    bool IsActive,
    string Reason,
    IReadOnlyDictionary<string, string> SettingsSnapshot);

public sealed class DoNotDisturbScheduleService
{
    public const string ScheduleSetting = "dnd_schedule_json";
    public const string QuickToggleUntilUtcSetting = "dnd_quick_toggle_until_utc";
    public const string EnabledSetting = "dnd_schedule_enabled";
    public const string DndStateChangedPacketKind = "dnd_state_changed";
    public const string DefaultSettingsFileName = "dnd-schedule.json";

    private readonly AuditLedgerService? _auditLedgerService;
    private readonly KillSwitchService? _killSwitchService;

    public DoNotDisturbScheduleService(
        AuditLedgerService? auditLedgerService = null,
        KillSwitchService? killSwitchService = null)
    {
        _auditLedgerService = auditLedgerService;
        _killSwitchService = killSwitchService;
    }

    public DoNotDisturbState Evaluate(IReadOnlyDictionary<string, string>? settings, DateTimeOffset nowUtc)
    {
        return EvaluateStatic(settings, nowUtc);
    }

    public static DoNotDisturbState EvaluateStatic(IReadOnlyDictionary<string, string>? settings, DateTimeOffset nowUtc)
    {
        var snapshot = settings ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (TryReadQuickToggleUntil(snapshot, out var quickUntil) && nowUtc < quickUntil)
        {
            return new DoNotDisturbState(true, $"DND quick toggle until {quickUntil:O}.", snapshot);
        }

        if (!ReadBool(snapshot, EnabledSetting, false))
        {
            return new DoNotDisturbState(false, "", snapshot);
        }

        foreach (var window in ReadSchedule(snapshot))
        {
            if (IsWithinWindow(window, nowUtc))
            {
                return new DoNotDisturbState(true, $"DND schedule active {window.Start}-{window.End}.", snapshot);
            }
        }

        return new DoNotDisturbState(false, "", snapshot);
    }

    public DoNotDisturbState ApplyQuickToggle(
        IReadOnlyDictionary<string, string> settings,
        DoNotDisturbQuickToggle toggle,
        DateTimeOffset nowUtc)
    {
        var next = new Dictionary<string, string>(settings, StringComparer.OrdinalIgnoreCase);
        var until = toggle switch
        {
            DoNotDisturbQuickToggle.Now => nowUtc.AddMinutes(30),
            DoNotDisturbQuickToggle.OneHour => nowUtc.AddHours(1),
            DoNotDisturbQuickToggle.UntilTomorrow => new DateTimeOffset(nowUtc.Year, nowUtc.Month, nowUtc.Day, 0, 0, 0, nowUtc.Offset).AddDays(1),
            DoNotDisturbQuickToggle.Off => (DateTimeOffset?)null,
            _ => null
        };

        if (until is null)
        {
            next[QuickToggleUntilUtcSetting] = "";
        }
        else
        {
            next[QuickToggleUntilUtcSetting] = until.Value.ToString("O");
        }

        Record(nowUtc, until is null ? "dnd quick toggle cleared" : $"dnd quick toggle until {until:O}");
        return Evaluate(next, nowUtc);
    }

    public string EnsureDefaultScheduleFile()
    {
        var path = ResolveDefaultSchedulePath();
        if (_killSwitchService?.IsActive() == true)
        {
            return path;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
        if (!File.Exists(path))
        {
            File.WriteAllText(path, "[]");
        }

        return path;
    }

    private static IReadOnlyList<DoNotDisturbWindow> ReadSchedule(IReadOnlyDictionary<string, string> settings)
    {
        if (!settings.TryGetValue(ScheduleSetting, out var raw) || string.IsNullOrWhiteSpace(raw))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<DoNotDisturbWindow>>(raw) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static bool IsWithinWindow(DoNotDisturbWindow window, DateTimeOffset nowUtc)
    {
        if (window.Days is { Count: > 0 } && !window.Days.Contains(nowUtc.DayOfWeek))
        {
            return false;
        }

        if (!TimeOnly.TryParse(window.Start, out var start) || !TimeOnly.TryParse(window.End, out var end))
        {
            return false;
        }

        var now = TimeOnly.FromDateTime(nowUtc.UtcDateTime);
        return start <= end
            ? now >= start && now < end
            : now >= start || now < end;
    }

    private static bool TryReadQuickToggleUntil(IReadOnlyDictionary<string, string> settings, out DateTimeOffset until)
    {
        until = default;
        return settings.TryGetValue(QuickToggleUntilUtcSetting, out var raw) &&
               DateTimeOffset.TryParse(raw, out until);
    }

    private static bool ReadBool(IReadOnlyDictionary<string, string> settings, string key, bool defaultValue)
    {
        return settings.TryGetValue(key, out var raw) && bool.TryParse(raw, out var parsed)
            ? parsed
            : defaultValue;
    }

    private void Record(DateTimeOffset nowUtc, string summary)
    {
        if (_killSwitchService?.IsActive() == true)
        {
            return;
        }

        _auditLedgerService?.Record(new EvidencePacket(
            Guid.NewGuid(),
            DndStateChangedPacketKind,
            null,
            nowUtc,
            DidUseNetwork: false,
            DidUseHostedAi: false,
            DidUseLocalModel: false,
            DidMutate: false,
            ArtifactPath: "",
            Summary: summary,
            Status: "Completed"));
    }

    private static string ResolveDefaultSchedulePath()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Wevito",
            "settings",
            DefaultSettingsFileName);
    }
}

public enum DoNotDisturbQuickToggle
{
    Off,
    Now,
    OneHour,
    UntilTomorrow
}
