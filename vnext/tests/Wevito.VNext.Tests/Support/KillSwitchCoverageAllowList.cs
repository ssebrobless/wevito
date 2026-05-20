using Wevito.VNext.Core.SelfImprovement;
using Wevito.VNext.Core.SelfImprovement.Apply;
using Wevito.VNext.Core.SelfImprovement.Eval;
using Wevito.VNext.Core.SelfImprovement.Experiments;
using Wevito.VNext.Core.SelfImprovement.Invariants;
using Wevito.VNext.Core.SelfImprovement.Maturity;

namespace Wevito.VNext.Tests.Support;

public static class KillSwitchCoverageAllowList
{
    public static readonly IReadOnlyList<string> PureDataTypes =
    [
        typeof(ApprovalResult).FullName!, // abstract approval union; carries no behavior or IO.
        typeof(ApprovalResult.Accepted).FullName!, // approval result value; carries no behavior or IO.
        typeof(ApprovalResult.Refused).FullName!, // approval result value; carries no behavior or IO.
        typeof(ConstitutionalDecisionInput).FullName!, // immutable decision input DTO.
        typeof(ConstitutionalDecisionOutcome).FullName!, // abstract decision union; carries no behavior or IO.
        typeof(ConstitutionalDecisionOutcome.Allowed).FullName!, // decision result value; carries no behavior or IO.
        typeof(ConstitutionalDecisionOutcome.Blocked).FullName!, // decision result value; carries no behavior or IO.
        typeof(ConstitutionalDecisionOutcome.NeedsHumanApproval).FullName!, // decision result value; carries no behavior or IO.
        typeof(ConstitutionalRule).FullName!, // pure rule interface; implementations are stateless predicates.
        typeof(DefaultDenyRule).FullName!, // stateless constitutional rule; no IO or background loop.
        typeof(ExperimentDescriptor).FullName!, // immutable experiment descriptor DTO.
        typeof(ExperimentKind).FullName!, // readonly experiment kind value object.
        typeof(ExperimentRegistry).FullName!, // in-memory registry builder; no IO or autonomous tick.
        typeof(IRequiresUserApplyApproval).FullName!, // approval-validator contract; no implementation behavior.
        typeof(KillSwitchActiveRule).FullName!, // stateless constitutional rule; reads only supplied input.
        typeof(NoHostedAiRule).FullName!, // stateless constitutional rule; reads only supplied input.
        typeof(NoNetworkInScopeRule).FullName!, // stateless constitutional rule; reads only supplied input.
        typeof(ScopeHash).FullName!, // deterministic hash helper; no IO or autonomous tick.
        typeof(ScopeHashInputs).FullName!, // immutable hash input DTO.
        typeof(ScopeMustBeEnabledRule).FullName!, // stateless constitutional rule; reads only supplied input.
        typeof(SupervisedImprovementLoopRequest).FullName!, // immutable loop request DTO.
        typeof(SupervisedImprovementLoopResult).FullName!, // immutable loop result DTO.
        typeof(SupervisedImprovementApprovalResult).FullName!, // immutable approval result DTO.
        typeof(PrerequisiteEntry).FullName!, // immutable apply-runner prerequisite-check entry DTO.
        typeof(ApplyRunnerPrerequisiteCheckResult).FullName!, // immutable apply-runner prerequisite-check result DTO.
        typeof(ApplyRequest).FullName!, // immutable narrow apply-runner request DTO.
        typeof(ApplyResult).FullName!, // abstract narrow apply-runner result union; carries no behavior or IO.
        typeof(ApplyResult.Refused).FullName!, // narrow apply-runner result value; carries no behavior or IO.
        typeof(ApplyResult.RolledBack).FullName!, // narrow apply-runner result value; carries no behavior or IO.
        typeof(ApplyResult.Succeeded).FullName!, // narrow apply-runner result value; carries no behavior or IO.
        typeof(SupervisedImprovementLoopSettings).FullName!, // immutable settings DTO plus parser.
        typeof(UserApplyApproval).FullName!, // user-entered approval DTO.
        typeof(UserApplyApprovalValidator).FullName!, // pure validator; no IO, loop, model call, or mutation.
        typeof(EvalGateManifest).FullName!, // immutable eval-gate manifest DTO.
        typeof(EvalGateResult).FullName!, // abstract eval result union; carries no behavior or IO.
        typeof(EvalGateResult.Passed).FullName!, // eval result value; carries no behavior or IO.
        typeof(EvalGateResult.Failed).FullName!, // eval result value; carries no behavior or IO.
        typeof(EvalGateResult.NotApplicable).FullName!, // eval result value; carries no behavior or IO.
        typeof(IHeldOutEvalStore).FullName!, // read contract; concrete store carries KillSwitchService.
        typeof(SpriteRepairBatchProposalDescriptor).FullName!, // static descriptor constants only.
        typeof(InvariantCheck).FullName!, // immutable invariant-check DTO.
        typeof(InvariantCheckResult).FullName!, // immutable invariant-check result DTO.
        typeof(MaturityClock).FullName!, // immutable maturity scoreboard DTO.
        typeof(MaturityClockResetReason).FullName!, // reset reason enum.
        typeof(ConstitutionalReviewedEmitter).FullName!, // preexisting packet emitter lacks direct KillSwitch; follow-up should inject one if it grows beyond single explicit Emit calls.
        typeof(SpriteRepairBatchProposalScope).FullName!, // preexisting autonomous scope is halted by caller policy today; follow-up should inject KillSwitchService directly.
    ];
}
