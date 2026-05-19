namespace Wevito.VNext.Core.SelfImprovement;

internal static class RefusedApprovalReasonCatalog
{
    public const string ApplyRunnerNotImplemented = "apply_runner_not_implemented_in_v0";
    public const string ScopeHashMismatch = "scope_hash_mismatch";
    public const string NotConfirmedInThisMessage = "not_confirmed_in_this_message";
    public const string EmptyConfirmationText = "empty_confirmation_text";
    public const string StaleConfirmation = "stale_confirmation";
    public const string ScopeIdMismatch = "scope_id_mismatch";
    public const string OperationIdMismatch = "operation_id_mismatch";
    public const string KillSwitchActive = "kill_switch=true";
    public const string ApprovalMissing = "approval_missing";

    public static readonly IReadOnlySet<string> Known = new HashSet<string>(StringComparer.Ordinal)
    {
        ApplyRunnerNotImplemented,
        ScopeHashMismatch,
        NotConfirmedInThisMessage,
        EmptyConfirmationText,
        StaleConfirmation,
        ScopeIdMismatch,
        OperationIdMismatch,
        KillSwitchActive,
        ApprovalMissing
    };

    public static string ToDisplayText(string reason)
    {
        return reason switch
        {
            ApplyRunnerNotImplemented => "Apply runner is intentionally not implemented in v0. The supervised loop refused this apply as designed.",
            ScopeHashMismatch => "The approved scope hash did not match the reviewed proposal scope.",
            NotConfirmedInThisMessage => "The user did not confirm apply approval in the current message.",
            EmptyConfirmationText => "The approval text was empty.",
            StaleConfirmation => "The approval expired before it could be used.",
            ScopeIdMismatch => "The approved scope id did not match the waiting card.",
            OperationIdMismatch => "The approved operation id did not match the waiting card.",
            KillSwitchActive => "Stop Everything was active, so the apply request was refused.",
            ApprovalMissing => "No approval payload was provided.",
            _ => "Other refused reason; raw text hidden."
        };
    }
}
