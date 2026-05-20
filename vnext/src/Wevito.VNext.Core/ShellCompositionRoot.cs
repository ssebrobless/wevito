using Wevito.VNext.Core.SelfImprovement;
using Wevito.VNext.Core.SelfImprovement.Eval;
using Wevito.VNext.Core.SelfImprovement.Experiments;
using Wevito.VNext.Core.SelfImprovement.Invariants;
using Wevito.VNext.Core.SelfImprovement.Judge;
using Wevito.VNext.Core.SelfImprovement.Readiness;
using Wevito.VNext.Core.SelfImprovement.Replay;
using Wevito.VNext.Core.SelfImprovement.Scoring;

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

    public static HeuristicJudgeService CreateHeuristicJudgeService(
        AuditLedgerService ledger,
        KillSwitchService? killSwitchService = null,
        Func<IReadOnlyDictionary<string, string>>? settingsProvider = null)
    {
        return new HeuristicJudgeService(ledger.DatabasePath, ledger, killSwitchService, settingsProvider);
    }

    public static ProposalDiffExplainerService CreateProposalDiffExplainerService(
        AuditLedgerService ledger,
        KillSwitchService? killSwitchService = null)
    {
        return new ProposalDiffExplainerService(ledger.DatabasePath, killSwitchService);
    }

    public static ApplyPrerequisiteExplainerService CreateApplyPrerequisiteExplainerService(
        AuditLedgerService ledger,
        KillSwitchService? killSwitchService = null)
    {
        var artifactsRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WevitoVNext",
            "artifacts");

        return new ApplyPrerequisiteExplainerService(ledger.DatabasePath, artifactsRoot, killSwitchService);
    }

    public static EvalCoverageHealthService CreateEvalCoverageHealthService(
        IHeldOutEvalStore heldOut,
        IInDistributionEvalStore inDistribution,
        KillSwitchService? killSwitchService = null)
    {
        return new EvalCoverageHealthService(heldOut, inDistribution, killSwitchService);
    }

    public static ProposalQualityMetricsService CreateProposalQualityMetricsService(
        AuditLedgerService ledger,
        KillSwitchService? killSwitchService = null)
    {
        var artifactsRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WevitoVNext",
            "artifacts");

        return new ProposalQualityMetricsService(ledger.DatabasePath, artifactsRoot, killSwitchService);
    }

    public static ApplyRunnerStatusReportService CreateApplyRunnerStatusReportService(
        AuditLedgerService ledger,
        KillSwitchService? killSwitchService = null,
        Func<IReadOnlyDictionary<string, string>>? settingsProvider = null)
    {
        return new ApplyRunnerStatusReportService(ledger, killSwitchService, settingsProvider);
    }

    public static ReplayResultStore CreateReplayResultStore(KillSwitchService killSwitchService)
    {
        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WevitoVNext",
            "artifacts");

        return new ReplayResultStore(root, killSwitchService);
    }

    public static ILocalScoringProvider CreateLocalScoringProvider(KillSwitchService? killSwitchService = null)
    {
        return new NotConfiguredScoringProvider(killSwitchService);
    }

    public static SupervisedScoringDryRunService CreateSupervisedScoringDryRunService(
        AuditLedgerService ledger,
        ILocalScoringProvider provider,
        KillSwitchService? killSwitchService = null,
        Func<IReadOnlyDictionary<string, string>>? settingsProvider = null)
    {
        return new SupervisedScoringDryRunService(
            ledger.DatabasePath,
            ledger,
            provider,
            killSwitchService,
            settingsProvider);
    }

    public static LocalOllamaReadinessProbeService CreateLocalOllamaReadinessProbeService(
        AuditLedgerService ledger,
        KillSwitchService? killSwitchService = null,
        Func<IReadOnlyDictionary<string, string>>? settingsProvider = null)
    {
        return new LocalOllamaReadinessProbeService(
            new DefaultScoringHttpClient(killSwitchService),
            ledger,
            killSwitchService,
            settingsProvider ?? (() => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)));
    }

    public static CapabilitiesAndGatesService CreateCapabilitiesAndGatesService(
        Func<IReadOnlyDictionary<string, string>> settingsProvider,
        KillSwitchService? killSwitchService = null)
    {
        return new CapabilitiesAndGatesService(settingsProvider, killSwitchService);
    }

    public static ApplyRunnerPrerequisiteCheckService CreateApplyRunnerPrerequisiteCheckService(
        AuditLedgerService ledger,
        IHeldOutEvalStore heldOutEvalStore,
        IInDistributionEvalStore inDistributionEvalStore,
        Func<IReadOnlyDictionary<string, string>>? settingsProvider = null,
        KillSwitchService? killSwitchService = null)
    {
        var artifactsRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WevitoVNext",
            "artifacts");

        return new ApplyRunnerPrerequisiteCheckService(
            artifactsRoot,
            ledger.DatabasePath,
            ledger,
            heldOutEvalStore,
            inDistributionEvalStore,
            killSwitchService,
            settingsProvider);
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
