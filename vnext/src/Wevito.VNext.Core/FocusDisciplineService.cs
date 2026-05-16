using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed record WindowShowRequest(
    string WindowName,
    bool UserInitiated,
    bool IsFirstLaunchWizard);

public sealed record FocusDisciplineDecision(
    bool ShowActivated,
    bool PreventedFocusSteal,
    string Reason);

public sealed class FocusDisciplineService
{
    public const string FocusProtectedPacketKind = "focus_protected";

    private readonly AuditLedgerService? _auditLedgerService;
    private readonly KillSwitchService? _killSwitchService;

    public FocusDisciplineService(
        AuditLedgerService? auditLedgerService = null,
        KillSwitchService? killSwitchService = null)
    {
        _auditLedgerService = auditLedgerService;
        _killSwitchService = killSwitchService;
    }

    public FocusDisciplineDecision Decide(WindowShowRequest request, DateTimeOffset nowUtc)
    {
        if (request.IsFirstLaunchWizard)
        {
            return new FocusDisciplineDecision(true, false, "first_launch_wizard_may_take_focus");
        }

        if (request.UserInitiated)
        {
            return new FocusDisciplineDecision(true, false, "explicit_user_click");
        }

        Record(nowUtc, $"Protected focus while showing {request.WindowName} in the background.");
        return new FocusDisciplineDecision(false, true, "background_show_without_focus");
    }

    private void Record(DateTimeOffset timestamp, string summary)
    {
        if (_killSwitchService?.IsActive() == true)
        {
            return;
        }

        _auditLedgerService?.Record(new EvidencePacket(
            Guid.NewGuid(),
            FocusProtectedPacketKind,
            null,
            timestamp,
            DidUseNetwork: false,
            DidUseHostedAi: false,
            DidUseLocalModel: false,
            DidMutate: false,
            ArtifactPath: "",
            Summary: summary,
            Status: "Protected",
            Error: ""));
    }
}
