using Wevito.VNext.Core.SelfImprovement;
using Wevito.VNext.Core.SelfImprovement.Eval;
using Wevito.VNext.Core.SelfImprovement.Invariants;
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
        new(PetModelAdapterEnabledSetting, bool.FalseString, "Allows helper previews to request model-written summaries after approval gates."),
        new(PetModelFirstCallApprovedSetting, bool.FalseString, "Records whether the first model-call consent notice has been acknowledged."),
        new(ModelProviderModeService.LocalProviderAvailableSetting, bool.FalseString, "Marks a loopback local model runtime as available for local-only routing."),
        new(ModelProviderModeService.InProcessLocalRuntimeEnabledSetting, bool.FalseString, "Allows the optional in-process local runtime fallback when weights are present."),
        new(ModelProviderModeService.HostedProviderApprovedSetting, bool.FalseString, "Would allow hosted provider routing only in an explicitly approved future mode."),
        new(WebResearchConnector.WebSearchEnabledSetting, bool.FalseString, "Allows approved web research surfaces to fetch external pages."),
        new(SettingKeys.LocalDocumentRetrievalEnabled, bool.FalseString, "Allows local document retrieval to build and query the local docs index."),
        new(LocalFileReadEnabledSetting, "", "Allows approved autonomous scopes to read local files through policy gates."),
        new(LocalToolExecutionEnabledSetting, "", "Allows approved local tool execution through policy gates.")
    ];
}
