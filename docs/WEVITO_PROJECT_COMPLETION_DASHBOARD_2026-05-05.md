# Wevito Project Completion Dashboard

Last updated: 2026-05-12

Purpose: living project dashboard for tracking Wevito's game, visual, tool, helper-agent, and release readiness work. Update this markdown file as phases complete, then regenerate the PDF reference copy.

## Current Overall Estimate

```text
Wevito full vision
|-- playable pet game foundation        #########-  ~88%
|-- visual/sprite asset lane            ########--  ~78%
|-- habitat/items/care integration      #########-  ~88%
|-- PET TASKS/tool hub                  ########--  ~82%
|-- advanced helper/AI/ML systems       #######---  ~70%
`-- final QA/release readiness          #########-  ~90%

Total project completion: ~84%
Remaining to complete full vision: ~16%
```

This estimate treats "everything done" as the expanded full Wevito vision: a working desktop pet game, clean sprites and animations, habitats, items, helper tools, screenshot/capture, translation, audio assist, coding helpers, Sprite Workflow V2, Creative Learning Lab, and pet-agent behavior.

If measuring only the basic playable pet-game foundation, Wevito is closer to 90%. The remaining gap is mostly manual player-facing QA, visual/content polish, optional animation coverage, and deliberately gated AI/model/tool execution.

## Area Status

| Area | Status | Done | What Remains |
| --- | --- | ---: | --- |
| Git/build baseline | Public RC3 green | 98% | Keep `main` clean, avoid asset prep unless intentionally approved, finish manual QA before stable tag. |
| Core pet sim | Strong foundation | 88% | Continue long-run balance tuning, visible care outcomes, and save/load edge proof. |
| Game interactions | Functional foundation | 82% | Fetch/drink release scenarios pass; finish richer care behavior and manual UX proof. |
| Sprites/visuals | Runtime contract green, content still active | 78% | Final in-motion QA, optional animations, no artifacts, no boxing/shrink-grow, final proof sweeps. |
| Color variants | Mostly green | 85% | Only targeted review/fixes, not broad regeneration. |
| Care/medicine/items | Mapped into tool/game plans | 72% | Confirm readability in runtime UI and remove stale expectations. |
| Habitat/environment | Manifest-backed and proofed | 90% | Richer enter/perch/hide/use behavior and final visual polish. |
| Overlay UI | Functional and resilient | 82% | Continue simplifying PET TASKS/task-card UX without replacing pet overlay. |
| PET TASKS/tool hub | Probe-green seed exists | 84% | More artifact buttons, clearer reports, and approval-gated execution polish. |
| Screenshot/capture tools | Probe and capture pass | 76% | Short proof clips and broader capture UX remain later work. |
| Translation/audio tools | Implemented behind safety posture | 74% | Provider polish, safe Windows volume UX, and external booster handoff. |
| Coding helpers | Report/plan surfaces working | 78% | Keep mutation approval-gated; add richer patch-review output later. |
| Sprite Workflow V2 | V2 apply/proof pattern exists | 76% | Expand from proven one-row pilots to broader guarded workflows. |
| Creative Learning Lab | Planned with seams | 68% | Reviewed examples, preference memory, dataset/export dashboard, no uncontrolled training. |
| Pet AI agents | Default-disabled model seam exists | 72% | Keep live calls gated; complete memory/vector and consent reviews later. |
| Final release QA | Public RC3 clean validation green | 90% | Run manual player-facing QA, fix focused findings, then decide stable release promotion. |

## C-PHASE 29 Release-Candidate Update

```text
final QA sweep
|-- build/test/publish             PASS
|-- runtime canvas contract        PASS, 0 mixed rows
|-- sprite contract audit          PASS, 0 errors
|-- optional readiness             PASS, 0 invalid optional art
|-- PET TASKS probes               PASS
|-- pinned overlay foreground test PASS
`-- visual capture                 PASS
```

Primary C-PHASE 29 artifact: `docs/C_PHASE29_FINAL_QA_2026-05-11.md`.

Important release note: optional animation readiness still reports most optional targets as fallback-only. This is safe for the C-PHASE 29 release-candidate sweep because invalid optional art is zero, but it remains a visual/content completion item.

## C-PHASE 30 Release Gate Update

