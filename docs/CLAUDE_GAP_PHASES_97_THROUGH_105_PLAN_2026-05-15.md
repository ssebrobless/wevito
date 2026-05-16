# Wevito Gap-Phase Plan: C-PHASE 97 through 105

Date: 2026-05-15
Author: Claude (Opus 4.7), commissioned by `ssebrobles@gmail.com`
Audience: Codex at medium reasoning effort
Companion file: `docs/CLAUDE_GAP_PHASES_97_THROUGH_105_CODEX_PHASE_PROMPTS_2026-05-15.md` (to be authored)
Predecessor plans:
- `docs/CLAUDE_LOCAL_FIRST_AI_FULL_IMPLEMENTATION_PLAN_2026-05-12.md`
- `docs/CLAUDE_POST_CPHASE75_REVIEW_AND_ROADMAP_2026-05-13.md`
- `docs/CLAUDE_POST_CPHASE85_BLOCKER_PLAN_2026-05-13.md`

## 0. Purpose

These nine phases close the capability gaps identified during the May 2026 plan
review. Each gap blocks one of these three outcomes:

1. Wevito has something useful to do on day 1 even when scope, registry, and
   allowlist are empty (C3-with-empty-state, see workflow autonomy gate 13c).
2. Wevito can actually act as an image generator at the level the project
   owner asked for (IG5: aesthetic-constrained general generation across
   multiple palette grammars including a "simpler pixel art" fallback).
3. Wevito's autonomous-improvement loop is directional rather than random,
   and the composite fitness scoreboard (C-PHASE 93) receives the most
   important signal (user thumbs-up/down).

This plan is independent of, but composes with, the planned C-PHASE 86-96
roadmap. The full sequence is:

```text
C-PHASE 85d   Autonomous Operations Promotion (residual, blocked on user soak)
C-PHASE 86    Pre-approved Scope Service (H3)
C-PHASE 87    Sandboxed Experiment Runner + Kind Registry
C-PHASE 88    Experiment Kind: sprite-template-candidate-generation
C-PHASE 89    Experiment Kind: lora-hyperparameter-search
C-PHASE 90    Constitutional Decision Service
C-PHASE 91    Approved Research Connector Expansion (R3 allowlist)
C-PHASE 92    Self-Improvement Promotion Gate
C-PHASE 93    Composite Fitness Scoreboard
C-PHASE 94    Maturity Promotion Service
C-PHASE 95    Build Hot-Swap Wrapper
C-PHASE 96    Codex Loop Runner
C-PHASE 97    Image LoRA Training Pipeline                      <-- this doc
C-PHASE 98    Local Image Generation Runtime                    <-- this doc
C-PHASE 99    Multi-Domain Palette Grammar Registry             <-- this doc
C-PHASE 100   General "Simpler Pixel Art" Fallback Grammar      <-- this doc
C-PHASE 101   Always-On Household Maintenance                   <-- this doc
C-PHASE 102   User Feedback Ingestion                           <-- this doc
C-PHASE 103   Memory Consolidation                              <-- this doc
C-PHASE 104   Strategic Planner                                 <-- this doc
C-PHASE 105   Daily/Weekly User-Facing Digest UI                <-- this doc
```

Some phases here logically precede earlier-numbered phases (e.g., C-PHASE 101
should land alongside C-PHASE 87 because it gives the runner something to
schedule on day 1). The numbers preserve write-order, not execute-order. The
companion phase-prompts file will list the recommended execution order.

## 0.1 Hard Invariants (carried forward, never weakened)

```text
- Wevito remains its own local AI; no GPT/Claude/Codex/Gemini at runtime.
- No hidden web access, no hidden local file access, no hidden training.
- No hidden tool execution, no hidden code or asset mutation.
- Every risky capability stays default-off OR ships with empty initial state.
- Pets remain visually regular pet-sim characters.
- Always-on behavior must not interfere with the user's PC.
- KillSwitch, audit ledger, evidence packets, rollback, supervisor modes
  remain load-bearing and cannot be weakened by any phase below.
- Every new packet kind MUST be covered by PlainLanguageExplainer.KnownPacketKinds.
- Every new service that may emit ledger rows MUST consult KillSwitchService
  and MUST set did_use_network/hosted_ai/local_model/mutate flags honestly.
- Every new local-model adapter MUST safely degrade when the runtime is absent.
- Every new mutation pathway MUST go through GuardedMutationService.
```

## 0.2 Codex-Medium Phase Template

Each phase below uses this exact template so Codex medium reasoning can pick
up a phase and execute it without architectural improvisation:

```text
Goal             (1-2 sentences)
Scope            (bulleted, concrete; named services/methods)
Pattern          (existing service Codex must imitate)
Files likely    (exact paths to edit)
  touched
New files       (exact paths to create)
Tests           (named test methods + assertion shape)
Validation       (literal copy-pasteable PowerShell)
  commands
Artifacts        (exact paths produced)
Stop gates       (boolean conditions; Codex halts on any true)
Rollback         (single-action revert)
Commit/PR        (branch, commit-prefix, PR title)
Auto-continue?   (Yes/No with one-line reason)
```

---

## C-PHASE 101 — Always-On Household Maintenance

**Goal:** Give wevito a small, always-allowed set of self-maintenance tasks
that run on a schedule from day 1 regardless of user-declared scope,
experiment registry contents, or R3 allowlist contents. Without this, the
C3-with-empty-state default leaves wevito alive but idle. With this, wevito
is immediately useful on first boot.

**Scope:**

- Add `HouseholdMaintenanceService` in `Wevito.VNext.Core`. The service
  exposes a single public method `TryRunScheduledChores(HouseholdMaintenanceRequest request)`
  that returns `HouseholdMaintenanceResult`. The service:
  - reads the current UTC time from a `Func<DateTimeOffset>` clock dependency.
  - reads the last-run timestamp per chore kind from
    `%LOCALAPPDATA%/Wevito/audit/household-maintenance.json`.
  - iterates over the registered chore kinds and runs any chore whose
    `LastRunAtUtc + Cadence < now`.
  - respects `RuntimeSupervisorStatus` (skip when mode is not `Active`).
  - respects `RuntimeBudgetMeter.TryReserve` (skip the chore if reservation
    fails; do not consume the reservation for any chore that ends up skipped).
  - respects `KillSwitchService.IsActive()` (refuse to run any chore).
  - writes one `household_maintenance_run` evidence packet per chore run.
- Add an `IHouseholdChore` interface with one method
  `HouseholdChoreResult Run(HouseholdChoreContext context)`. Each chore is a
  small class implementing this interface.
- Register five day-1 chores:
  - `DailySelfImprovementReportChore` — calls `SelfImprovementReportService.Run`
    with a 24-hour window. Cadence: 24 hours.
  - `LedgerIntegrityCheckChore` — runs `PRAGMA integrity_check` against the
    audit ledger sqlite DB and writes a `ledger_integrity_check` packet.
    Cadence: 24 hours.
  - `EmbeddingRefreshChore` — re-embeds the most recent N=50 audit ledger
    summaries via `OnnxTextEmbeddingService.Embed` and writes them to
    `PetMemoryStore.AddExample` (only those not already present). Cadence:
    12 hours.
  - `GoldenEvalRefreshChore` — invokes `LearningEvalService.RunLatest`
    against the current promoted dataset version and records the result.
    Cadence: 24 hours.
  - `EvidenceSummaryChore` — generates a one-paragraph plain-language
    summary of the last 24h of audit ledger via `PlainLanguageExplainer`.
    Cadence: 24 hours.
- Wire the service into `ShellCoordinator.Tick` so it is invoked once per
  tick. Cadence checks ensure no chore actually runs more often than its
  declared cadence.
- Add settings keys (all default to enabled because C3-with-empty-state):
  - `household_maintenance_enabled` (default true)
  - `household_maintenance_max_chores_per_tick` (default 1; prevents
    burst-on-startup)

**Pattern:** Follow the shape of `DailyEvidenceSnapshotService` at
`vnext/src/Wevito.VNext.Core/DailyEvidenceSnapshotService.cs`. That service
already shows the per-UTC-day cadence pattern, the state-file persistence
shape, and the kill-switch consultation. Generalize it to N chores with
configurable cadences.

**Files likely touched:**

```text
vnext/src/Wevito.VNext.Shell/ShellCoordinator.cs
vnext/src/Wevito.VNext.Core/PlainLanguageExplainer.cs
vnext/content/tool_definitions.json
```

**New files:**

```text
vnext/src/Wevito.VNext.Core/HouseholdMaintenanceService.cs
vnext/src/Wevito.VNext.Core/IHouseholdChore.cs
vnext/src/Wevito.VNext.Core/Chores/DailySelfImprovementReportChore.cs
vnext/src/Wevito.VNext.Core/Chores/LedgerIntegrityCheckChore.cs
vnext/src/Wevito.VNext.Core/Chores/EmbeddingRefreshChore.cs
vnext/src/Wevito.VNext.Core/Chores/GoldenEvalRefreshChore.cs
vnext/src/Wevito.VNext.Core/Chores/EvidenceSummaryChore.cs
vnext/tests/Wevito.VNext.Tests/HouseholdMaintenanceServiceTests.cs
vnext/tests/Wevito.VNext.Tests/DailySelfImprovementReportChoreTests.cs
vnext/tests/Wevito.VNext.Tests/LedgerIntegrityCheckChoreTests.cs
vnext/tests/Wevito.VNext.Tests/EmbeddingRefreshChoreTests.cs
vnext/tests/Wevito.VNext.Tests/GoldenEvalRefreshChoreTests.cs
vnext/tests/Wevito.VNext.Tests/EvidenceSummaryChoreTests.cs
docs/C_PHASE101_HOUSEHOLD_MAINTENANCE_2026-05-15.md
```

