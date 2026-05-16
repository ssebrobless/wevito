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
        "ai_identity_set",
        "agent_slot_assigned",
        "agent_slot_renamed",
        "agent_slot_status_changed",
        "assetInventory",
        "audioAssist",
        "benchmark_baseline_updated",
        "benchmark_case_approved",
        "benchmark_case_bookmarked_from_chat",
        "benchmark_case_drafted",
        "benchmark_case_rejected",
        "benchmark_case_revised",
        "benchmark_regression_detected",
        "benchmark_run",
        "buildProof",
        "chat_search_performed",
        "chat_session_started",
        "chat_session_title_set",
        "chat_turn_cancelled",
        "chat_turn_completed",
        "codePatchPlan",
        "codeReview",
        "codex_loop_heartbeat",
        "codex_loop_paused",
        "codex_loop_resumed",
        "codex_phase_blocked",
        "codex_phase_completed",
        "codex_phase_retried",
        "codex_phase_started",
        "coexistence_trigger_cleared",
        "coexistence_trigger_fired",
        "dnd_state_changed",
        "guardedMutation",
        "kill_switch",
        "learning_promotion",
        "localDocs",
        "local_file_preview",
        "local_model_call",
        "model_bootstrap_required",
        "model_bootstrap_runtime_absent",
        "mutation_apply",
        "mutation_proposal",
        "mutation_rollback",
        "petState",
        "pet_animation_forced",
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
        "first_launch_completed",
        "first_launch_step_completed",
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
        "tool_invocation_started",
        "tool_invocation_completed",
        "tool_registry_loaded",
        "tool_registry_setting",
        "tier_priority_adjusted",
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
            "ai_identity_set" => "Updated Wevito's local AI identity name.",
            "agent_slot_assigned" => "Assigned a visible pet to an agent slot.",
            "agent_slot_renamed" => "Updated an agent slot name from the active pet roster.",
            "agent_slot_status_changed" => "Updated an agent slot status.",
            "assetInventory" => "Reviewed the local asset inventory.",
            "audioAssist" => "Prepared an audio helper result.",
            "benchmark_baseline_updated" => "Updated a benchmark baseline after a reviewed measurement run.",
            "benchmark_case_approved" => "Approved a reviewed benchmark case.",
            "benchmark_case_bookmarked_from_chat" => "Saved an assistant chat response as a draft benchmark case.",
            "benchmark_case_drafted" => "Drafted a benchmark case for review.",
            "benchmark_case_rejected" => "Rejected a draft benchmark case.",
            "benchmark_case_revised" => "Revised a draft benchmark case.",
            "benchmark_regression_detected" => "Detected a benchmark regression and selected the required safety response.",
            "benchmark_run" => "Ran the local benchmark suite and recorded the per-axis results.",
            "buildProof" => "Prepared or ran a build proof.",
            "chat_search_performed" => "Searched local chat history.",
            "chat_session_started" => "Started a local chat session.",
            "chat_session_title_set" => "Named a local chat session.",
            "chat_turn_cancelled" => "Cancelled a local chat response safely.",
            "chat_turn_completed" => "Completed a local chat response.",
            "codePatchPlan" => "Prepared a code-fix plan.",
            "codeReview" => "Reviewed code and wrote a report.",
            "codex_loop_heartbeat" => "Recorded that the Codex phase loop is still alive.",
            "codex_loop_paused" => "Paused the Codex phase loop.",
            "codex_loop_resumed" => "Resumed the Codex phase loop.",
            "codex_phase_blocked" => "Blocked a Codex phase and recorded the reason.",
            "codex_phase_completed" => "Completed a Codex phase after validation.",
            "codex_phase_retried" => "Retried a Codex phase once with failure context.",
            "codex_phase_started" => "Started a Codex phase from the phase queue.",
            "coexistence_trigger_cleared" => "Cleared a PC-coexistence pause after the user workload was safe again.",
            "coexistence_trigger_fired" => "Paused helper work because a PC-coexistence trigger fired.",
            "dnd_state_changed" => "Updated Do Not Disturb state for helper work.",
            "guardedMutation" => "Prepared a guarded mutation preview.",
            "kill_switch" => "Blocked helper work because Stop Everything is active.",
            "learning_promotion" => "Promoted reviewed learning data.",
            "localDocs" => "Summarized local project documents.",
            "local_file_preview" => "Previewed approved local file access.",
            "local_model_call" => "Prepared a local model response.",
            "model_bootstrap_required" => "Local reasoning model needs to be pulled before chat can use it.",
            "model_bootstrap_runtime_absent" => "Local Ollama runtime was not reachable, so Wevito used safe fallback behavior.",
            "mutation_apply" => "Applied a reviewed guarded mutation.",
            "mutation_proposal" => "Prepared a mutation proposal.",
            "mutation_rollback" => "Rolled back a guarded mutation.",
            "petState" => "Reviewed pet state.",
            "pet_animation_forced" => "Temporarily showed a calm pet animation while helper work yielded.",
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
            "first_launch_completed" => "Completed the first-launch setup wizard.",
            "first_launch_step_completed" => "Completed one step of the first-launch setup wizard.",
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
            "tool_invocation_started" => "Started an AI-requested local tool invocation.",
            "tool_invocation_completed" => "Completed an AI-requested local tool invocation.",
            "tool_registry_loaded" => "Loaded the AI-callable local tool registry.",
            "tool_registry_setting" => "Changed a local tool registry setting.",
            "tier_priority_adjusted" => "Adjusted helper work priority so foreground PC use stays protected.",
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
