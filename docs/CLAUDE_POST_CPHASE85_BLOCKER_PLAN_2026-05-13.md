# Wevito Post-C-PHASE-85 Blocker Plan and Evidence-Collection Roadmap

Date: 2026-05-13
Author: Claude (Opus 4.7), commissioned by `ssebrobles@gmail.com`
Audience: Codex at medium reasoning effort + project owner review
Companion file: `docs/CLAUDE_POST_CPHASE85_BLOCKER_CODEX_PHASE_PROMPTS_2026-05-13.md`
Predecessor plans reviewed:
- `docs/CLAUDE_POST_CPHASE75_REVIEW_AND_ROADMAP_2026-05-13.md`
- `docs/CLAUDE_POST_CPHASE75_CODEX_PHASE_PROMPTS_2026-05-13.md`
- `docs/C_PHASE82_GOLDEN_EVAL_AND_REGRESSION_GATE_2026-05-13.md`
- `docs/C_PHASE83_SELF_IMPROVEMENT_REPORT_LOOP_2026-05-13.md`
- `docs/C_PHASE84_GUARDED_CODE_MUTATION_PILOT_2026-05-13.md`
- `docs/C_PHASE85_BLOCKER_2026-05-13.md`

This document covers:

1. The current blocker on autonomous-beta promotion.
2. Root-cause analysis of why no evidence has accumulated.
3. Answers to the six review questions the project owner asked.
4. A new phase roadmap C-PHASE 85a through C-PHASE 85d that gates retry of C-PHASE 85.
5. Hard invariants locked across all phases.
6. References.

## 0. Executive Picture

```text
where we are
|
+-- main is at 57d1dcb75 "C-PHASE 85: document promotion evidence blocker"
+-- C-PHASE 82 (golden eval), 83 (self-improvement report), 84 (guarded code
|   mutation pilot) are all merged and green
+-- C-PHASE 85 correctly STOPPED before opening a promotion PR
|   |
|   `-- audit ledger has only 1 row (a single self_improvement_report from
|       2026-05-13), focus-steal counter file is missing, budget meter file
|       is missing, no soak window has actually been run
|
`-- the C-PHASE 78 hardening + run-soak-validation.ps1 are scaffolds
    |
    +-- WindowsForegroundFullscreenMonitor + WindowsPowerHandler +
    |   FocusStealCounter + SoakRunnerService exist as Core services
    +-- BUT run-soak-validation.ps1 only writes ONE snapshot file; it does
    |   not actually keep Wevito running, does not emit periodic ledger rows
    +-- AND no service emits the "runtime_session uptime_hours>=4" row that
    |   AutonomousBetaDecisionService checks for
    `-- AND every WindowsPowerHandler sleep/lock event writes Status="Blocked",
        which the same decision service treats as a policy violation

next move
|
+-- DO NOT fabricate evidence
+-- DO NOT flip runtime_autonomous_beta_enabled
+-- DO build the missing instrumentation so the next 7-day window actually
|   accumulates honest evidence
+-- DO build the promotion-eval snapshot tool now (no real evidence required
|   to build the tool; it must refuse to compute promotion without 7 days)
`-- THEN the user runs a 7-day soak with normal PC use
    `-- THEN open the C-PHASE 85 promotion PR with real packets
```

Hard product constraints (unchanged):

- Wevito remains its own local AI; no GPT/Claude/Codex/Gemini at runtime.
- No hidden web access, no hidden local file access, no hidden training.
- No hidden tool execution, no hidden code or asset mutation.
- Every risky capability stays default-off until reviewed.
- Pets remain visually regular pet-sim characters.
- Always-on behavior must not interfere with the user's PC.
- KillSwitch, audit ledger, evidence packets, rollback, supervisor modes
  remain load-bearing and cannot be weakened by any new phase below.

## 1. The Blocker (recap from C-PHASE 85 report)

C-PHASE 85 stopped without opening a promotion PR because:

- audit ledger at `%LOCALAPPDATA%/Wevito/audit/ledger.sqlite` contains 1 row
- `%LOCALAPPDATA%/Wevito/audit/focus-steal.json` is missing
- `%LOCALAPPDATA%/Wevito/audit/budget-meter.json` is missing
- the only soak artifact is a smoke setup, not a 7-day soak

`AutonomousBetaDecisionService` therefore cannot return
`enable_autonomous_beta` because the ledger has no `runtime_session` row,
no preview/proposal activity, no mutation+proof pairs, and no multi-day
window. The C-PHASE 85 stop was correct.

## 2. Root-Cause Analysis

Verified by reading source.

```text
why the soak window never produced evidence
|
+-- run-soak-validation.ps1 (tools/) writes ONE JSON snapshot and exits
|   it does not:
|     - hold the shell process alive for the configured hours
|     - emit any audit ledger rows
|     - run the self-improvement report at the end of the day
|     - call SoakRunnerService.StartExplicitPreview / CompletePreview
|
+-- SoakRunnerService exists in Core and DOES write soak_session_start /
|   soak_session_end packets, but no script and no shell tick invokes it
|
+-- the shell tick already wires FocusStealCounter + RuntimeBudgetMeter +
|   WindowsForegroundFullscreenMonitor + WindowsPowerHandler, but they
|   only persist when something happens:
|     - FocusStealCounter.RecordActivation writes only on WM_ACTIVATE
|     - RuntimeBudgetMeter.FlushIfDue writes only every 5 min if a tick
|       runs; if Wevito is idle, no flush is queued either
|     - WindowsForegroundFullscreenMonitor writes only on state transitions
|     - WindowsPowerHandler writes only on Sleep/Resume/Lock/Unlock
|
+-- AutonomousBetaDecisionService.BuildChecks requires:
|     - a row with packet_kind="runtime_session" AND summary contains
|       "uptime_hours>=4" -- but NO service in the repo emits this row
|     - 0 rows where Status="Blocked" -- but WindowsPowerHandler.Record
|       writes Status="Blocked" on every sleep/lock event during normal
|       PC use, which would fail the check spuriously
|     - mutations_have_proof_packets -- the C-PHASE 84 pilot writes a
|       proof_packet, but only when explicitly invoked
|     - 0 hosted-AI rows in LocalOnly -- fine
|     - 0 focus_steal=true substrings -- nothing writes that exact string
|       today, so this check is vacuous, not informative
|     - 0 budget_exceeded=true substrings -- nothing writes that exact
|       string today either
|     - >=1 preview/proposal row -- happens once when the user invokes a
|       helper, but not daily by itself
|
`-- AutonomousOperationsLoop is gated by runtime_autonomous_beta_enabled,
    which is default-off, so it never runs during normal use and never
    accumulates rows -- a chicken-and-egg situation if we let the loop
    be the only source of the soak window's evidence.
```

