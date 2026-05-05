# Wevito Pet Agent Text Bar Plan

Date: 2026-05-05

Purpose: add the user's requested text bar for communicating with named pet AI helpers, while preserving Wevito's simulation, tool-safety, visual, and ML boundaries.

This is a planning document only. It does not implement UI, connect tools, train models, or authorize pet agents to execute real tasks.

## Target Shape

```text
╔══════════════════════════════════════════════════════════════════════╗
║ Wevito Pet Command Surface                                          ║
╠════════════════════════╦═════════════════════════════════════════════╣
║ User text bar           ║ "Milo, make a sprite QA checklist"         ║
║ Pet roster              ║ up to 3 named helper pets                  ║
║ Command parser          ║ text -> structured TaskIntent              ║
║ Pet agent layer         ║ persona, memory, suggestions, explanation  ║
║ Tool broker             ║ permissions, guardrails, approvals, runs   ║
║ Task card               ║ requested -> waiting -> running -> review  ║
║ Learning layer          ║ reviewed outcomes -> prefs/evals later     ║
╚════════════════════════╩═════════════════════════════════════════════╝
```

The text bar should sit near tool/task sections in vNext Shell or the future tool dashboard. It should feel like the user is talking directly to their pets, but the implementation should keep all dangerous work behind structured task cards and broker-controlled tools.

## Animal Personality Vs Agent Capability

Wevito should deliberately separate the visible pet personality from the hidden AI-agent capability.

```text
Visible pet layer
  ├── simple animal temperament
  ├── species-flavored habits
  ├── moods, needs, likes, dislikes
  ├── cute animations and reactions
  └── light memory of care and routines

Agent helper layer
  ├── structured command parsing
  ├── tool planning and task cards
  ├── safe tool-broker execution
  ├── audit logs and review states
  ├── model-assisted summaries/research/checklists
  └── reviewed learning/eval data
```

This should not copy Papilionem's deeper personality architecture. Papilionem can inspire life-simulation boundaries, memory categories, and ML ideas, but Wevito's animals should remain understandable pets: playful, shy, stubborn, curious, sleepy, hungry, affectionate, skittish, energetic, or calm.

The clever part is the disguise: Wevito can expose powerful AI helpers through a cozy pet simulator surface. The pet can act like a little animal while the app behind it runs serious, permissioned agent workflows.

## Can We Achieve This?

Yes, but it should be staged.

```text
Stage 1  command bar + mock pets
Stage 2  structured TaskIntent parser
Stage 3  named pet roster and simple memory
Stage 4  safe tool-broker task cards
Stage 5  model-assisted planning/suggestions
Stage 6  reviewed learning datasets/evals
Stage 7  optional fine-tuning/local ML experiments
```

Do not start at Stage 7. The "ML capability" should first mean the pets learn preferences, routines, accepted outcomes, and user feedback inside Wevito's own data model. Fine-tuning or local model training only becomes appropriate after evals and reviewed datasets exist.

## Three Pet Helper Model

```text
╔═══════════════╦══════════════════════════════════════════════════════╗
║ Helper concept ║ Contract                                             ║
╠═══════════════╬══════════════════════════════════════════════════════╣
║ Named pet       ║ user chooses name; command bar can address it        ║
║ Helper role     ║ optional user-assigned focus such as art, files, QA  ║
║ Personality     ║ simple animal tone, animation, confidence display    ║
║ Memory          ║ stores preferences/outcomes, not secret raw logs      ║
║ Capabilities    ║ only declared safe tools from Tool Broker             ║
║ Autonomy        ║ suggestions by default; execution requires policy     ║
╚═══════════════╩══════════════════════════════════════════════════════╝
```

Initial roster limit:

- Maximum active pet AI helpers: `3`.
- Additional pets can exist in the game, but only three should be active helper agents at once.
- Each active helper can be addressed by name, for example: `Milo, check the latest sprite audit`.
- Unaddressed commands can route to the currently selected pet or a small coordinator that proposes which pet should own the task.

## Command Bar Behavior

```text
User input
  │
  ▼
Parse target
  ├── explicit pet name
  ├── current selected pet
  └── route-to-best-helper
  │
  ▼
Structured TaskIntent
  ├── task_kind
  ├── target_files / target_docs / target_assets
  ├── requested_tool_family
  ├── risk_level
  ├── needs_approval
  └── expected_output
  │
  ▼
Task card
  ├── user reviews plan
  ├── tool broker validates
  ├── execution runs if approved
  ├── result enters review
  └── feedback updates memory/evals
```

The text bar should support:

- `@PetName` addressing.
- Natural addressing such as `Milo, ...`.
- Commands without a pet name.
- Draft-only mode.
- Approval-before-execution mode.
- Safe read-only commands.
- Explicitly blocked commands with a friendly explanation.

## Ownership Boundaries

| System | Owns | Does not own |
| --- | --- | --- |
| Pet identity/life sim | pet name, simple animal personality, mood, needs, lightweight routines | tool permission, file writes, model execution, deep Papilionem-style psychology |
| Pet agent profile | helper role, tone, task preferences, learned non-secret preferences | durable game truth, direct tool execution |
| Command parser | convert text into structured `TaskIntent` | execute tools |
| Tool broker | tool registry, permissions, guardrails, approvals, logs | pet personality or memory |
| Task card service | task state, status, review, result summary | pet needs/mood truth |
| AI adapter | model calls, structured outputs, summaries, proposals | filesystem mutation |
| Learning lab | reviewed examples, evals, datasets, preference bundles | live self-modification |

## ML Capability Definition

```text
ML capability
  │
  ├── now
  │     ├── preference memory
  │     ├── task outcome summaries
  │     ├── reviewed examples
  │     └── eval-ready task datasets
  │
  ├── later
  │     ├── embeddings/search over approved memory
  │     ├── prompt/profile adaptation
  │     ├── local lightweight classifiers
  │     └── model-assisted task routing
  │
  └── much later
        ├── supervised fine-tuning
        ├── reinforcement fine-tuning
        └── local art/tool model experiments
```

The pets should not train themselves live from raw logs. They should gather reviewed examples and user feedback first. This follows the current model-optimization guidance: build evals, measure behavior, then consider prompt iteration or fine-tuning.

## Research Notes

OpenAI's current agent docs support this architecture:

- Agents are applications that plan, call tools, collaborate across specialists, and maintain enough state for multi-step work; the application should own orchestration, tool execution, approvals, and state.
- Guardrails should wrap tool calls, especially when custom tools are used.
- Agent evals should use traces, graders, datasets, and eval runs to measure reliability.
- Structured Outputs are the right fit for command parsing and task cards because they enforce schemas instead of freeform text.
- Model optimization should start with evals and prompt iteration; fine-tuning comes later after representative data exists.

Sources:

- `https://developers.openai.com/api/docs/guides/agents`
- `https://openai.github.io/openai-agents-js/guides/guardrails/`
- `https://developers.openai.com/api/docs/guides/agent-evals`
- `https://developers.openai.com/api/docs/guides/structured-outputs`
- `https://developers.openai.com/api/docs/guides/model-optimization`

## UI Placement

Recommended first placement:

```text
vNext Shell / Tools
  ├── Pet helper roster
  │     ├── Pet A card
  │     ├── Pet B card
  │     └── Pet C card
  │
  ├── Pet command bar
  │     └── "Ask or assign a task to Milo, Bean, or Juniper..."
  │
  ├── Generated task card
  │     ├── intent
  │     ├── target
  │     ├── risk
  │     ├── approval
  │     └── status
  │
  └── Audit / memory / learning feedback
```

Claude Design should prototype this as part of Phase 1B, but implementation should stay in code-side phases.

## First Prototype Prompt Addition

Add this to the Claude Design prompt when prototyping the tool dashboard:

```text
Add a compact pet command bar near the tool/task-card section.

The user can assign tasks to up to three named pet helpers. Show three helper pet cards with names, moods, helper roles, availability, and current task status. The text bar should support examples like "Milo, check the sprite audit" and "@Bean summarize the latest failed tasks."

The UI must make it clear that pets suggest and prepare task cards, but tools only run through approval and guardrail states. Show draft, waiting for approval, running, reviewing, done, and blocked states.
```

## Phase Integration

- Phase 1: define ownership, schemas, persistence, and guardrail boundaries.
- Phase 1B: prototype the command bar and three-helper dashboard in Claude Design.
- Phase 6: connect pet life/personality state to helper behavior without replacing deterministic simulation.
- Phase 8: implement command bar, task cards, broker routing, and audit log.
- Phase 9: pilot one pet agent on safe read-only tasks, then expand to three active helpers.
- Phase 10: store reviewed outcomes as learning/eval data.
- Phase 11: add provider/local model adapters only behind stable schemas.

## Hard Rules

- A pet name can route a task, but cannot bypass permissions.
- Pet personality should stay animal-simple; it can affect tone and visible animation, but not risk policy.
- ML can rank suggestions and remember preferences, but cannot directly mutate game truth or files.
- Tool execution always belongs to Tool Broker.
- Risky tasks require explicit approval.
- All task outcomes must be visible in an audit log.
