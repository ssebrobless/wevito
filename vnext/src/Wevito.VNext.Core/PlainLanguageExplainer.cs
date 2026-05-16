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
        "chat_search_performed",
        "chat_session_started",
        "chat_session_title_set",
        "chat_turn_cancelled",
        "chat_turn_completed",
        "codePatchPlan",
        "codeReview",
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
        "proof_packet",
        "promotion_criteria_snapshot",
        "promotion_decision",
        "budget_meter_snapshot",
        "focus_steal_snapshot",
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
            "chat_search_performed" => "Searched local chat history.",
            "chat_session_started" => "Started a local chat session.",
            "chat_session_title_set" => "Named a local chat session.",
            "chat_turn_cancelled" => "Cancelled a local chat response safely.",
            "chat_turn_completed" => "Completed a local chat response.",
            "codePatchPlan" => "Prepared a code-fix plan.",
            "codeReview" => "Reviewed code and wrote a report.",
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
            "proof_packet" => "Recorded a proof packet.",
            "promotion_criteria_snapshot" => "Computed the autonomous-beta promotion criteria snapshot.",
            "promotion_decision" => "Recorded an autonomous-beta promotion decision packet.",
            "budget_meter_snapshot" => "Recorded the daily resource budget snapshot.",
            "focus_steal_snapshot" => "Recorded the daily focus-steal counter snapshot.",
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