In short: the instrumentation pretends to be present, but the only writer
that is meant to run during normal use (the autonomous loop) is gated off,
and the script that is supposed to keep the soak going is a stub. The
solution is to add explicit instrumentation that runs during normal use
without enabling any new capability, plus a real soak driver script.

## 3. Answering the Six Review Questions

### 3.1 What must be done before C-PHASE 85 can be retried?

```text
preconditions for the retry
|
+-- a real heartbeat-style "runtime_session" row is being emitted while
|   Wevito is open during normal use (hourly + on graceful shutdown)
|
+-- a daily "focus_steal_snapshot" row is being emitted that records the
|   focus-steal counter total, day-by-day delta, and an explicit
|   "focus_steal=true|false" substring so the gate is informative
|
+-- a daily "budget_meter_snapshot" row is being emitted that records
|   the budget meter counters, an explicit "budget_exceeded=true|false"
|   substring, and resource snapshots
|
+-- WindowsPowerHandler emits its sleep/lock rows with Status="Completed"
|   (NOT "Blocked") and instead writes a structured power_event reason in
|   the summary; OR the BetaDecisionService treats only true policy
|   violations as Blocked
|
+-- tools/run-soak-validation.ps1 actually keeps a heartbeat going,
|   triggers daily reports, and writes a soak-summary at the end
|
+-- a real 7-day soak window has been completed by the user, with the
|   ledger snapshot, focus-steal snapshot, and budget meter snapshot all
|   showing zero violations across the window
|
+-- tools/run-promotion-eval.ps1 has been built and dry-run against the
|   7-day evidence and emits a structured decision packet
|
`-- the Settings UI is ready to expose a confirmation-gated "Try the
    autonomous beta" entry but does NOT flip the flag by default
```

### 3.2 Do we need a new C-PHASE 85a/86 evidence-collection phase before promotion?

Yes. Three implementation phases plus a wall-clock window:

```text
C-PHASE 85a -- Evidence-Collection Instrumentation
C-PHASE 85b -- Soak Driver + Status UX
C-PHASE 85c -- Promotion Snapshot + Confirmation-Gated UI Entry
   |
   v
USER WALL-CLOCK SOAK (7 days) -- not a Codex phase
   |
   v
C-PHASE 85d -- Final Promotion PR (uses the evidence the soak collected)
```

Each implementation phase is a separate PR sized for Codex medium effort.
The wall-clock window is the user's job; Codex cannot compress time, and
must not fabricate evidence.

### 3.3 Instrumentation that is missing right now

```text
required instrumentation              currently present?     phase
|
+-- focus-steal counter                yes (file written on  85a fixes
|   on demand)                         WM_ACTIVATE only)     (daily snap)
+-- runtime uptime/session row         NO (decision service  85a adds
|   ("runtime_session uptime_hours>=4")  checks for it but     RuntimeSession
|                                       no service writes it) Tracker
+-- budget meter snapshot row          NO (file flushed but   85a adds
|   ("budget_exceeded=true|false")      no ledger row)        BudgetSnapshot
+-- policy violations channel          partial (UnifiedPolicy 85a clarifies
|                                       writes block rows;     "Blocked"
|                                       PowerHandler also      semantics
|                                       writes Status=Blocked  + adds power
|                                       which is wrong)        event status
+-- mutation proof coverage            partial (C-PHASE 84    85a adds
|   (every mutation packet has a       pilot writes both       proof-coverage
|   proof_packet sibling within        manifest entries; broad index in
|   10 min)                            scan not automated)    self-improve
+-- daily self-improvement reports     yes (tool exists) but  85b runs it
|                                       not invoked daily      daily via
|                                                              soak driver
+-- citation coverage ratio            yes (C-PHASE 81 packet 85c rolls
|   per local_reasoning packet         field) but not rolled  it into the
|                                       up across the window   promotion
|                                                              snapshot
+-- autonomous-operations rows         NO during normal use   intentional
    (loop is default-off)              -- this is the         (the loop
                                        chicken-and-egg case   stays off
                                        the plan must avoid)   until the
                                                               promotion
                                                               PR ships)
