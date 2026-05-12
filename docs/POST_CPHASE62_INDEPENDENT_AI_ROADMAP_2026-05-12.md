# Post-C-PHASE 62 Independent AI Roadmap

Date: 2026-05-12

## Goal

Move Wevito from a stable desktop pet plus safe helper tools into an independently functional always-on AI companion.

The target is not "Codex inside a pet skin." The target is:

```text
Wevito-owned AI runtime
|
+-- pets remain normal pets visually
+-- user has one simple command entry
+-- app can run quietly all day
+-- app owns task scheduling and evidence logs
+-- app can use local/offline intelligence where possible
+-- cloud model calls are explicit opt-in only
+-- learning happens from reviewed local data
`-- no Codex/Gemini/Claude/browser handoff required for normal operation
```

## Guiding Rules

```text
rules
|
+-- no hidden provider calls
+-- no hidden training
+-- no hidden memory promotion
+-- no screen recording by default
+-- no external audio/system control by default
+-- no focus stealing
+-- no runtime sprite mutation without backup/hash/rollback/proof
+-- report-only before execution
`-- user-visible activity summary for background work
```

## Dependency Direction

Wevito should move through these layers:

```text
dependency ladder
|
+-- now
|   +-- Codex implements code
|   +-- external AI tools help create assets/plans
|   `-- Wevito runs safe previews and reports
|
+-- next
|   +-- Wevito owns local scheduler
|   +-- Wevito owns local memory/retrieval
|   +-- Wevito owns provider settings and consent
|   `-- Wevito can call approved providers directly
|
+-- later
|   +-- Wevito runs local/offline models for many tasks
|   +-- Wevito promotes reviewed examples into memory/evals
|   +-- Wevito proposes safe improvements itself
|   `-- Codex is only a development tool, not a runtime dependency
|
`-- ideal
    +-- Wevito self-maintains within strict gates
    +-- user reviews higher-risk changes
    +-- local learning improves behavior over time
    `-- external providers are optional accelerators
