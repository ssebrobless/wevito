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
+-- app can research, compare sources, and synthesize findings
+-- app uses local/offline intelligence by default
+-- hosted AI calls are optional bootstrap/research aids only
+-- learning happens from reviewed local data
`-- no Codex/GPT/Claude/Gemini/browser handoff required for normal operation
```

## Guiding Rules

```text
rules
|
+-- no hidden provider calls
+-- no hidden training
+-- no hidden memory promotion
+-- no hosted AI dependency for normal operation
+-- no Codex/GPT/Claude/Gemini runtime brain
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
|   +-- Wevito owns local/offline provider settings
|   `-- hosted providers stay disabled unless explicitly approved for a narrow task
|
+-- later
|   +-- Wevito runs local/offline models for normal helper tasks
|   +-- Wevito promotes reviewed examples into memory/evals
|   +-- Wevito proposes safe improvements itself
|   `-- Codex is only a development tool, not a runtime dependency
|
`-- ideal
    +-- Wevito self-maintains within strict gates
    +-- user reviews higher-risk changes
    +-- local learning improves behavior over time
    `-- external providers are optional accelerators, not required services
```

## Local-First Independence Bar

Wevito should be considered independently functional only when the core loop below works without Codex, GPT, Claude, Gemini, or another hosted model service.

```text
independent loop
|
+-- observe local app/game/tool state
+-- retrieve local memory and reviewed examples
+-- collect source evidence through app-owned research tools
+-- classify task with app-owned rules or a local model
+-- synthesize findings into preview/report/proof artifact
+-- learn from approved user feedback
+-- improve local routing/evals/preferences
`-- summarize what changed without contacting hosted AI
```

Hosted models can still be useful during development or with explicit user approval, but they should not be required for the everyday pet-agent experience.

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

### C-PHASE 64 - Local-First App-Owned AI And Research Strategy

Goal: remove Codex, GPT, Claude, Gemini, and browser handoffs as assumed reasoning runtimes.

Scope:

- Define model provider modes:
  - `disabled`
  - `local_only`
  - `approved_cloud`
- Keep cloud off by default.
- Make `local_only` the first real target, not a future afterthought.
- Add a local/offline model provider abstraction even if initially backed by deterministic heuristics.
- Add a research planner abstraction that can collect source records and produce citation/evidence packets without needing a hosted model.
- Add a synthesis contract for research reports:
  - user question
  - local memory used
  - sources inspected
  - claims extracted
  - confidence/uncertainty
  - next recommended action
- Document possible local providers separately from implementation, including install/runtime requirements and how Wevito behaves when none are installed.
- Do not make live calls unless explicitly approved.
- Treat hosted providers as temporary bootstrap/research adapters, not as required product dependencies.

Likely files:

```text
vnext/src/Wevito.VNext.Core/ModelProviderMode.cs
vnext/src/Wevito.VNext.Core/LocalModelAdapter.cs
vnext/src/Wevito.VNext.Core/ResearchPlannerService.cs
vnext/src/Wevito.VNext.Core/ResearchEvidencePacket.cs
vnext/src/Wevito.VNext.Shell/ShellCoordinator.cs
vnext/tests/Wevito.VNext.Tests/ModelProviderModeTests.cs
vnext/tests/Wevito.VNext.Tests/ResearchPlannerServiceTests.cs
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
- Stop if cloud provider selection becomes implicit or required for normal PET TASKS.
- Stop if `local_only` cannot produce a deterministic no-network preview.
- Stop if research reports lack source/evidence records.

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
- Store promoted examples in a local, versioned dataset that can later power retrieval, routing, evals, and local model improvement.

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

Goal: add measurable local improvement without uncontrolled training or hosted AI dependency.

Scope:

- Build eval sets from reviewed examples.
- Run retrieval/memory quality checks.
- Record before/after scores.
- Prefer local embeddings and deterministic evals.
- Produce "learning changed behavior" reports.
- Add a no-network test mode that proves evals and learning reports run offline.
- Separate memory/retrieval improvement, lightweight local model/config improvement, and future training/fine-tuning.

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
- Stop if the app cannot explain what local data changed behavior.

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
- Offline/local-only proof.
- Manual user acceptance checklist.

Required outcome:

```text
decision labels
|
+-- enable_limited_autonomy
+-- keep_preview_only
`-- pause_for_safety_work
```

`enable_limited_autonomy` is only valid if Wevito can complete the core helper loop in `local_only` mode. If hosted AI is required, the correct decision is `keep_preview_only` or `pause_for_safety_work`.

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