```

The fix is to make "Wevito is alive and behaving" produce honest evidence
rows even when the autonomous-operations loop is dormant. The autonomous
loop is the consequence of passing the gate, not the source of the
evidence that opens the gate.

### 3.4 Scripts and UI surfaces Codex should add for reliable 7-day collection

```text
new scripts (under tools/)
|
+-- tools/run-soak-driver.ps1
|   - keeps a heartbeat going (writes runtime_session every hour)
|   - writes a daily focus_steal_snapshot + budget_meter_snapshot
|   - runs run-self-improvement-report.ps1 each day at midnight UTC
|   - persists session start/end and the soak_summary at the end
|   - never enables any default-off capability
|   - never opens the network; never calls hosted AI
|   - argument: -Days <1-14>, -ArtifactRoot <path>
|
+-- tools/run-promotion-eval.ps1
|   - reads 7 days of ledger rows + budget + focus snapshots
|   - runs golden eval (run-golden-eval.ps1 inline)
|   - invokes PromotionCriteriaSnapshot
|   - writes snapshot.json + decision.json + run-summary.md
|   - exit code 1 if decision != enable_autonomous_beta
|
+-- tools/check-evidence-readiness.ps1
|   - prints the current "are we ready for promotion?" table
|   - safe to run any time; no side effects
|
new UI surfaces (under Settings -> PET TASKS / HomePanel)
|
+-- Activity panel: new "Evidence collection" tab
|   - shows: current soak window (started, day N of 7, ends)
|   - per-day row count, flagged rows, mutation count
|   - last self-improvement report timestamp + flagged row count
|   - per-day focus-steal counter delta
|   - per-day budget meter result
|   - readiness badge: not_started | day_N_of_7 | ready_to_eval |
|     blocked_by_<criterion>
|
+-- HomePanel: small "Evidence: day N of 7" badge near the existing
|   "Stop everything" toggle, only visible when a soak driver session
|   is active
|
+-- Settings -> Autonomous beta panel: confirmation-gated "Try the
|   autonomous beta" entry (only enabled when the snapshot returns
|   enable_autonomous_beta; clicking opens a confirmation dialog that
|   writes an explicit user-consent audit row and ONLY THEN sets the
|   setting)
```

### 3.5 Validating each phase without hosted AI, hidden network, hidden mutation, or hidden training

```text
validation rules (apply to every phase below)
|
+-- no test downloads model weights or tokenizer.json
+-- no test opens any TCP/UDP socket; assert via a fake HttpClient that
|   throws on Send
+-- no test enables any default-off setting; assert via a settings sweep
|   at the end of each test
+-- no test mutates a real repo source file; mutation pilots use the
|   pilot-only root (vnext/content/guarded-mutation-pilot/) or temp dirs
+-- no test trains weights or invokes ONNX inference outside the
|   existing tokenizer/fixture path
+-- every new service writes exactly one ledger row per emission, with
|   did_use_network=false, did_use_hosted_ai=false, did_mutate=false
|   (with one explicit, audited exception for mutation_apply rows the
|   pilot already produces)
+-- KillSwitch is asserted to block every new service: the service must
|   refuse to do work and must not write a ledger row when the kill
|   switch is active
+-- every PR runs the full validation baseline:
|     dotnet build .\vnext\Wevito.VNext.sln
|     dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
|     powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
+-- every phase report doc states the resulting test count (current
|   baseline: 505/505 green per C-PHASE 84) and any new packet kinds
|   added to PlainLanguageExplainer.KnownPacketKinds
+-- every phase report doc states what would have been promoted by this
|   phase and what is still default-off
```

### 3.6 Exact promotion criteria when evidence is finally collected

These are the criteria `PromotionCriteriaSnapshot` must compute, encoded
exactly so the gate is auditable. Drawn verbatim from the C-PHASE 85
plan and reinforced by the C-PHASE 85 blocker review.

```text
criterion                                  threshold                source
|
+-- window_days                            >= 7                     ledger range
+-- active_uptime_per_active_day           >= 50% of waking hours   runtime_session
|                                                                   rows
+-- policy_violations                      == 0                     UnifiedPolicy
|                                                                   blocked rows
|                                                                   (NOT power
|                                                                    sleep/lock
|                                                                    rows)
+-- mutations_with_proof_packet_pct        == 100%                  every mutate=1
|                                                                   row has a
|                                                                   proof_packet
|                                                                   within +/-10m
+-- hosted_ai_calls_in_local_only          == 0                     did_use_hosted_ai
|                                                                   when pet_model_
|                                                                   mode=LocalOnly
+-- focus_steal_events                     == 0                     focus_steal
|                                                                   counter delta
+-- resource_budget_within_pct             abs <= 10%               budget meter
|                                                                   snapshots
+-- citation_coverage_ratio                >= 0.6                   latest golden
|                                                                   eval result
+-- golden_eval_result                     PASS                     latest gate
|                                                                   row not flagged
+-- self_improvement_reports_per_day       >= 1 per active day      report packets
|
`-- kill_switch_active_during_window       == false                 any kill_switch
                                                                    row in window
                                                                    flips decision
                                                                    to keep_supervised
                                                                    _preview
```

If all pass: decision = `enable_autonomous_beta`, and the promotion PR
opens a confirmation-gated Settings entry (but does NOT flip the flag).

If any fail: decision = `pause_for_reliability_work` (for safety-class
failures: hosted_ai_calls, policy_violations, mutation proof, focus
steal, kill switch) or `keep_supervised_preview` (for liveness-class
failures: uptime, citation coverage, golden eval).

## 4. Phase Roadmap C-PHASE 85a → C-PHASE 85d

Each phase below follows the same template:

```text
Goal
Scope (exact)
Files likely touched
New files
Tests
Validation commands
Artifacts / reports
Stop gates
Rollback plan
Commit / PR instructions
Auto-continue?
```

### C-PHASE 85a — Evidence-Collection Instrumentation

Goal: emit honest ledger rows during normal use without enabling any
new capability so a 7-day soak window can collect real evidence.

Scope:

