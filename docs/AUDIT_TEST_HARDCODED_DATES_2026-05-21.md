# AUDIT: Hardcoded Calendar Dates in vnext\tests (2026-05-21)

## Background

PR #286 documented a Critical regression where five apply/rollback audit-packet tests saw an empty packet list. PR #287 diagnosed the root cause: the tests queried the audit ledger through a fixed `2026-05-21T00:00:00Z` upper bound while the runners stamped packets with real `DateTimeOffset.UtcNow`. PR #288 fixed that specific time bomb by widening the two affected test readback windows.

This audit checks whether other hardcoded calendar-date literals under `vnext\tests` have the same shape: a fixed query/comparison window that must capture rows or events stamped with real wall-clock time. This is read-only; no source or test file was changed.

## Search Methodology

The requested `rg` searches were attempted first:

```powershell
rg --no-heading --line-number '"20[2-9]\d-\d{2}-\d{2}' .\vnext\tests
rg --no-heading --line-number 'new\s+DateTime(Offset)?\s*\(\s*20[2-9]\d' .\vnext\tests
rg --no-heading --line-number 'DateTime(Offset)?\.Parse\(\s*"20[2-9]\d-\d{2}-\d{2}' .\vnext\tests
rg --no-heading --line-number 'new\s+DateOnly\s*\(\s*20[2-9]\d' .\vnext\tests
```

PowerShell quoting made the ISO-string `rg` patterns under-match in this shell, so the audit used the requested PowerShell fallback as the authoritative full pass:

```powershell
Get-ChildItem -Path .\vnext\tests -Recurse -Include *.cs |
    Select-String -Pattern '"20[2-9]\d-\d{2}-\d{2}',
        'new\s+DateTime(Offset)?\s*\(\s*20[2-9]\d',
        'DateTime(Offset)?\.Parse\(\s*"20[2-9]\d-\d{2}-\d{2}',
        'new\s+DateOnly\s*\(\s*20[2-9]\d'
```

I then reviewed the dangerous shapes separately:

- `Ledger.Snapshot(...)` windows with fixed dates.
- Report request windows with fixed dates.
- Fixed `Now` constants used as injected clocks.
- Real `DateTimeOffset.UtcNow` tests to confirm they do not combine with stale fixed query bounds.

## Match Summary

```
┌──────────────────────────────┬───────┐
│ Metric                       │ Count │
├──────────────────────────────┼───────┤
│ Total unique matched lines   │   402 │
│ ISO date string lines        │   400 │
│ DateTimeOffset constructor   │     2 │
│ DateOnly constructor         │     0 │
│ TIME-BOMB                    │     0 │
│ BENIGN                       │   402 │
│ UNCLEAR                      │     0 │
└──────────────────────────────┴───────┘
```

## Time-Bomb Candidates

None found.

The F1-shaped bug requires this combination:

```
fixed query/comparison window ──▶ production/test subject stamps row with real UtcNow ──▶ row falls outside fixed window
```

The remaining fixed ledger/report windows reviewed here are closed fixtures: the test seeds the rows or passes an injected request timestamp inside the same fixed window it later queries.

## Benign Matches

All 402 matched lines are classified BENIGN.

### Fixed Fixture Clocks

Most matches are frozen `DateTimeOffset.Parse(...)` values passed directly into services as explicit `nowUtc`, `CreatedAtUtc`, `RequestedAtUtc`, or fixture `Now` values. These are deterministic test fixtures, not wall-clock capture windows.

Representative files:

- `ActivitySummaryServiceTests.cs`
- `AgentTaskCardQueueServiceTests.cs`
- `AutonomousScopeServiceTests.cs`
- `CodexLoopRunnerServiceTests.cs`
- `DailyEvidenceSnapshotServiceTests.cs`
- `DoNotDisturbScheduleServiceTests.cs`
- `EvalRegressionGateTests.cs`
- `LearningLabLabelBundleTests.cs`
- `PetSimulationEngineTests.cs`
- `RuntimeSessionTrackerTests.cs`
- `SpriteWorkflowApplyRollbackTests.cs`

