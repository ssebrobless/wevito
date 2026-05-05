# Wevito Sprite Workflow V2 Overlay Compatibility Spec

Date: 2026-05-05

Purpose: define how Claude Design's "Sprite Workflow V2 - One-Screen Console" should influence the separate Sprite Workflow App/workbench without replacing Wevito's desktop-pet overlay.

This is a visual-side planning document. It does not authorize sprite import, generation, runtime/source PNG mutation, or work in the separate Sprite Workflow App without coordination with that thread.

## Main Decision

Sprite Workflow V2 should be a summoned workbench, not the primary Wevito overlay.

```text
desktop pet overlay
  |
  +-- quick actions
  +-- tools
  +-- PET TASKS report-only popup
  |
  +-- opens / references
        |
        v
Sprite Workflow V2 workbench
  |
  +-- queue
  +-- selected row
  +-- proof evidence
  +-- apply gate
  +-- rollback evidence
```

The overlay remains the living game surface. Sprite Workflow V2 is the serious art-production console.

## What To Borrow From Claude Design

| Claude element | Keep? | Wevito interpretation |
| --- | --- | --- |
| Left queue column | Yes | Required for batch clarity. |
| Selected row header | Yes | Must show species/age/gender/color/family/frame ids. |
| Source strip | Yes | Shows original/reference frames or boards. |
| Runtime strip | Yes | Shows current live runtime frames. |
| Candidate strip | Yes | Shows proposed frames, never assumed accepted. |
| Proof preview | Yes | Animated or contact-sheet proof before mutation. |
| Validator findings | Yes | Error/warning distinction. |
| Provenance panel | Yes | Manifest, provider/source, hashes, geometry. |
| Actions panel | Yes | Dry-run/apply/rollback/export, but gated. |
| Tool Broker badge | Yes | Permission owner, not decoration. |
| Dense tiny text everywhere | Revise | Increase readability before implementation. |

## Target One-Row Layout

```text
┌────────────────────────────────────────────────────────────────────────────┐
│ Sprite Workflow V2                                                         │
├──────────────┬───────────────────────────────────────────┬─────────────────┤
│ Queue        │ Current Row                               │ Actions         │
│              │ species / age / gender / color / family   │                 │
│ queued       ├───────────────────────────────────────────┤ approval gate   │
│ reviewing    │ Source                                    │ dry-run         │
│ applied      │ Runtime                                   │ apply           │
│ failed       │ Candidate                                 │ rollback        │
│              │ Proof                                     │ export report   │
├──────────────┴───────────────────────────────────────────┼─────────────────┤
│ Validator findings                                       │ Provenance      │
└──────────────────────────────────────────────────────────┴─────────────────┘
```

Core rule: one selected row should be reviewable without mandatory scrolling.

## Required Evidence Before Any Apply

Every mutation-capable flow must show:

- exact target row
- backup/hash status
- source/runtime/candidate frame comparison
- candidate geometry
- runtime overlay policy, if relevant
- validator errors and warnings
- packaged proof or accepted proof plan
- rollback path
- human approval state

For ball/drink families:

- props remain runtime overlays unless a future policy explicitly says otherwise
- `prop_anchors.json` metadata use must be visible in proof notes
- contact sheets should label raw sprite vs overlay approximation

## Compatibility With Current Visual Work

Current visual-side state:

- all color variant folder coverage is complete across the four main sprite roots
- shared runtime/source cleanup has been performed
- remaining visual cleanup should be targeted and evidence-backed
- optional animation apply/proof remains code-side/manual

Sprite Workflow V2 should not introduce a broad "fix everything" button.

## Relationship To PET TASKS

PET TASKS can prepare or summarize reports.

PET TASKS should not run Sprite Workflow V2 mutation workflows yet.

```text
PET TASKS
  |
  +-- "review goose baby female blue sprites"
  |     +-- creates report-only spriteAudit
  |
  +-- "summarize failed proof"
  |     +-- reads existing artifacts
  |
  +-- blocked for now
        +-- apply candidate
        +-- import sprite
        +-- generate frames
        +-- launch Godot proof
```

Future connection:

- PET TASKS may link to a Sprite Workflow V2 row.
- Sprite Workflow V2 owns the proof/apply UI.
- Tool Broker owns permission and execution.

## Separate Sprite Workflow App Coordination

The other thread owns the Avalonia/.NET Sprite Workflow App work. This visual-side spec should be handed to that thread before any UI refactor.

Recommended ask for that thread:

1. Compare current app layout with Claude's no-scroll console.
2. Identify what can be adopted without destabilizing current work.
3. Keep the app as a workbench, not an overlay replacement.
4. Preserve clear proof/apply/rollback gates.
5. Avoid implementing direct generation/import until the manifest/provenance workflow is settled.

## Visual Acceptance Criteria

- The user can tell what row is selected within two seconds.
- Source/runtime/candidate/proof are visually distinct.
- Apply controls cannot be mistaken for preview controls.
- Warnings do not look like blockers.
- Blockers do not look like optional notes.
- A row can be reviewed without vertical scrolling.
- Proof outputs include overlay metadata when props are involved.
- The design can coexist with the desktop-pet overlay.

## Not In Scope

- editing pixel art by hand inside the app
- replacing Godot packaged proof
- replacing code-side apply/rollback ownership
- mutating `sprites_runtime`
- mutating source boards
- generating new optional families
- all-color propagation

