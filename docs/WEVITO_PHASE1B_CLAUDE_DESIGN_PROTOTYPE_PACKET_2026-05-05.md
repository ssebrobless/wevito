# Wevito Phase 1B Claude Design Prototype Packet

Date: 2026-05-05

Phase: 1B - Claude Design System And Prototype Lane

Purpose: provide a focused, safe packet for Claude Design to prototype Wevito's next UI/tool surfaces without uploading broad repo state, mutating code, generating sprites, or touching runtime assets.

This packet is intended to be pasted into Claude Design or used as a source brief. It does not authorize browser automation, sprite generation, runtime/source PNG mutation, or production code implementation.

## Phase 1B Shape

```text
╔══════════════════════════════════════════════════════════════════════╗
║ Phase 1B Design Lane                                                ║
╠════════════════════════╦═════════════════════════════════════════════╣
║ Input                  ║ focused docs, screenshots, design brief      ║
║ Tool                   ║ Claude Design research preview               ║
║ Output                 ║ prototypes, design-system seed, handoff      ║
║ Code owner             ║ Codex/code-side after human review           ║
║ Visual owner           ║ visual thread for art/style QA coordination  ║
║ No-touch lanes         ║ code, sprites, source boards, runtime PNGs   ║
╚════════════════════════╩═════════════════════════════════════════════╝
```

## Safety Boundaries

Do not provide Claude Design:

- API keys, secrets, credentials, or personal account data.
- Broad repo dumps.
- Large dirty working tree state.
- Runtime/source PNG folders.
- Anything that implies direct sprite mutation or code implementation.

Use Claude Design only for:

- UI structure.
- Information architecture.
- Component ideas.
- Design system seed.
- Prototype screenshots/exports.
- Implementation handoff notes.

## Design Intent

Wevito should feel like a cozy desktop pet simulator that quietly exposes serious helper tools through a cute, understandable surface.

```text
Visible product fantasy
  ├── small animal pets
  ├── simple animal personalities
  ├── moods, needs, care, play
  ├── charming animations
  └── task help feels like pet behavior

Hidden capability layer
  ├── structured command parsing
  ├── task cards
  ├── tool broker
  ├── approvals and guardrails
  ├── audit logs
  ├── sprite proof/apply/rollback
  └── reviewed learning/eval data
```

Do not make the animals look like enterprise assistants. The pets should remain animal-simple. The UI can reveal advanced capability, but the vibe should stay companionable, playful, and safe.

## Prototype Targets

```text
Phase 1B targets
  │
  ├── A. Wevito design-system seed
  │     └── compact desktop pet tool style
  │
  ├── B. Sprite Workflow V2 one-screen console
  │     └── no-scroll sprite proof/review/apply surface
  │
  ├── C. Pet command bar + 3 helper cards
  │     └── named pet task assignment and task cards
  │
  ├── D. Creative Learning Lab dashboard
  │     └── reviewed examples, evals, datasets
  │
  └── E. Implementation handoff
        └── component map and data ownership notes
```

Recommended order:

1. Create design-system seed.
2. Prototype the combined tool dashboard with Sprite Workflow V2 plus pet command bar.
3. Prototype Creative Learning Lab as a second screen or tab.
4. Export screenshots/handoff artifacts.

## Design System Seed

Style direction:

- Friendly pixel-pet desktop companion.
- Clear, compact, no mandatory scrolling for core task flow.
- Warm, practical, readable.
- Not dark sci-fi.
- Not generic purple dashboard.
- Not enterprise SaaS.
- Not toy-only; it should still feel capable and trustworthy.

UI ingredients:

- Compact cards.
- Status chips.
- Clear approval gates.
- Side-by-side visual proof strips.
- Friendly pet helper cards.
- Calm audit trail.
- No hidden work below the fold.
- Strong "nothing mutates until approved" signals.

## Prototype A Prompt: Design System Seed

```text
Create a design system seed for a desktop app called Wevito.

Wevito is a cozy desktop pet simulator that also exposes serious AI helper tools through pet companions. The visible pets should feel like simple animals with readable moods and habits. The hidden tool layer is powerful, but the interface must make every task visible, safe, reversible, and logged.

Style:
- friendly pixel-pet companion
- compact desktop tool
- warm and readable
- not dark sci-fi
- not generic purple SaaS
- not childish despite the pet theme

Design system should include:
- color palette
- typography direction
- cards and panels
- status chips
- task cards
- approval/blocked/running/review states
- pet helper cards
- command bar
- audit log
- visual proof strip styling
- no-scroll workspace rules

Important UX rule:
Core work must fit on one screen without mandatory vertical scrolling.
```

## Prototype B Prompt: Sprite Workflow V2 One-Screen Console