**Tests:**

- `HouseholdMaintenanceServiceTests.RespectsKillSwitch` — assert no chores
  run when kill switch active; assert no ledger row written.
- `HouseholdMaintenanceServiceTests.RespectsQuietSupervisorMode` — assert
  no chores run when supervisor mode is `Quiet` or `PetOnly`.
- `HouseholdMaintenanceServiceTests.RespectsBudgetReservation` — assert
  chore is skipped when `RuntimeBudgetMeter.TryReserve` returns `Allowed=false`.
- `HouseholdMaintenanceServiceTests.HonorsCadenceWindow` — call twice within
  cadence window; second call must skip the chore.
- `HouseholdMaintenanceServiceTests.HonorsMaxChoresPerTick` — register 3
  chores all due; assert only `max_chores_per_tick` (default 1) run per tick.
- `HouseholdMaintenanceServiceTests.PersistsLastRunAcrossInstances` —
  instantiate service, run, dispose, instantiate fresh service, verify
  cadence respected based on persisted state.
- `HouseholdMaintenanceServiceTests.WritesEvidencePacketPerChoreRun` —
  assert exactly one `household_maintenance_run` packet per chore run, with
  `did_use_network=false`, `did_use_hosted_ai=false`, `did_mutate=false`.
- Per-chore tests: `<ChoreName>RunsSuccessfullyOnEmptyInput`,
  `<ChoreName>WritesExpectedPacketKind`, `<ChoreName>HonorsKillSwitch`.
- `PlainLanguageExplainerTests.CoversHouseholdMaintenanceRunKind` — assert
  the new packet kind has a plain-language sentence.

**Validation commands:**

```powershell
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "HouseholdMaintenance|Chore|PlainLanguage"
dotnet build .\vnext\Wevito.VNext.sln
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
```

**Artifacts:**

```text
%LOCALAPPDATA%/Wevito/audit/household-maintenance.json
vnext/artifacts/household-maintenance/<ts>-<chore-name>/run-summary.md
vnext/artifacts/household-maintenance/<ts>-<chore-name>/packet.json
docs/C_PHASE101_HOUSEHOLD_MAINTENANCE_2026-05-15.md
```

**Stop gates:**

- Stop if any chore runs when kill switch is active.
- Stop if any chore runs when supervisor mode is `Quiet` or `PetOnly`.
- Stop if any chore mutates user-facing assets (chores are read-and-write-own-data only).
- Stop if any chore opens a network connection (assert with fake HttpClient).
- Stop if cadence persistence is broken across instances.
- Stop if `max_chores_per_tick` cap is bypassable.
- Stop if any new packet kind is missing from `PlainLanguageExplainer.KnownPacketKinds`.

**Rollback:** revert PR; wevito returns to idle-on-empty-state.

**Commit/PR:**

- Branch: `claude-implementation/c-phase-101-household-maintenance`
- Commit messages reference `C-PHASE 101`.
- PR title: `C-PHASE 101: Always-on household maintenance`.
- Phase report: `docs/C_PHASE101_HOUSEHOLD_MAINTENANCE_2026-05-15.md`.

**Auto-continue?** **Yes.** No new capability, no mutation, no model surface;
just scheduling existing reads-and-writes-own-data. Safe to advance.

---

## C-PHASE 102 — User Feedback Ingestion

**Goal:** Add thumbs-up / thumbs-down feedback UI on every wevito proposal,
store the signal locally, and surface it as a component of the composite
fitness scoreboard (C-PHASE 93). Without this, the scoreboard lacks the
most important signal — whether the user actually likes what wevito is doing.

**Scope:**

- Add `UserFeedbackStore` in `Wevito.VNext.Core`. Backed by
  `%LOCALAPPDATA%/Wevito/audit/user-feedback.sqlite`. Schema:
  - `feedback_id INTEGER PRIMARY KEY`
  - `created_at_utc TEXT NOT NULL`
  - `subject_kind TEXT NOT NULL` (e.g., `task_card`, `proposal`,
    `template_candidate`, `experiment_result`)
  - `subject_id TEXT NOT NULL` (GUID or stable hash)
  - `signal TEXT NOT NULL` (`thumbs_up` or `thumbs_down`)
  - `note TEXT` (optional free-text from user)
  - `source_packet_id TEXT` (links back to the evidence packet that produced
    the subject)
- Append-only schema (mirror `AuditLedgerService` triggers blocking
  `UPDATE`/`DELETE`).
- Add `IUserFeedbackSurface` interface exposed to UI. One method:
  `void RecordFeedback(string subjectKind, string subjectId, UserFeedbackSignal signal, string note = "")`.
- Add `UserFeedbackEvidencePacket` recording: which subject, which signal,
  cross-link to source packet, no network/hosted/mutation. Written to audit
  ledger as `user_feedback` packet kind.
- UI surfaces (XAML edits):
  - `ToolPopupWindow.xaml`: every Task Card in the queue grows a small
    `[👍] [👎]` row when the card transitions to a terminal state
    (`Completed`, `Cancelled`, `Failed`).
  - `CreativeLearningLabWindow.xaml`: every review row keeps its
    accept/reject/etc. label vocabulary BUT also exposes a separate
    thumbs row — the label is the *gate signal*, the thumbs is the
    *preference signal*.
  - `HomePanelWindow.xaml`: when wevito proposes anything via the overlay
    (e.g., "I just finished a self-improvement report" toast), the toast
    grows a small thumbs row.
- Add `UserFeedbackSummaryReader` that computes per-window aggregates:
  - `count_thumbs_up_last_7d`
  - `count_thumbs_down_last_7d`
  - `ratio_up_to_total_last_7d`
  - `recent_thumbs_down_subjects_sample` (up to 10 most recent for context)
- C-PHASE 93's composite scoreboard (planned, not yet shipped) consumes
  this reader directly. This phase ships the reader; the scoreboard
  consumes it later.

**Pattern:** Follow `AuditLedgerService` at
`vnext/src/Wevito.VNext.Core/AuditLedgerService.cs` for the append-only
sqlite-with-triggers pattern. Follow `LearningLabLabelStore` at
`vnext/src/Wevito.VNext.Core/LearningLabLabelStore.cs` for the label-vs-signal
separation pattern.

**Files likely touched:**

```text
vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml
vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml.cs
vnext/src/Wevito.VNext.Shell/HomePanelWindow.xaml
vnext/src/Wevito.VNext.Shell/HomePanelWindow.xaml.cs
vnext/src/Wevito.VNext.Shell/CreativeLearningLabWindow.xaml
vnext/src/Wevito.VNext.Shell/CreativeLearningLabWindow.xaml.cs
vnext/src/Wevito.VNext.Core/PlainLanguageExplainer.cs
vnext/content/tool_definitions.json
```

**New files:**

```text
vnext/src/Wevito.VNext.Core/UserFeedbackStore.cs
vnext/src/Wevito.VNext.Core/UserFeedbackSignal.cs
vnext/src/Wevito.VNext.Core/IUserFeedbackSurface.cs
vnext/src/Wevito.VNext.Core/UserFeedbackEvidencePacket.cs
vnext/src/Wevito.VNext.Core/UserFeedbackSummaryReader.cs
vnext/tests/Wevito.VNext.Tests/UserFeedbackStoreTests.cs
vnext/tests/Wevito.VNext.Tests/UserFeedbackSummaryReaderTests.cs
vnext/tests/Wevito.VNext.Tests/ToolPopupWindowFeedbackUiTests.cs
docs/C_PHASE102_USER_FEEDBACK_INGESTION_2026-05-15.md
```

**Tests:**

- `UserFeedbackStoreTests.AppendsRowAndWritesPacket`
- `UserFeedbackStoreTests.AppendIsAppendOnly` — assert sqlite UPDATE/DELETE
  triggers block direct row modification.
- `UserFeedbackStoreTests.RespectsKillSwitch` — assert no row written when
  kill switch active.
- `UserFeedbackStoreTests.AcceptsBothSignalsForSameSubject` — user changes
  their mind; both rows are kept (the *latest* is what summary reader uses).
- `UserFeedbackSummaryReaderTests.CountsWithin7DayWindow`
- `UserFeedbackSummaryReaderTests.IgnoresFeedbackOutsideWindow`
- `UserFeedbackSummaryReaderTests.HandlesEmptyStore` — returns zeros, no exception.
- `UserFeedbackSummaryReaderTests.PrefersLatestSignalPerSubject`
- `ToolPopupWindowFeedbackUiTests.ThumbsButtonsAppearOnTerminalCards`
- `ToolPopupWindowFeedbackUiTests.ThumbsClickInvokesRecordFeedback`
- `PlainLanguageExplainerTests.CoversUserFeedbackKind`

