using Wevito.VNext.Core.SelfImprovement;
using Wevito.VNext.Core.SelfImprovement.Eval;
using Wevito.VNext.Core.SelfImprovement.Experiments;
using Wevito.VNext.Core.SelfImprovement.Invariants;

namespace Wevito.VNext.Core;

public static class ShellCompositionRoot
{
    public static SupervisedImprovementLoop CreateSupervisedImprovementLoop(
        AuditLedgerService ledger,
        KillSwitchService? killSwitchService = null)
    {
        return new SupervisedImprovementLoop(ledger, new UserApplyApprovalValidator(), killSwitchService);
    }

    public static InvariantViolationWatchdog CreateInvariantViolationWatchdog(
        AuditLedgerService ledger,
        KillSwitchService? killSwitchService = null,
        Func<IReadOnlyDictionary<string, string>>? settingsProvider = null)
    {
        return new InvariantViolationWatchdog(ledger.DatabasePath, ledger, killSwitchService, settingsProvider);
    }

    public static InDistributionEvalStore CreateInDistributionEvalStore(KillSwitchService? killSwitchService = null)
    {
        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WevitoVNext",
            "eval",
            "in-distribution");

        return new InDistributionEvalStore(root, killSwitchService);
    }

    public static EvalCoverageProposalScope CreateEvalCoverageProposalScope(
        AuditLedgerService ledger,
        KillSwitchService? killSwitchService = null,
        Action<string>? commandObserver = null)
    {
        return new EvalCoverageProposalScope(
            ledger.DatabasePath,
            ledger,
            CreateConstitutionalDecisionService(killSwitchService),
            new ConstitutionalReviewedEmitter(ledger),
            new EvalGateRunner(killSwitchService: killSwitchService),
            killSwitchService,
            commandObserver);
    }

    public static ConstitutionalDecisionService CreateConstitutionalDecisionService(KillSwitchService? killSwitchService = null)
    {
        return new ConstitutionalDecisionService(killSwitchService, CreateExperimentRegistry());
    }

    public static ExperimentRegistry CreateExperimentRegistry()
    {
        return ExperimentRegistry.ForCompositionRoot(
            SpriteRepairBatchProposalDescriptor.Descriptor,
            EvalCoverageProposalDescriptor.Descriptor);
    }
}
