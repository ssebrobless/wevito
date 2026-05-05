# Wevito Pet Helper Functions Roadmap

Date: 2026-05-05

Purpose: define useful pet-helper functions that fit Wevito's desktop-pet fantasy while respecting current code-side safety boundaries.

This roadmap is visual/product planning. It does not authorize PET TASKS execution adapters, sprite mutation, browser automation, generation, imports, prop-anchor edits, or runtime/source PNG rewrites.

## Design Principle

Pets should feel like pets first and helpers second.

```text
bad shape
  |
  +-- pet becomes a generic enterprise assistant
  +-- overlay becomes a dashboard
  +-- tools hide the living pet experience

good shape
  |
  +-- pet stays visible in the desktop world
  +-- helper work appears as pet behavior
  +-- serious tools remain visible, safe, and reversible
  +-- every external action has a clear approval gate
```

## Function Tiers

```text
Tier 0 - existing / safe
  |
  +-- care actions
  +-- link bin
  +-- PET TASKS localDocs preview
  +-- PET TASKS spriteAudit preview

Tier 1 - next safe visual/product work
  |
  +-- better report summaries
  +-- contact sheets in spriteAudit reports
  +-- Clipboard Shelf planning
  +-- proof-summary reader
  +-- visual QA queue summaries

Tier 2 - coordinated implementation
  |
  +-- report-only proofCapture
  +-- artifact open/copy controls
  +-- PET TASKS helper timeline
  +-- Sprite Workflow V2 row links

Tier 3 - explicit approval only
  |
  +-- Godot packaged proof launch
  +-- candidate apply/proof wrapper
  +-- provider generation handoff
  +-- sprite import
  +-- runtime/source mutation
```

## Helper Function Catalog

| Function | Pet expression | User value | First safe implementation |
| --- | --- | --- | --- |
| Local docs lookup | pet fetches a note | Find project docs without hunting. | PET TASKS `localDocs`, report-only. |
| Sprite audit | pet inspects a sheet | Identify visual or contract concerns. | PET TASKS `spriteAudit`, markdown/JSON first. |
| Sprite contact sheet | pet lays out samples | Visual review at a glance. | Add contact-sheet output to report-only audits. |
| Proof summary | pet points at a proof folder | Understand latest proof result. | Read existing `proof-summary.md` / reports. |
| Link bin | pet keeps links in a basket | Save/open useful URLs. | Existing webtools link bin. |
| Clipboard Shelf | pet holds snippets | Save chosen text snippets safely. | New manual-save webtool, no passive logging. |
| Care reminder | pet nudges about needs | Keep pets alive/healthy. | Existing needs/action state, visual refinement. |
| Medicine/care guide | pet points to correct care item | Make care items understandable. | Local docs/content lookup first. |
| Visual QA queue | pet waits by review pile | Know what art still needs review. | Read visual-review artifacts. |
| Optional animation status | pet tracks family progress | See what optional art is ready. | Read manifests/checklists, no apply. |
| Work companion state | pet waves/waits/reviews | Make helper state visible. | Existing companion states, visual mapping. |

## Clipboard Shelf Concept

The Clipboard Shelf is the most useful next webtool concept after the link bin.

```text
Clipboard Shelf
  |
  +-- user explicitly saves a snippet
  |
  +-- shelf item
  |     +-- title
  |     +-- text preview
  |     +-- source label if provided
  |     +-- timestamp
  |     +-- tags
  |
  +-- actions
        +-- copy
        +-- open source when applicable
        +-- delete
        +-- clear
```

Safety rules:

- no passive clipboard scraping
- no automatic saving of sensitive content
- no background upload
- clear delete controls
- visible item count
- user approval before any external transmission

Overlay expression:

- a small shelf or basket icon in TOOLS
- a pet may "hold" one saved snippet visually
- full text appears only in the popup

## Sprite Audit Contact Sheets

Visual-side wants `spriteAudit` to eventually produce contact sheets when useful.

Minimum useful sheet:

```text
contact sheet
  |
  +-- target row label
  +-- frame thumbnails
  +-- issue chips
  +-- source/runtime/candidate labels when applicable
  +-- hash unchanged note when report-only
```

This should remain report-only. It can write new artifacts but must not rewrite source/runtime assets.

## Proof Summary Reader

Useful for code-side/manual proof flows.

Input examples:

```text
Bean, summarize the latest goose hold_ball proof
Pip, review the drop_ball candidate manifest
Nix, what changed in the last visual-review folder?
```

Allowed behavior:

- read existing markdown/JSON artifacts
- summarize pass/fail
- list changed files
- list proof screenshots/contact sheets
- warn when proof is missing

Blocked:

- rerun proof
- apply candidate
- rollback
- overwrite frames

## Medicine And Care Visual Functions

The user specifically called out medicine/care assets as important. The pet-helper layer can make them easier to understand without new code-heavy automation.

Near-term visual plan:

- keep care item icons clean and readable
- ensure medicine, syringe, grooming, bath, food, water, rest, and play assets remain visually distinct
- create care-item review sheets when new items are added
- map care items to pet needs/statuses in docs before adding new UI

Possible future helper:

```text
"Bean, what does this pet need?"
  |
  +-- reads current needs/status
  +-- suggests care actions
  +-- never administers care without user click/approval
```

## Work Companion States

Existing companion state direction remains useful:

| State | Visual meaning | Fallback |
| --- | --- | --- |
| Waving | pet noticed / greeting / task ready | happy/idle |
| Jumping | excited / success / playful work | happy/play |
| Failed | task failed or blocked | sad/idle |
| Waiting | awaiting approval or report | idle |
| Review | report/proof ready | idle/happy |

State use should not override explicit pet actions like eating, drinking, playing, or sleeping.

## Function Implementation Order

Recommended order:

1. PET TASKS visual refinement with report-only badge.
2. Artifact open/copy controls.
3. `spriteAudit` contact-sheet planning and later implementation.
4. Proof summary reader for existing artifacts.
5. Clipboard Shelf spec.
6. Care/medicine item visual mapping.
7. Creative Learning Lab planning.
8. Manual proof/apply workflow wrappers only after code-side asks for that lane.

## Hard Boundaries

- Pets can suggest; Tool Broker approves.
- PET TASKS can preview reports; it cannot mutate.
- Clipboard Shelf saves only explicit user-selected content.
- Sprite Workflow V2 owns proof/apply UI.
- Godot packaged proof remains manual/code-side until explicitly expanded.
- Ball/drink props remain runtime overlays unless a future policy changes that.

