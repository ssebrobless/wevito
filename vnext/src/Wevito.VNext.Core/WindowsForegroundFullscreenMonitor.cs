using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed record FullscreenMonitorObservation(
    bool DidTransition,
    bool IsFullscreenOther,
    string Reason);

public sealed class WindowsForegroundFullscreenMonitor
{
    public static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);
    public static readonly TimeSpan EnterThreshold = TimeSpan.FromSeconds(5);
    public static readonly TimeSpan ExitThreshold = TimeSpan.FromSeconds(15);

    private readonly AuditLedgerService? _auditLedgerService;
    private bool _isFullscreenOther;
    private bool? _candidateValue;
    private DateTimeOffset? _candidateStartedAtUtc;
    private DateTimeOffset? _lastAuditAtUtc;

    public WindowsForegroundFullscreenMonitor(AuditLedgerService? auditLedgerService = null)
    {
        _auditLedgerService = auditLedgerService;
    }

    public bool IsFullscreenOther => _isFullscreenOther;

    public FullscreenMonitorObservation Observe(DesktopContext? desktopContext, DateTimeOffset nowUtc)
    {
        var observed = desktopContext?.ForegroundWindow is { IsFullscreenApp: true, IsShellSurface: false };
        if (observed == _isFullscreenOther)
        {
            _candidateValue = null;
            _candidateStartedAtUtc = null;
            return new FullscreenMonitorObservation(false, _isFullscreenOther, "");
        }

        if (_candidateValue != observed)
        {
            _candidateValue = observed;
            _candidateStartedAtUtc = nowUtc;
            return new FullscreenMonitorObservation(false, _isFullscreenOther, observed ? "enter-candidate" : "exit-candidate");
        }

        var elapsed = nowUtc - (_candidateStartedAtUtc ?? nowUtc);
        var threshold = observed ? EnterThreshold : ExitThreshold;
        if (elapsed < threshold)
        {
            return new FullscreenMonitorObservation(false, _isFullscreenOther, observed ? "enter-wait" : "exit-wait");
        }

        _isFullscreenOther = observed;
        _candidateValue = null;
        _candidateStartedAtUtc = null;
        RecordTransition(nowUtc, observed);
        return new FullscreenMonitorObservation(true, _isFullscreenOther, observed ? "entered-fullscreen-other" : "exited-fullscreen-other");
    }

    private void RecordTransition(DateTimeOffset nowUtc, bool fullscreenOther)
    {
        if (_lastAuditAtUtc == nowUtc)
        {
            return;
        }

        _lastAuditAtUtc = nowUtc;
        _auditLedgerService?.Record(new EvidencePacket(
            Guid.NewGuid(),
            "fullscreen_state_change",
            null,
            nowUtc,
            DidUseNetwork: false,
            DidUseHostedAi: false,
            DidUseLocalModel: false,
            DidMutate: false,
            ArtifactPath: "",
            Summary: fullscreenOther ? "Foreground switched into sustained fullscreen-other mode." : "Foreground exited sustained fullscreen-other mode.",
            Status: "Completed"));
    }
}
