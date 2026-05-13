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
        "proof_packet",
        "runtime_session",
        "scheduler_proposal",
        "screenCapture",
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
            "proof_packet" => "Recorded a proof packet.",
            "runtime_session" => "Recorded runtime supervisor activity.",
            "scheduler_proposal" => "Proposed a background helper task.",
            "screenCapture" => "Prepared a screen capture helper result.",
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