### Closed Ledger / Report Windows

The following files contain fixed query/report windows, but each one is paired with fixed rows or request timestamps inside the same fixture window:

- `AuditLedgerServiceTests.cs`: intentionally verifies `AuditLedgerService.Snapshot(...)` includes a row whose `CreatedAtUtc` is seeded inside the fixed window.
- `BenchmarkSuiteServiceTests.cs`: `BenchmarkSuiteService` records `request.CreatedAtUtc`; the request fixture uses `2026-05-15T12:00:00Z`, inside the queried `2026-05-15` to `2026-05-16` window.
- `RollbackProposalServiceTests.cs`: all proposal source rows and request windows are fixed historical fixtures for the 24-hour rollback rule.
- `SelfImprovementReportServiceTests.cs`: report windows are explicit inputs and all tested ledger rows are seeded inside or outside those windows intentionally.
- `SoakRunnerServiceTests.cs`: `SoakRunnerService` records `request.StartedAtUtc`; the request fixture uses `2026-05-13T12:00:00Z`, inside the queried `2026-05-13` to `2026-05-14` window.

### Apply/Rollback Runner Fixtures After PR #288

`ArtifactRenameApplyRunnerTests.cs`, `ArtifactRenameRollbackRunnerTests.cs`, and `PostC183ApplyRunnerProductTruthTests.cs` still contain fixed `2026-05-20T00:00:00Z` values for injected clocks or `ApplyAwaitingApproval` fixture rows. Those values are not used as the upper bound for reading real-`UtcNow` packets. PR #288 changed the actual apply/rollback packet readback helpers to `DateTimeOffset.MinValue` / `DateTimeOffset.MaxValue`.

### DateTimeOffset Constructor Literals

- `ProposalDiffExplainerServiceTests.cs:209`: fixed replay/evidence fixture timestamp; no real-time readback window.
- `SnapshotExportCliTests.cs:195`: fixed CLI fixture timestamp used to create deterministic snapshot input; no real-time readback window.

## Unclear / Needs Triage

None.

## Recommended Follow-Up

No F2 fix phase is needed from this audit. The only known F1-shaped hardcoded upper-bound issue was fixed in PR #288, and no additional time-bomb candidates were found.

Optional future hygiene: if the test suite adds new audit-ledger tests that write packets using real `DateTimeOffset.UtcNow`, prefer `DateTimeOffset.MinValue` / `DateTimeOffset.MaxValue` or computed bounds around the operation instead of calendar-date literals.

## Files Read

- `vnext/tests/Wevito.VNext.Tests/ArtifactRenameApplyRunnerTests.cs`
- `vnext/tests/Wevito.VNext.Tests/ArtifactRenameRollbackRunnerTests.cs`
- `vnext/tests/Wevito.VNext.Tests/BenchmarkSuiteServiceTests.cs`
- `vnext/tests/Wevito.VNext.Tests/RollbackProposalServiceTests.cs`
- `vnext/tests/Wevito.VNext.Tests/SelfImprovementReportServiceTests.cs`
- `vnext/tests/Wevito.VNext.Tests/SoakRunnerServiceTests.cs`
- `vnext/tests/Wevito.VNext.Tests/PostC183ApplyRunnerProductTruthTests.cs`
- `vnext/src/Wevito.VNext.Core/BenchmarkSuiteService.cs`
- `vnext/src/Wevito.VNext.Core/SoakRunnerService.cs`

## Search Output (Raw)

Raw command totals:

```text
total_pattern_hits=402
unique_line_matches=402
"20[2-9]\d-\d{2}-\d{2}=400
new\s+DateTime(Offset)?\s*\(\s*20[2-9]\d=2
new\s+DateOnly\s*\(\s*20[2-9]\d=0
```

Raw per-file match counts:

