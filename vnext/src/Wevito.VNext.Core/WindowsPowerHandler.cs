using Microsoft.Win32;
using System.Runtime.Versioning;

namespace Wevito.VNext.Core;

public enum WindowsPowerRuntimeEvent
{
    Sleep,
    Resume,
    Lock,
    Unlock
}

public sealed record WindowsPowerHandlerResult(
    IReadOnlyDictionary<string, string> SettingsSnapshot,
    bool ForcedQuiet,
    bool ResumedWithoutAutoActive,
    string PacketKind);

[SupportedOSPlatform("windows")]
public sealed class WindowsPowerHandler : IDisposable
{
    private readonly AuditLedgerService? _auditLedgerService;
    private bool _isSubscribed;

    public WindowsPowerHandler(AuditLedgerService? auditLedgerService = null)
    {
        _auditLedgerService = auditLedgerService;
    }

    public event Action<WindowsPowerRuntimeEvent>? RuntimeEventObserved;

    public bool IsSubscribed => _isSubscribed;

    public void Subscribe()
    {
        if (_isSubscribed)
        {
            return;
        }

        SystemEvents.PowerModeChanged += OnPowerModeChanged;
        SystemEvents.SessionSwitch += OnSessionSwitch;
        _isSubscribed = true;
    }

    public WindowsPowerHandlerResult ApplyRuntimeEvent(
        IReadOnlyDictionary<string, string> settings,
        WindowsPowerRuntimeEvent runtimeEvent,
        DateTimeOffset nowUtc)
    {
        var next = new Dictionary<string, string>(settings, StringComparer.OrdinalIgnoreCase);
        var packetKind = runtimeEvent switch
        {
            WindowsPowerRuntimeEvent.Sleep => "power_sleep",
            WindowsPowerRuntimeEvent.Resume => "power_resume",
            WindowsPowerRuntimeEvent.Lock => "session_lock",
            WindowsPowerRuntimeEvent.Unlock => "session_unlock",
            _ => "power_event"
        };

        var forceQuiet = runtimeEvent is WindowsPowerRuntimeEvent.Sleep or WindowsPowerRuntimeEvent.Lock;
        if (forceQuiet)
        {
            next[RuntimeSupervisorService.QuietModeSetting] = bool.TrueString;
            next[RuntimeSupervisorService.BackgroundWorkAllowedSetting] = bool.FalseString;
        }

        Record(packetKind, nowUtc, forceQuiet);
        return new WindowsPowerHandlerResult(
            next,
            ForcedQuiet: forceQuiet,
            ResumedWithoutAutoActive: runtimeEvent is WindowsPowerRuntimeEvent.Resume or WindowsPowerRuntimeEvent.Unlock,
            packetKind);
    }

    public void Dispose()
    {
        if (!_isSubscribed)
        {
            return;
        }

        SystemEvents.PowerModeChanged -= OnPowerModeChanged;
        SystemEvents.SessionSwitch -= OnSessionSwitch;
        _isSubscribed = false;
    }

    private void OnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
    {
        var runtimeEvent = e.Mode switch
        {
            PowerModes.Suspend => WindowsPowerRuntimeEvent.Sleep,
            PowerModes.Resume => WindowsPowerRuntimeEvent.Resume,
            _ => (WindowsPowerRuntimeEvent?)null
        };

        if (runtimeEvent is not null)
        {
            RuntimeEventObserved?.Invoke(runtimeEvent.Value);
        }
    }

    private void OnSessionSwitch(object sender, SessionSwitchEventArgs e)
    {
        var runtimeEvent = e.Reason switch
        {
            SessionSwitchReason.SessionLock => WindowsPowerRuntimeEvent.Lock,
            SessionSwitchReason.SessionUnlock => WindowsPowerRuntimeEvent.Unlock,
            _ => (WindowsPowerRuntimeEvent?)null
        };

        if (runtimeEvent is not null)
        {
            RuntimeEventObserved?.Invoke(runtimeEvent.Value);
        }
    }

    private void Record(string packetKind, DateTimeOffset nowUtc, bool forceQuiet)
    {
        _auditLedgerService?.Record(new EvidencePacket(
            Guid.NewGuid(),
            packetKind,
            null,
            nowUtc,
            DidUseNetwork: false,
            DidUseHostedAi: false,
            DidUseLocalModel: false,
            DidMutate: false,
            ArtifactPath: "",
            Summary: forceQuiet ? "Power/session event forced Quiet mode and halted helper work." : "Power/session event observed; Wevito did not auto-return to Active.",
            Status: forceQuiet ? "Blocked" : "Completed"));
    }
}
