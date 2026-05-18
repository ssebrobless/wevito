using Wevito.VNext.Core.SelfImprovement;
using Wevito.VNext.Core.SelfImprovement.Experiments;

namespace Wevito.VNext.Core;

public static class ShellCompositionRoot
{
    public static SupervisedImprovementLoop CreateSupervisedImprovementLoop(
        AuditLedgerService ledger,
        KillSwitchService? killSwitchService = null)
    {
        return new SupervisedImprovementLoop(ledger, new UserApplyApprovalValidator(), killSwitchService);
    }

    public static ConstitutionalDecisionService CreateConstitutionalDecisionService(KillSwitchService? killSwitchService = null)
    {
        return new ConstitutionalDecisionService(killSwitchService, CreateExperimentRegistry());
    }

    public static ExperimentRegistry CreateExperimentRegistry()
    {
        return ExperimentRegistry.ForCompositionRoot(SpriteRepairBatchProposalDescriptor.Descriptor);
    }
}
