using System.Text.Json;

namespace Wevito.VNext.Core;

public sealed record RuntimeSessionTrackerResult(
    bool Emitted,
    string PacketKind,
    string Summary);

public sealed class RuntimeSessionTracker
{
    public const string HeartbeatMinutesSetting = "runtime_session_heartbeat_minutes";
    public const int DefaultHeartbeatMinutes = 60;
    public const int MinimumHeartbeatMinutes = 15;

    private const string SchemaVersion = "1";
    private readonly string _statePath;
    private readonly AuditLedgerService _ledger;
    private readonly KillSwitchService? _killSwitchService;

    public RuntimeSessionTracker(AuditLedgerService ledger, string? statePath = null, KillSwitchService? killSwitchService = null)
    {
        _ledger = ledger;
        _statePath = string.IsNullOrWhiteSpace(statePath) ? ResolveDefaultPath() : Path.GetFullPath(statePath);
        _killSwitchService = killSwitchService;
    }

    public RuntimeSessionTrackerResult Tick(DateTimeOffset nowUtc, IReadOnlyDictionary<string, string>? settings = null)
    {
        try
        {
            if (_killSwitchService?.IsActive() == true || KillSwitchService.IsActive(settings))
            {
                var paused = Record("runtime_session_paused", nowUtc, ReadState().SessionStartedAtUtc, "kill_switch_active=true");
                return paused;
            }

            var state = ReadState();
            if (state.SessionStartedAtUtc is null)
            {
                state = new RuntimeSessionState(SchemaVersion, nowUtc, nowUtc);
                WriteState(state);
                return Record("runtime_session_start", nowUtc, state.SessionStartedAtUtc, "session_started=true");
            }

            var minutes = ResolveHeartbeatMinutes(settings);
            if (state.LastHeartbeatAtUtc is not null && nowUtc - state.LastHeartbeatAtUtc < TimeSpan.FromMinutes(minutes))
            {
                return new RuntimeSessionTrackerResult(false, "", "");
            }

            state = state with { LastHeartbeatAtUtc = nowUtc };
            WriteState(state);
            return Record("runtime_session_heartbeat", nowUtc, state.SessionStartedAtUtc, "heartbeat=true");
        }
        catch (Exception)
        {
            return new RuntimeSessionTrackerResult(false, "", "");
        }
    }

    public RuntimeSessionTrackerResult End(DateTimeOffset nowUtc)
    {
        try
        {
            var state = ReadState();
            if (state.SessionStartedAtUtc is null)
            {
                return new RuntimeSessionTrackerResult(false, "", "");
            }

            var result = Record("runtime_session_end", nowUtc, state.SessionStartedAtUtc, "session_ended=true");
            WriteState(new RuntimeSessionState(SchemaVersion, null, null));
            return result;
        }
        catch (Exception)
        {
            return new RuntimeSessionTrackerResult(false, "", "");
        }
    }

    private RuntimeSessionTrackerResult Record(string packetKind, DateTimeOffset nowUtc, DateTimeOffset? startedAtUtc, string suffix)
    {
        var uptime = startedAtUtc is null ? TimeSpan.Zero : nowUtc - startedAtUtc.Value;
        var uptimeHours = Math.Max(0, (int)Math.Floor(uptime.TotalHours));
        var proof = uptimeHours >= 4 ? " uptime_hours>=4" : "";
        var summary = $"runtime_session uptime_hours={uptimeHours}{proof} {suffix}";
        _ledger.Record(new EvidencePacket(
            Guid.NewGuid(),
            packetKind,
            TaskCardId: null,
            nowUtc,
            DidUseNetwork: false,
            DidUseHostedAi: false,
            DidUseLocalModel: false,
            DidMutate: false,
            ArtifactPath: _statePath,
            Summary: summary,
            Status: "Completed"));
        return new RuntimeSessionTrackerResult(true, packetKind, summary);
    }

    private static int ResolveHeartbeatMinutes(IReadOnlyDictionary<string, string>? settings)
    {
        if (settings is not null &&
            settings.TryGetValue(HeartbeatMinutesSetting, out var raw) &&
            int.TryParse(raw, out var minutes))
        {
            return Math.Max(MinimumHeartbeatMinutes, minutes);
        }

        return DefaultHeartbeatMinutes;
    }

    private RuntimeSessionState ReadState()
    {
        try
        {
            if (!File.Exists(_statePath))
            {
                return new RuntimeSessionState(SchemaVersion, null, null);
            }

            var state = JsonSerializer.Deserialize<RuntimeSessionState>(File.ReadAllText(_statePath));
            return state is null || state.SchemaVersion != SchemaVersion
                ? new RuntimeSessionState(SchemaVersion, null, null)
                : state;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            return new RuntimeSessionState(SchemaVersion, null, null);
        }
    }

    private void WriteState(RuntimeSessionState state)
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
            "runtime-session.json");
    }

    private sealed record RuntimeSessionState(
        string SchemaVersion,
        DateTimeOffset? SessionStartedAtUtc,
        DateTimeOffset? LastHeartbeatAtUtc);
}