- Add `RuntimeSessionTracker` Core service:
  - records `runtime_session` packets with summaries that include the
    literal substring `uptime_hours>=4` once the running session has
    exceeded 4 wall-clock hours.
  - emits on:
    - process startup (a `runtime_session_start` row with status
      `Completed`, summary includes `uptime_hours=0`)
    - hourly thereafter (a `runtime_session_heartbeat` row with status
      `Completed`, summary includes `uptime_hours=<int>` and
      `uptime_hours>=4` once the threshold is reached)
    - graceful shutdown (a `runtime_session_end` row with status
      `Completed`, summary includes the final `uptime_hours=<int>` value)
  - never blocks the UI thread; uses the existing tick + a small monotonic
    timer.
  - respects KillSwitch: when kill switch is active, the tracker still
    emits a `runtime_session_paused` row with status `Completed` and
    summary `kill_switch_active=true` (this is informational; it must
    not flag as a policy violation).
  - default state path: `%LOCALAPPDATA%/Wevito/audit/runtime-session.json`
    persists `session_started_at_utc` so a crash-restart still records
    the prior session's final row.
- Add `DailyEvidenceSnapshotService` Core service:
  - once per UTC day at the first tick after midnight, emits:
    - `budget_meter_snapshot` row with summary including either
      `budget_exceeded=true` or `budget_exceeded=false` (computed from
      the most recent reservation result), plus the day's used / max
      counts.
    - `focus_steal_snapshot` row with summary including either
      `focus_steal=true` or `focus_steal=false` (true if the day's delta
      > 0), plus the day's delta and the counter total.
  - persists `last_snapshot_date_utc` to
    `%LOCALAPPDATA%/Wevito/audit/daily-snapshot.json` to avoid duplicates.
  - respects KillSwitch: when active, no row is written (the next active
    day will write the next snapshot).
- Adjust `WindowsPowerHandler.Record`:
  - sleep / lock rows: `Status = "Completed"`, summary = "Power/session
    event entered quiet mode automatically." (the existing
    `forceQuiet=true` action still occurs, but the row no longer reads
    as a policy violation).
  - resume / unlock rows: `Status = "Completed"` (unchanged content).
  - rationale: a sleeping laptop is not a policy violation, and the
    `AutonomousBetaDecisionService` zero_policy_violations check should
    only fire on `UnifiedPolicyService` block rows.
- Adjust `AutonomousBetaDecisionService.BuildChecks` to be more precise:
  - `zero_policy_violations` now requires:
    - 0 rows from `UnifiedPolicyService` with `Status="Blocked"`, AND
    - 0 rows from `PetTaskAdapterPreviewDispatcher` with status indicating
      a refused approval, AND
    - 0 rows from `KillSwitchService` that indicate a user-triggered
      kill switch event that lasted > 1 hour.
  - power_sleep / session_lock / runtime_session_paused are NOT policy
    violations.
  - `zero_focus_steal_events` is now satisfied when:
    - at least one `focus_steal_snapshot` row exists in the window AND
    - no `focus_steal_snapshot` summary contains `focus_steal=true`.
  - `resource_budget_within_tolerance` is now satisfied when:
    - at least one `budget_meter_snapshot` row exists in the window AND
    - no `budget_meter_snapshot` summary contains `budget_exceeded=true`.
  - `active_uptime_present` is satisfied when at least one
    `runtime_session` row exists with `uptime_hours>=4` in the window.
- Extend `PlainLanguageExplainer.KnownPacketKinds` and `ExplainPacketKind`
  to cover:
  - `runtime_session_start`, `runtime_session_heartbeat`,
    `runtime_session_end`, `runtime_session_paused`
  - `budget_meter_snapshot`, `focus_steal_snapshot`
  - `power_sleep`, `power_resume`, `session_lock`, `session_unlock`
    (these already exist as kinds in WindowsPowerHandler, but the
    explainer must explicitly cover them so the Activity panel shows
    plain-language text)

Files likely touched:

```text
vnext/src/Wevito.VNext.Core/AutonomousBetaDecisionService.cs
vnext/src/Wevito.VNext.Core/PlainLanguageExplainer.cs
vnext/src/Wevito.VNext.Core/WindowsPowerHandler.cs
vnext/src/Wevito.VNext.Shell/ShellCoordinator.cs
vnext/content/tool_definitions.json                       (new packet kind docs)
```

New files:

```text
vnext/src/Wevito.VNext.Core/RuntimeSessionTracker.cs
vnext/src/Wevito.VNext.Core/DailyEvidenceSnapshotService.cs
vnext/tests/Wevito.VNext.Tests/RuntimeSessionTrackerTests.cs
vnext/tests/Wevito.VNext.Tests/DailyEvidenceSnapshotServiceTests.cs
vnext/tests/Wevito.VNext.Tests/AutonomousBetaDecisionServicePrecisionTests.cs
docs/C_PHASE85A_EVIDENCE_INSTRUMENTATION_2026-05-13.md
```

Tests:

- RuntimeSessionTracker emits the start row exactly once.
- RuntimeSessionTracker emits one heartbeat per simulated hour; the
  4th heartbeat summary contains `uptime_hours>=4`.
- RuntimeSessionTracker emits the end row on graceful shutdown.
- DailyEvidenceSnapshotService emits exactly one budget snapshot and
  exactly one focus snapshot per UTC day (mocked clock).
- DailyEvidenceSnapshotService does not emit when KillSwitch is active.
- WindowsPowerHandler sleep/lock rows now have `Status="Completed"`.
- AutonomousBetaDecisionService no longer flags a power_sleep row as a
  policy violation.
- AutonomousBetaDecisionService requires both `focus_steal_snapshot` and
  `budget_meter_snapshot` rows to satisfy their checks (no row -> fail).
- PlainLanguageExplainer covers every new packet kind.
- Full regression: 505/505 + the new tests stay green.

Validation commands:

