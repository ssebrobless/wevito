# Wevito Post-C-PHASE-85 Blocker — Codex Phase Prompts (Medium Effort)

Date: 2026-05-13
Author: Claude (Opus 4.7)
Audience: Codex at medium reasoning effort
Source of truth: `docs/CLAUDE_POST_CPHASE85_BLOCKER_PLAN_2026-05-13.md`

These prompts are Codex-ready. Each is self-contained, sized for one PR
at medium reasoning effort, and reuses the conventions established in
C-PHASE 65–84. Run them in order. Do not skip phases or bundle them.

The wall-clock 7-day soak window between C-PHASE 85c and C-PHASE 85d is
not a Codex phase — that work is the user's, and Codex must wait for
the evidence the user collects before opening C-PHASE 85d.

## Shared Preamble (mentally include with each prompt)

```text
Repo: C:\Users\fishe\Documents\projects\wevito (or current worktree)

Read first:
  docs/CLAUDE_POST_CPHASE85_BLOCKER_PLAN_2026-05-13.md
  docs/CLAUDE_POST_CPHASE75_REVIEW_AND_ROADMAP_2026-05-13.md
  docs/C_PHASE85_BLOCKER_2026-05-13.md
  docs/C_PHASE84_GUARDED_CODE_MUTATION_PILOT_2026-05-13.md
  docs/C_PHASE83_SELF_IMPROVEMENT_REPORT_LOOP_2026-05-13.md
  docs/C_PHASE82_GOLDEN_EVAL_AND_REGRESSION_GATE_2026-05-13.md
  vnext/src/Wevito.VNext.Core/AuditLedgerService.cs
  vnext/src/Wevito.VNext.Core/AutonomousBetaDecisionService.cs
  vnext/src/Wevito.VNext.Core/PlainLanguageExplainer.cs
  vnext/src/Wevito.VNext.Core/WindowsPowerHandler.cs
  vnext/src/Wevito.VNext.Core/FocusStealCounter.cs
  vnext/src/Wevito.VNext.Core/RuntimeBudgetMeter.cs
  vnext/src/Wevito.VNext.Core/SoakRunnerService.cs
  vnext/src/Wevito.VNext.Shell/ShellCoordinator.cs
  tools/run-soak-validation.ps1
  tools/run-self-improvement-report.ps1

Hard invariants (apply to every phase):
- no hosted-AI dependency for normal runtime
- no hidden web access, no hidden training, no hidden local file access
- no hidden tool execution, no hidden code/asset mutation
- pause/quiet/pet-only/fullscreen-quiet is always honored
- pets remain regular pet sprites (no task animation)
- every mutation: exact scope + dry-run + backup hash + rollback + post-proof
- every learning step: reviewed data + eval gate + rollback
- every web fetch: citation + provenance + privacy filter
- every local model use degrades safely when runtime/model absent
- every adapter writes an EvidencePacket row to AuditLedgerService
- every adapter honors KillSwitchService
- every file read goes through UnifiedPolicyService
- every new packet kind appears in PlainLanguageExplainer.KnownPacketKinds
  and ExplainPacketKind
- the soak driver and promotion-eval scripts NEVER alter settings
- the promotion PR (85d) NEVER flips runtime_autonomous_beta_enabled
- PromotionCriteriaSnapshot refuses to compute on insufficient data

Validation baseline (always run at the end of phase before opening PR):
  dotnet build .\vnext\Wevito.VNext.sln
  dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
  powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests

PR rules:
- one phase per PR
- branch: claude-implementation/c-phase-<N>-<short-slug>
- commit messages reference C-PHASE <N>
- include phase report under docs/
- do NOT merge if any test, build, or smoke fails
- do NOT flip any default-off setting in any phase below
- do NOT fabricate ledger evidence; tests use synthetic ledger fixtures
- after committing, push the branch and stop for user review
```

## C-PHASE 85a — Evidence-Collection Instrumentation

