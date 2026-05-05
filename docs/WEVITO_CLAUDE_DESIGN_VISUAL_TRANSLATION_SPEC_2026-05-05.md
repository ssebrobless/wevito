# Wevito Claude Design Visual Translation Spec

Date: 2026-05-05

Purpose: translate the rendered Claude Design Phase 1B examples into Wevito-specific UI components while preserving the existing living desktop-pet overlay.

Primary plan: `C:\Users\fishe\Documents\projects\wevito\docs\WEVITO_CLAUDE_DESIGN_OVERLAY_INTEGRATION_PLAN_2026-05-05.md`

Observed screenshots: `C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\design\phase1b-claude-design\observed-render-20260505\`

## Translation Shape

```text
Claude Design artboards
  |
  +-- extract component ideas
  |
  +-- assign to Wevito surface
  |     +-- overlay HUD
  |     +-- tool popup
  |     +-- full workbench
  |     +-- docs/review dashboard
  |
  +-- preserve safety boundary
        +-- report-only
        +-- approval required
        +-- mutation blocked unless explicitly approved
```

## Component Placement

| Component | From Claude example | Wevito target surface | Notes |
| --- | --- | --- | --- |
| Top HUD actions | All examples | Existing overlay HUD | Keep compact. Do not add dashboard chrome to the idle overlay. |
| Pet helper cards | Pet Command Bar | PET TASKS popup | Three slots is enough for v1. Use real pet names later. |
| Command bar | Pet Command Bar | PET TASKS popup | Keep close to current input. Add clearer state labels before adding execution. |
| Task card timeline | Pet Command Bar | PET TASKS popup / report artifact | Good candidate for code-side UI refinement. |
| Tool Broker badge | Sprite Workflow / Pet Command Bar | PET TASKS popup and Sprite Workflow V2 | Should visibly own approval/execution. |
| Source/runtime/candidate strips | Sprite Workflow V2 | Sprite Workflow App / full workbench | Too dense for overlay. |
| Animated proof panel | Sprite Workflow V2 | Sprite Workflow App / proof workbench | Required before future sprite mutation. |
| Validator findings | Sprite Workflow V2 | Sprite Workflow App and visual audit reports | Use existing SpriteIssueKind vocabulary where possible. |
| Provenance/hash panel | Sprite Workflow V2 | Sprite Workflow App / run manifests | Keep plain and inspectable. |
| Dataset bundle cards | Creative Learning Lab | Future Creative Learning Lab | Planning only for now. |
| Eval benchmark bars | Creative Learning Lab | Future Creative Learning Lab | Useful later; no training/export now. |
| Ownership map | Handoff Notes | Code/visual handoff docs | Adopt immediately as coordination format. |

## Overlay HUD Rules

The overlay HUD should remain small, glanceable, and pet-centered.

Keep:

- current pet status
- needs bars or compact need indicators
- primary actions
- tools/settings entry points
- pinned/unfocused behavior
- roaming pet visibility

Avoid:

- full queue tables
- large validator panes
- dense audit logs
- multi-column dashboards
- mandatory scroll areas
- anything that hides the pet world by default

## PET TASKS Popup Rules

PET TASKS can borrow heavily from Claude's Pet Command Bar artboard.

Recommended visible structure:

```text
PET TASKS popup
  |
  +-- helper strip
  |     +-- helper 1
  |     +-- helper 2
  |     +-- helper 3
  |
  +-- command input
  |     +-- PREPARE
  |     +-- PREVIEW
  |
  +-- selected task card
  |     +-- target
  |     +-- adapter
  |     +-- risk
  |     +-- allowed mode
  |     +-- output artifact path
  |
  +-- timeline
  |     +-- drafted
  |     +-- policy checked
  |     +-- preview ready
  |     +-- blocked/cancelled/done
  |
  +-- safety footer
        +-- report-only
        +-- no execution
        +-- no sprite mutation
```

Near-term visual improvements:

- add a persistent `REPORT ONLY` state badge
- make `PREPARE` mean "draft task card"
- make `PREVIEW` mean "create/read non-mutating report"
- show output paths with copy/open affordances
- show blocked reasons in a compact chip
- group audit entries by helper pet

Do not add:

- `EXECUTE`
- sprite apply buttons
- proof-launch controls
- provider generation controls
- hidden automation

## Sprite Workflow V2 Workbench Rules

Claude's first artboard should guide the separate Sprite Workflow App or future full workbench.

Target layout:

```text
Sprite Workflow V2
  |
  +-- left: queue
  |
  +-- center top: selected target
  |
  +-- center middle: visual strips
  |     +-- source
  |     +-- runtime
  |     +-- candidate
  |     +-- proof
  |
  +-- center bottom: validator findings
  |
  +-- right: provenance and actions
        +-- dry-run
        +-- apply
        +-- rollback
        +-- export
```

Important visual rule: all decision-critical information for one selected row should be visible without scrolling. Secondary logs can scroll.

Must include before future mutation:

- source/runtime/candidate frame comparison
- contact sheet or animated proof
- exact target row
- hashes or provenance
- error vs warning classification
- human approval state
- rollback evidence

## Creative Learning Lab Rules

Treat this as a future review dashboard.

Use Claude's third artboard for:

- reviewing examples before they become training/eval data
- accept/reject/revise labels
- preference comparisons
- bundle status
- eval status
- release readiness

Do not use it yet for:

- automatic training
- importing unreviewed examples
- mutating sprite folders
- replacing the Sprite Workflow App

## Visual Style Guidance

Keep from Claude:

- warm dark-brown workbench surfaces
- pixel-pet accents
- compact cards
- status chips
- visible safety labels
- monospace technical evidence where useful

Revise before implementation:

- increase contrast on small text
- reduce decorative density in PET TASKS
- make disabled controls visually clear
- keep buttons larger than the tiny prototype labels
- ensure all pet/tool icons sit on integer pixels
- avoid making the main overlay feel like a full dashboard

## Function Roadmap

```text
safe now
  |
  +-- localDocs preview
  +-- spriteAudit preview
  +-- link bin
  +-- care/action controls
  +-- report-only proof summaries

next after UI refinement
  |
  +-- contact-sheet output in spriteAudit
  +-- proofCapture report-only adapter
  +-- better artifact open/copy controls

later with explicit approval
  |
  +-- Godot packaged proof launch
  +-- vNext proof capture
  +-- manual candidate apply/proof wrappers

blocked until coordinated
  |
  +-- visual generation from PET TASKS
  +-- sprite import from PET TASKS
  +-- runtime/source PNG mutation
  +-- prop-anchor edits
  +-- all-color propagation
```

## Code-Side Coordination Questions

Before implementation, code-side should answer:

1. Should PET TASKS refinement happen in the current `ToolPopupWindow` or a separate popup view?
2. Can the current popup support helper cards without covering too much of the pet overlay?
3. Should Sprite Workflow V2 remain entirely separate from the vNext Shell?
4. Which artifact paths should get `Open` and `Copy Path` affordances first?
5. Can report-only `spriteAudit` add contact sheets without destabilizing the current preview adapter?

## Visual-Side Next Work

Continue visual-side work in this order:

1. preserve Claude render screenshots and docs
2. prepare code-side handoff prompt when requested
3. continue targeted asset cleanup only when a concrete issue is found
4. keep color variant coverage green
5. coordinate optional animation expansion with code-side apply/proof gates
6. plan Clipboard Shelf and other useful pet functions as overlay-native tools

