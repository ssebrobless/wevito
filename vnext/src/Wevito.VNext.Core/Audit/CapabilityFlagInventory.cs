using Wevito.VNext.Core.SelfImprovement;
using Wevito.VNext.Core.SelfImprovement.Apply;
using Wevito.VNext.Core.SelfImprovement.Eval;
using Wevito.VNext.Core.SelfImprovement.Invariants;
using Wevito.VNext.Core.SelfImprovement.Judge;
using Wevito.VNext.Core.SelfImprovement.Readiness;
using Wevito.VNext.Core.SelfImprovement.Scoring;
using Wevito.VNext.Core.Settings;

namespace Wevito.VNext.Core.Audit;

public sealed record CapabilityFlagInventoryEntry(
    string Name,
    string DefaultValue,
    string PlainLanguage);

public static class CapabilityFlagInventory
{
    public const string PetModelAdapterEnabledSetting = "pet_model_adapter_enabled";
    public const string PetModelFirstCallApprovedSetting = "pet_model_first_call_approved";
    public const string LocalFileReadEnabledSetting = "local_access_file_read_enabled";
    public const string LocalToolExecutionEnabledSetting = "local_tool_exec_enabled";

    public static IReadOnlyList<CapabilityFlagInventoryEntry> Entries { get; } =
    [
        new(KillSwitchService.KillSwitchSetting, bool.FalseString, "Stop Everything override for adapters, schedulers, loops, and autonomous scopes."),
        new(RuntimeSupervisorService.BackgroundWorkAllowedSetting, bool.FalseString, "Allows background helper work only when the runtime supervisor says the PC experience is safe."),
        new(AutonomousTaskScheduler.SchedulerEnabledSetting, bool.FalseString, "Allows the scheduler to draft task cards from approved local triggers."),
        new(AutonomousTaskScheduler.SchedulerPreviewDispatchApprovedSetting, bool.FalseString, "Allows scheduler-created cards to dispatch a preview without another manual click."),
        new(AutonomousOperationsConfig.EnabledSetting, bool.FalseString, "Allows the autonomous beta loop to tick after explicit promotion and user consent."),
        new(AutonomousScopeService.BuildEnabledSettingKey(AutonomousScopeService.SpriteRepairTriageScopeId), bool.FalseString, "Allows the sprite-repair triage scope to draft review-only findings."),
        new(AutonomousScopeService.BuildEnabledSettingKey(AutonomousScopeService.AuditLedgerCleanupScopeId), bool.FalseString, "Allows the audit-ledger cleanup scope to propose cleanup work."),
        new(AutonomousScopeService.BuildEnabledSettingKey(AutonomousScopeService.SpriteRepairBatchProposalScopeId), bool.FalseString, "Allows the sprite-repair batch proposal scope to draft proposal packets."),
        new(AutonomousScopeService.BuildEnabledSettingKey(AutonomousScopeService.EvalCoverageProposalScopeId), bool.FalseString, "Allows the eval-coverage proposal scope to draft review-only eval-gap proposal packets."),
        new(SupervisedImprovementLoopSettings.EnabledSetting, bool.FalseString, "Allows the supervised self-improvement loop to draft proposal-only task cards."),
        new(InvariantViolationWatchdog.EnabledSetting, bool.FalseString, "Allows the invariant watchdog to scan the audit ledger and write reset packets only."),
        new(EvalGateRunner.EnabledSetting, bool.FalseString, "Allows the eval gate runner v1 to execute the cheap deterministic gates only (Build / UnitTests / ScopeHash). All other gates remain NotApplicable."),
        new(HeuristicJudgeService.EnabledSetting, bool.FalseString, "Allows the heuristic judge service to write deterministic critique packets for open self-improvement proposals."),
        new(NotConfiguredScoringProvider.EnabledSetting, bool.FalseString, "Allows a local-only scoring provider to score self-improvement proposals via loopback. Default off; provider must also declare itself configured."),
        new(OllamaLoopbackScoringProvider.OllamaEnabledSetting, bool.FalseString, "Allows the loopback Ollama scoring provider type to be selected. Default off; the broader local_scoring_provider_enabled flag must also be true."),
        new(OllamaLoopbackScoringProvider.LoopbackEndpointSetting, "", "Loopback host:port for the local Ollama provider. Empty uses 127.0.0.1:11434; non-loopback hosts are refused."),
        new(OllamaLoopbackScoringProvider.OllamaModelSetting, "", "Model name passed to the loopback Ollama provider. Empty uses qwen2.5:7b-instruct-q4_k_m; used only when local_scoring_provider_ollama_enabled is true."),
        new(SupervisedScoringDryRunService.EnabledSetting, bool.FalseString, "Allows the supervised self-improvement scoring dry-run to execute against the configured ILocalScoringProvider. Default off. Even when on, the default provider refuses."),
        new(LocalOllamaReadinessProbeService.EnabledSetting, bool.FalseString, "Allows the loopback Ollama readiness probe to issue a single GET /api/tags to 127.0.0.1. Default off; the probe never sends a prompt."),
        new(LocalOllamaReadinessProbeService.EndpointSetting, "", "Optional override for the loopback Ollama readiness probe endpoint (host:port). Empty falls back to local_scoring_provider_loopback_endpoint, then 127.0.0.1:11434."),
        new(PetModelAdapterEnabledSetting, bool.FalseString, "Allows helper previews to request model-written summaries after approval gates."),
        new(PetModelFirstCallApprovedSetting, bool.FalseString, "Records whether the first model-call consent notice has been acknowledged."),
        new(ModelProviderModeService.LocalProviderAvailableSetting, bool.FalseString, "Marks a loopback local model runtime as available for local-only routing."),
        new(ModelProviderModeService.InProcessLocalRuntimeEnabledSetting, bool.FalseString, "Allows the optional in-process local runtime fallback when weights are present."),
        new(ModelProviderModeService.HostedProviderApprovedSetting, bool.FalseString, "Would allow hosted provider routing only in an explicitly approved future mode."),
        new(ApplyRunnerPrerequisiteCheckService.EnabledSetting, bool.FalseString, "Allows the apply-runner prerequisite checklist service to emit audit packets describing whether every gate for a real apply runner is currently met."),
        new(ApplyRunnerStatusReportService.EnabledSetting, bool.FalseString, "Allows the apply-runner status report service to emit a packet confirming the apply runner is still not implemented. Default off."),
        new(ArtifactRenameApplyRunner.DesignApprovedSetting, bool.FalseString, "Records explicit approval of the narrow v0 artifact-rename apply-runner design."),
        new(ArtifactRenameApplyRunner.ImplementationPhaseApprovedSetting, bool.FalseString, "Records explicit approval to implement the narrow v0 artifact-rename apply-runner phase."),
        new(ArtifactRenameApplyRunner.EnabledSetting, bool.FalseString, "Allows the narrow v0 apply runner to rename one artifact draft JSON to its approved JSON counterpart."),
        new(ArtifactRenameApplyRunner.DryRunRequiredSetting, bool.FalseString, "Requires the v0 apply runner to write a dry-run record before any target artifact rename."),
        new(ArtifactRenameApplyRunner.BackupRequiredSetting, bool.FalseString, "Requires the v0 apply runner to write and verify a backup before any target artifact rename."),
        new(ArtifactRenameApplyRunner.PostProofRequiredSetting, bool.FalseString, "Requires the v0 apply runner to verify post-apply sha256 evidence after the rename."),
        new(ArtifactRenameApplyRunner.RollbackRequiredSetting, bool.FalseString, "Requires the v0 apply runner to roll back automatically after any mid-flight failure."),
        new(ArtifactRenameApplyRunner.ExtendedArtifactTypesEnabledSetting, bool.FalseString, "Allows the narrow v0 apply/rollback runners to rename artifact-only draft/approved txt, md, and svg files. Default off; JSON remains the base format."),
        new(ArtifactRenameRollbackRunner.ExplicitRollbackEnabledSetting, bool.FalseString, "Allows the narrow v0 rollback runner to rename one approved JSON artifact back to its draft JSON counterpart."),
        new(ArtifactRenameRollbackRunner.ExplicitRollbackDesignApprovedSetting, bool.FalseString, "Records explicit approval of the narrow v0 artifact-rename rollback-runner design."),
        new("mutation_scope_audit_emit_enabled", bool.FalseString, "Allows future mutation-scope guard calls to emit audit packets. Default off; the C-PHASE 186 guard never emits by default."),
        new("apply_v0_invariant_check_emit_enabled", bool.FalseString, "Allows the invariant watchdog to emit a self-improvement apply-v0 invariant-violation packet per failing rule. Default off; the maturity-clock reset path is unchanged."),
        new("apply_v0_invariant_observer_in_activity_service_enabled", bool.FalseString, "Allows the read-only apply-runner activity surface to invoke the invariant watchdog facade. Default off."),
        new(WebResearchConnector.WebSearchEnabledSetting, bool.FalseString, "Allows approved web research surfaces to fetch external pages."),
        new(SettingKeys.LocalDocumentRetrievalEnabled, bool.FalseString, "Allows local document retrieval to build and query the local docs index."),
        new(LocalFileReadEnabledSetting, bool.FalseString, "Allows approved autonomous scopes to read local files through policy gates."),
        new(LocalToolExecutionEnabledSetting, bool.FalseString, "Allows approved local tool execution through policy gates.")
    ];
}
