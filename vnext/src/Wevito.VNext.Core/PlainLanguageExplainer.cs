namespace Wevito.VNext.Core;

public sealed class PlainLanguageExplainer
{
    private readonly Action<string>? _warnUnknownPacketKind;

    public PlainLanguageExplainer(Action<string>? warnUnknownPacketKind = null)
    {
        _warnUnknownPacketKind = warnUnknownPacketKind;
    }

    public static IReadOnlyList<string> KnownPacketKinds { get; } =
    [
        "activity_summary",
        "assetInventory",
        "audioAssist",
        "buildProof",
        "codePatchPlan",
        "codeReview",
        "guardedMutation",
        "kill_switch",
        "learning_promotion",
        "localDocs",
        "local_file_preview",
        "local_model_call",
        "mutation_apply",
        "mutation_proposal",
        "mutation_rollback",
        "petState",
        "pet_fps_snapshot",
        "pet_fps_violation",
        "proof_packet",
        "process_crash_recovery",
        "promotion_criteria_snapshot",
        "promotion_decision",
        "ram_pressure_emergency",
        "ram_pressure_event",
        "budget_meter_snapshot",
        "focus_steal_snapshot",
        "image_gen_idle_guard_blocked",
        "power_resume",
        "power_sleep",
        "runtime_session",
        "runtime_session_end",
        "runtime_session_heartbeat",
        "runtime_session_paused",
        "runtime_autonomous_beta_user_consent",
        "runtime_session_start",
        "scheduler_proposal",
        "screenCapture",
        "session_lock",
        "session_unlock",
        "soak_window_end",
        "spriteAudit",
        "train_plan",
        "translateText",
        "tuning_apply",
        "tuning_rollback",
        "user_interacting_state_cleared",
        "user_interacting_state_entered",
        "web_fetch"
    ];

    public string Explain(AuditLedgerRow row)
    {
        var label = ExplainPacketKind(row.PacketKind);
        var badges = new List<string>();
        if (row.DidUseNetwork)
        {
            badges.Add("used network");
        }

        if (row.DidUseHostedAi)
        {
            badges.Add("used hosted AI");
        }

        if (row.DidMutate)
        {
            badges.Add("changed files");
        }

        return badges.Count == 0 ? label : $"{label} ({string.Join(", ", badges)})";
    }

    public string ExplainPacketKind(string packetKind)
    {
        return packetKind switch
        {
            "activity_summary" => "Summarized Wevito's recent helper activity.",
            "assetInventory" => "Reviewed the local asset inventory.",
            "audioAssist" => "Prepared an audio helper result.",
            "buildProof" => "Prepared or ran a build proof.",
            "codePatchPlan" => "Prepared a code-fix plan.",
            "codeReview" => "Reviewed code and wrote a report.",
            "guardedMutation" => "Prepared a guarded mutation preview.",
            "kill_switch" => "Blocked helper work because Stop Everything is active.",
            "learning_promotion" => "Promoted reviewed learning data.",
            "localDocs" => "Summarized local project documents.",
            "local_file_preview" => "Previewed approved local file access.",
            "local_model_call" => "Prepared a local model response.",
            "mutation_apply" => "Applied a reviewed guarded mutation.",
            "mutation_proposal" => "Prepared a mutation proposal.",
            "mutation_rollback" => "Rolled back a guarded mutation.",
            "petState" => "Reviewed pet state.",
            "pet_fps_snapshot" => "Recorded pet-game frame-rate health.",
            "pet_fps_violation" => "Protected the pet game after frame rate fell below the floor.",
            "proof_packet" => "Recorded a proof packet.",
            "process_crash_recovery" => "Recorded child-process crash recovery status.",
            "promotion_criteria_snapshot" => "Computed the autonomous-beta promotion criteria snapshot.",
            "promotion_decision" => "Recorded an autonomous-beta promotion decision packet.",
            "ram_pressure_event" => "Reduced AI work because system memory was low.",
            "ram_pressure_emergency" => "Stopped helper work because memory pressure was unsafe.",
            "budget_meter_snapshot" => "Recorded the daily resource budget snapshot.",
            "focus_steal_snapshot" => "Recorded the daily focus-steal counter snapshot.",
            "image_gen_idle_guard_blocked" => "Blocked background image generation because a pet was not idle.",
            "power_sleep" => "Wevito entered Quiet mode because the system slept.",
            "power_resume" => "Wevito recorded the system resuming.",
            "runtime_session" => "Recorded runtime supervisor activity.",
            "runtime_session_start" => "Wevito recorded the start of an active runtime session.",
            "runtime_session_heartbeat" => "Wevito recorded a runtime session heartbeat (still alive).",
            "runtime_session_end" => "Wevito recorded the end of an active runtime session.",
            "runtime_session_paused" => "Wevito paused background work because Stop Everything is active.",
            "runtime_autonomous_beta_user_consent" => "Recorded explicit user consent to enable the autonomous-beta loop.",
            "scheduler_proposal" => "Proposed a background helper task.",
            "screenCapture" => "Prepared a screen capture helper result.",
            "session_lock" => "Wevito entered Quiet mode because the session locked.",
            "session_unlock" => "Wevito recorded the session unlocking.",
            "soak_window_end" => "Recorded the end of an evidence-collection soak window.",
            "spriteAudit" => "Audited sprite or animation files.",
            "train_plan" => "Prepared a local training plan.",
            "translateText" => "Prepared a translation result.",
            "tuning_apply" => "Applied reviewed local tuning configuration.",
            "tuning_rollback" => "Rolled back local tuning configuration.",
            "user_interacting_state_entered" => "Paused background AI because the user interacted with a pet.",
            "user_interacting_state_cleared" => "Cleared the short pet-interaction pause.",
            "web_fetch" => "Fetched approved web research records.",
            _ => WarnUnknown(packetKind)
        };
    }

    private string WarnUnknown(string packetKind)
    {
        _warnUnknownPacketKind?.Invoke(packetKind);
        return $"Unknown {packetKind}";
    }
}
