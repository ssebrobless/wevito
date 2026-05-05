# Wevito Phase 1 Contract And Ownership Plan

Date: 2026-05-05

Phase: 1 - Canonical Contracts And Ownership Boundaries

Purpose: define exactly what owns simulation truth, visuals, sprite/proof state, helper tools, the new pet-agent command bar, ML learning, and apply/rollback safety before code-side implementation or asset mutation continues.

This document is a plan and contract target. It does not authorize runtime/source PNG mutation, broad code rewrites, sprite generation, model training, or autonomous pet-agent execution.

## Phase 1 Goal

```text
╔══════════════════════════════════════════════════════════════════════╗
║ Phase 1 Question                                                    ║
╠══════════════════════════════════════════════════════════════════════╣
║ When Wevito changes, who owns the truth, who may suggest work,       ║
║ who may execute work, and what proof is required before mutation?    ║
╚══════════════════════════════════════════════════════════════════════╝
```

Phase 0 established that the main repo and the code-side worktree are not safely interchangeable. Phase 1 therefore must define ownership and contracts before Phase 2 touches runtime assets or reconciles worktree/main differences.

## Phase 0 Constraints Carried Forward

```text
main repo
  ├── optional-rich runtime assets
  ├── accepted goose/baby/female/blue/hold_ball endpoint present
  ├── 456 mixed-canvas required base rows
  ├── vNext tests fail 53/54
  └── very dirty working state

worktree 36d6
  ├── base runtime canvas contract green
  ├── vNext tests pass 26/26
  ├── exactly 10800 required runtime frames
  └── lacks accepted hold_ball runtime endpoint
```

Phase 1 rule: do not copy worktree runtime assets over main. Do not normalize main assets. Do not stage or clean large unrelated dirty state. Define the contract first.

Source:

```text
C:\Users\fishe\Documents\projects\wevito\docs\CODE_SIDE_REPO_RECONCILIATION_2026-05-05.md
```

## Ownership Map

```text
╔═══════════════════════╦══════════════════════════════════════════════╗
║ Owner                 ║ Owns                                         ║
╠═══════════════════════╬══════════════════════════════════════════════╣
║ vNext core             ║ durable pet identity, needs, habits, mood    ║
║ Godot runtime          ║ player-facing game proof and live visuals    ║
║ vNext shell            ║ desktop companion UI and tool surfaces       ║
║ Asset manifest         ║ path, hash, geometry, family, frame count    ║
║ Sprite Workflow V2     ║ candidate, review, apply, rollback state     ║
║ Pet Command Bar        ║ user text input and named helper routing     ║
║ Tool Broker            ║ tool registry, permissions, execution, logs  ║
║ AI adapters            ║ model calls, structured outputs, proposals   ║
║ Creative Learning Lab  ║ reviewed examples, evals, preference data    ║
║ Claude Design          ║ prototypes and design handoff only           ║
╚═══════════════════════╩══════════════════════════════════════════════╝
```

## Current Code Anchors

The current vNext code already has useful seams:

- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Contracts\Models.cs` contains `PetActor`, `PetPersonalityProfile`, `PetHabitProfile`, `PetAnimationState`, and `CompanionState`.
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Core\PetSimulationEngine.cs` should remain the deterministic simulation owner.
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Shell\ShellCoordinator.cs` currently coordinates shell windows, broker events, basket state, pet actions, and feedback.
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Broker\` is the natural home for brokered local desktop capabilities.
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Shell\ToolPopupWindow.xaml.cs` is a likely first UI surface for the pet command bar and task cards.

Phase 1 does not change these files. It defines how later phases should extend them.

## Pet Life-Sim Contract

```text
PetActor
  ├── identity
  │     ├── Id
  │     ├── Name
  │     ├── SpeciesId
  │     ├── AgeStage
  │     ├── Gender
  │     └── ColorVariant
  │
  ├── life truth
  │     ├── Hunger / Thirst / Energy
  │     ├── Cleanliness / Affection / Comfort
  │     ├── Health / Fitness
  │     ├── Personality
  │     ├── HabitProfile
  │     └── ActiveConditions / ActiveStatuses
  │
  └── presentation state
        ├── BehaviorState
        ├── FacingDirection
        ├── CurrentAnimationState
        └── OverrideAnimationState
```

Rules:

- Pet identity and life truth belong to vNext core.
- Tool execution never mutates life truth directly.
- Pet-agent helper behavior may read personality, mood, habits, and memory summaries.
- Pet personality affects presentation, phrasing, visible confidence, and suggestion style.
- Pet personality does not alter security policy, file permissions, or approval requirements.
- Wevito pet personalities should remain animal-simple, not Papilionem-complex. Use traits like playful, shy, stubborn, curious, sleepy, affectionate, skittish, energetic, or calm.
- The advanced part lives in the agent helper layer: tool planning, task cards, broker execution, review states, audit logs, and learning data.

Future contract target:

```text
WEVITO_LIFE_SIM_OWNERSHIP_MAP.md
```

## Pet Command Bar Contract