**Validation commands:**

```powershell
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "UserFeedback|ToolPopupWindow|PlainLanguage"
dotnet build .\vnext\Wevito.VNext.sln
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
```

**Artifacts:**

```text
%LOCALAPPDATA%/Wevito/audit/user-feedback.sqlite
vnext/artifacts/user-feedback/<ts>-feedback/packet.json
docs/C_PHASE102_USER_FEEDBACK_INGESTION_2026-05-15.md
```

**Stop gates:**

- Stop if feedback rows can be UPDATEd or DELETEd via SQL injection or
  any API.
- Stop if a single click can write more than one row (debounce check).
- Stop if `RecordFeedback` writes a row when kill switch is active.
- Stop if summary reader leaks rows from outside the requested window.
- Stop if the new packet kind is missing from `PlainLanguageExplainer`.
- Stop if any UI surface fails to wire the buttons to `IUserFeedbackSurface`.

**Rollback:** revert PR; existing UI surfaces lose the thumbs row, no
data is lost (sqlite file remains but is unread).

**Commit/PR:**

- Branch: `claude-implementation/c-phase-102-user-feedback-ingestion`
- Commit messages reference `C-PHASE 102`.
- PR title: `C-PHASE 102: User feedback ingestion (thumbs UI + store)`.
- Phase report: `docs/C_PHASE102_USER_FEEDBACK_INGESTION_2026-05-15.md`.

**Auto-continue?** **Yes.** Read-write-own-data, no mutation of user-facing
assets, no model surface, no autonomy expansion.

---

## C-PHASE 98 — Local Image Generation Runtime

**Goal:** Add a localhost-only image-inference seam so wevito can call a
local image model (Stable Diffusion via Automatic1111 / ComfyUI / or ONNX
SD), with deterministic safe-degrade when no runtime is available. This
phase ships the *seam*, not the model — same shape as C-PHASE 72 (Ollama
text) and C-PHASE 76 (ONNX text embedder).

**Scope:**

- Add `LocalImageRuntimeProbeService` mirroring `LocalRuntimeProbeService`
  at `vnext/src/Wevito.VNext.Core/LocalRuntimeProbeService.cs`. Probes:
  - Automatic1111: `GET http://127.0.0.1:7860/sdapi/v1/sd-models`
  - ComfyUI: `GET http://127.0.0.1:8188/system_stats`
  - Returns `LocalImageRuntimeProbeResult { IsAvailable, Provider, Reason }`.
  - Loopback-only enforcement (mirror `LocalRuntimeProbeService.IsLocalhostEndpoint`).
  - Dormant when `Quiet`/`PetOnly` supervisor modes.
  - KillSwitch consultation.
- Add `IImageModelAdapter` interface in `Wevito.VNext.Core`. Two methods:
  - `Task<ImageModelResponse> GenerateAsync(ImageModelRequest request, CancellationToken ct)`
  - `Task<bool> SupportsPaletteConstraintAsync(string paletteGrammarId, CancellationToken ct)`
- Add `ImageModelRequest` record:
  - `prompt` (string), `negative_prompt` (string),
  - `palette_grammar_id` (nullable — links to C-PHASE 99 grammar),
  - `width`, `height` (defaults 64x64 for sprite, 128x128 for portrait),
  - `seed` (nullable), `steps` (default 20), `cfg_scale` (default 7.5),
  - `requested_at_utc` (DateTimeOffset).
- Add `ImageModelResponse` record:
  - `provider`, `model`, `image_bytes` (byte[] PNG),
  - `did_call_provider` (bool), `block_reason` (string),
  - `audit_log_path` (string), `latency` (TimeSpan),
  - `palette_conforms` (bool — set only if a grammar was provided).
- Add `Automatic1111ImageAdapter : IImageModelAdapter`. Mirror
  `OllamaLocalModelAdapter`:
  - POST `http://127.0.0.1:7860/sdapi/v1/txt2img`
  - JSON payload: `prompt`, `negative_prompt`, `width`, `height`, `steps`,
    `cfg_scale`, `seed`.
  - Returns base64 PNG in response, decoded to bytes.
  - Settings keys: `local_image_runtime_a1111_endpoint`,
    `local_image_runtime_a1111_model`.
- Add `ComfyUIImageAdapter : IImageModelAdapter`. Mirror
  `Automatic1111ImageAdapter` but uses ComfyUI's queue API
  (`POST /prompt` with a graph JSON).
- Add `DeterministicImageFallbackAdapter : IImageModelAdapter`. Always
  available. Returns a deterministic checkerboard-pattern PNG sized to the
  request, with a header pixel encoding the request hash so consumers can
  detect the fallback. This is the safe-degrade target.
- Add `ImageInferenceEvidencePacket`:
  - prompt hash, response sha256, runtime id (`a1111` / `comfy` / `fallback`),
  - model id, latency_ms, fallback status, palette_conforms,
  - did_use_local_model, did_use_network=false, did_use_hosted_ai=false,
  - did_mutate=false.
- Settings UI changes in `ToolPopupWindow`:
  - "Local AI settings" panel grows new rows for image-runtime endpoint
    and model, with status (probe result) inline.
  - Default endpoint is empty; user pastes their own.
- `tool_definitions.json` metadata for the new local image provider.
- New packet kind: `image_inference` (added to
  `PlainLanguageExplainer.KnownPacketKinds`).

**Pattern:** Mirror `OllamaLocalModelAdapter` at
`vnext/src/Wevito.VNext.Core/OllamaLocalModelAdapter.cs` *closely*. Same
provider/fallback split, same evidence packet shape, same probe-first
loopback discipline, same kill-switch consultation. The only structural
difference is that `IImageModelAdapter` returns bytes rather than text.

**Files likely touched:**

```text
vnext/src/Wevito.VNext.Core/PlainLanguageExplainer.cs
vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml
vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml.cs
vnext/content/tool_definitions.json
```

**New files:**

```text
vnext/src/Wevito.VNext.Core/IImageModelAdapter.cs
vnext/src/Wevito.VNext.Core/ImageModelRequest.cs
vnext/src/Wevito.VNext.Core/ImageModelResponse.cs
vnext/src/Wevito.VNext.Core/LocalImageRuntimeProbeService.cs
vnext/src/Wevito.VNext.Core/Automatic1111ImageAdapter.cs
vnext/src/Wevito.VNext.Core/ComfyUIImageAdapter.cs
vnext/src/Wevito.VNext.Core/DeterministicImageFallbackAdapter.cs
vnext/src/Wevito.VNext.Core/ImageInferenceEvidencePacket.cs
vnext/tests/Wevito.VNext.Tests/LocalImageRuntimeProbeServiceTests.cs
vnext/tests/Wevito.VNext.Tests/Automatic1111ImageAdapterTests.cs
vnext/tests/Wevito.VNext.Tests/ComfyUIImageAdapterTests.cs
vnext/tests/Wevito.VNext.Tests/DeterministicImageFallbackAdapterTests.cs
docs/C_PHASE98_LOCAL_IMAGE_GENERATION_RUNTIME_2026-05-15.md
```

**Tests:**

- `LocalImageRuntimeProbeServiceTests.RejectsNonLoopbackEndpoint` — pass
  `http://192.168.1.1:7860` → `IsAvailable=false`, `Reason` contains
  "loopback".
- `LocalImageRuntimeProbeServiceTests.HonorsKillSwitch`
- `LocalImageRuntimeProbeServiceTests.DormantInQuietMode`
- `Automatic1111ImageAdapterTests.FallsBackWhenProbeUnavailable` — fake
  HTTP returns 503 → adapter returns fallback, evidence packet shows
  `did_fallback=true`.
- `Automatic1111ImageAdapterTests.SuccessPathRecordsLocalModelTrue` — fake
  HTTP returns valid base64 PNG → packet has `did_use_local_model=true`,
  `did_use_network=false` (loopback is not "network" in our schema —
  matches existing OllamaLocalModelAdapter behavior).
- `Automatic1111ImageAdapterTests.RefusesNonLoopbackEndpoint`
- `ComfyUIImageAdapterTests.<mirror of A1111 tests>`
- `DeterministicImageFallbackAdapterTests.ReturnsDeterministicBytesForSameRequest`
- `DeterministicImageFallbackAdapterTests.EncodesRequestHashInHeaderPixel`
- `PlainLanguageExplainerTests.CoversImageInferenceKind`
- `ToolPopupWindowImageRuntimeTests.SettingsRowsRenderProbeStatus`

**Validation commands:**

```powershell
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "Image|LocalImageRuntime|PlainLanguage"
dotnet build .\vnext\Wevito.VNext.sln
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
```

**Artifacts:**

```text
vnext/artifacts/image-inference/<ts>-image/packet.json
vnext/artifacts/image-inference/<ts>-image/response.png
docs/C_PHASE98_LOCAL_IMAGE_GENERATION_RUNTIME_2026-05-15.md
```

**Stop gates:**