```text
release gate
|-- vNext Release build             PASS
|-- vNext Release tests             PASS, 278/278
|-- vNext Release publish           PASS with -SkipAssetPrep
|-- user help guide                 DRAFTED
|-- Godot desktop export            BLOCKED, importer/export timeout
`-- release tag                     NOT CREATED
```

Primary C-PHASE 30 artifacts:

- `docs/C_PHASE30_RELEASE_BLOCKER_2026-05-12.md`
- `docs/WEVITO_USER_HELP_GUIDE_2026-05-05.md`

Historical decision: tag `v0.1.0-vnext-rc1` as a vNext-only release candidate from the validated `-SkipAssetPrep` package, then defer Godot export hardening to the next focused phase.

This was superseded by the later desktop RC line below.

## C-PHASE 35-38 Desktop RC Update

```text
desktop RC line
|-- RC1              SUPERSEDED, missing-script packaging issue
|-- RC2              SUPERSEDED, fetch visual-completion issue
`-- RC3              CURRENT VALIDATED BASELINE
    |-- clean download               PASS
    |-- bundle structure             PASS
    |-- full automation              PASS
    |-- drink forced scenario        PASS
    |-- fetch forced scenario        PASS
    `-- app-rendered goose proof     PASS
```

Primary artifacts:

- `docs/C_PHASE35_MANUAL_RC_QA_AND_RC2_FIX_2026-05-12.md`
- `docs/C_PHASE36_RC2_CLEAN_SMOKE_2026-05-12.md`
- `docs/C_PHASE37_RELEASE_FACING_QA_2026-05-12.md`
- `docs/C_PHASE38_RC3_CLEAN_VALIDATION_2026-05-12.md`
- `docs/C_PHASE39_RELEASE_QA_AND_PRODUCT_POLISH_BACKLOG_2026-05-12.md`

Current release recommendation: keep `v0.1.0-desktop-rc3` as the validated prerelease baseline, run the C-PHASE 40 manual player-facing QA packet, then decide whether to promote the current RC, publish a later RC, or hold for fixes.

## Remaining Work By Category

### Game Core

```text
game core
|-- simulation
|   |-- personality development
|   |-- needs/drives/emotions
|   |-- affection/relationships
|   |-- health/body condition
|   |-- aging/death/ghost state
|   `-- save/load migrations
|
|-- interactions
|   |-- feeding
|   |-- drinking
|   |-- medicine/care
|   |-- bathing/grooming
|   |-- sleep/rest
|   `-- ball play/fetch sequence
|
`-- proof
    |-- forced dev scenarios
    |-- UI/control verification
    `-- in-game interaction sweeps
```

Priority: high.

Main reason: this is the actual game spine. Visuals and tools become easier to validate once pet behavior is deterministic and visibly connected to actions.

### Visuals And Animation

```text
visual lane
|-- keep
|   |-- current color folder coverage
|   |-- cleaned shared assets
|   |-- runtime overlay prop contract
|   `-- visual-side review discipline
|
|-- finish
|   |-- final in-motion sprite QA
|   |-- optional ball animations
|   |-- snake/frog/bird motion quality
|   |-- size consistency
|   |-- no fake PNG backgrounds
|   |-- no holes/noise/edge residue
|   `-- final color variant acceptance
|
`-- defer until base quality is stable
    |-- ghost variants
    |-- fat/skinny variants
    `-- broad new generation
```

Priority: high, coordinated with visual-side.

Main reason: sprite quality is the most visible product risk. Runtime contracts are green, but final in-motion visual proof still matters.

### Habitat, Items, And Environment

```text
world/content
|-- care/medicine mapping
|-- food/water/toy mapping
|-- egg lifecycle assets
|-- habitat object loadouts
|-- runtime interaction anchors
|-- depth bands and occlusion
|-- contact shadows
`-- stale asset reference cleanup
```

Priority: medium-high.

Main reason: this turns Wevito from "pets on a screen" into pets that live in a readable world.

### Overlay UI And Tool Hub

```text
overlay/tool hub
|-- overlay remains home
|-- summoned tool surfaces
|-- PET TASKS command bar
|-- three helper pets
|-- task cards
|-- result cards
|-- artifact buttons
|-- report-only labels first
`-- approval gates before execution
```

Priority: high for code-side.

Main reason: this is the bridge between the pet sim and the powerful helper-tool fantasy. It must stay simple and should not hijack the user's foreground app.

### Helper Tools

```text
helper tools
|-- safe/report-only first
|   |-- localDocs
|   |-- spriteAudit
|   |-- assetInventory
|   |-- petState
|   |-- codeReview
|   `-- codePatchPlan
|
|-- approval-gated next
|   |-- buildProof
|   |-- Wevito-window screenshot
|   |-- selected-region screenshot
|   |-- translation execution
|   `-- safe Windows volume control
|
`-- later / high-risk
    |-- screen recording
    |-- browser automation
    |-- external audio boosters
    |-- sprite generation/import
    `-- mutation/apply workflows