```text
.\vnext\tests\Wevito.VNext.Tests\ActivitySummaryServiceTests.cs|4
.\vnext\tests\Wevito.VNext.Tests\AgentSlotServiceTests.cs|2
.\vnext\tests\Wevito.VNext.Tests\AgentTaskCardQueueServiceTests.cs|10
.\vnext\tests\Wevito.VNext.Tests\AgentToolDispatcherTests.cs|2
.\vnext\tests\Wevito.VNext.Tests\AiIdentityServiceTests.cs|1
.\vnext\tests\Wevito.VNext.Tests\AnthropicModelAdapterTests.cs|1
.\vnext\tests\Wevito.VNext.Tests\ApplyPrerequisiteExplainerServiceTests.cs|1
.\vnext\tests\Wevito.VNext.Tests\ApplyRunnerActivityServiceTests.cs|1
.\vnext\tests\Wevito.VNext.Tests\ApplyRunnerPrerequisiteCheckServiceTests.cs|1
.\vnext\tests\Wevito.VNext.Tests\ApplyRunnerStatusReportCliTests.cs|1
.\vnext\tests\Wevito.VNext.Tests\ApplyRunnerStatusReportServiceTests.cs|1
.\vnext\tests\Wevito.VNext.Tests\AppRepositoryTests.cs|1
.\vnext\tests\Wevito.VNext.Tests\ApprovalCardDetailServiceTests.cs|1
.\vnext\tests\Wevito.VNext.Tests\ArtifactRenameApplyRunnerTests.cs|3
.\vnext\tests\Wevito.VNext.Tests\ArtifactRenameRollbackRunnerTests.cs|3
.\vnext\tests\Wevito.VNext.Tests\AudioAssistPreviewAdapterTests.cs|1
.\vnext\tests\Wevito.VNext.Tests\AudioBoostHandoffAdapterTests.cs|4
.\vnext\tests\Wevito.VNext.Tests\AudioOutputPolicyServiceTests.cs|1
.\vnext\tests\Wevito.VNext.Tests\AuditLedgerServiceTests.cs|4
.\vnext\tests\Wevito.VNext.Tests\AutonomousBetaDecisionServicePrecisionTests.cs|1
.\vnext\tests\Wevito.VNext.Tests\AutonomousBetaDecisionServiceTests.cs|1
.\vnext\tests\Wevito.VNext.Tests\AutonomousOperationsLoopTests.cs|1
.\vnext\tests\Wevito.VNext.Tests\AutonomousScopeServiceTests.cs|1
.\vnext\tests\Wevito.VNext.Tests\AutonomousTaskSchedulerTests.cs|4
.\vnext\tests\Wevito.VNext.Tests\BenchmarkCaseDraftServiceTests.cs|2
.\vnext\tests\Wevito.VNext.Tests\BenchmarkCurationViewModelTests.cs|2
.\vnext\tests\Wevito.VNext.Tests\BenchmarkSuiteServiceTests.cs|2
.\vnext\tests\Wevito.VNext.Tests\BuildProofExecutionAdapterTests.cs|4
.\vnext\tests\Wevito.VNext.Tests\ChatHistoryStoreTests.cs|4
.\vnext\tests\Wevito.VNext.Tests\ChatSessionServiceTests.cs|4
.\vnext\tests\Wevito.VNext.Tests\ChatTitleServiceTests.cs|5
.\vnext\tests\Wevito.VNext.Tests\CitationEnforcerTests.cs|1
.\vnext\tests\Wevito.VNext.Tests\CodexCompileThrottleServiceTests.cs|4
.\vnext\tests\Wevito.VNext.Tests\CodexLoopRunnerServiceTests.cs|13
.\vnext\tests\Wevito.VNext.Tests\CodexLoopWatchdogServiceTests.cs|4
.\vnext\tests\Wevito.VNext.Tests\CodexPhaseQueueServiceTests.cs|3
.\vnext\tests\Wevito.VNext.Tests\CoexistenceTriggerServiceTests.cs|1
.\vnext\tests\Wevito.VNext.Tests\ConstitutionalReviewedEmitterTests.cs|1
.\vnext\tests\Wevito.VNext.Tests\CursorReactivityServiceTests.cs|1
.\vnext\tests\Wevito.VNext.Tests\DailyEvidenceSnapshotServiceTests.cs|5
.\vnext\tests\Wevito.VNext.Tests\DevControlContractsTests.cs|1
.\vnext\tests\Wevito.VNext.Tests\DevControlSnapshotBuilderTests.cs|2
.\vnext\tests\Wevito.VNext.Tests\DiskIoBudgetServiceTests.cs|3
.\vnext\tests\Wevito.VNext.Tests\DoNotDisturbScheduleServiceTests.cs|5
.\vnext\tests\Wevito.VNext.Tests\EvalCoverageHealthHeldOutAccessTests.cs|1
.\vnext\tests\Wevito.VNext.Tests\EvalCoverageHealthServiceTests.cs|1
.\vnext\tests\Wevito.VNext.Tests\EvalCoverageProposalScopeTests.cs|1
.\vnext\tests\Wevito.VNext.Tests\EvalRegressionGateTests.cs|9
.\vnext\tests\Wevito.VNext.Tests\EvidenceCollectionStatusServiceTests.cs|1
.\vnext\tests\Wevito.VNext.Tests\EvidenceSummaryServiceTests.cs|5
.\vnext\tests\Wevito.VNext.Tests\FakeCommandRunnerTests.cs|1
.\vnext\tests\Wevito.VNext.Tests\FirstLaunchWizardTests.cs|1
.\vnext\tests\Wevito.VNext.Tests\FirstRunSpriteCleanupCardTests.cs|2
.\vnext\tests\Wevito.VNext.Tests\FocusDisciplineServiceTests.cs|1
.\vnext\tests\Wevito.VNext.Tests\FocusStealCounterTests.cs|2
.\vnext\tests\Wevito.VNext.Tests\GameModeDetectorServiceTests.cs|1
.\vnext\tests\Wevito.VNext.Tests\GoldenEvalSmokeTests.cs|1
.\vnext\tests\Wevito.VNext.Tests\GuardedMutationPilotTests.cs|4
.\vnext\tests\Wevito.VNext.Tests\GuardedMutationServiceTests.cs|1
.\vnext\tests\Wevito.VNext.Tests\HeldOutEvalCaseSchemaTests.cs|1
.\vnext\tests\Wevito.VNext.Tests\HeuristicJudgeServiceTests.cs|1
.\vnext\tests\Wevito.VNext.Tests\ImageGenIdleGuardServiceTests.cs|1
.\vnext\tests\Wevito.VNext.Tests\IndependentAssistantBetaGateServiceTests.cs|5
.\vnext\tests\Wevito.VNext.Tests\InDistributionEvalStoreTests.cs|1
.\vnext\tests\Wevito.VNext.Tests\InvariantViolationWatchdogTests.cs|3
.\vnext\tests\Wevito.VNext.Tests\KillSwitchServiceTests.cs|6
.\vnext\tests\Wevito.VNext.Tests\LearningEvalServiceTests.cs|5
.\vnext\tests\Wevito.VNext.Tests\LearningLabArtifactIndexerTests.cs|1
.\vnext\tests\Wevito.VNext.Tests\LearningLabLabelBundleTests.cs|11
.\vnext\tests\Wevito.VNext.Tests\LearningLabPromotionServiceTests.cs|4
.\vnext\tests\Wevito.VNext.Tests\LethalTrifectaTests.cs|1
.\vnext\tests\Wevito.VNext.Tests\LibreTranslateClientTests.cs|1
.\vnext\tests\Wevito.VNext.Tests\LiveStatusFeedTests.cs|13
.\vnext\tests\Wevito.VNext.Tests\LocalBrainBenchmarkSmokeTestTests.cs|4
.\vnext\tests\Wevito.VNext.Tests\LocalBrainHeartbeatServiceTests.cs|4
.\vnext\tests\Wevito.VNext.Tests\LocalBrainStatusPanelServiceTests.cs|4
.\vnext\tests\Wevito.VNext.Tests\LocalDocumentIngestServiceTests.cs|3
.\vnext\tests\Wevito.VNext.Tests\LocalOllamaReadinessProbeServiceTests.cs|1
.\vnext\tests\Wevito.VNext.Tests\LocalReasoningServiceTests.cs|5
.\vnext\tests\Wevito.VNext.Tests\LocalRetrievalServiceTests.cs|6
.\vnext\tests\Wevito.VNext.Tests\LocalRuntimeOnboardingServiceTests.cs|2
.\vnext\tests\Wevito.VNext.Tests\LocalTrainingPlanServiceTests.cs|1
.\vnext\tests\Wevito.VNext.Tests\LocalTuningRunnerTests.cs|1
.\vnext\tests\Wevito.VNext.Tests\MaturityScoreboardServiceTests.cs|8
.\vnext\tests\Wevito.VNext.Tests\ModelCapabilitySettingsTests.cs|1
.\vnext\tests\Wevito.VNext.Tests\ModelProviderModeTests.cs|1
.\vnext\tests\Wevito.VNext.Tests\NotificationPolicyServiceTests.cs|1
.\vnext\tests\Wevito.VNext.Tests\OllamaLocalModelAdapterTests.cs|1
.\vnext\tests\Wevito.VNext.Tests\OllamaModelBootstrapServiceTests.cs|5
.\vnext\tests\Wevito.VNext.Tests\OnnxPhiLocalModelAdapterTests.cs|1
.\vnext\tests\Wevito.VNext.Tests\OperationTimelineServiceTests.cs|1
.\vnext\tests\Wevito.VNext.Tests\PetAgentContractTests.cs|5
.\vnext\tests\Wevito.VNext.Tests\PetDebugTruthReportBuilderTests.cs|4
.\vnext\tests\Wevito.VNext.Tests\PetFpsMonitorServiceTests.cs|1
.\vnext\tests\Wevito.VNext.Tests\PetFpsSoakTest.cs|1
.\vnext\tests\Wevito.VNext.Tests\PetInteractionLoggerTests.cs|2
.\vnext\tests\Wevito.VNext.Tests\PetMemoryStoreTests.cs|3
.\vnext\tests\Wevito.VNext.Tests\PetMemoryWriteGateTests.cs|4
.\vnext\tests\Wevito.VNext.Tests\PetSimulationEngineTests.cs|8
.\vnext\tests\Wevito.VNext.Tests\PetStateContextInjectorTests.cs|4
.\vnext\tests\Wevito.VNext.Tests\PetStatePreviewAdapterTests.cs|3
.\vnext\tests\Wevito.VNext.Tests\PetStateToolTests.cs|3
.\vnext\tests\Wevito.VNext.Tests\PetVisualPolishLoggerTests.cs|1
.\vnext\tests\Wevito.VNext.Tests\PlainLanguageExplainerTests.cs|1
.\vnext\tests\Wevito.VNext.Tests\PostC174SelfImprovementBaselineTests.cs|1
.\vnext\tests\Wevito.VNext.Tests\PostC183ApplyRunnerProductTruthTests.cs|3
.\vnext\tests\Wevito.VNext.Tests\ProcessPriorityManagerServiceTests.cs|1
.\vnext\tests\Wevito.VNext.Tests\PromotionCriteriaSnapshotTests.cs|1
.\vnext\tests\Wevito.VNext.Tests\ProofExecutionAllowlistEvaluatorTests.cs|2
.\vnext\tests\Wevito.VNext.Tests\ProposalDiffExplainerServiceTests.cs|1
.\vnext\tests\Wevito.VNext.Tests\ProposalQualityMetricsServiceTests.cs|1
.\vnext\tests\Wevito.VNext.Tests\RamPressureCascadeServiceTests.cs|1
.\vnext\tests\Wevito.VNext.Tests\RefusedApprovalAggregateServiceTests.cs|1
.\vnext\tests\Wevito.VNext.Tests\ReplayHarnessTests.cs|1
.\vnext\tests\Wevito.VNext.Tests\ReplayResultStoreTests.cs|4
.\vnext\tests\Wevito.VNext.Tests\ReplayRunnerCliTests.cs|1
.\vnext\tests\Wevito.VNext.Tests\RerankHeadTests.cs|5
.\vnext\tests\Wevito.VNext.Tests\ResearchPlannerServiceTests.cs|2
.\vnext\tests\Wevito.VNext.Tests\RollbackProposalServiceTests.cs|10
.\vnext\tests\Wevito.VNext.Tests\RuntimeBudgetMeterTests.cs|3
.\vnext\tests\Wevito.VNext.Tests\RuntimeBudgetMeterUserProtectionFloorTests.cs|2
.\vnext\tests\Wevito.VNext.Tests\RuntimeSessionTrackerTests.cs|4
.\vnext\tests\Wevito.VNext.Tests\RuntimeSupervisorTests.cs|2
.\vnext\tests\Wevito.VNext.Tests\ScreenCaptureExecutionAdapterTests.cs|6
.\vnext\tests\Wevito.VNext.Tests\SelfImprovementProductTruthTests.cs|1
.\vnext\tests\Wevito.VNext.Tests\SelfImprovementReportServiceTests.cs|11
.\vnext\tests\Wevito.VNext.Tests\SnapshotExportCliTests.cs|1
.\vnext\tests\Wevito.VNext.Tests\SoakDriverCommandServiceTests.cs|1
.\vnext\tests\Wevito.VNext.Tests\SoakRunnerServiceTests.cs|4
.\vnext\tests\Wevito.VNext.Tests\SpriteAssetServiceFrameSelectionTests.cs|3
.\vnext\tests\Wevito.VNext.Tests\SpriteRepairBatchProposalScopeTests.cs|1
.\vnext\tests\Wevito.VNext.Tests\SpriteRepairBatchRunnerTests.cs|1
.\vnext\tests\Wevito.VNext.Tests\SpriteRepairQueueReaderTests.cs|2
.\vnext\tests\Wevito.VNext.Tests\SpriteWorkflowApplyRollbackTests.cs|10
.\vnext\tests\Wevito.VNext.Tests\SpriteWorkflowCandidateImporterTests.cs|2
.\vnext\tests\Wevito.VNext.Tests\SpriteWorkflowDryRunApplyServiceTests.cs|2
.\vnext\tests\Wevito.VNext.Tests\SpriteWorkflowManifestReaderTests.cs|1
.\vnext\tests\Wevito.VNext.Tests\SupervisedImprovementLoopSafetyTests.cs|1
.\vnext\tests\Wevito.VNext.Tests\SupervisedImprovementLoopTests.cs|1
.\vnext\tests\Wevito.VNext.Tests\SupervisedScoringDryRunServiceTests.cs|1
.\vnext\tests\Wevito.VNext.Tests\UnifiedPolicyServiceTests.cs|1
.\vnext\tests\Wevito.VNext.Tests\VisualQaIssueReportWriterTests.cs|2
.\vnext\tests\Wevito.VNext.Tests\VisualQaManifestParityTests.cs|1
.\vnext\tests\Wevito.VNext.Tests\WebResearchConnectorTests.cs|1
.\vnext\tests\Wevito.VNext.Tests\WevitoProcessWatchdogServiceTests.cs|1
.\vnext\tests\Wevito.VNext.Tests\WindowsForegroundFullscreenMonitorTests.cs|4
.\vnext\tests\Wevito.VNext.Tests\WindowsPowerHandlerTests.cs|3