- Stop if any adapter accepts a non-loopback endpoint.
- Stop if the fallback adapter is non-deterministic for identical inputs.
- Stop if the success path sets `did_use_network=true` for loopback calls.
- Stop if the kill switch is bypassable.
- Stop if any adapter writes to disk *outside* `vnext/artifacts/image-inference/`.
- Stop if `tool_definitions.json` exposes a non-loopback default endpoint.

**Rollback:** revert PR; image generation seam is gone; sprite work
continues using existing rungs 1-3 (curation-only, no generation).

**Commit/PR:**

- Branch: `claude-implementation/c-phase-98-local-image-generation-runtime`
- Commit messages reference `C-PHASE 98`.
- PR title: `C-PHASE 98: Local image generation runtime seam (A1111 + ComfyUI + fallback)`.
- Phase report: `docs/C_PHASE98_LOCAL_IMAGE_GENERATION_RUNTIME_2026-05-15.md`.

**Auto-continue?** **No.** First image-side capability lands; stop for user
review before C-PHASE 99 (palette grammar registry) builds on top of it.

---

## C-PHASE 99 — Multi-Domain Palette Grammar Registry

**Goal:** Define the data structure and storage for IG5's palette grammars
(sprite, portrait, environment, item, icon). Each grammar declares slot
semantics, canonical natural palette, 6 variant palettes, and conformance
rules. This is the foundation for everything image-generation-related.

**Scope:**

- Add `PaletteGrammar` record with fields:
  - `GrammarId` (string, kebab-case, e.g., `pet-sprite-rat-adult`)
  - `DomainKind` (enum: `Sprite`, `Portrait`, `Environment`, `Item`, `Icon`,
    `GeneralFallback`)
  - `SlotMap` (Dictionary<string, PaletteSlotSemantic>)
    - `outline`, `body_shadow`, `body_mid`, `body_highlight`, `eye`,
      `accent` for sprite domain (P5 universal)
    - Per-domain slot maps for other domains (portrait may add `bg`, item
      may add `metal`, etc.)
  - `CanonicalPalette` (Dictionary<string, string> slot→hex)
  - `Variants` (Dictionary<string, PaletteVariant> e.g., `red`, `blue`)
  - `ConformanceRules` (PaletteConformanceRules: max unique colors,
    transparency threshold, anti-alias disallowed, etc.)
  - `CreatedAtUtc`, `Version` (int)
- Add `PaletteSlotSemantic` enum:
  - `Outline`, `BodyShade`, `Eye`, `Accent`, `Background`, `Detail`,
    `Material`, `Edge`, `Highlight`, `Other`
- Add `PaletteGrammarRegistry` in `Wevito.VNext.Core`:
  - Stored as JSON under `vnext/content/palette-grammars/<grammar-id>.json`
  - `LoadAll()` reads all JSON files in that folder
  - `GetById(string grammarId)` returns nullable grammar
  - `Register(PaletteGrammar grammar)` is a guarded write (mutation
    pathway via `GuardedMutationService` — not free-form mutate)
  - `ValidateConformance(byte[] pngBytes, string grammarId)` returns
    `PaletteConformanceResult { Conforms, Reason, OffPaletteColors }`
- Initial seed of grammars:
  - 30 sprite grammars: 10 species × 3 ages, gender baked into accent
    slot (rat is `pet-sprite-rat-adult`, fox is `pet-sprite-fox-baby`, etc.)
  - 1 portrait grammar: `pet-portrait-generic` (shared across species,
    species identity is the *content*, not the palette)
  - 4 environment grammars: `env-grass`, `env-stone`, `env-water`, `env-sky`
  - 1 item grammar: `item-pickup-generic`
  - 1 icon grammar: `icon-ui-generic`
- Canonical palettes for sprite grammars are *natural* per 11b:
  - rat: warm grey-brown (`#3a2a1f`, `#5a4438`, `#8a6b56`, `#b89070`,
    `#1f1810`, `#c8a17a`) for outline/shadow/mid/highlight/eye/accent
  - fox: orange-brown analogous
  - snake: green-brown analogous
  - etc.
- Variant palettes per existing rainbow:
  - 6 variants per grammar (red, orange, yellow, blue, indigo, violet)
  - Each variant declares only the *body* slots (shadow/mid/highlight)
    and optionally accent; outline and eye remain canonical per 11b
    sub-decision.
- Add `PaletteConformer` service:
  - `byte[] Conform(byte[] inputPng, string grammarId)` — nearest-neighbor
    remap to declared palette, alpha threshold for transparency
  - Returns conformed PNG bytes; writes a conformance report alongside
- New packet kinds: `palette_grammar_loaded`, `palette_grammar_registered`,
  `palette_conformance_result`.

**Pattern:** Follow `LearningLabBundleService` at
`vnext/src/Wevito.VNext.Core/LearningLabBundleService.cs` for the
manifest-on-disk + load-all pattern. Follow `LocalDocumentIngestService` at
`vnext/src/Wevito.VNext.Core/LocalDocumentIngestService.cs` for the
file-globbing read pattern.

**Files likely touched:**

```text
vnext/src/Wevito.VNext.Core/PlainLanguageExplainer.cs
vnext/content/tool_definitions.json
```

**New files:**

```text
vnext/src/Wevito.VNext.Core/PaletteGrammar.cs
vnext/src/Wevito.VNext.Core/PaletteSlotSemantic.cs
vnext/src/Wevito.VNext.Core/PaletteVariant.cs
vnext/src/Wevito.VNext.Core/PaletteConformanceRules.cs
vnext/src/Wevito.VNext.Core/PaletteConformanceResult.cs
vnext/src/Wevito.VNext.Core/PaletteGrammarRegistry.cs
vnext/src/Wevito.VNext.Core/PaletteConformer.cs
vnext/content/palette-grammars/pet-sprite-rat-adult.json
vnext/content/palette-grammars/pet-sprite-rat-baby.json
vnext/content/palette-grammars/pet-sprite-rat-teen.json
vnext/content/palette-grammars/pet-sprite-fox-adult.json
... (28 more sprite grammars)
vnext/content/palette-grammars/pet-portrait-generic.json
vnext/content/palette-grammars/env-grass.json
vnext/content/palette-grammars/env-stone.json
vnext/content/palette-grammars/env-water.json
vnext/content/palette-grammars/env-sky.json
vnext/content/palette-grammars/item-pickup-generic.json
vnext/content/palette-grammars/icon-ui-generic.json
vnext/tests/Wevito.VNext.Tests/PaletteGrammarRegistryTests.cs
vnext/tests/Wevito.VNext.Tests/PaletteConformerTests.cs
vnext/tests/Wevito.VNext.Tests/PaletteGrammarSeedDataTests.cs
docs/C_PHASE99_PALETTE_GRAMMAR_REGISTRY_2026-05-15.md
```

**Tests:**

- `PaletteGrammarRegistryTests.LoadAllParsesAllJsonInFolder`
- `PaletteGrammarRegistryTests.GetByIdReturnsNullForUnknown`
- `PaletteGrammarRegistryTests.RegisterRequiresGuardedMutationPath`
- `PaletteConformerTests.ConformsInPaletteImageIdempotent` — image already
  conforms, output bytes equal input bytes.
- `PaletteConformerTests.RemapsOffPaletteToNearest` — feed an image with
  one off-palette pixel, assert that pixel is remapped to the nearest
  in-palette color.
- `PaletteConformerTests.RespectsAlphaThreshold` — semitransparent pixels
  below threshold become fully transparent; above threshold become fully
  opaque.
- `PaletteConformerTests.RejectsUnknownGrammarId`
- `PaletteGrammarSeedDataTests.AllSeedGrammarsParse` — every JSON in
  `vnext/content/palette-grammars/` parses without error.
- `PaletteGrammarSeedDataTests.AllSpriteGrammarsHaveSixVariants`
- `PaletteGrammarSeedDataTests.AllSpriteGrammarsShareUniversalSlotMap`
- `PaletteGrammarSeedDataTests.AllSpriteCanonicalPalettesUseDistinctColors`
- `PlainLanguageExplainerTests.CoversPaletteGrammarLoadedKind`
- `PlainLanguageExplainerTests.CoversPaletteConformanceResultKind`

**Validation commands:**

```powershell
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "PaletteGrammar|PaletteConformer|PlainLanguage"
dotnet build .\vnext\Wevito.VNext.sln
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
```

**Artifacts:**

```text
vnext/content/palette-grammars/*.json                    (seed data, committed)
vnext/artifacts/palette-conformance/<ts>-<grammar-id>/conformance.json
docs/C_PHASE99_PALETTE_GRAMMAR_REGISTRY_2026-05-15.md
```

**Stop gates:**

- Stop if any seed grammar JSON fails to parse.
- Stop if `Register` is callable without a guarded mutation plan.
- Stop if the conformer can produce more than the declared max unique
  colors for any grammar.
- Stop if the conformer mutates palettes in memory (must be pure-function).
- Stop if any new packet kind is missing from `PlainLanguageExplainer`.

**Rollback:** revert PR; existing sprite work continues with implicit
palette assumptions in `propagate_authored_colors.py`.

**Commit/PR:**