The command bar is the user-facing entry point for pet AI helper tasks.

Design intent:

```text
simple animal on screen
  │
  ├── name, mood, small habits
  ├── cute reaction/animation
  └── light personality flavor
        │
        ▼
powerful helper behind the curtain
  ├── structured TaskIntent
  ├── tool-broker policy
  ├── safe execution
  ├── review/audit
  └── learning feedback
```

This keeps the product fantasy intact: Wevito feels like a pet simulator game, while the helper architecture can still be significantly stronger than a normal game pet system.

```text
User text
  │
  ▼
PetCommandParser
  │
  ├── explicit target: "@Milo ..." or "Milo, ..."
  ├── selected-pet target
  └── route-to-best-helper target
  │
  ▼
TaskIntent
  │
  ├── target_pet_id
  ├── target_pet_name_snapshot
  ├── task_kind
  ├── requested_tool_family
  ├── target_paths_or_assets
  ├── risk_level
  ├── needs_approval
  ├── expected_output
  └── refusal_or_clarification_reason
  │
  ▼
TaskCard
```

Initial feature rules:

- Maximum active helper pets: `3`.
- Helpers are referred to by user-assigned pet names.
- Extra pets can remain normal pets, but only three are active helpers.
- Unaddressed tasks route to the selected pet or produce a route suggestion.
- Commands become structured `TaskIntent` before any tool can run.
- Commands that are ambiguous, unsafe, or out of scope become a blocked/draft task card, not a tool call.

The pet command-bar plan is here:

```text
C:\Users\fishe\Documents\projects\wevito\docs\WEVITO_PET_AGENT_TEXT_BAR_PLAN_2026-05-05.md
```

Future contract target:

```text
WEVITO_PET_AGENT_COMMAND_CONTRACT.md
```

## Three Helper Agent Contract

```text
ActiveHelperRoster
  ├── Helper 1
  │     ├── PetActor Id
  │     ├── user-assigned name
  │     ├── helper role
  │     ├── availability
  │     └── current task card
  │
  ├── Helper 2
  │     └── same shape
  │
  └── Helper 3
        └── same shape
```

Suggested helper roles:

- `SpriteReviewHelper`
- `ResearchHelper`
- `ChecklistHelper`
- `FileOrganizerHelper`
- `BuildProofHelper`
- `ReminderHelper`

Rules:

- Helper role influences task routing and UI labels.
- Role does not grant permissions.
- Animal temperament influences presentation only; helper role and tool policy drive task behavior.
- If two pets could handle a task, Wevito should propose a route before execution.
- If a named pet is unavailable, the task should pause or ask whether to reroute.

## Tool Broker Contract

```text
TaskIntent
  │
  ▼
Policy check
  ├── allowed tool family?
  ├── approved project root?
  ├── read-only vs write?
  ├── network/browser/auth risk?
  └── approval required?
  │
  ▼
Tool execution
  ├── run
  ├── capture output
  ├── validate result
  └── record audit log
```

Safe first task families:

- Search local docs.
- Summarize selected docs.
- Create a checklist draft.
- Capture a screenshot/proof.
- Export a no-mutation report.
- Open a local Wevito document.
- Save a copied link to basket.

Blocked or approval-heavy task families:

- Shell writes outside approved roots.
- Runtime/source sprite mutation.
- Browser automation on logged-in sites.
- Sending messages/emails.
- Purchases, account changes, credential handling.
- Long-running autonomous loops.

Future contract targets:

```text
WEVITO_TOOL_BROKER_CONTRACT.md
WEVITO_TASK_CARD_SCHEMA.md
WEVITO_PERMISSION_POLICY.md
```

## Asset And Proof Contract

```text
Candidate
  ├── target: species / age / gender / color / family / frame ids
  ├── source paths
  ├── runtime before hashes
  ├── candidate paths
  ├── validation report
  └── visual proof

Apply
  ├── dry-run report
  ├── backup-before-apply
  ├── after hashes
  ├── Godot proof first
  └── rollback command/procedure
```

Rules:

- Candidate generated does not mean completed.
- Apply requires target, before hashes, after hashes, proof, and rollback.
- Runtime ball overlay remains separate from body-pose PNGs.
- Main optional runtime rows must not be erased by base-canvas repair.
- Accepted endpoint must be protected:

```text
sprites_runtime/goose/baby/female/blue/hold_ball_00.png
sprites_runtime/goose/baby/female/blue/hold_ball_01.png
sprites_runtime/goose/baby/female/blue/hold_ball_02.png
sprites_runtime/goose/baby/female/blue/hold_ball_03.png
```

Future contract targets:

```text
WEVITO_ACTION_ANIMATION_PROP_CONTRACT.md
WEVITO_SPRITE_APPLY_ROLLBACK_CONTRACT.md
WEVITO_RUNTIME_ASSET_LANE_POLICY.md
```

## ML And Learning Contract