```

Priority: medium-high.

Main reason: tools make Wevito meaningfully useful beyond a game, but unsafe automation too early would create chaos.

### Sprite Workflow V2

```text
Sprite Workflow V2
|-- one selected row visible at a time
|-- source/runtime/candidate/proof strips
|-- validator findings
|-- provenance and hashes
|-- no-mutation preview mode
|-- one-row apply plan
|-- backup-before-apply
|-- exact rollback
`-- post-apply proof packet
```

Priority: medium.

Main reason: this should eventually replace the confusing external sprite workflow experience, but it should start read-only and compact.

### Creative Learning Lab And AI Agents

```text
AI/helper-agent lane
|-- exactly three active helper pets
|-- names/roles assigned by user
|-- task bar accepts natural language
|-- pet personality affects presentation, not permissions
|-- reviewed examples become learning data
|-- preference snapshots
|-- artifact-backed memory
|-- local/vector search later
`-- no uncontrolled self-training
```

Priority: medium-low until tool foundations are stable.

Main reason: this is exciting, but it only becomes trustworthy after artifacts, permissions, and rollback rules are solid.

## Recommended Execution Order

```text
next best sequence
|-- 1. tag v0.1.0-vnext-rc1 from main after PR #33 merges
|-- 2. harden Godot export or move runtime sprites outside Godot import pressure
|-- 3. let visual-side finish targeted sprite/animation QA
|-- 4. finish action/animation/prop contract polish
|-- 5. implement staged fetch/drink/care proof flows
|-- 6. map care/items/habitats into runtime UI
|-- 7. add safe helper tools one at a time
|-- 8. build Sprite Workflow V2 read-only workbench
|-- 9. add controlled apply/proof/rollback gates
`-- 10. add Creative Learning Lab reviewed-data surface
```

## Current Source-Of-Truth Docs

Use these documents when updating this dashboard:

- `docs/C_PHASE29_FINAL_QA_2026-05-11.md`
- `docs/C_PHASE30_RELEASE_BLOCKER_2026-05-12.md`
- `docs/C_PHASE30_VNEXT_RC_RELEASE_DECISION_2026-05-12.md`
- `docs/WEVITO_USER_HELP_GUIDE_2026-05-05.md`
- `docs/CLAUDE_FULL_WEVITO_IMPLEMENTATION_PLAN_2026-05-05.md`
- `docs/CLAUDE_CODEX_MEDIUM_PHASE_PROMPTS_2026-05-05.md`
- `docs/WEVITO_FULL_PROJECT_STATUS_AND_COMPLETION_ROADMAP_2026-05-05.md`
- `docs/WEVITO_VISUAL_COMPLETION_TRACKER_2026-05-05.md`
- `docs/WEVITO_VISUAL_STATUS_DASHBOARD_2026-05-05.md`
- `docs/WEVITO_TOOL_AGENT_UI_MASTER_PLAN_2026-05-05.md`
- `docs/WEVITO_PET_HELPER_FUNCTIONS_ROADMAP_2026-05-05.md`
- `docs/WEVITO_OVERLAY_FIRST_IMPLEMENTATION_ROADMAP_2026-05-05.md`
- `docs/CODE_SIDE_DEEP_REVIEW_AND_AGENT_TOOLS_PLAN_2026-05-05.md`

## Update Workflow

1. Edit this markdown file when a phase completes or a percentage changes.
2. Regenerate the quick PDF snapshot:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\export-project-dashboard-pdf.ps1
```

3. Regenerate the polished PDF packet:

```powershell
python .\tools\export-project-dashboard-packet-pdf.py
```

4. Confirm the PDFs exist at:

```text
docs/WEVITO_PROJECT_COMPLETION_DASHBOARD_2026-05-05.pdf
docs/WEVITO_PROJECT_COMPLETION_DASHBOARD_PACKET_2026-05-05.pdf
```

5. If the dashboard becomes materially outdated, create a new dated copy instead of overwriting historical context.