- Branch: `claude-implementation/c-phase-99-palette-grammar-registry`
- Commit messages reference `C-PHASE 99`.
- PR title: `C-PHASE 99: Multi-domain palette grammar registry`.
- Phase report: `docs/C_PHASE99_PALETTE_GRAMMAR_REGISTRY_2026-05-15.md`.

**Auto-continue?** **No.** Defines a new content schema that downstream
phases depend on; stop for user review of the schema before proceeding.

---

## C-PHASE 100 — General "Simpler Pixel Art" Fallback Grammar

**Goal:** Add a single `general-pixel-art` palette grammar that handles any
"draw whatever" request when no specific domain grammar matches. Aesthetic
constraint (≤ 8 unique colors, crisp pixel boundaries, alpha discipline,
no anti-alias) without subject constraint. This is the IG5 fallback that
keeps "draw a sword" or "draw a planet" requests in theme without forcing
them into a domain grammar.

**Scope:**

- Add a single new grammar file `general-pixel-art.json` to
  `vnext/content/palette-grammars/`:
  - `GrammarId = "general-pixel-art"`
  - `DomainKind = GeneralFallback`
  - `SlotMap`: 8 generic slots (`color_1` through `color_8`), each tagged
    `PaletteSlotSemantic.Other`.
  - `CanonicalPalette`: a balanced 8-color palette suitable for varied
    subjects (DB16 or PICO-8 style; suggest the Endesga-16 8-subset:
    `#1b1b1b`, `#6d6d6d`, `#b3b3b3`, `#e9e9e9`, `#5a3921`, `#a06035`,
    `#3d5a4c`, `#7ab69c`).
  - `Variants`: this grammar has no rainbow variants. The canonical
    palette IS the variant. Color-variant generation isn't meaningful
    for "draw whatever."
  - `ConformanceRules`:
    - `MaxUniqueColors = 8`
    - `MaxImageWidth = 256` (no painterly resolutions)
    - `MaxImageHeight = 256`
    - `AntiAliasDisallowed = true`
    - `AlphaThreshold = 0.5` (binary: fully transparent or fully opaque)
    - `RequiresOddPixelGridSnapping = true` (every pixel snaps to grid;
      no fractional positions)
- Add `GrammarRouterService` in `Wevito.VNext.Core`:
  - `string RouteRequest(string subject, string? hintGrammarId)` — returns
    the grammar ID to use.
  - Hint takes precedence if provided and valid.
  - Otherwise simple keyword routing (`subject contains "pet" → pet-sprite-*`,
    `subject contains "ground" → env-*`, etc.).
  - Falls back to `general-pixel-art` if no match.
  - Routing decision is recorded in a `grammar_routing_decision` packet.
- Extend `PaletteConformer.Conform` to support the `general-pixel-art`
  grammar specifically:
  - When the grammar's max unique colors is parametric, use it.
  - When `RequiresOddPixelGridSnapping=true`, run an additional check
    that no pixel has a fractional coordinate (in PNG terms: image
    dimensions are integers, no metadata claims sub-pixel resolution).
- Add `GeneralPixelArtPolicyService`:
  - One method `bool IsRequestInScope(string subject)` returns whether
    a "draw whatever" request is allowed.
  - Hard-coded denylist of prohibited subject keywords (violence, NSFW,
    real persons by name, etc.). Default-deny on ambiguity.
- New packet kind: `grammar_routing_decision`.

**Pattern:** Follow `WebQueryPrivacyFilter` at
`vnext/src/Wevito.VNext.Core/WebQueryPrivacyFilter.cs` for the denylist-based
filter pattern.

**Files likely touched:**

```text
vnext/src/Wevito.VNext.Core/PaletteConformer.cs
vnext/src/Wevito.VNext.Core/PlainLanguageExplainer.cs
vnext/content/tool_definitions.json
```

**New files:**

```text
vnext/content/palette-grammars/general-pixel-art.json
vnext/src/Wevito.VNext.Core/GrammarRouterService.cs
vnext/src/Wevito.VNext.Core/GeneralPixelArtPolicyService.cs
vnext/tests/Wevito.VNext.Tests/GrammarRouterServiceTests.cs
vnext/tests/Wevito.VNext.Tests/GeneralPixelArtPolicyServiceTests.cs
vnext/tests/Wevito.VNext.Tests/GeneralPixelArtConformerTests.cs
docs/C_PHASE100_GENERAL_FALLBACK_GRAMMAR_2026-05-15.md
```

**Tests:**

- `GrammarRouterServiceTests.HintGrammarTakesPrecedence`
- `GrammarRouterServiceTests.KeywordRoutesToSpriteForPetSubject`
- `GrammarRouterServiceTests.FallsBackToGeneralWhenNoMatch`
- `GrammarRouterServiceTests.RoutingDecisionWritesPacket`
- `GeneralPixelArtPolicyServiceTests.DefaultDeniesAmbiguousSubject`
- `GeneralPixelArtPolicyServiceTests.AllowsNeutralSubject`
- `GeneralPixelArtPolicyServiceTests.BlocksDenylistKeywords`
- `GeneralPixelArtConformerTests.RejectsImagesExceeding8Colors`
- `GeneralPixelArtConformerTests.RejectsImagesLargerThan256x256`
- `GeneralPixelArtConformerTests.RejectsImagesWithAntiAlias` — feed a
  PNG with smooth gradients, assert rejection with reason.
- `PaletteGrammarSeedDataTests.GeneralPixelArtGrammarLoads`

**Validation commands:**

```powershell
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "GrammarRouter|GeneralPixelArt|PaletteGrammar"
dotnet build .\vnext\Wevito.VNext.sln
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
```

**Artifacts:**

```text
vnext/content/palette-grammars/general-pixel-art.json
vnext/artifacts/grammar-routing/<ts>-decision/packet.json
docs/C_PHASE100_GENERAL_FALLBACK_GRAMMAR_2026-05-15.md
```

**Stop gates:**

- Stop if the conformer accepts >8 colors for the general grammar.
- Stop if the policy service defaults to allow on ambiguous subject.
- Stop if routing decisions don't write evidence packets.
- Stop if the denylist is empty (must contain at least the basic
  prohibited keywords).
- Stop if `PlainLanguageExplainer` is missing the new packet kind.

**Rollback:** revert PR; image generation defaults to per-domain grammars
only; "draw whatever" requests are unsupported until reintroduced.

**Commit/PR:**

- Branch: `claude-implementation/c-phase-100-general-fallback-grammar`
- Commit messages reference `C-PHASE 100`.
- PR title: `C-PHASE 100: General "simpler pixel art" fallback grammar + router`.
- Phase report: `docs/C_PHASE100_GENERAL_FALLBACK_GRAMMAR_2026-05-15.md`.

**Auto-continue?** **Yes.** Pure addition on top of C-PHASE 99's schema;
no new mutation surface; routing is read-only.

---

## C-PHASE 97 — Image LoRA Training Pipeline

**Goal:** Extend C-PHASE 73D's plan-only text-LoRA scaffold to a real
*image* LoRA training pipeline. Sandboxed, eval-gated, rollbackable.
Trains a small LoRA adapter on the approved sprite-template corpus so
wevito's image generation (C-PHASE 98) can produce in-style candidates.
Remains opt-in (`tuning_lora_enabled=false` and a new
`tuning_image_lora_enabled=false` default).

**Scope:**

- Add `ImageLoRATrainingPlanService` in `Wevito.VNext.Core`:
  - Reads the latest approved sprite-template corpus (templates with
    `accept` label + thumbs-up signal from C-PHASE 102).
  - Reads the target palette grammar (C-PHASE 99).
  - Writes an `image_train_plan` evidence packet with:
    - Selected base model (default `runwayml/stable-diffusion-v1-5` or
      another local SD checkpoint)
    - Hyperparameters (`learning_rate`, `lora_rank`, `lora_alpha`,
      `epochs`, `batch_size`, `image_resolution`)
    - Training data manifest (file paths + sha256)
    - Eval set manifest
    - Expected output path `vnext/content/local-ai/image-lora/<vNNNN-ts>/`
  - Does *not* execute training in this phase. Plan-only. Just like
    C-PHASE 73D's text equivalent.
- Add `ImageLoRATrainingRunner` in `Wevito.VNext.Core`:
  - Reads an approved `image_train_plan` (TaskCard `Status=Approved` and
    `ToolFamily=imageLoraTraining`).
  - Refuses to run if `tuning_image_lora_enabled=false` (the default).
  - Refuses to run if `RuntimeBudgetMeter.TryReserve` returns false.
  - Refuses to run if `KillSwitchService.IsActive()`.
  - Invokes a Python sidecar process via `ProcessCommandRunner`:
    - Script: `tools/image-lora-training/train.py` (delivered as part of
      this phase)
    - Args: passed via env vars (`WEVITO_TRAIN_PLAN_PATH`,
      `WEVITO_OUTPUT_PATH`)
    - No network access; the script uses only local checkpoint files.
  - Writes a `tuning_apply` evidence packet on success (mirror C-PHASE 73
    pattern).
  - On regression > 2% vs prior eval, auto-rollback the new LoRA folder
    (mirror existing `LearningEvalService.RunRegression` pattern).