```powershell
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "RuntimeSession|DailyEvidenceSnapshot|AutonomousBeta|PlainLanguage|WindowsPower"
dotnet build .\vnext\Wevito.VNext.sln
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
```

Artifacts:

```text
%LOCALAPPDATA%/Wevito/audit/runtime-session.json
%LOCALAPPDATA%/Wevito/audit/daily-snapshot.json
docs/C_PHASE85A_EVIDENCE_INSTRUMENTATION_2026-05-13.md
```

Stop gates:

- Stop if a heartbeat row is emitted more than once per hour.
- Stop if the AutonomousBetaDecisionService change weakens any
  safety-class check (hosted AI, mutation proof, focus steal, budget).
- Stop if the power-handler change loses the existing `forceQuiet=true`
  side effect (the supervisor must still go quiet on sleep/lock).
- Stop if any test enables a default-off capability.
- Stop if any test opens a network connection.
- Stop if the explainer does not cover a new packet kind.

Rollback: revert PR; the ledger writes resume to the prior (insufficient)
shape and the C-PHASE 85 blocker remains in place.

Commit / PR instructions:

- Branch: `claude-implementation/c-phase-85a-evidence-instrumentation`
- Commit messages reference `C-PHASE 85a`.
- PR title: `C-PHASE 85a: Evidence-collection instrumentation`.
- Phase report: `docs/C_PHASE85A_EVIDENCE_INSTRUMENTATION_2026-05-13.md`.

Auto-continue? **No.** First wall-clock-aware instrumentation; stop for
user review before C-PHASE 85b lands the soak driver script.

### C-PHASE 85b — Soak Driver + Status UX

Goal: build the tooling and UI that actually keeps a 7-day evidence
collection window honest, and that lets the user see how far into the
soak they are without enabling anything.

Scope:

- Add `tools/run-soak-driver.ps1`:
  - argument: `-Days <int>` (1-14, default 7), `-ArtifactRoot <path>`,
    `-HeartbeatMinutes <int>` (default 60), `-StopOnPowerSleep` (default
    false; if true, exits cleanly on Suspend so the next run can resume
    a fresh window).
  - probes that Wevito is running (uses `Get-Process wevito-vnext` or
    the equivalent shell process name; if not running, prints a clear
    instruction and exits 2).
  - invokes a thin dotnet helper (`tools/soak-driver-cli/`) that:
    - writes `runtime_session_start` once
    - writes a `runtime_session_heartbeat` every HeartbeatMinutes
    - writes a `daily_evidence_marker` row at UTC midnight (informational;
      does not duplicate the snapshots emitted by the shell)
    - runs `tools/run-self-improvement-report.ps1 -Window day` once per
      active day
    - writes `runtime_session_end` and the soak summary on Ctrl+C or
      end-of-window
  - never opens a network connection (asserted by argument constraint
    `--no-network` always set in the dotnet helper).
  - never enables a default-off capability; the script reads the current
    settings snapshot, and refuses to start if any of these are true:
      `runtime_autonomous_beta_enabled`, `pet_model_adapter_enabled`,
      `web_search_enabled`, `local_tool_exec_enabled`, `tuning_lora_enabled`,
      `runtime_kill_switch`.
  - writes the soak window manifest to
    `vnext/artifacts/soak/<ts>-soak-window/manifest.json` with the
    start, end, day count, ledger row count, flagged row count,
    focus-steal totals, and budget meter snapshot.
- Add a tiny `tools/soak-driver-cli/` console project that:
  - shares the `Wevito.VNext.Core` assembly (no duplicated logic).
  - exposes `wevito-soak-driver heartbeat --reason <reason>`,
    `wevito-soak-driver day-end`, `wevito-soak-driver window-end`,
    `wevito-soak-driver status`.
  - is built by `tools/build-vnext.ps1` so the run-soak-driver script
    can call the built binary.
- Add `tools/check-evidence-readiness.ps1`:
  - reads the ledger via the existing AuditLedgerService snapshot path.
  - prints the readiness table without side effects.
- Add `EvidenceCollectionStatusService` Core service:
  - reads ledger range + the manifest at
    `vnext/artifacts/soak/` to determine `Day N of 7` style status.
  - returns `EvidenceCollectionStatus { Active, StartedAtUtc, DayN,
    DayMax, RowsToday, FlaggedRowsToday, LastReportAtUtc,
    LastReadinessLabel }`.
- Wire `EvidenceCollectionStatusService` into:
  - `ToolPopupWindow` Activity panel: a new "Evidence collection" tab
    with the per-day breakdown.
  - `HomePanelWindow`: a small "Evidence: day N of 7" badge near the
    existing Stop everything toggle, hidden unless a soak window is
    active.
  - `RoamBandWindow` overlay banner: appended to the existing live
    status line when active.

Files likely touched:

```text
vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml(.cs)
vnext/src/Wevito.VNext.Shell/HomePanelWindow.xaml(.cs)
vnext/src/Wevito.VNext.Shell/RoamBandWindow.xaml(.cs)
vnext/src/Wevito.VNext.Shell/ShellCoordinator.cs
tools/build-vnext.ps1                                       (build the cli)
tools/run-soak-validation.ps1                               (keep as a thin
                                                             wrapper that
                                                             delegates to
                                                             run-soak-driver.ps1
                                                             for compatibility)
```

New files:

```text
vnext/src/Wevito.VNext.Core/EvidenceCollectionStatusService.cs
vnext/tests/Wevito.VNext.Tests/EvidenceCollectionStatusServiceTests.cs
tools/run-soak-driver.ps1
tools/check-evidence-readiness.ps1
tools/soak-driver-cli/Wevito.VNext.SoakDriver.csproj
tools/soak-driver-cli/Program.cs
tools/soak-driver-cli/README.md
docs/C_PHASE85B_SOAK_DRIVER_AND_STATUS_UX_2026-05-13.md
```

