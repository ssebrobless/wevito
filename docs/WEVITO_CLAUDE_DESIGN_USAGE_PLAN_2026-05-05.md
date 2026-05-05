# Wevito Claude Design Usage Plan

Date: 2026-05-05

Purpose: define how Claude Design should fit into Wevito without confusing it with sprite generation, code implementation, or runtime asset mutation.

This is a planning document only. It does not authorize generating art, importing sprites, mutating runtime PNGs, or changing game code.

## Recommended Role

```text
╔══════════════════════════════════════════════════════════════════════╗
║ Claude Design Lane                                                  ║
╠════════════════════════╦═════════════════════════════════════════════╣
║ Best for               ║ UI/UX prototypes, design systems, screens  ║
║ Not for                ║ Sprite PNG edits, code truth, asset apply   ║
║ Input                  ║ screenshots, selected docs, focused prompts ║
║ Output                 ║ prototypes, design handoffs, screenshots    ║
║ Implementation owner   ║ Codex/code-side after human review          ║
╚════════════════════════╩═════════════════════════════════════════════╝
```

Claude Design should be used as a compact prototyping and information-architecture tool. It is especially useful for the problems the current sprite workflow exposed: too much scrolling, cramped editing windows, unclear state, and no visible proof that work is happening.

It should not become another autonomous production surface. The safest lane is: prototype first, review visually, then implement manually in Wevito or Sprite Workflow V2.

## What The Current Window Gives Us

The screenshot shows access to:

- Prototype creation.
- Slide deck creation.
- Template-based projects.
- High fidelity and wireframe modes.
- Organization-level design systems.
- A published default design system.

That is enough to create design artifacts for Wevito UI and workflow surfaces without connecting directly to production code.

## Research Summary

Anthropic's Claude Design support docs describe it as a research-preview feature for creating designs, interactive prototypes, presentations, and related artifacts through conversation with Claude. The documented workflow is: create a project, add relevant context such as screenshots/assets/codebase references, describe the desired output, review the canvas, iterate with chat or inline comments, then export/share.

Relevant support docs:

- `https://support.claude.com/en/articles/14604416-get-started-with-claude-design`
- `https://support.claude.com/en/articles/14604397-set-up-your-design-system-in-claude-design`

Important constraints from the docs:

- It is a research preview.
- Inline comments can occasionally disappear, so critical feedback should also be pasted into chat.
- Large codebases can cause lag or browser issues, so Wevito should provide focused docs/screenshots rather than the whole repo.
- Design systems can be created from codebases, screenshots, slide decks, documents, logos, palettes, typography specs, and other product references.

## Best Wevito Uses

```text
Wevito Need
  │
  ├── Sprite Workflow V2
  │     └── one-screen no-scroll repair console
  │
  ├── vNext Shell Cleanup
  │     └── compact pet status, actions, settings, task log
  │
  ├── Tool Broker
  │     └── visible permissioned task cards and audit trail
  │
  ├── Pet Agent Pilot
  │     └── friendly task assignment, waiting, review, failed states
  │
  ├── Creative Learning Lab
  │     └── reviewed examples, dataset bundles, eval dashboards
  │
  └── Habitat/Visual QA
        └── contact-sheet review, issue labels, before/after proofs
```

The first prototype should be Sprite Workflow V2 because it directly addresses the user's strongest pain: the app felt messy, scroll-heavy, cramped, and invisible while work was supposedly happening.

## Do Not Use Claude Design For

- Direct sprite PNG generation.
- Runtime asset import.
- Applying candidates into `sprites_runtime`.
- Editing Godot scenes or C# source as the source of truth.
- Uploading secrets, API keys, personal accounts, or broad private directories.
- Large repo ingestion unless a very small subdirectory or curated file bundle is enough.
- Browser automation of a logged-in Claude account unless the user explicitly asks for that exact action.

## Proposed Phase 1B Lane

Phase 1B should happen after contract planning and before UI-heavy implementation.

```text
Phase 1 contracts
  │
  ▼
Phase 1B Claude Design prototypes
  │
  ├── Wevito design system seed
  ├── Sprite Workflow V2 no-scroll prototype
  ├── pet task-card/tool-broker prototype
  ├── Creative Learning Lab prototype
  └── reviewed handoff artifacts
  │
  ▼
Phase 3+ implementation
```

Deliverables:

- Claude Design project names and export locations.
- Screenshots or standalone HTML/PDF exports under a design artifact folder.
- A small implementation handoff doc per prototype.
- A component map: screen, component, data needed, implementation owner.
- A pet command bar concept for up to three named helper pets near the tool/task-card section.

Recommended artifact location:

```text
C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\design\
```

## First Claude Design Prompt

Use this when we are ready to create the first prototype:

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
- source strip, current runtime strip, candidate strip, animated proof preview
- geometry/hash/provenance panel
- validator findings and visual issue labels
- dry-run apply, apply, rollback, export report actions
- human approval gate before mutation
- no hidden work below the fold

Style direction:
Friendly pixel-pet desktop tool, clear and calm, not dark sci-fi, not purple default.
Use compact cards, strong hierarchy, readable status chips, and a visible audit trail.

Constraints:
Do not design a sprite editor that requires painting inside this app.
The app should orchestrate proof, review, apply, rollback, and logs.
Actual sprite creation/editing happens in external art tools or provider candidate lanes.
```

## Follow-Up Claude Design Prompts

Pet task-card/tool-broker prototype:

```text
Design a compact Wevito pet helper dashboard where a desktop pet can suggest or run approved helper tasks.

Show:
- a text bar near the tool/task-card section where the user can assign a task to a named pet
- three active helper pet cards with user-assigned names, helper roles, moods, and availability
- pet mood/personality state
- task cards with requested action, tools required, risk level, approval state
- waiting, reviewing, failed, success, and cancelled states
- audit log
- clear boundaries between suggestion, approval, and execution

The text bar should support examples like "Milo, check the sprite audit" and "@Bean summarize the latest failed tasks." Make it obvious that pets prepare task cards, but Tool Broker owns permission and execution.

The design should feel like a friendly living desktop companion, not an enterprise dashboard.
```

Creative Learning Lab prototype:

```text
Design a Wevito Creative Learning Lab dashboard for reviewing sprite/art examples before they become training or evaluation data.

Show:
- raw examples
- cleaned examples
- accepted/rejected labels
- preference comparisons
- dataset bundle status
- eval benchmark status
- export/import controls

Make it obvious that nothing enters training unless it is reviewed and approved.
```

## Practical Recommendation

Use Claude Design soon, but only for the first Sprite Workflow V2 prototype. That gives us a fast way to fix the UI/organization problem before implementing another tool surface.

Do not connect it to the full repo yet. Start with screenshots, the relevant docs, and the focused prompt above.