```text
Implement C-PHASE 85a per docs/CLAUDE_POST_CPHASE85_BLOCKER_PLAN_2026-05-13.md §4.

Branch: claude-implementation/c-phase-85a-evidence-instrumentation

Goal:
Make Wevito emit honest ledger rows during normal use without enabling
any new capability, so a 7-day soak window can collect real evidence.

Add (new files):
- vnext/src/Wevito.VNext.Core/RuntimeSessionTracker.cs
- vnext/src/Wevito.VNext.Core/DailyEvidenceSnapshotService.cs
- vnext/tests/Wevito.VNext.Tests/RuntimeSessionTrackerTests.cs
- vnext/tests/Wevito.VNext.Tests/DailyEvidenceSnapshotServiceTests.cs
- vnext/tests/Wevito.VNext.Tests/AutonomousBetaDecisionServicePrecisionTests.cs
- docs/C_PHASE85A_EVIDENCE_INSTRUMENTATION_2026-05-13.md

Modify:
- vnext/src/Wevito.VNext.Core/AutonomousBetaDecisionService.cs
  (tighten checks; see hard rules below)
- vnext/src/Wevito.VNext.Core/PlainLanguageExplainer.cs
  (add the new packet kinds and the existing power_sleep/power_resume/
   session_lock/session_unlock kinds)
- vnext/src/Wevito.VNext.Core/WindowsPowerHandler.cs
  (sleep/lock rows now use Status="Completed"; the forceQuiet side
   effect is preserved)
- vnext/src/Wevito.VNext.Shell/ShellCoordinator.cs
  (wire RuntimeSessionTracker + DailyEvidenceSnapshotService into the
   startup / tick / shutdown path)
- vnext/content/tool_definitions.json
  (document the new packet kinds as part of the existing audit_ledger
   entry, if applicable)

RuntimeSessionTracker rules:
- packet kinds emitted: runtime_session_start, runtime_session_heartbeat,
  runtime_session_end, runtime_session_paused
- summaries always include uptime_hours=<int>; once uptime_hours>=4,
  the summary must literally contain the substring "uptime_hours>=4"
  so AutonomousBetaDecisionService can find it via Summary.Contains.
- emits start row at first tick after the shell is ready
- emits one heartbeat per wall-clock hour (configurable via setting
  runtime_session_heartbeat_minutes default 60, minimum 15)
- emits end row on graceful shutdown (Application.Exit hook)
- on KillSwitch active: emits runtime_session_paused with status
  Completed and summary kill_switch_active=true; never blocks the UI
- persists session_started_at_utc + last_heartbeat_at_utc to
  %LOCALAPPDATA%/Wevito/audit/runtime-session.json
- never opens a network connection
- never throws out of its public methods

DailyEvidenceSnapshotService rules:
- packet kinds emitted: budget_meter_snapshot, focus_steal_snapshot
- emits once per UTC day at the first tick after midnight UTC
- budget_meter_snapshot summary includes:
  - "budget_exceeded=true" if any reservation in the prior 24h failed
    with Reason starting with "CPU budget" or "Memory budget" or
    "Hourly background"; otherwise "budget_exceeded=false"
  - used_this_hour, max_this_hour at the time of emission
- focus_steal_snapshot summary includes:
  - "focus_steal=true" if the day's delta in the FocusStealCounter
    was > 0; otherwise "focus_steal=false"
  - day_delta=<n>, total=<n>
- persists last_snapshot_date_utc to
  %LOCALAPPDATA%/Wevito/audit/daily-snapshot.json
- KillSwitch active: no row is written; the next active day will catch up
- never opens a network connection
- never throws

WindowsPowerHandler change:
- Record(...) must use Status="Completed" for ALL packet kinds, not
  "Blocked"; the forceQuiet=true side effect (setting QuietModeSetting
  and BackgroundWorkAllowedSetting to false in the next snapshot) is
  preserved.
- Update WindowsPowerHandlerTests to assert Status="Completed" on
  Sleep / Lock and that forceQuiet still happens.

AutonomousBetaDecisionService.BuildChecks tightening:
- zero_policy_violations now requires:
    - no row with PacketKind="policy_block" (UnifiedPolicyService) AND
    - no row with PacketKind contains "policy" AND Error.Length > 0
  Power/session events are NEVER policy violations.
- zero_focus_steal_events is satisfied when:
    - at least one focus_steal_snapshot row exists in the window, AND
    - no focus_steal_snapshot summary contains focus_steal=true
- resource_budget_within_tolerance is satisfied when:
    - at least one budget_meter_snapshot row exists in the window, AND
    - no budget_meter_snapshot summary contains budget_exceeded=true
- active_uptime_present is satisfied when at least one runtime_session
  packet kind exists with summary containing uptime_hours>=4
- preview_activity_present remains as is
- mutations_have_proof_packets remains as is (still safety-class)
- zero_hosted_ai_local_only remains as is (still safety-class)

PlainLanguageExplainer additions:
- runtime_session_start: "Wevito recorded the start of an active runtime
  session."
- runtime_session_heartbeat: "Wevito recorded a runtime session
  heartbeat (still alive)."
- runtime_session_end: "Wevito recorded the end of an active runtime
  session."
- runtime_session_paused: "Wevito paused background work because
  Stop Everything is active."
- budget_meter_snapshot: "Recorded the daily resource budget snapshot."
- focus_steal_snapshot: "Recorded the daily focus-steal counter
  snapshot."
- power_sleep: "Wevito entered Quiet mode because the system slept."
- power_resume: "Wevito recorded the system resuming."
- session_lock: "Wevito entered Quiet mode because the session locked."
- session_unlock: "Wevito recorded the session unlocking."

Tests required:
- RuntimeSessionTracker emits start row exactly once (mocked clock)
- 4th hourly heartbeat summary contains uptime_hours>=4
- end row emitted on graceful shutdown call
- runtime_session_paused emitted when KillSwitch is active mid-window
- DailyEvidenceSnapshotService emits exactly one budget snapshot and
  one focus snapshot per UTC day (mocked clock)
- KillSwitch suppresses DailyEvidenceSnapshotService emissions
- WindowsPowerHandler sleep/lock rows have Status="Completed"
- AutonomousBetaDecisionService no longer flags a power_sleep row as a
  policy violation
- AutonomousBetaDecisionService requires both focus_steal_snapshot AND
  budget_meter_snapshot rows to satisfy their respective checks (no
  row -> the check fails)
- PlainLanguageExplainer covers every new packet kind without falling
  through to the WarnUnknown branch
- Regression: full test sweep stays green (expected: 505 + new tests)

Validation: baseline plus
  dotnet test --filter "RuntimeSession|DailyEvidenceSnapshot|AutonomousBeta|PlainLanguage|WindowsPower"

Phase report: docs/C_PHASE85A_EVIDENCE_INSTRUMENTATION_2026-05-13.md
in the style of the C-PHASE 82-84 reports. Include:
- Goal
- Scope (what was changed)
- Safety Boundaries (no setting flips, no network, KillSwitch honored)
- Validation (test counts + commands run)
- Next Phase (C-PHASE 85b; auto-continue is NO)

PR title: "C-PHASE 85a: Evidence-collection instrumentation"

Stop gates (do not merge if any are violated):
- a heartbeat row is emitted more than once per hour
- the AutonomousBetaDecisionService change weakens a safety-class check
- the WindowsPowerHandler change loses the forceQuiet=true side effect
- a test enables a default-off capability
- a test opens a network connection
- a new packet kind is not covered by the explainer

Auto-continue: NO. Stop for user review.
```