Tests:

- `EvidenceCollectionStatusService` returns `Active=false, DayN=0` when
  no soak manifest exists.
- It returns `Day=N` correctly across a multi-day manifest (mocked
  clock).
- The soak driver script refuses to start when any default-off setting
  is enabled (smoke-tested by a PowerShell Pester or by a small dotnet
  integration test against a fake settings snapshot).
- The CLI writes exactly one ledger row per `heartbeat` call.
- The CLI honors KillSwitch (refuses to write when active).
- Activity-panel rendering does not include any private text from the
  ledger rows (only counts, kinds, plain-language sentences).

Validation commands:

```powershell
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "EvidenceCollectionStatus|SoakDriver"
dotnet build .\vnext\Wevito.VNext.sln
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\check-evidence-readiness.ps1
```

Manual smoke (user-run, 1 hour or less, optional before the full soak):

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\run-soak-driver.ps1 -Days 1 -HeartbeatMinutes 5 -ArtifactRoot .\vnext\artifacts\soak\
```

Artifacts:

```text
vnext/artifacts/soak/<ts>-soak-window/manifest.json
vnext/artifacts/soak/<ts>-soak-window/heartbeats.jsonl
vnext/artifacts/soak/<ts>-soak-window/run-summary.md
docs/C_PHASE85B_SOAK_DRIVER_AND_STATUS_UX_2026-05-13.md
```

Stop gates:

- Stop if the driver script can be coerced into enabling any default-off
  capability.
- Stop if the driver script opens any network connection.
- Stop if the Activity-panel "Evidence collection" tab displays any
  private text from the ledger rows.
- Stop if the driver script silently keeps running after a Wevito
  process exit (it must detect the missing process and stop).
- Stop if the CLI can be invoked without the dotnet helper actually
  being built (the script must fail clearly if the build is missing).

Rollback: revert PR; the C-PHASE 78 `run-soak-validation.ps1` stub
remains as a placeholder but does not collect evidence.

Commit / PR instructions:

- Branch: `claude-implementation/c-phase-85b-soak-driver-and-status-ux`
- Commit messages reference `C-PHASE 85b`.
- PR title: `C-PHASE 85b: Soak driver and evidence-collection status UX`.
- Phase report: `docs/C_PHASE85B_SOAK_DRIVER_AND_STATUS_UX_2026-05-13.md`.

Auto-continue? **No.** First end-to-end soak surface; stop for review.

### C-PHASE 85c — Promotion Snapshot + Confirmation-Gated UI Entry

Goal: implement the promotion-eval tooling and the Settings UI entry
that the final C-PHASE 85 PR will flip — *without* enabling the entry,
and using synthetic test fixtures only so no real evidence is required
to land this phase.

Scope:

- Add `PromotionCriteriaSnapshot` Core service:
  - inputs: ledger snapshot range (default 7 days), latest golden eval
    result, budget-meter snapshot, focus-steal snapshot, settings
    snapshot.
  - outputs: structured pass/fail table matching the criteria in
    section 3.6 above.
  - emits a `promotion_criteria_snapshot` audit row with status
    `Completed` and a summary like `decision=enable_autonomous_beta,
    passes=10/10`.
  - refuses to compute when:
    - ledger window < 7 days (decision = `keep_supervised_preview`,
      reason `window_too_short`)
    - any safety-class check fails (decision =
      `pause_for_reliability_work`)
    - any liveness-class check fails (decision =
      `keep_supervised_preview`)
    - kill switch was active at any point in the window (decision =
      `keep_supervised_preview`, reason `kill_switch_observed`)
- Add `tools/run-promotion-eval.ps1`:
  - argument: `-ArtifactRoot <path>`, `-Window <days>` (default 7),
    optional `-EmitDecisionOnly` (skip the golden eval re-run).
  - runs:
    - reads the soak manifest at `vnext/artifacts/soak/*-soak-window/`
      (latest)
    - runs `tools/run-golden-eval.ps1` and captures the result
    - invokes `PromotionCriteriaSnapshot` via a small `dotnet run`
      wrapper or the soak-driver CLI
    - writes `decision.json` + `snapshot.json` + `run-summary.md`
  - exit code 1 if decision != `enable_autonomous_beta`.
- Settings UI:
  - In `ToolPopupWindow`'s Settings -> Autonomous beta panel:
    - Show the latest PromotionCriteriaSnapshot table (one row per
      criterion with pass/fail + value).
    - Show a "Try the autonomous beta" entry, default-disabled.
    - The entry is enabled IFF the latest snapshot returned
      `enable_autonomous_beta` AND `runtime_autonomous_beta_enabled`
      is currently false.
    - Clicking opens a confirmation dialog that:
      - explains exactly what flipping the setting means (proposal-only
        loop, daily cap, KillSwitch unaffected)
      - requires the user to type or click an explicit confirm action
      - on confirm: writes a `runtime_autonomous_beta_user_consent` row
        with status `Completed` and summary including the confirmation
        timestamp, THEN sets `runtime_autonomous_beta_enabled=true`.
    - The entry remains visible but disabled if the snapshot returned
      anything else; the panel explains why.
- Make sure the new packet kinds are covered by the PlainLanguageExplainer.

Files likely touched:

```text
vnext/src/Wevito.VNext.Core/AutonomousBetaDecisionService.cs   (consume snapshot)
vnext/src/Wevito.VNext.Core/PlainLanguageExplainer.cs
vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml(.cs)         (Settings panel)
vnext/content/tool_definitions.json                            (packet kinds)
```

New files:

```text
vnext/src/Wevito.VNext.Core/PromotionCriteriaSnapshot.cs
vnext/src/Wevito.VNext.Core/PromotionDecisionLabel.cs
vnext/tests/Wevito.VNext.Tests/PromotionCriteriaSnapshotTests.cs
tools/run-promotion-eval.ps1
docs/C_PHASE85C_PROMOTION_SNAPSHOT_AND_UI_ENTRY_2026-05-13.md
```

Tests (use synthetic ledger fixtures; never the real ledger):

- Snapshot returns `keep_supervised_preview` when ledger window < 7d.
- Snapshot returns `pause_for_reliability_work` on any safety-class
  failure (parametric over: hosted AI > 0, policy violations > 0,
  mutation proof < 100%, focus steal > 0, kill switch observed).
- Snapshot returns `keep_supervised_preview` on any liveness-class
  failure (parametric over: uptime < 50%, citation coverage < 0.6,
  golden eval fail).
- Snapshot returns `enable_autonomous_beta` when all checks pass on a
  fully-populated fixture (7+ days of synthetic ledger rows that
  satisfy every criterion).
- Settings UI entry is disabled by default in the rendered state.
- The `runtime_autonomous_beta_user_consent` row is written ONLY when
  the user types the explicit confirm action, never on a stray click
  (test via the dispatcher's click-handler unit tests).

Validation commands:

```powershell
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "PromotionCriteria|AutonomousBeta|ToolPopup"
dotnet build .\vnext\Wevito.VNext.sln
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\run-promotion-eval.ps1 -ArtifactRoot .\vnext\artifacts\promotion\ -EmitDecisionOnly
```

Artifacts:

```text
vnext/artifacts/promotion/<ts>-promotion/snapshot.json
vnext/artifacts/promotion/<ts>-promotion/decision.json
vnext/artifacts/promotion/<ts>-promotion/run-summary.md
docs/C_PHASE85C_PROMOTION_SNAPSHOT_AND_UI_ENTRY_2026-05-13.md
```

Stop gates:

- Stop if the snapshot's safety-class failure path returns anything
  other than `pause_for_reliability_work`.
- Stop if the Settings UI entry can be enabled by anything other than
  a snapshot returning `enable_autonomous_beta`.
- Stop if the confirmation dialog can be bypassed.
- Stop if `runtime_autonomous_beta_enabled` can be flipped without the
  consent audit row.
- Stop if any default-off setting becomes default-on as a side effect.

Rollback: revert PR; the C-PHASE 78/83/84 surfaces remain functional
and the C-PHASE 85 blocker remains in place.

Commit / PR instructions:

- Branch: `claude-implementation/c-phase-85c-promotion-snapshot-and-ui`
- Commit messages reference `C-PHASE 85c`.
- PR title: `C-PHASE 85c: Promotion snapshot service and confirmation-gated UI entry`.
- Phase report: `docs/C_PHASE85C_PROMOTION_SNAPSHOT_AND_UI_ENTRY_2026-05-13.md`.

Auto-continue? **No.** This is the last code phase before the wall-clock
soak window.

### Wall-Clock Window — 7-Day Soak (User Runs This)

This is not a Codex phase. After C-PHASE 85a, 85b, and 85c land:

```text
the user runs (once, then daily as needed)
|
+-- Wevito normally during their PC use
+-- powershell -File .\tools\run-soak-driver.ps1 -Days 7 -ArtifactRoot .\vnext\artifacts\soak\
+-- (optional, any time) powershell -File .\tools\check-evidence-readiness.ps1
+-- (optional, any time) powershell -File .\tools\run-self-improvement-report.ps1 -Window day -ArtifactRoot .\vnext\artifacts\pet-tasks
|
exit criteria for the window
|
+-- 7 calendar days elapsed since the soak driver started
+-- at least one runtime_session_heartbeat with uptime_hours>=4 per
|   active day
+-- 0 focus_steal_snapshot rows with focus_steal=true
+-- 0 budget_meter_snapshot rows with budget_exceeded=true
+-- 0 hosted-AI rows (LocalOnly is in effect)
+-- 0 UnifiedPolicyService Blocked rows that were not user-initiated
+-- at least 1 self_improvement_report row per active day
+-- the latest golden eval is PASS
+-- no kill_switch row indicating an active kill switch that persisted
|   for > 1 hour
|
if any criterion fails
|
`-- fix the underlying cause; restart the soak (the soak driver does
    NOT auto-reset; the user explicitly starts a fresh window when
    they are confident the cause is fixed)
```

Codex does not implement anything during this window. The plan returns
to Codex once the user has finished the 7-day window successfully.

### C-PHASE 85d — Promotion PR (with real evidence)

Goal: open the actual promotion PR. This is the original C-PHASE 85,
retried, this time with honest evidence.

Scope (Codex implements after evidence is collected):

- Run `tools/run-promotion-eval.ps1 -ArtifactRoot .\vnext\artifacts\promotion\`
  against the latest 7-day soak window.
- Confirm the decision packet says `enable_autonomous_beta`.
- Snapshot the relevant evidence into the PR:
  - `vnext/artifacts/promotion/<ts>-promotion/snapshot.json`
  - `vnext/artifacts/promotion/<ts>-promotion/decision.json`
  - `vnext/artifacts/promotion/<ts>-promotion/run-summary.md`
  - `vnext/artifacts/soak/<ts>-soak-window/manifest.json`
  - The latest golden eval report
- Update `docs/C_PHASE85_AUTONOMOUS_OPERATIONS_PROMOTION_2026-05-13.md`
  with:
  - the decision packet contents
  - the criteria table
  - the date range of the window
  - the per-day flagged-row counts
  - the explicit confirmation that no default-off setting was flipped
    by this PR
- The PR DOES NOT change any default-off setting. The Settings UI entry
  shipped in C-PHASE 85c becomes interactive because the snapshot now
  returns `enable_autonomous_beta`; the user still has to click the
  confirmation dialog to flip the flag.

Files likely touched:

```text
docs/C_PHASE85_AUTONOMOUS_OPERATIONS_PROMOTION_2026-05-13.md   (replaces blocker doc as the live promotion record; keep blocker doc for history)
vnext/artifacts/promotion/<ts>-promotion/...                  (committed evidence snapshot)
```

No new files outside the artifact snapshot.

Tests: regression baseline only; this phase is evidence-bearing, not
code-bearing.

Validation:

```powershell
dotnet build .\vnext\Wevito.VNext.sln
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\run-promotion-eval.ps1 -ArtifactRoot .\vnext\artifacts\promotion\
```

Stop gates:

- Stop if the decision packet says anything other than
  `enable_autonomous_beta`.
- Stop if `runtime_autonomous_beta_enabled` is set to true by this PR.
- Stop if any default-off capability is enabled by this PR.
- Stop if the consent audit row is missing from the test sweep.

Rollback: revert PR; the Settings entry shipped in C-PHASE 85c remains
visible but disabled until the next successful snapshot.

Commit / PR instructions:

- Branch: `claude-implementation/c-phase-85d-autonomous-operations-promotion`
- Commit messages reference `C-PHASE 85d`.
- PR title: `C-PHASE 85d: Autonomous operations promotion (with real evidence)`.
- Phase report: update
  `docs/C_PHASE85_AUTONOMOUS_OPERATIONS_PROMOTION_2026-05-13.md`.

Auto-continue? **No.** Final user-decision gate.

## 5. Hard Invariants Locked Across All Phases

```text
+-- runtime_kill_switch=true halts every adapter, scheduler, loop,
|   tracker, snapshot service, soak driver, and promotion-eval script
+-- ModelProviderMode default Disabled; LocalOnly remains opt-in
+-- web_search_enabled=false default; OfflineWebSearchBackend default
+-- local_tool_exec_enabled=false default
+-- tuning_lora_enabled=false default
+-- runtime_autonomous_beta_enabled=false default until 85d ships AND
|   the user explicitly clicks the confirmation-gated entry
+-- AutonomousTaskScheduler never executes; only Drafts
+-- AutonomousOperationsLoop never mutates; only proposes
+-- GuardedMutationService never applies without an approved TaskCard
+-- UnifiedPolicyService denylist beats allowlist; traversal/symlink blocked
+-- AuditLedgerService is append-only (sqlite triggers); never UPDATE/DELETE
+-- every local model adapter must support deterministic fallback
+-- local model adapters never read hosted-provider API keys
+-- only loopback endpoints permitted for local runtime
+-- every evidence packet has did_use_network/hosted_ai/local_model/mutate set
+-- no helper / no adapter / no service may call a hosted AI in LocalOnly mode
+-- pets remain visually regular pet-sim characters; no task animation
+-- new packet kinds: every kind added by these phases MUST appear in
|   PlainLanguageExplainer.KnownPacketKinds with a plain-language sentence
+-- the soak driver and promotion-eval scripts NEVER alter settings
+-- the promotion PR (85d) NEVER flips runtime_autonomous_beta_enabled;
|   only the user can, via the confirmation-gated entry
`-- no Codex phase produces a row in the ledger labeled as
    "promotion criteria met" without the real evidence actually being
    present; PromotionCriteriaSnapshot refuses to compute on insufficient
    data
```

## 6. References

- `docs/CLAUDE_POST_CPHASE75_REVIEW_AND_ROADMAP_2026-05-13.md`
- `docs/CLAUDE_POST_CPHASE75_CODEX_PHASE_PROMPTS_2026-05-13.md`
- `docs/C_PHASE78_QUIET_HARDENING_AND_SOAK_VALIDATION_2026-05-13.md`
- `docs/C_PHASE82_GOLDEN_EVAL_AND_REGRESSION_GATE_2026-05-13.md`
- `docs/C_PHASE83_SELF_IMPROVEMENT_REPORT_LOOP_2026-05-13.md`
- `docs/C_PHASE84_GUARDED_CODE_MUTATION_PILOT_2026-05-13.md`
- `docs/C_PHASE85_BLOCKER_2026-05-13.md`
- `vnext/src/Wevito.VNext.Core/AuditLedgerService.cs`
- `vnext/src/Wevito.VNext.Core/AutonomousBetaDecisionService.cs`
- `vnext/src/Wevito.VNext.Core/AutonomousOperationsLoop.cs`
- `vnext/src/Wevito.VNext.Core/FocusStealCounter.cs`
- `vnext/src/Wevito.VNext.Core/RuntimeBudgetMeter.cs`
- `vnext/src/Wevito.VNext.Core/SoakRunnerService.cs`
- `vnext/src/Wevito.VNext.Core/WindowsForegroundFullscreenMonitor.cs`
- `vnext/src/Wevito.VNext.Core/WindowsPowerHandler.cs`
- `vnext/src/Wevito.VNext.Core/SelfImprovementReportService.cs`
- `vnext/src/Wevito.VNext.Core/PlainLanguageExplainer.cs`
- `vnext/src/Wevito.VNext.Shell/ShellCoordinator.cs`
- `tools/run-soak-validation.ps1`
- `tools/run-self-improvement-report.ps1`
- `tools/run-golden-eval.ps1`

## 7. Where to Start Right Now

Begin C-PHASE 85a using the copy-paste prompt at the end of
`docs/CLAUDE_POST_CPHASE85_BLOCKER_CODEX_PHASE_PROMPTS_2026-05-13.md`.