```

## Phase Plan

### C-PHASE 63 - Always-On Runtime Supervisor And Quiet Mode

Goal: make Wevito safe to keep launched while the user does other PC work.

Scope:

- Add a runtime supervisor state.
- Add pause/resume/quiet mode.
- Add "background work allowed" vs "pet-only mode".
- Add no-focus-steal policy.
- Add fullscreen/game detection policy if feasible without invasive hooks.
- Add CPU/memory/task-rate budget settings.
- Add visible "what is Wevito doing?" status.

Likely files:

```text
vnext/src/Wevito.VNext.Shell/ShellCoordinator.cs
vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml
vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml.cs
vnext/src/Wevito.VNext.Core/RuntimeSupervisorService.cs
vnext/tests/Wevito.VNext.Tests/RuntimeSupervisorTests.cs
docs/C_PHASE63_ALWAYS_ON_RUNTIME_SUPERVISOR_2026-05-12.md
```

Validation:

```powershell
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "RuntimeSupervisor|PetTask|Settings"
dotnet build .\vnext\Wevito.VNext.sln
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
```

Stop gates:

- Stop if background work can start while quiet mode is active.
- Stop if a task can steal focus.
- Stop if the supervisor bypasses PET TASKS approval rules.

### C-PHASE 64 - App-Owned Model Provider Strategy

Goal: remove Codex as the assumed reasoning runtime.

Scope:

- Define model provider modes:
  - `disabled`
  - `local_only`
  - `approved_cloud`
- Keep cloud off by default.
- Add a local/offline model provider abstraction even if initially stubbed.
- Document possible local providers separately from implementation.
- Do not make live calls unless explicitly approved.

Likely files:

```text
vnext/src/Wevito.VNext.Core/ModelProviderMode.cs
vnext/src/Wevito.VNext.Core/LocalModelAdapter.cs
vnext/src/Wevito.VNext.Shell/ShellCoordinator.cs
vnext/tests/Wevito.VNext.Tests/ModelProviderModeTests.cs
docs/C_PHASE64_APP_OWNED_MODEL_PROVIDER_STRATEGY_2026-05-12.md
```

Validation:

```powershell
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "Model|Provider|Consent"
dotnet build .\vnext\Wevito.VNext.sln
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
```

Stop gates:

- Stop if the app reads credentials automatically.
- Stop if a network call is made.
- Stop if cloud provider selection becomes implicit.

### C-PHASE 65 - Autonomous Task Scheduler Preview

Goal: allow Wevito to propose background work but not execute risky work.

Scope:

- Add a scheduler that can create task cards from safe triggers.
- Safe triggers:
  - stale dashboard/report
  - available reviewed bundle
  - failed prior proof packet
  - user-requested recurring check
- Scheduler writes preview cards only.
- User approves execution.

Likely files:

```text
vnext/src/Wevito.VNext.Core/AutonomousTaskScheduler.cs
vnext/src/Wevito.VNext.Core/PetTaskCardQueueService.cs
vnext/src/Wevito.VNext.Shell/ShellCoordinator.cs
vnext/tests/Wevito.VNext.Tests/AutonomousTaskSchedulerTests.cs
docs/C_PHASE65_AUTONOMOUS_TASK_SCHEDULER_PREVIEW_2026-05-12.md
```

Stop gates:

- Stop if scheduler can execute directly.
- Stop if scheduler ignores quiet mode.
- Stop if task cards lack an artifact/evidence path.

### C-PHASE 66 - Reviewed Learning Promotion Gate

Goal: let reviewed Creative Learning Lab bundles feed local memory/eval datasets only after approval.

Scope:

- Import `labels.json`, `sources.json`, `summary.md`.
- Block `blocked`, `reject`, and unlabeled rows from promotion.
- Promote `accept` rows only after explicit user approval.
- Write promotion report.
- Do not train a model yet.

Likely files:

```text
vnext/src/Wevito.VNext.Core/LearningLabPromotionService.cs
vnext/src/Wevito.VNext.Core/PetMemoryStore.cs
vnext/tests/Wevito.VNext.Tests/LearningLabPromotionTests.cs
docs/C_PHASE66_REVIEWED_LEARNING_PROMOTION_GATE_2026-05-12.md
```

Stop gates:

- Stop if labels promote automatically.
- Stop if binary assets are copied.
- Stop if rejected/blocked rows enter memory.

### C-PHASE 67 - Local Learning And Evaluation Loop

Goal: add measurable improvement without true uncontrolled training.

Scope:

- Build eval sets from reviewed examples.
- Run retrieval/memory quality checks.
- Record before/after scores.
- Prefer local embeddings and deterministic evals.
- Produce "learning changed behavior" reports.

Likely files:

```text
vnext/src/Wevito.VNext.Core/LearningEvalService.cs
vnext/tests/Wevito.VNext.Tests/LearningEvalServiceTests.cs
docs/C_PHASE67_LOCAL_LEARNING_EVAL_LOOP_2026-05-12.md
```

Stop gates:

- Stop if this becomes model fine-tuning.
- Stop if evals require external providers.
- Stop if results are not reproducible.

### C-PHASE 68 - Self-Improvement Activity Report Loop

Goal: make always-on behavior explain itself.

Scope:

- Daily/periodic activity summaries.
- "What did Wevito do?"
- "What did Wevito learn?"
- "What changed?"
- "What needs approval?"
- "What was blocked?"

Likely files:

```text
vnext/src/Wevito.VNext.Core/ActivitySummaryService.cs
vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml
vnext/tests/Wevito.VNext.Tests/ActivitySummaryServiceTests.cs
docs/C_PHASE68_SELF_IMPROVEMENT_ACTIVITY_REPORTS_2026-05-12.md
```

Stop gates:

- Stop if reports include private content without user-visible source labels.
- Stop if reports imply a task executed when it only previewed.

### C-PHASE 69 - Independent Assistant Beta Gate

Goal: decide whether Wevito is ready for limited autonomous execution.

Scope:

- Long-session proof.
- Quiet-mode proof.
- Resource-budget proof.
- Tool preview/execution history proof.
- Learning promotion proof.
- Manual user acceptance checklist.

Required outcome:

```text
decision labels
|
+-- enable_limited_autonomy
+-- keep_preview_only
`-- pause_for_safety_work
```

Stop gates:

- Stop if user cannot easily pause the app.
- Stop if resource usage is not measured.
- Stop if any tool can run without visible history.

## Immediate Recommendation

Start with C-PHASE 63.

Reason:

```text
autonomy without low-interference runtime control = annoying
autonomy with quiet mode and resource budgets = potentially trustworthy
```

Before Wevito learns or acts more, it needs to prove it can stay open while the user plays games, watches videos, codes, browses, and uses other apps without being intrusive.