```text
Learning ladder
  │
  ├── Level 0: deterministic rules and mock outputs
  ├── Level 1: reviewed preference memory
  ├── Level 2: structured command/task datasets
  ├── Level 3: eval suites and trace grading
  ├── Level 4: prompt/profile adaptation
  ├── Level 5: local classifiers or retrieval
  └── Level 6: supervised/RFT/local model experiments
```

Rules:

- "ML capability" starts as reviewed preference memory and eval-ready datasets.
- No raw unreviewed logs enter training data.
- Pet helpers can learn preferences, routines, and accepted task patterns.
- Pets do not live-train themselves from everything the user types.
- Fine-tuning waits until there are evals and enough approved examples.

Future contract target:

```text
WEVITO_CREATIVE_LEARNING_DATA_CONTRACT.md
```

## AI Adapter Contract

```text
AI adapter
  ├── command parser
  ├── task summarizer
  ├── route recommender
  ├── review labeler
  ├── image candidate provider
  └── mock provider for tests
```

Rules:

- AI adapters produce proposals, structured outputs, summaries, and candidates.
- AI adapters do not write files directly.
- Tool calls go through Tool Broker.
- Image candidates go through Sprite Workflow V2 candidate lane.
- Provider-specific logic stays behind stable interfaces.

Research basis:

- Agents SDK guidance says the application should own orchestration, tool execution, approvals, and state.
- Structured Outputs should be used for command parsing and task cards.
- Guardrails should wrap function-tool calls.
- Agent evals should use traces, graders, datasets, and eval runs.
- Model optimization should start with evals and prompt iteration before fine-tuning.

Sources:

- `https://developers.openai.com/api/docs/guides/agents`
- `https://developers.openai.com/api/docs/guides/structured-outputs`
- `https://openai.github.io/openai-agents-js/guides/guardrails/`
- `https://developers.openai.com/api/docs/guides/agent-evals`
- `https://developers.openai.com/api/docs/guides/model-optimization`

## Claude Design Contract

Claude Design is useful for Phase 1B only:

```text
Claude Design
  ├── prototype Sprite Workflow V2 one-screen console
  ├── prototype pet command bar + 3 helper cards
  ├── prototype task-card/tool-broker dashboard
  └── export design handoff
```

Rules:

- Do not upload secrets or broad private repo dumps.
- Do not treat generated prototype code as production code.
- Do not use Claude Design for sprite PNG mutation.
- Do not use browser automation on Claude unless explicitly assigned.

Source:

```text
C:\Users\fishe\Documents\projects\wevito\docs\WEVITO_CLAUDE_DESIGN_USAGE_PLAN_2026-05-05.md
```

## Phase 1 Deliverables

Documents to create or refine in later Phase 1 implementation:

- `WEVITO_RUNTIME_CONTRACTS.md`
- `WEVITO_ACTION_ANIMATION_PROP_CONTRACT.md`
- `WEVITO_LIFE_SIM_OWNERSHIP_MAP.md`
- `WEVITO_PET_AGENT_COMMAND_CONTRACT.md`
- `WEVITO_TOOL_BROKER_CONTRACT.md`
- `WEVITO_TASK_CARD_SCHEMA.md`
- `WEVITO_PERMISSION_POLICY.md`
- `WEVITO_SPRITE_APPLY_ROLLBACK_CONTRACT.md`
- `WEVITO_CREATIVE_LEARNING_DATA_CONTRACT.md`

Minimum schema set:

- `SpriteRowManifest`
- `AnimationCandidateManifest`
- `ApplyProofManifest`
- `TaskIntent`
- `TaskCard`
- `PetHelperProfile`
- `ToolDescriptor`
- `ToolPolicy`
- `ReviewedExample`
- `LearningFeedback`

## Phase 1 Validation Plan

No sprite mutation validation:

- Confirm all Phase 1 docs say they do not authorize sprite/runtime mutation.
- Confirm accepted endpoint is named in protected assets.
- Confirm Tool Broker owns execution.
- Confirm pet life-sim owns pet truth.
- Confirm AI adapters only propose.
- Confirm ML starts with reviewed examples/evals, not live self-training.

Later code validation targets:

- Schema tests for `TaskIntent`, `TaskCard`, and `PetHelperProfile`.
- Parser tests for explicit pet names, `@PetName`, current-pet routing, and ambiguous tasks.
- Permission tests for read-only, write, network, browser, and external-message tasks.
- Save/load tests for helper roster and pet profile persistence.
- Trace/audit-log tests for every brokered task.

## Phase 1 Audit

```text
Phase 1 audit
  ├── ownership boundaries defined: yes
  ├── pet command bar integrated: yes
  ├── three-helper model integrated: yes
  ├── ML capability staged safely: yes
  ├── asset mutation avoided: yes
  ├── accepted endpoint protected: yes
  ├── ready for Phase 1B prototyping: yes
  └── ready for Phase 2 mutation: no, contracts must be implemented first
```

Phase 1 planning passes with one major constraint: it is safe to proceed to Phase 1B Claude Design prototyping or to Phase 1 implementation of schemas/contracts, but it is not yet safe to proceed to Phase 2 runtime asset mutation.