## C-PHASE 85b — Soak Driver + Status UX

```text
Implement C-PHASE 85b per docs/CLAUDE_POST_CPHASE85_BLOCKER_PLAN_2026-05-13.md §4.

Branch: claude-implementation/c-phase-85b-soak-driver-and-status-ux

Goal:
Build the soak driver script, CLI, and Activity-panel UI surface that
allows a 7-day evidence-collection window without enabling any new
capability.

Add (new files):
- vnext/src/Wevito.VNext.Core/EvidenceCollectionStatusService.cs
- vnext/tests/Wevito.VNext.Tests/EvidenceCollectionStatusServiceTests.cs
- tools/run-soak-driver.ps1
- tools/check-evidence-readiness.ps1
- tools/soak-driver-cli/Wevito.VNext.SoakDriver.csproj
- tools/soak-driver-cli/Program.cs
- tools/soak-driver-cli/README.md
- docs/C_PHASE85B_SOAK_DRIVER_AND_STATUS_UX_2026-05-13.md

Modify:
- vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml(.cs)
  (add an "Evidence collection" tab in the Activity panel)
- vnext/src/Wevito.VNext.Shell/HomePanelWindow.xaml(.cs)
  ("Evidence: day N of 7" badge near the Stop everything toggle, hidden
   when no soak window is active)
- vnext/src/Wevito.VNext.Shell/RoamBandWindow.xaml(.cs)
  (append soak status to the existing live status line when active)
- vnext/src/Wevito.VNext.Shell/ShellCoordinator.cs
  (wire EvidenceCollectionStatusService into the render path)
- tools/build-vnext.ps1
  (also build tools/soak-driver-cli; emit the binary path so
   run-soak-driver.ps1 can find it)
- tools/run-soak-validation.ps1
  (keep as a thin compatibility wrapper that delegates to
   run-soak-driver.ps1; document the deprecation note)

EvidenceCollectionStatusService rules:
- reads %LOCALAPPDATA%/Wevito/audit/ledger.sqlite via AuditLedgerService
  and the soak manifest at vnext/artifacts/soak/<latest>/manifest.json
- returns EvidenceCollectionStatus {
    Active, StartedAtUtc, DayN, DayMax, RowsToday, FlaggedRowsToday,
    LastReportAtUtc, LastReadinessLabel,
    HeartbeatCountToday, FocusStealDeltaToday, BudgetExceededToday
  }
- never writes any ledger row itself; pure read-side service
- never opens a network connection
- KillSwitch active: returns Active=false even if a window exists

tools/run-soak-driver.ps1 rules:
- argument: -Days <int 1-14, default 7>, -ArtifactRoot <path>,
  -HeartbeatMinutes <int 15-240, default 60>,
  -StopOnPowerSleep (switch)
- preflight checks:
  - wevito-vnext process is running (Get-Process); if not, write a
    clear message and exit 2
  - settings snapshot read via the soak-driver-cli's "status" command;
    refuse to start if ANY of these are true:
      runtime_autonomous_beta_enabled
      pet_model_adapter_enabled
      web_search_enabled
      local_tool_exec_enabled
      tuning_lora_enabled
      runtime_kill_switch
    (the script must NOT flip these; it just refuses to run)
- writes a soak window manifest:
  vnext/artifacts/soak/<ts>-soak-window/manifest.json
  with started_at_utc, requested_days, heartbeat_minutes, artifact_root,
  initial_settings_snapshot_sha256
- enters a loop that:
  - calls "wevito-soak-driver heartbeat --reason scheduled" every
    HeartbeatMinutes
  - calls "wevito-soak-driver day-end" once per UTC day at first tick
    after midnight; that command also invokes
    tools/run-self-improvement-report.ps1 -Window day
  - when -Days elapsed: calls "wevito-soak-driver window-end", writes
    a soak-summary.md and run-summary.md, exits 0
  - on Ctrl+C: calls "wevito-soak-driver window-end" with reason=manual,
    writes the same artifacts, exits 0
- never opens a network connection (rely on the CLI's --no-network
  enforced argument)
- never flips a setting; if KillSwitch goes active during the run,
  log it and continue writing heartbeats (the heartbeats themselves
  will record paused state)

tools/soak-driver-cli/ rules:
- single console project referencing Wevito.VNext.Core
- commands:
  - "heartbeat --reason <text>" -> calls RuntimeSessionTracker
    directly to emit a runtime_session_heartbeat row
  - "day-end" -> calls SelfImprovementReportService once for the
    current UTC day (since=00:00Z, until=now), writes the report
    packet via the existing service
  - "window-end --reason <text>" -> writes a soak_window_end row
    (new packet kind; explainer must cover it) and the soak-summary
  - "status" -> prints a JSON document of the EvidenceCollection
    status + the current settings snapshot, exits 0
- every command must:
  - never open a network connection
  - never flip a setting
  - exit non-zero on any error; print one-line diagnostic to stderr

tools/check-evidence-readiness.ps1 rules:
- argument: -ArtifactRoot <path optional>
- prints a one-screen readiness table using EvidenceCollection status
- safe to run any time; no side effects (calls the CLI's "status" only)

ToolPopupWindow Activity panel additions:
- new "Evidence collection" tab
- header: "Soak window: <not started | Day N of M | Completed>"
- per-day rows: date, heartbeats, flagged rows, focus_steal delta,
  budget_exceeded, last self-improvement report timestamp
- readiness badge at the bottom: not_started | day_N_of_7 |
  ready_to_eval | blocked_by_<criterion>
- tab is hidden when EvidenceCollectionStatus.Active=false AND no
  prior soak manifest exists

HomePanelWindow:
- small "Evidence: Day N of 7" badge near the Stop everything toggle
- only visible when EvidenceCollectionStatus.Active=true

RoamBandWindow:
- when active, append a small ". soak day N of 7" suffix to the
  existing live status line; never causes focus steal

Tests required:
- EvidenceCollectionStatusService returns Active=false, DayN=0 when
  no soak manifest exists
- returns Day=N correctly for a multi-day fake manifest (mocked clock)
- KillSwitch active -> Active=false
- soak-driver-cli "heartbeat" writes exactly one ledger row
- soak-driver-cli "heartbeat" refuses to write when KillSwitch active
- soak-driver-cli "day-end" writes a self-improvement report
- soak-driver-cli "window-end" writes the soak_window_end row
- soak-driver-cli "status" returns JSON with the current settings
  snapshot keys
- Activity-panel tab renders only counts, kinds, plain-language
  sentences (no private text)
- soak_window_end appears in PlainLanguageExplainer.KnownPacketKinds

Validation: baseline plus
  dotnet test --filter "EvidenceCollectionStatus|SoakDriver"
  powershell -File .\tools\check-evidence-readiness.ps1
  (optional smoke, user-run, 1h or less):
    powershell -File .\tools\run-soak-driver.ps1 -Days 1 -HeartbeatMinutes 5 -ArtifactRoot .\vnext\artifacts\soak

Phase report: docs/C_PHASE85B_SOAK_DRIVER_AND_STATUS_UX_2026-05-13.md

PR title: "C-PHASE 85b: Soak driver and evidence-collection status UX"

Stop gates (do not merge if any are violated):
- driver script can be coerced into enabling any default-off capability
- driver script opens a network connection
- Activity panel tab displays private text from ledger rows
- driver script silently continues after the Wevito process exits
- the CLI can be invoked without the soak-driver-cli being built
- a soak window manifest can be created without the preflight settings
  check passing

Auto-continue: NO. First end-to-end soak surface.
```