```text
Create a high-fidelity desktop app prototype for Wevito Sprite Workflow V2.

Goal:
Make sprite repair work visible, compact, and understandable without scrolling.

Audience:
A solo developer/player who wants to watch AI-assisted sprite repair happen safely.

Layout:
One screen only. No required vertical scrolling. Use a clear left-to-right workflow:
Queue -> Current Row -> Source/Runtime/Candidate/Proof -> Evidence -> Actions.

Required visible elements:
- concrete target: species, age, gender, color, family, frame ids
- current state: queued, generating, reviewing, applied, awaiting review, failed
- source strip
- current runtime strip
- candidate strip
- animated proof preview
- geometry/hash/provenance panel
- validator findings and visual issue labels
- dry-run apply button
- apply button
- rollback button
- export report button
- human approval gate before mutation
- no hidden work below the fold

Safety rules:
- Do not design a sprite editor that requires painting inside this app.
- The app orchestrates proof, review, apply, rollback, and logs.
- Actual sprite creation/editing happens in external art tools or provider candidate lanes.
- Make it obvious that candidate generated is not finished until reviewed, proven, and applied.
```

## Prototype C Prompt: Pet Command Bar And Three Helper Cards

```text
Design a compact Wevito pet helper dashboard where the user can assign tasks to up to three named pet AI helpers.

Visible concept:
The pets are simple animals on the surface: cute, moody, sleepy, curious, stubborn, playful, shy, or energetic. They are not complex human-like personalities. But behind the scenes, each pet can act as a capable AI helper through structured task cards and safe tools.

Required layout:
- a text command bar near the tool/task-card section
- three active pet helper cards with user-assigned names
- each pet card shows species, mood, helper role, availability, and current task
- examples in the command bar:
  - "Milo, check the sprite audit"
  - "@Bean summarize the latest failed tasks"
  - "Who should handle this?"
- generated task card area
- task status timeline
- approval gate
- audit log

Required states:
- available
- drafting
- waiting for approval
- running
- reviewing
- blocked
- done
- failed

Critical safety message:
Pets suggest and prepare task cards. Tool Broker owns permission and execution. A pet name can route a task, but it cannot bypass approvals, guardrails, or policy.

Tone:
Friendly living desktop companion, not enterprise dashboard.
```

## Prototype D Prompt: Creative Learning Lab

```text
Design a Wevito Creative Learning Lab dashboard for reviewing sprite/art/task examples before they become training or evaluation data.

Goal:
Let Wevito learn from reviewed outcomes without live self-modification.

Show:
- raw examples
- cleaned examples
- accepted/rejected labels
- preference comparisons
- pet command/task outcomes
- reviewer feedback
- dataset bundle status
- eval benchmark status
- export/import controls

Make it obvious:
- nothing enters training unless reviewed and approved
- raw logs do not become training data directly
- evals come before fine-tuning
- sprite/image generation remains candidate-only
```

## Component And Data Ownership Map

| Prototype Component | Data Needed | Owner Later | Notes |
| --- | --- | --- | --- |
| Pet command bar | user text, selected pet, active helper roster | vNext Shell | UI input only; parser creates `TaskIntent`. |
| Pet helper card | `PetActor`, helper role, availability, current task id | vNext Core + Shell | Pet truth from core; helper role/profile from tool layer. |
| Task card | `TaskIntent`, status, approval, result summary | Tool Broker / Task Card Service | Tool execution must not live in pet personality. |
| Tool policy panel | tool id, risk, permissions, approvals | Tool Broker | Required before any execution. |
| Audit log | who/what/when/why/result | Tool Broker | Should be exportable and visible. |
| Sprite queue | species/age/gender/color/family/frame ids | Sprite Workflow V2 | Read-only until apply contract exists. |
| Visual proof strip | source/runtime/candidate/proof paths | Sprite Workflow V2 | No mutation from prototype. |
| Apply controls | dry-run/apply/rollback/report | Sprite Workflow V2 | Disabled unless manifest/proof/rollback available. |
| Learning examples | reviewed outcomes, labels, feedback | Creative Learning Lab | No raw unreviewed logs in training data. |
| Design system | colors, typography, components, layout rules | Claude Design handoff | Implementation remains code-side. |

## Expected Handoff From Claude Design

Claude Design should produce:

- Design-system seed.
- Prototype screenshot or export for Sprite Workflow V2.
- Prototype screenshot or export for pet command bar/tool dashboard.
- Prototype screenshot or export for Creative Learning Lab.
- Component names and layout notes.
- A short explanation of how the one-screen/no-scroll rule is satisfied.
- A short explanation of how approval and blocked states are visible.

Recommended artifact location after export:

```text
C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\design\phase1b-claude-design\
```

## Phase 1B Audit Checklist

Pass criteria:

- Core task flow fits on one screen.
- Pet command bar is visible near tool/task sections.
- Three named helper pets are shown.
- Pets look animal-simple, not human-like office assistants.
- Tool Broker ownership is visible.
- Approval/blocked/review states are visible.
- Sprite mutation controls are gated and reversible.
- Creative Learning Lab shows reviewed data and eval-first learning.
- No runtime/source PNG mutation is implied.
- No code implementation is implied.

Fail criteria:

- Design requires scrolling to see current work.
- Design hides task execution.
- Design makes pets look like unrestricted agents.
- Design implies generated candidates are complete.
- Design bakes sprite editing/generation directly into the UI without review/proof.
- Design treats raw logs as training data.
