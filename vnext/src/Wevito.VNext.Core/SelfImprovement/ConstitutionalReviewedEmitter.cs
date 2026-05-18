using Wevito.VNext.Core.Audit;

namespace Wevito.VNext.Core.SelfImprovement;

public sealed class ConstitutionalReviewedEmitter
{
    private readonly AuditLedgerService _auditLedgerService;

    public ConstitutionalReviewedEmitter(AuditLedgerService auditLedgerService)
    {
        _auditLedgerService = auditLedgerService;
    }

    public long Emit(
        ConstitutionalDecisionInput input,
        ConstitutionalDecisionOutcome outcome,
        DateTimeOffset timestampUtc,
        Guid? taskCardId = null)
    {
        return _auditLedgerService.Record(new EvidencePacket(
            Guid.NewGuid(),
            SelfImprovementPacketKinds.ConstitutionalReviewed,
            taskCardId,
            timestampUtc,
            DidUseNetwork: false,
            DidUseHostedAi: false,
            DidUseLocalModel: false,
            DidMutate: false,
            ArtifactPath: "",
            Summary: $"Reviewed self-improvement experiment '{input.ExperimentKind}' for scope '{input.ScopeId}': {Describe(outcome)}.",
            Status: outcome is ConstitutionalDecisionOutcome.Blocked ? "Blocked" : "Completed",
            Error: outcome is ConstitutionalDecisionOutcome.Blocked blocked ? blocked.Reason : ""));
    }

    private static string Describe(ConstitutionalDecisionOutcome outcome)
    {
        return outcome switch
        {
            ConstitutionalDecisionOutcome.Allowed => "allowed",
            ConstitutionalDecisionOutcome.Blocked blocked => $"blocked:{blocked.Reason}",
            ConstitutionalDecisionOutcome.NeedsHumanApproval approval => $"needs_human_approval:{approval.Reason}",
            _ => "unknown"
        };
    }
}