## C-PHASE 85c — Promotion Snapshot + Confirmation-Gated UI Entry

```text
Implement C-PHASE 85c per docs/CLAUDE_POST_CPHASE85_BLOCKER_PLAN_2026-05-13.md §4.

Branch: claude-implementation/c-phase-85c-promotion-snapshot-and-ui

Goal:
Implement PromotionCriteriaSnapshot, the promotion-eval script, and the
Settings UI entry. Use synthetic ledger fixtures only; no real evidence
is required to land this phase. The Settings entry MUST NOT be enabled
by this PR — only the next phase (C-PHASE 85d) can ship the real
evidence that lets it become interactive.

Add (new files):
- vnext/src/Wevito.VNext.Core/PromotionCriteriaSnapshot.cs
- vnext/src/Wevito.VNext.Core/PromotionDecisionLabel.cs
- vnext/tests/Wevito.VNext.Tests/PromotionCriteriaSnapshotTests.cs
- tools/run-promotion-eval.ps1
- docs/C_PHASE85C_PROMOTION_SNAPSHOT_AND_UI_ENTRY_2026-05-13.md

Modify:
- vnext/src/Wevito.VNext.Core/AutonomousBetaDecisionService.cs
  (consume PromotionCriteriaSnapshot when available; otherwise fall back
   to its existing BuildChecks path)
- vnext/src/Wevito.VNext.Core/PlainLanguageExplainer.cs
  (add promotion_criteria_snapshot, promotion_decision,
   runtime_autonomous_beta_user_consent)
- vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml(.cs)
  (Settings -> Autonomous beta panel: snapshot table + Try-the-beta
   entry, default-disabled)
- vnext/content/tool_definitions.json
  (document the new packet kinds + the autonomous-beta entry)

PromotionCriteriaSnapshot rules:
- inputs:
  - ledger snapshot range (default 7 days, configurable)
  - latest golden eval result (loaded from
    vnext/artifacts/eval-golden/<latest>/eval-report.json)
  - budget meter snapshot (the file at
    %LOCALAPPDATA%/Wevito/audit/budget-meter.json)
  - focus-steal snapshot (the file at
    %LOCALAPPDATA%/Wevito/audit/focus-steal.json)
  - settings snapshot
- outputs: PromotionDecision { Label, Reasons, Criteria } where
  Criteria is an ordered list of PromotionCriterion {
    Id, Passed, Class (Safety|Liveness), ObservedValue, Threshold,
    Detail
  }
- Criteria IDs and thresholds (verbatim):
  1. window_days >= 7 (Liveness)
  2. active_uptime_per_active_day >= 50% (Liveness; computed from
     runtime_session heartbeats vs. active hours from waking-hour
     proxy: any hour with at least one ledger row)
  3. policy_violations == 0 (Safety; counts UnifiedPolicyService
     Blocked rows; explicitly excludes power_sleep / session_lock /
     runtime_session_paused)
  4. mutations_with_proof_packet_pct == 100% (Safety; every did_mutate
     row has a proof_packet row within +/- 10 minutes)
  5. hosted_ai_calls_in_local_only == 0 (Safety; counts did_use_hosted_ai
     rows while pet_model_mode=LocalOnly in the settings snapshot)
  6. focus_steal_events == 0 (Safety; sum of focus_steal_snapshot
     rows with focus_steal=true)
  7. resource_budget_within_pct: abs delta <= 10% (Liveness; computed
     from budget_meter_snapshot rows)
  8. citation_coverage_ratio >= 0.6 (Liveness; from latest golden
     eval report)
  9. golden_eval_result == PASS (Liveness; eval-report.json says pass)
  10. self_improvement_reports_per_active_day >= 1 (Liveness)
  11. kill_switch_active_during_window == false (Safety; any
      kill_switch_user_initiated row that lasted > 1 hour)
- Decision rule:
  - if window_days < 7: Label = KeepSupervisedPreview, Reason
    "window_too_short"
  - else if any Safety criterion fails: Label =
    PauseForReliabilityWork, list the failing criteria
  - else if any Liveness criterion fails: Label =
    KeepSupervisedPreview, list the failing criteria
  - else: Label = EnableAutonomousBeta
- emits a promotion_criteria_snapshot audit row with status Completed
  and summary "decision=<label>, passes=<n>/<n>"
- KillSwitch active: refuses to compute; returns Label =
  KeepSupervisedPreview, Reason "kill_switch_active"

PromotionDecisionLabel enum:
- EnableAutonomousBeta
- KeepSupervisedPreview
- PauseForReliabilityWork

tools/run-promotion-eval.ps1 rules:
- argument: -ArtifactRoot <path>, -Window <days, default 7>,
  -EmitDecisionOnly (switch, skips the golden eval re-run)
- preflight: refuses to run if KillSwitch is active
- runs (in order):
  - (unless -EmitDecisionOnly) calls
    tools/run-golden-eval.ps1 -ArtifactRoot .\vnext\artifacts\eval-golden
  - reads the latest soak manifest at
    vnext/artifacts/soak/<latest>/manifest.json
  - invokes the soak-driver-cli (from C-PHASE 85b) with a new
    "promotion-snapshot --window <days>" command that calls
    PromotionCriteriaSnapshot.Compute(...) and writes the decision packet
  - writes:
    vnext/artifacts/promotion/<ts>-promotion/decision.json
    vnext/artifacts/promotion/<ts>-promotion/snapshot.json
    vnext/artifacts/promotion/<ts>-promotion/run-summary.md
- exit code 1 if decision != EnableAutonomousBeta
- exit code 2 if KillSwitch is active

Settings UI (ToolPopupWindow Autonomous beta panel) rules:
- Snapshot table: one row per criterion with pass/fail icon, observed
  value, threshold, detail
- "Try the autonomous beta" entry:
  - rendered visible at all times once C-PHASE 85c lands
  - enabled state IFF:
      latest promotion_decision.json says EnableAutonomousBeta AND
      runtime_autonomous_beta_enabled is currently false AND
      runtime_kill_switch is currently false
  - clicking opens a confirmation dialog:
      title: "Enable the autonomous operations beta?"
      body: plain-English explanation of what the loop does, that
        nothing mutates, daily caps still apply, KillSwitch still works
      requires the user to click an explicit "Yes, enable the beta"
      button; pressing Escape or clicking outside cancels with no
      side effect
  - on confirm:
      writes a runtime_autonomous_beta_user_consent audit row
      (status Completed, summary "user_consent_at=<utc>") then sets
      runtime_autonomous_beta_enabled=true in the next settings snapshot
  - C-PHASE 85c MUST NOT ship a snapshot that returns EnableAutonomousBeta
    by default; the entry stays disabled until C-PHASE 85d provides
    the real decision.json

PlainLanguageExplainer additions:
- promotion_criteria_snapshot: "Computed the autonomous-beta promotion
  criteria snapshot."
- promotion_decision: "Recorded an autonomous-beta promotion decision
  packet."
- runtime_autonomous_beta_user_consent: "Recorded explicit user consent
  to enable the autonomous-beta loop."

Tests required (use synthetic fixtures; never the real ledger):
- snapshot returns KeepSupervisedPreview when ledger window < 7d
- snapshot returns PauseForReliabilityWork on any safety-class failure
  (parametric over: hosted AI > 0, policy violations > 0, mutation
  proof < 100%, focus steal > 0, kill switch observed)
- snapshot returns KeepSupervisedPreview on any liveness-class failure
  (parametric over: uptime < 50%, citation coverage < 0.6, golden eval
  fail, self_improvement_reports < per_day_minimum)
- snapshot returns EnableAutonomousBeta when all 11 criteria pass on a
  fully-populated synthetic 7-day fixture
- snapshot refuses to compute when KillSwitch is active
- Settings UI entry rendered disabled by default
- Settings UI entry enabled state requires the three conditions above
- runtime_autonomous_beta_user_consent row written ONLY when the user
  clicks the explicit confirm button (test via the dispatcher's
  click-handler unit tests)
- PlainLanguageExplainer covers every new packet kind

Validation: baseline plus
  dotnet test --filter "PromotionCriteria|AutonomousBeta|ToolPopup"
  powershell -File .\tools\run-promotion-eval.ps1 -ArtifactRoot .\vnext\artifacts\promotion\ -EmitDecisionOnly

Phase report: docs/C_PHASE85C_PROMOTION_SNAPSHOT_AND_UI_ENTRY_2026-05-13.md

PR title: "C-PHASE 85c: Promotion snapshot service and confirmation-gated UI entry"

Stop gates (do not merge if any are violated):
- snapshot's safety-class failure path returns anything other than
  PauseForReliabilityWork
- Settings UI entry can be enabled by anything other than a snapshot
  returning EnableAutonomousBeta
- confirmation dialog can be bypassed (Escape, click-outside, or any
  keyboard shortcut)
- runtime_autonomous_beta_enabled can be flipped without the consent
  audit row
- any default-off setting becomes default-on as a side effect
- the panel ships with a default decision.json that says
  EnableAutonomousBeta (must be missing or KeepSupervisedPreview)

Auto-continue: NO. Last code phase before the wall-clock soak window.
```