- Add `tools/image-lora-training/`:
  - `train.py` — uses `diffusers.train_dreambooth_lora` or equivalent
    (pinned versions in `requirements.txt`)
  - `requirements.txt` — `diffusers`, `transformers`, `accelerate`, `peft`,
    `torch>=2.0` (CPU + CUDA variants documented separately)
  - `README.md` — user must `pip install -r requirements.txt` once; the
    runner refuses to launch if `pip list` doesn't show the required
    packages
  - `verify-deps.ps1` — wrapper that checks Python + pip deps + writes a
    `python_dep_check` packet
- Add `ImageLoRAEvalService`:
  - Reads the trained LoRA from disk
  - Generates N candidate images (N=20, deterministic seed) via
    `IImageModelAdapter`
  - Scores via `PaletteConformer.ValidateConformance` + a similarity
    metric (CLIP-image embedding cosine distance to golden set —
    implemented as a separate ONNX model load, mirror C-PHASE 76 pattern
    for ONNX session management)
  - Writes `image_lora_eval` packet with pass/fail decision
- Settings keys:
  - `tuning_image_lora_enabled` (default false)
  - `tuning_image_lora_python_path` (default `python`, user can override)
  - `tuning_image_lora_max_minutes` (default 30; runner kills after timeout)
- New packet kinds: `image_train_plan`, `image_lora_eval`,
  `python_dep_check`.

**Pattern:** Follow `LocalTrainingPlanService` at
`vnext/src/Wevito.VNext.Core/LocalTrainingPlanService.cs` and
`LocalTuningRunner` at `vnext/src/Wevito.VNext.Core/LocalTuningRunner.cs`.
This phase is the *image* equivalent. Plan service + tuning runner +
eval-gated rollback — same shape.

**Files likely touched:**

```text
vnext/src/Wevito.VNext.Core/PlainLanguageExplainer.cs
vnext/content/tool_definitions.json
```

**New files:**

```text
vnext/src/Wevito.VNext.Core/ImageLoRATrainingPlanService.cs
vnext/src/Wevito.VNext.Core/ImageLoRATrainingRunner.cs
vnext/src/Wevito.VNext.Core/ImageLoRAEvalService.cs
vnext/src/Wevito.VNext.Core/ImageEmbeddingBackend.cs
vnext/src/Wevito.VNext.Core/OnnxImageEmbeddingService.cs
tools/image-lora-training/train.py
tools/image-lora-training/requirements.txt
tools/image-lora-training/README.md
tools/image-lora-training/verify-deps.ps1
vnext/tests/Wevito.VNext.Tests/ImageLoRATrainingPlanServiceTests.cs
vnext/tests/Wevito.VNext.Tests/ImageLoRATrainingRunnerTests.cs
vnext/tests/Wevito.VNext.Tests/ImageLoRAEvalServiceTests.cs
vnext/tests/Wevito.VNext.Tests/OnnxImageEmbeddingServiceTests.cs
docs/C_PHASE97_IMAGE_LORA_TRAINING_PIPELINE_2026-05-15.md
```

**Tests:**

- `ImageLoRATrainingPlanServiceTests.PlanWritesEvidencePacket`
- `ImageLoRATrainingPlanServiceTests.PlanIncludesDatasetManifestHash`
- `ImageLoRATrainingPlanServiceTests.RefusesWithoutApprovedCorpus`
- `ImageLoRATrainingRunnerTests.RefusesWhenFlagDisabled` — default state.
- `ImageLoRATrainingRunnerTests.RefusesWhenKillSwitchActive`
- `ImageLoRATrainingRunnerTests.RefusesWhenBudgetExhausted`
- `ImageLoRATrainingRunnerTests.RequiresApprovedTaskCard`
- `ImageLoRATrainingRunnerTests.TimesOutAfterConfiguredMinutes` — use a
  fake `ICommandRunner` that sleeps; assert kill after timeout.
- `ImageLoRATrainingRunnerTests.RollsBackOnEvalRegression`
- `ImageLoRATrainingRunnerTests.PythonDepCheckMustPassBeforeLaunch`
- `ImageLoRAEvalServiceTests.ScoresAgainstGoldenSet`
- `OnnxImageEmbeddingServiceTests.FallsBackWhenModelMissing`
- `OnnxImageEmbeddingServiceTests.DeterministicForSameImage`
- `PlainLanguageExplainerTests.CoversImageTrainPlanKind`
- `PlainLanguageExplainerTests.CoversImageLoraEvalKind`
- `PlainLanguageExplainerTests.CoversPythonDepCheckKind`

**Validation commands:**

```powershell
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "ImageLoRA|OnnxImageEmbedding|PlainLanguage"
dotnet build .\vnext\Wevito.VNext.sln
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\image-lora-training\verify-deps.ps1
```

**Artifacts:**

```text
vnext/content/local-ai/image-lora/<vNNNN-ts>/lora.safetensors
vnext/content/local-ai/image-lora/<vNNNN-ts>/manifest.json
vnext/artifacts/image-train-plan/<ts>/train-plan.json
vnext/artifacts/image-lora-eval/<ts>/eval-report.json
docs/C_PHASE97_IMAGE_LORA_TRAINING_PIPELINE_2026-05-15.md
```

**Stop gates:**

- Stop if the runner can launch without the flag enabled.
- Stop if the runner can launch without an approved task card.
- Stop if any code path opens the network (asserted via fake HttpClient
  throws-on-Send).
- Stop if the regression gate is bypassable.
- Stop if the Python sidecar can be configured to call hosted AI (`huggingface_hub`
  must be patched to refuse remote download in our `requirements.txt` pin).
- Stop if any test downloads model weights.

**Rollback:** revert PR; LoRA training surface returns to plan-only text
(C-PHASE 73D state).

**Commit/PR:**

- Branch: `claude-implementation/c-phase-97-image-lora-training-pipeline`
- Commit messages reference `C-PHASE 97`.
- PR title: `C-PHASE 97: Image LoRA training pipeline (opt-in, eval-gated, rollbackable)`.
- Phase report: `docs/C_PHASE97_IMAGE_LORA_TRAINING_PIPELINE_2026-05-15.md`.

**Auto-continue?** **No.** First Python sidecar with real ML training;
stop for explicit user review of the sandboxing and dependency pinning.

---

## C-PHASE 103 — Memory Consolidation

**Goal:** Periodically summarize and archive old audit ledger rows so the
ledger stays useful at year-scale. Extract recurring patterns into a
"learned-pattern" memory the strategic planner (C-PHASE 104) consumes.
Extends `SelfImprovementReportService` rather than replacing it.

**Scope:**

