# Wevito Sprite Workflow App Thread Handoff Prompt

Date: 2026-05-05

Purpose: single copy-paste prompt for the separate Sprite Workflow App thread after visual-side reviewed Claude Design Phase 1B and wrote the overlay/workbench compatibility specs.

## Copy-Paste Prompt

```text
You are the thread working on the separate Sprite Workflow App for Wevito.

Repo:
C:\Users\fishe\Documents\projects\wevito

Important lane boundary:
The Sprite Workflow App is a separate workbench/tool. It should not replace Wevito's living desktop-pet overlay. The overlay remains the game/home surface; the Sprite Workflow App is the art-production and review console.

Please read these current visual-side docs before making layout or workflow decisions:

1. C:\Users\fishe\Documents\projects\wevito\docs\SPRITE_WORKFLOW_APP_BUILD_SPEC.md
2. C:\Users\fishe\Documents\projects\wevito\docs\SPRITE_WORKFLOW_APP_V1_BACKLOG.md
3. C:\Users\fishe\Documents\projects\wevito\docs\WEVITO_SPRITE_WORKFLOW_V2_OVERLAY_COMPATIBILITY_SPEC_2026-05-05.md
4. C:\Users\fishe\Documents\projects\wevito\docs\WEVITO_CLAUDE_DESIGN_OVERLAY_INTEGRATION_PLAN_2026-05-05.md
5. C:\Users\fishe\Documents\projects\wevito\docs\WEVITO_CLAUDE_DESIGN_VISUAL_TRANSLATION_SPEC_2026-05-05.md
6. C:\Users\fishe\Documents\projects\wevito\docs\WEVITO_VISUAL_STATUS_DASHBOARD_2026-05-05.md

Observed Claude Design screenshots are here:
C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\design\phase1b-claude-design\observed-render-20260505\

Main visual-side request:
Compare the current Sprite Workflow App plan/work-in-progress against Claude's "Sprite Workflow V2 - One-Screen Console" concept. Adopt the useful layout principles only where they fit safely:

- no-scroll selected-row review
- left queue
- selected target row
- source/runtime/candidate/proof strips
- validator findings
- provenance/hash/geometry panel
- dry-run/apply/rollback/export actions
- clear proof-before-mutation gate

Do not turn this into:

- a replacement for the desktop pet overlay
- a broad visual rewrite
- automatic generation/import
- runtime/source PNG mutation
- PET TASKS execution
- Godot packaged proof launcher without code-side coordination

Key UX acceptance criteria:

- A user can tell the selected species/age/gender/color/family within two seconds.
- Source, runtime, candidate, and proof are visually distinct.
- Preview/dry-run cannot be confused with apply.
- Error vs warning states are clear.
- One selected row can be reviewed without mandatory vertical scrolling.
- Ball/drink overlay metadata is visible when relevant.
- Apply/rollback remains gated and auditable.

Please produce a short implementation compatibility plan:

1. what you can adopt immediately from the no-scroll V2 design,
2. what conflicts with current app work,
3. what should be deferred,
4. what files/modules you expect to touch,
5. what validation/screenshot proof you will produce,
6. and a single copy-paste prompt back to the visual-side thread when your review is complete.
```