## Wall-Clock Window — 7-Day Soak (User Runs, Not Codex)

Codex does not implement anything during this window. Wait until the
user reports the soak window completed with no violations and that
`tools/run-promotion-eval.ps1` returned `EnableAutonomousBeta`.

The user runs:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\run-soak-driver.ps1 -Days 7 -ArtifactRoot .\vnext\artifacts\soak\
```

While the driver runs, the user uses their PC normally. The soak driver:

- emits hourly heartbeats
- runs daily self-improvement reports
- writes the per-window manifest
- never enables any default-off capability

At day 7, the user runs:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\run-promotion-eval.ps1 -ArtifactRoot .\vnext\artifacts\promotion\
```

If the script exits 0 and the `decision.json` says
`EnableAutonomousBeta`, proceed to C-PHASE 85d. If not, the user fixes
the underlying cause (the script and snapshot doc explain which
criterion failed) and restarts the soak window. Codex does nothing
until the user signals readiness.

## C-PHASE 85d — Promotion PR (with real evidence)

```text
Implement C-PHASE 85d per docs/CLAUDE_POST_CPHASE85_BLOCKER_PLAN_2026-05-13.md §4.

PRECONDITIONS (Codex MUST verify before opening the PR):
- C-PHASE 85a, 85b, 85c are merged
- vnext/artifacts/soak/<latest>-soak-window/manifest.json exists and
  spans >= 7 calendar days
- vnext/artifacts/promotion/<latest>-promotion/decision.json exists and
  says decision = "EnableAutonomousBeta"
- the latest golden eval report is PASS and dated within the soak window
- tools/run-promotion-eval.ps1 returns exit 0 when re-run

If ANY precondition is missing: STOP. Write a new blocker doc
(docs/C_PHASE85D_BLOCKER_<date>.md) explaining which precondition is
missing and what the user needs to do, then exit. Do NOT fabricate
evidence. Do NOT open the promotion PR.

Branch: claude-implementation/c-phase-85d-autonomous-operations-promotion

Goal:
Open the final autonomous-beta promotion PR using the real 7-day
evidence the user collected. This PR DOES NOT flip
runtime_autonomous_beta_enabled; the user must still click the
confirmation-gated entry shipped in C-PHASE 85c.

Add (new files):
- docs/C_PHASE85_AUTONOMOUS_OPERATIONS_PROMOTION_2026-05-13.md
- vnext/artifacts/promotion/<ts>-promotion/ (snapshot.json,
  decision.json, run-summary.md) committed as evidence
- vnext/artifacts/soak/<ts>-soak-window/manifest.json committed as
  evidence
- (optional) vnext/artifacts/eval-golden/<ts>-golden-eval/ committed
  if not already present

Modify:
- (optional) vnext/content/tool_definitions.json: bump version if
  needed
- C-PHASE 85 promotion report doc replaces the C-PHASE 85 blocker doc
  as the live promotion record (the blocker doc stays in place for
  history; do NOT delete it).

The PR MUST NOT modify any of:
- runtime_autonomous_beta_enabled (still default-off)
- pet_model_adapter_enabled
- web_search_enabled
- local_tool_exec_enabled
- tuning_lora_enabled
- runtime_kill_switch
- the BetaDecisionService thresholds
- the PromotionCriteriaSnapshot thresholds

Tests required:
- regression baseline only; this PR is evidence-bearing, not code-bearing
- a small documentation-only test that asserts the new doc references
  the decision packet's path and SHA256

Validation: baseline plus
  powershell -File .\tools\run-promotion-eval.ps1 -ArtifactRoot .\vnext\artifacts\promotion\
  (must exit 0)

Phase report: docs/C_PHASE85_AUTONOMOUS_OPERATIONS_PROMOTION_2026-05-13.md
in the style of the C-PHASE 82-84 reports. Include:
- Goal
- Scope (no code change; evidence-bearing)
- Evidence (decision packet excerpt, soak window dates, golden eval
  result)
- Safety Boundaries (no setting flipped by this PR; user consent
  required)
- Validation
- Next Phase (none; future autonomy work is a fresh roadmap)

PR title: "C-PHASE 85d: Autonomous operations promotion (with real evidence)"

Stop gates (do not merge if any are violated):
- decision.json says anything other than EnableAutonomousBeta
- runtime_autonomous_beta_enabled is set to true by this PR
- any default-off capability is enabled by this PR
- the consent audit row write path is altered

Auto-continue: NO. Final user-decision gate.
```