- Add `MemoryConsolidationService` in `Wevito.VNext.Core`:
  - Runs on a 7-day cadence (registered as a household-maintenance chore
    in C-PHASE 101's registry).
  - Reads audit ledger rows older than `consolidation_age_days` (default
    30 days).
  - Groups rows by `packet_kind` + week.
  - For each group, emits a `consolidated_summary` packet containing:
    - Row count
    - Distribution of `Status` values
    - `did_use_network`, `did_use_hosted_ai`, `did_use_local_model`,
      `did_mutate` aggregate counts
    - Most-frequent `Summary` tokens (top 10)
    - Sample of 3 row IDs (oldest, median, newest)
  - After emitting summaries for a group, the original rows are *kept*
    but moved to a sidecar table `archived_ledger_rows` so the main
    table stays lean. Original rows remain queryable but are not
    counted in the active window for promotion criteria etc.
- Add `LearnedPatternStore`:
  - sqlite under `%LOCALAPPDATA%/Wevito/audit/learned-patterns.sqlite`
  - Schema:
    - `pattern_id INTEGER PRIMARY KEY`
    - `discovered_at_utc TEXT NOT NULL`
    - `pattern_kind TEXT NOT NULL` (`experiment_failure`,
      `user_preference`, `cadence`, `seasonal`)
    - `summary TEXT NOT NULL`
    - `confidence REAL NOT NULL` (0-1)
    - `source_evidence_packet_ids TEXT NOT NULL` (JSON list of GUIDs)
    - `last_observed_at_utc TEXT NOT NULL`
  - Append-only (UPDATE/DELETE blocked by triggers).
- Add `PatternDiscoveryService`:
  - Reads consolidated summaries + recent ledger rows + user-feedback
    summary.
  - Applies heuristic pattern detectors:
    - "X% of `tuning_apply` rows were rolled back within 1h" →
      `experiment_failure` pattern
    - "User thumbs-down rate on `<kind>` is 3x average" →
      `user_preference` pattern
    - "Activity spikes every Sunday at 14:00 UTC" → `cadence` pattern
  - Writes new rows to `LearnedPatternStore` only when confidence > 0.7.
  - Writes a `pattern_discovery_run` evidence packet per run.
- C-PHASE 104 (Strategic Planner) reads `LearnedPatternStore` to
  bias experiment selection.
- Settings keys:
  - `memory_consolidation_age_days` (default 30)
  - `memory_consolidation_cadence_days` (default 7)
  - `pattern_discovery_min_confidence` (default 0.7)
- New packet kinds: `consolidated_summary`, `pattern_discovery_run`,
  `archived_ledger_marker`.

**Pattern:** Follow `SelfImprovementReportService` at
`vnext/src/Wevito.VNext.Core/SelfImprovementReportService.cs` for the
window-aggregate-then-write pattern. Follow `AuditLedgerService` at
`vnext/src/Wevito.VNext.Core/AuditLedgerService.cs` for the append-only
sqlite triggers pattern.

**Files likely touched:**

```text
vnext/src/Wevito.VNext.Core/AuditLedgerService.cs
vnext/src/Wevito.VNext.Core/HouseholdMaintenanceService.cs  (register new chore)
vnext/src/Wevito.VNext.Core/PlainLanguageExplainer.cs
vnext/content/tool_definitions.json
```

**New files:**

```text
vnext/src/Wevito.VNext.Core/MemoryConsolidationService.cs
vnext/src/Wevito.VNext.Core/LearnedPatternStore.cs
vnext/src/Wevito.VNext.Core/LearnedPattern.cs
vnext/src/Wevito.VNext.Core/PatternDiscoveryService.cs
vnext/src/Wevito.VNext.Core/Chores/MemoryConsolidationChore.cs
vnext/tests/Wevito.VNext.Tests/MemoryConsolidationServiceTests.cs
vnext/tests/Wevito.VNext.Tests/LearnedPatternStoreTests.cs
vnext/tests/Wevito.VNext.Tests/PatternDiscoveryServiceTests.cs
docs/C_PHASE103_MEMORY_CONSOLIDATION_2026-05-15.md
```

**Tests:**

- `MemoryConsolidationServiceTests.ConsolidatesRowsOlderThanThreshold`
- `MemoryConsolidationServiceTests.KeepsOriginalRowsInArchiveTable`
- `MemoryConsolidationServiceTests.EmitsOneSummaryPerKindPerWeek`
- `MemoryConsolidationServiceTests.RespectsKillSwitch`
- `LearnedPatternStoreTests.AppendIsAppendOnly`
- `LearnedPatternStoreTests.QueryByKindReturnsLatest`
- `PatternDiscoveryServiceTests.DetectsExperimentFailurePatternAboveThreshold`
- `PatternDiscoveryServiceTests.DetectsUserPreferencePattern`
- `PatternDiscoveryServiceTests.IgnoresLowConfidenceMatches`
- `PatternDiscoveryServiceTests.WritesEvidencePacketEachRun`
- `PlainLanguageExplainerTests.CoversConsolidatedSummaryKind`
- `PlainLanguageExplainerTests.CoversPatternDiscoveryRunKind`

**Validation commands:**

```powershell
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "MemoryConsolidation|LearnedPattern|PatternDiscovery|PlainLanguage"
dotnet build .\vnext\Wevito.VNext.sln
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
```

**Artifacts:**

```text
%LOCALAPPDATA%/Wevito/audit/learned-patterns.sqlite
vnext/artifacts/memory-consolidation/<ts>/summary.json
vnext/artifacts/pattern-discovery/<ts>/patterns.json
docs/C_PHASE103_MEMORY_CONSOLIDATION_2026-05-15.md
```

**Stop gates:**

- Stop if original ledger rows are deleted (must be archived, not deleted).
- Stop if `LearnedPatternStore` can be UPDATEd or DELETEd.
- Stop if low-confidence patterns are written.
- Stop if consolidation runs more often than its declared cadence.
- Stop if any new packet kind is missing from `PlainLanguageExplainer`.

**Rollback:** revert PR; ledger rows continue to accumulate without
archive table; pattern discovery is gone.

**Commit/PR:**

- Branch: `claude-implementation/c-phase-103-memory-consolidation`
- Commit messages reference `C-PHASE 103`.
- PR title: `C-PHASE 103: Memory consolidation + learned-pattern store`.
- Phase report: `docs/C_PHASE103_MEMORY_CONSOLIDATION_2026-05-15.md`.

**Auto-continue?** **Yes.** Read-summarize-archive pattern; no mutation of
user-facing assets; no model surface; archive table preserves all data.

---

## C-PHASE 104 — Strategic Planner

**Goal:** Replace the experiment runner's default weighted-random kind
selection with a directional planner that reads the composite fitness
scoreboard (C-PHASE 93), identifies the weakest axis, and prefers
experiments that historically improved that axis. Combined with the
learned-pattern store (C-PHASE 103), this makes wevito's improvement
loop *directional* rather than random.

**Scope:**

- Add `StrategicPlannerService` in `Wevito.VNext.Core`:
  - One public method:
    `ExperimentKindSelection Plan(ExperimentPlannerContext context)`
  - Inputs (via context):
    - Current `CompositeFitnessScoreboard` snapshot
    - List of registered experiment kinds + their per-axis historical
      improvement record
    - Learned patterns relevant to "what's likely to improve <axis>"
    - User-feedback summary (latest 7d)
  - Algorithm:
    1. Identify the bottom-2 axes on the scoreboard.
    2. For each registered kind, compute an "expected value" score:
       expected gain on bottom axes × past success rate × user-feedback
       multiplier × inverse-cost (kinds with shorter runtime score
       higher when all else equal).
    3. Sort by expected value.
    4. Top kind is selected unless its expected value falls below the
       `min_expected_value_threshold`, in which case the planner
       returns `NoExperimentSelected` and the runner sleeps the cycle.
    5. Selection is recorded as a `strategic_plan_decision` packet
       including the scoreboard snapshot, the bottom axes, and the
       full ranked candidate list.
  - Respects KillSwitch and supervisor mode (returns
    `NoExperimentSelected` with reason).
- Add `ExperimentKindHistoricalRecord` store:
  - sqlite under
    `%LOCALAPPDATA%/Wevito/audit/experiment-kind-history.sqlite`
  - Schema:
    - `kind_id`, `axis_targeted`, `outcome` (`improved` / `regressed`
      / `neutral`), `magnitude` (delta), `recorded_at_utc`
  - Append-only.
  - Written to by the experiment runner (C-PHASE 87) when each
    experiment completes.
- C-PHASE 87's experiment runner is *modified* (this phase touches it)
  to consult `StrategicPlannerService.Plan` *first*; only fall back to
  weighted-random if the planner returns `NoExperimentSelected`.
- Settings keys:
  - `strategic_planning_enabled` (default true; under C3 capabilities
    default-on with empty-state — but registry is also empty so planner
    has nothing to plan with on day 1)
  - `min_expected_value_threshold` (default 0.05)
  - `strategic_planning_bottom_axes_count` (default 2)
- New packet kinds: `strategic_plan_decision`,
  `experiment_kind_history_recorded`.

**Pattern:** Follow `AutonomousBetaDecisionService` at
`vnext/src/Wevito.VNext.Core/AutonomousBetaDecisionService.cs` for the
decision-with-reason pattern. Follow `EvalRegressionGate` at
`vnext/src/Wevito.VNext.Core/EvalRegressionGate.cs` for the threshold-based
decision pattern.

**Files likely touched:**

```text
vnext/src/Wevito.VNext.Core/ExperimentRunnerService.cs  (shipped in C-PHASE 87)
vnext/src/Wevito.VNext.Core/PlainLanguageExplainer.cs
vnext/content/tool_definitions.json
```

**New files:**

```text
vnext/src/Wevito.VNext.Core/StrategicPlannerService.cs
vnext/src/Wevito.VNext.Core/ExperimentPlannerContext.cs
vnext/src/Wevito.VNext.Core/ExperimentKindSelection.cs
vnext/src/Wevito.VNext.Core/ExperimentKindHistoricalRecord.cs
vnext/src/Wevito.VNext.Core/ExperimentKindHistoryStore.cs
vnext/tests/Wevito.VNext.Tests/StrategicPlannerServiceTests.cs
vnext/tests/Wevito.VNext.Tests/ExperimentKindHistoryStoreTests.cs
docs/C_PHASE104_STRATEGIC_PLANNER_2026-05-15.md
```

**Tests:**

- `StrategicPlannerServiceTests.SelectsKindWithHighestExpectedValue`
- `StrategicPlannerServiceTests.IdentifiesBottomAxes`
- `StrategicPlannerServiceTests.ReturnsNoExperimentWhenBelowThreshold`
- `StrategicPlannerServiceTests.UsesUserFeedbackAsMultiplier`
- `StrategicPlannerServiceTests.RespectsKillSwitch`
- `StrategicPlannerServiceTests.PrefersShorterRunningKindsWhenTied`
- `StrategicPlannerServiceTests.WritesEvidencePacketWithFullRanking`
- `ExperimentKindHistoryStoreTests.AppendIsAppendOnly`
- `ExperimentKindHistoryStoreTests.QueryReturnsHistoryByAxis`
- `ExperimentKindHistoryStoreTests.AggregateImprovementRateByKind`
- `PlainLanguageExplainerTests.CoversStrategicPlanDecisionKind`

**Validation commands:**

```powershell
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "StrategicPlanner|ExperimentKindHistory|PlainLanguage"
dotnet build .\vnext\Wevito.VNext.sln
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
```

**Artifacts:**

```text
%LOCALAPPDATA%/Wevito/audit/experiment-kind-history.sqlite
vnext/artifacts/strategic-planning/<ts>/decision.json
docs/C_PHASE104_STRATEGIC_PLANNER_2026-05-15.md
```

**Stop gates:**

- Stop if the planner can override KillSwitch or supervisor mode.
- Stop if the planner can select an unregistered kind.
- Stop if `min_expected_value_threshold` is bypassable.
- Stop if history rows can be UPDATEd or DELETEd.
- Stop if `PlainLanguageExplainer` is missing the new packet kind.

**Rollback:** revert PR; experiment runner falls back to weighted-random
kind selection.

**Commit/PR:**

- Branch: `claude-implementation/c-phase-104-strategic-planner`
- Commit messages reference `C-PHASE 104`.
- PR title: `C-PHASE 104: Strategic planner (directional experiment selection)`.
- Phase report: `docs/C_PHASE104_STRATEGIC_PLANNER_2026-05-15.md`.

**Auto-continue?** **No.** First decision-authority component beyond
weighted-random; stop for explicit user review of the algorithm.

---

## C-PHASE 105 — Daily/Weekly User-Facing Digest UI

**Goal:** Add a user-facing "Activity Digest" tab in `ToolPopupWindow`
that shows wevito's recent work as a human-readable summary. Reads
`SelfImprovementReportService` output and `MemoryConsolidationService`
summaries and `LearnedPatternStore` rows. Lets the user see what
wevito did, learned, and proposed — without having to read JSON.

**Scope:**

- Add `ActivityDigestService` in `Wevito.VNext.Core`:
  - One method `ActivityDigest BuildDigest(ActivityDigestRequest request)`.
  - Reads:
    - The latest self-improvement reports within the window
    - The latest consolidated summaries
    - Recent learned patterns
    - Recent task cards (terminal-state) + their thumbs feedback
    - Recent experiment results (with strategic-plan context)
  - Produces a structured `ActivityDigest`:
    - `WindowStart`, `WindowEnd`
    - `OneSentenceSummary` (from PlainLanguageExplainer)
    - `TopKindsByActivityCount` (top 5)
    - `RecentMutations` (last 10, with link to their proof packets)
    - `RecentExperiments` (last 10, with outcome and target axis)
    - `RecentLearnedPatterns` (last 5)
    - `ThumbsUpDownRatio7d`
    - `NotableEvents` (any kill-switch, regression, or stop-gate hits)
- Add `ActivityDigestPanel` XAML in `ToolPopupWindow.xaml`:
  - New tab "Activity Digest"
  - Three view modes: Today / This Week / This Month
  - Each section renders as a card with a title, one-sentence summary,
    and a "details" expander
  - "Recent Mutations" cards link to their proof packets (file://) when
    clicked
  - "Recent Patterns" cards show the pattern summary + confidence + a
    "this changed wevito's plan" badge if a strategic-plan decision
    consumed the pattern
- Settings keys:
  - `activity_digest_default_window` (default `week`)
  - `activity_digest_show_in_overlay` (default false; digest is for the
    popup, not the always-on overlay)
- New packet kind: `activity_digest_view` (recorded once per user-opened
  digest, for "user is engaged" signal).

**Pattern:** Follow `ActivitySummaryService` at
`vnext/src/Wevito.VNext.Core/ActivitySummaryService.cs` for the
read-aggregate-summarize pattern (this service is the read-only one
that produces a snapshot; the UI is the new piece). Follow the
existing `ToolPopupWindow.xaml` "Evidence collection" tab pattern from
C-PHASE 85b for the tab + cards + expanders layout.

**Files likely touched:**

```text
vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml
vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml.cs
vnext/src/Wevito.VNext.Core/PlainLanguageExplainer.cs
vnext/content/tool_definitions.json
```

**New files:**

```text
vnext/src/Wevito.VNext.Core/ActivityDigestService.cs
vnext/src/Wevito.VNext.Core/ActivityDigest.cs
vnext/src/Wevito.VNext.Core/ActivityDigestRequest.cs
vnext/tests/Wevito.VNext.Tests/ActivityDigestServiceTests.cs
vnext/tests/Wevito.VNext.Tests/ToolPopupWindowActivityDigestUiTests.cs
docs/C_PHASE105_DAILY_WEEKLY_DIGEST_UI_2026-05-15.md
```

**Tests:**

- `ActivityDigestServiceTests.RendersOneSentenceSummary`
- `ActivityDigestServiceTests.AggregatesTopKindsCorrectly`
- `ActivityDigestServiceTests.IncludesProofPacketLinks`
- `ActivityDigestServiceTests.RespectsTimeWindow`
- `ActivityDigestServiceTests.HandlesEmptyLedger` — returns empty digest
  with friendly "wevito has not been active in this window" sentence,
  no exception.
- `ActivityDigestServiceTests.RespectsKillSwitch`
- `ToolPopupWindowActivityDigestUiTests.TabRendersWithCards`
- `ToolPopupWindowActivityDigestUiTests.WindowToggleSwitchesData`
- `ToolPopupWindowActivityDigestUiTests.MutationCardLinkClickOpensProofFile`
- `ToolPopupWindowActivityDigestUiTests.OpeningTabWritesViewPacket`
- `PlainLanguageExplainerTests.CoversActivityDigestViewKind`

**Validation commands:**

```powershell
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "ActivityDigest|ToolPopupWindow|PlainLanguage"
dotnet build .\vnext\Wevito.VNext.sln
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
```

**Artifacts:**

```text
vnext/artifacts/activity-digest/<ts>-digest/packet.json
docs/C_PHASE105_DAILY_WEEKLY_DIGEST_UI_2026-05-15.md
```

**Stop gates:**

- Stop if the digest leaks raw private text from any ledger row (the
  digest must use PlainLanguageExplainer summaries only, never raw
  row content).
- Stop if mutation links open arbitrary files (must be restricted to
  `vnext/artifacts/` and similar audited folders via UnifiedPolicyService).
- Stop if opening the tab writes more than one `activity_digest_view`
  row per second (debounce).
- Stop if `PlainLanguageExplainer` is missing the new packet kind.

**Rollback:** revert PR; users continue to read raw `self-improvement-report`
JSON files for the same info.

**Commit/PR:**

- Branch: `claude-implementation/c-phase-105-daily-weekly-digest-ui`
- Commit messages reference `C-PHASE 105`.
- PR title: `C-PHASE 105: Activity digest UI (daily / weekly / monthly)`.
- Phase report: `docs/C_PHASE105_DAILY_WEEKLY_DIGEST_UI_2026-05-15.md`.

**Auto-continue?** **Yes.** Read-only UI on top of existing services;
no mutation surface; no new model surface; no autonomy expansion.

---

## Recommended Execution Order

Although the phase numbers above reflect write-order, the recommended
*execute* order (for the Codex loop runner from C-PHASE 96) is:

```text
1.   C-PHASE 101   Household maintenance     (day-1 critical)
2.   C-PHASE 102   User feedback             (composite-fitness signal)
3.   C-PHASE 99    Palette grammar registry  (foundation for images)
4.   C-PHASE 100   General fallback grammar  (depends on 99)
5.   C-PHASE 98    Image generation runtime  (foundation for image LoRA)
6.   C-PHASE 97    Image LoRA training       (depends on 98, 99, 102)
7.   C-PHASE 103   Memory consolidation      (extends 83/101)
8.   C-PHASE 104   Strategic planner         (extends 87, depends on 93)
9.   C-PHASE 105   Activity digest UI        (extends everything above)
```

C-PHASE 86-96 (the foundation phases) MUST land before any of the above
that depend on them — specifically:

- 101 depends on C-PHASE 65 (RuntimeBudgetMeter) + 83 (SelfImprovementReport)
- 102 has no prior-phase dependency (can land independently)
- 99-100 have no prior-phase dependency
- 98 has no prior-phase dependency
- 97 depends on 98 + 99 + 102 (it consumes user-thumb-approved templates
  as training data) + 87 (experiment runner is the natural caller) but
  can also be invoked directly via task card
- 103 depends on 83 + 101 (registered as a chore)
- 104 depends on 87 (experiment runner) + 93 (composite scoreboard) +
  103 (learned patterns)
- 105 depends on 83 + 103 + 87 + 93

## Closing Notes for Codex

Each phase above is sized for medium reasoning effort:

- 3-9 new files per phase
- 2-5 existing files touched per phase
- 7-15 named tests per phase
- Single architectural concern per phase

When picking up a phase, follow the **Pattern** field exactly. The named
existing services already pass the safety tests we need; the new services
inherit those properties by mimicking the shape.

When in doubt:

- Read the named pattern file end-to-end before writing the new file.
- Mirror its tests one-for-one with new test names.
- Use the same dependency-injection shape (constructors with optional
  parameters for HttpClient, KillSwitchService, AuditLedgerService).
- Use the same evidence-packet shape (always set the four did-flags
  honestly).
- Use the same kill-switch consultation point (top of any public method
  that can write a ledger row or mutate state).

If validation fails or stop gates trigger, do not auto-merge; open the
PR in Draft, write a `phase_blocked` audit row, and halt the loop runner
(C-PHASE 96 will see the halt signal and stop fetching the next phase).