## Phase Sequencing Checklist

```text
phase    auto-continue?    notes
85a      no                first evidence-collection instrumentation
85b      no                first end-to-end soak surface
85c      no                last code phase before wall-clock window
SOAK     n/a               user runs 7-day window; Codex waits
85d      no                evidence-bearing promotion PR
```

## Final Single Copy-Paste Prompt to Begin C-PHASE 85a

Paste this into Codex (medium effort) to start the next phase.
Everything needed is in this prompt; supporting docs are referenced.
This produces one PR for C-PHASE 85a and stops for user review.

```text
You are Codex at medium reasoning effort, working in the Wevito repo at
C:\Users\fishe\Documents\projects\wevito (or the current worktree).

Read first:
  docs/CLAUDE_POST_CPHASE85_BLOCKER_PLAN_2026-05-13.md
  docs/CLAUDE_POST_CPHASE85_BLOCKER_CODEX_PHASE_PROMPTS_2026-05-13.md
  docs/C_PHASE85_BLOCKER_2026-05-13.md
  docs/CLAUDE_POST_CPHASE75_REVIEW_AND_ROADMAP_2026-05-13.md
  vnext/src/Wevito.VNext.Core/AuditLedgerService.cs
  vnext/src/Wevito.VNext.Core/AutonomousBetaDecisionService.cs
  vnext/src/Wevito.VNext.Core/PlainLanguageExplainer.cs
  vnext/src/Wevito.VNext.Core/WindowsPowerHandler.cs
  vnext/src/Wevito.VNext.Core/FocusStealCounter.cs
  vnext/src/Wevito.VNext.Core/RuntimeBudgetMeter.cs
  vnext/src/Wevito.VNext.Shell/ShellCoordinator.cs

Implement C-PHASE 85a: Evidence-Collection Instrumentation.

Branch: claude-implementation/c-phase-85a-evidence-instrumentation

Goal:
Emit honest ledger rows during normal use so a 7-day soak window can
collect real autonomous-beta promotion evidence. Do NOT enable any
default-off capability. Do NOT fabricate evidence. Use synthetic
ledger fixtures in tests.

Add (new files):
- vnext/src/Wevito.VNext.Core/RuntimeSessionTracker.cs
- vnext/src/Wevito.VNext.Core/DailyEvidenceSnapshotService.cs
- vnext/tests/Wevito.VNext.Tests/RuntimeSessionTrackerTests.cs
- vnext/tests/Wevito.VNext.Tests/DailyEvidenceSnapshotServiceTests.cs
- vnext/tests/Wevito.VNext.Tests/AutonomousBetaDecisionServicePrecisionTests.cs
- docs/C_PHASE85A_EVIDENCE_INSTRUMENTATION_2026-05-13.md

Modify:
- vnext/src/Wevito.VNext.Core/AutonomousBetaDecisionService.cs
  (tighten checks per hard rules below)
- vnext/src/Wevito.VNext.Core/PlainLanguageExplainer.cs
  (cover all new packet kinds + the existing power/session kinds)
- vnext/src/Wevito.VNext.Core/WindowsPowerHandler.cs
  (sleep/lock rows: Status="Completed", preserve forceQuiet=true side
   effect)
- vnext/src/Wevito.VNext.Shell/ShellCoordinator.cs
  (wire RuntimeSessionTracker + DailyEvidenceSnapshotService into the
   startup / tick / shutdown path)
- vnext/content/tool_definitions.json
  (document new packet kinds, if applicable)

RuntimeSessionTracker rules:
- packet kinds: runtime_session_start, runtime_session_heartbeat,
  runtime_session_end, runtime_session_paused
- summaries include uptime_hours=<int>; once >= 4 wall-clock hours,
  summary literally contains "uptime_hours>=4"
- emits start at first ready tick, hourly heartbeat thereafter
  (configurable runtime_session_heartbeat_minutes default 60, min 15),
  end on graceful shutdown
- KillSwitch active: emits runtime_session_paused with status Completed
  and summary kill_switch_active=true (informational; not a violation)
- persists session state under
  %LOCALAPPDATA%/Wevito/audit/runtime-session.json
- never opens a network connection; never throws

DailyEvidenceSnapshotService rules:
- packet kinds: budget_meter_snapshot, focus_steal_snapshot
- emits exactly once per UTC day at first tick after midnight
- budget_meter_snapshot summary: "budget_exceeded=true|false" + counts
- focus_steal_snapshot summary: "focus_steal=true|false" + delta/total
- persists last_snapshot_date_utc to
  %LOCALAPPDATA%/Wevito/audit/daily-snapshot.json
- KillSwitch active: no row written; never throws

WindowsPowerHandler change:
- Record(...) uses Status="Completed" for ALL packet kinds
- the forceQuiet=true side effect (settings quiet + background off) is
  preserved
- update WindowsPowerHandlerTests accordingly

AutonomousBetaDecisionService change (tighten, do NOT weaken):
- zero_policy_violations: counts only UnifiedPolicyService Blocked rows
  + policy-named rows with non-empty Error; explicitly excludes
  power/session/runtime_session_paused
- zero_focus_steal_events: requires at least one focus_steal_snapshot
  row in window AND no focus_steal=true substring in any snapshot summary
- resource_budget_within_tolerance: requires at least one
  budget_meter_snapshot row in window AND no budget_exceeded=true
  substring in any snapshot summary
- active_uptime_present: at least one runtime_session_* row with
  uptime_hours>=4 in summary
- preview_activity_present, mutations_have_proof_packets,
  zero_hosted_ai_local_only: unchanged

PlainLanguageExplainer additions (must not fall through to WarnUnknown
on any of these):
- runtime_session_start: "Wevito recorded the start of an active runtime session."
- runtime_session_heartbeat: "Wevito recorded a runtime session heartbeat (still alive)."
- runtime_session_end: "Wevito recorded the end of an active runtime session."
- runtime_session_paused: "Wevito paused background work because Stop Everything is active."
- budget_meter_snapshot: "Recorded the daily resource budget snapshot."
- focus_steal_snapshot: "Recorded the daily focus-steal counter snapshot."
- power_sleep: "Wevito entered Quiet mode because the system slept."
- power_resume: "Wevito recorded the system resuming."
- session_lock: "Wevito entered Quiet mode because the session locked."
- session_unlock: "Wevito recorded the session unlocking."

Tests required:
- RuntimeSessionTracker emits start row exactly once (mocked clock)
- 4th hourly heartbeat summary contains "uptime_hours>=4"
- end row emitted on graceful shutdown call
- runtime_session_paused emitted when KillSwitch active mid-window
- DailyEvidenceSnapshotService emits exactly one budget snapshot and
  one focus snapshot per UTC day (mocked clock)
- KillSwitch suppresses DailyEvidenceSnapshotService emissions
- WindowsPowerHandler sleep/lock rows have Status="Completed"
- AutonomousBetaDecisionService no longer flags a power_sleep row as a
  policy violation
- AutonomousBetaDecisionService requires both focus_steal_snapshot AND
  budget_meter_snapshot rows to satisfy their checks
- PlainLanguageExplainer covers every new and existing power/session
  packet kind
- full regression sweep stays green (expected: 505 + new tests)

Hard rules:
- never enable a default-off capability in tests or production paths
- never open a network connection in tests (assert via fake HttpClient
  that throws on send)
- never throw out of any of the new public methods
- exactly one ledger row per emission
- KillSwitch is honored uniformly

Validation (run all):
  dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "RuntimeSession|DailyEvidenceSnapshot|AutonomousBeta|PlainLanguage|WindowsPower"
  dotnet build .\vnext\Wevito.VNext.sln
  dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
  powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests

Phase report: docs/C_PHASE85A_EVIDENCE_INSTRUMENTATION_2026-05-13.md in
the style of the C-PHASE 82-84 reports.

Commit / PR:
- Branch: claude-implementation/c-phase-85a-evidence-instrumentation
- Commit messages reference C-PHASE 85a
- PR title: "C-PHASE 85a: Evidence-collection instrumentation"
- After commit + push: STOP for user review.

Stop gates (do not merge if any are violated):
- a heartbeat row is emitted more than once per hour
- the AutonomousBetaDecisionService change weakens a safety-class check
- WindowsPowerHandler loses the forceQuiet=true side effect
- a test enables a default-off capability
- a test opens a network connection
- a new packet kind is not covered by the explainer
- KillSwitch behavior is weakened in any path

Auto-continue: NO. Stop for user review.
```
