# Wevito Project Completion Dashboard

Last updated: 2026-05-12

Purpose: living project dashboard for tracking Wevito's game, visual, tool, helper-agent, and release readiness work. This file should stay as the quick percentage dashboard; deeper evidence lives in the phase reports listed below.

## Current Overall Estimate

```text
Wevito full vision after stable v0.1.0
|-- stable desktop release              ########## 100%
|-- playable pet game foundation        #########-  ~93%
|-- release/build/QA infrastructure     #########-  ~95%
|-- runtime sprite structure/contract   #########-  ~92%
|-- visual/content quality full vision  #######---  ~72%
|-- optional action animation coverage  #---------  ~8%
|-- habitat/items/care integration      ########--  ~84%
|-- PET TASKS/tool hub                  ########--  ~82%
|-- screenshot/translation/audio tools  #######---  ~74%
|-- advanced helper/AI/ML systems       ######----  ~60%
|-- Sprite Workflow V2 / Creative Lab   ######----  ~62%
`-- full expanded Wevito vision         ########--  ~84%

Remaining to complete full expanded vision: ~16%
```

The basic shipped desktop pet is complete enough for stable `v0.1.0`. The remaining work belongs to the expanded Wevito vision: richer optional animations, source-faithful visual cleanup, safer and simpler tool execution, AI-helper consent/memory, Sprite Workflow V2, and the Creative Learning Lab.

## Current Release Baseline

```text
stable release
|
+-- tag: v0.1.0-desktop
+-- release: https://github.com/ssebrobless/wevito/releases/tag/v0.1.0-desktop
+-- asset: WevitoDesktopPet-v0.1.0-desktop-win64.zip
+-- sha256: c12a61d0ddd55bfa611bc14480dd298584e2e9d3849ea8e01064beb71c4efacc
+-- source candidate: v0.1.0-desktop-rc4
+-- target commit: 97285ad9887423fc42cc3b562087180ff0d8f90e
`-- status: stable shipped
```

Stable release evidence:

- `docs/C_PHASE49_RC4_PACKAGE_AND_CLEAN_VALIDATION_2026-05-12.md`
- `docs/C_PHASE50_RC4_MANUAL_PLAYER_QA_2026-05-12.md`
- `docs/C_PHASE52_STABLE_RELEASE_PROMOTION_2026-05-12.md`
- `docs/C_PHASE53_PROJECT_PROGRESS_REPORT_2026-05-12.md`

## Area Status

| Area | Status | Done | What Remains |
| --- | --- | ---: | --- |
| Stable desktop release | Shipped | 100% | Protect the shipped baseline; future changes should be phased and proven. |
| Git/build baseline | Green | 98% | Keep `main` clean; use PRs and avoid accidental asset-prep regeneration. |
| Core pet sim | Strong foundation | 93% | Long-run balance, richer personality development, visible care outcomes, aging/death/ghost depth. |
| Game interactions | Stable functional set | 88% | More species-specific action readings and richer toy/care behavior. |
| Runtime sprite contract | Structurally green | 92% | Preserve 0 mixed-canvas/missing/invalid rows while improving source/provenance depth. |
| Sprites/visual quality | Stable-safe, not final vision | 72% | Final in-motion QA, optional animations, authored-source completion, no artifact regressions. |
| Optional action animations | Mostly fallback-only | 8% | 2516/2520 optional targets remain fallback-only; expand with proof/rollback gates. |
| Color variants | Structurally present | 85% | Targeted color QA only; no broad all-color mutation without proof plan. |
| Care/medicine/items | Mapped and usable | 84% | Runtime readability polish and more visible care-state feedback. |
| Habitat/environment | Manifest-backed and proofed | 88% | Richer per-species enter/perch/hide/use behavior and final composition polish. |
| Overlay UI | Functional and resilient | 84% | Continue simplifying PET TASKS/task-card UX without replacing the pet overlay. |
| PET TASKS/tool hub | Report/preview foundation | 82% | Simpler one-entry task flow, richer artifact buttons, adapter proof packets. |
| Screenshot/capture tools | Implemented cautiously | 74% | More proof packets; keep screen recording gated. |
| Translation/audio tools | Implemented behind safety posture | 74% | Provider clarity, safe Windows volume UX, no hidden third-party booster control. |
| Coding helpers | Report/plan surfaces working | 78% | Approval-gated patch execution and clearer review artifacts. |
| Sprite Workflow V2 | Pattern exists, product incomplete | 62% | Compact single-row workbench, source/runtime/candidate/proof lanes, provenance manifests. |
| Creative Learning Lab | Planned with seams | 62% | Reviewed examples, preference snapshots, exportable learning packs, no uncontrolled training. |
| Pet AI agents | Dormant safe foundation | 60% | Disabled-by-default model flag, consent UI, fake-adapter proof, then first-call approval. |

## Fresh Audit Snapshot

From `docs/C_PHASE53_PROJECT_PROGRESS_REPORT_2026-05-12.md`:

```text
verification
|
+-- dotnet build                         PASS, 0 warnings/errors
+-- dotnet test                          PASS, 280/280
+-- vNext publish -SkipAssetPrep         PASS
+-- stable release download/hash         PASS
+-- runtime mixed-canvas audit           PASS
+-- sprite source/runtime contract audit PASS
`-- optional readiness audit             PASS with fallback-only caveat
```

Runtime sprite contract:

| Metric | Value |
| --- | ---: |
| Checked sequences | 2880 |
| Checked frames | 10800 |
| Mixed-canvas sequences | 0 |
| Missing/count mismatch sequences | 0 |
| Invalid/non-alpha PNG frames | 0 |
| Runtime variant dirs found / expected | 360 / 360 |
| Runtime frames found / expected | 10818 / 10800 |

Optional animation state:

| Metric | Value |
| --- | ---: |
| Optional targets | 2520 |
| Authored complete | 0 |
| Runtime-only complete | 4 |
| Fallback-only pending | 2516 |
| Invalid optional art | 0 |
| Errors | 0 |

Important interpretation:

- The stable game is safe to ship and structurally sound.
- The biggest visible gap is content depth, especially optional/action-specific animation families.
- The biggest tool/AI gap is safe enablement, not raw code existence.

## Remaining Work By Category

### Game Core

```text
game core
|-- keep stable
|   |-- desktop overlay
|   |-- three-pet loop
|   |-- care/action/save/recovery flows
|   `-- packaged release proof
|
|-- deepen next
|   |-- personality development
|   |-- needs/drives/emotions
|   |-- affection/relationships
|   |-- body condition
|   |-- aging/death/ghost state
|   `-- long-run balance/lived-quality tuning
|
`-- prove continuously
    |-- forced dev scenarios
    |-- packaged proof runs
    `-- user-facing play sweeps
```

Priority: high, but changes should be incremental because the stable release is now protected.

### Visuals And Animation

```text
visual lane
|-- current strength
|   |-- runtime contract green
|   |-- 360 runtime variant folders present
|   |-- base required frames valid
|   `-- visual-side cleanup discipline exists
|
|-- biggest gap
|   |-- optional drink/play/hold/carry/pickup/drop families
|   |-- authored-source coverage
|   |-- in-motion quality sweeps
|   |-- no boxing/shrink-grow regressions
|   |-- no fake backgrounds/holes/noise
|   `-- species-specific movement believability
|
`-- next rule
    `-- source/provenance/contact-sheet/proof before broad mutation
```

Priority: high. This is the most visible difference between stable `v0.1.0` and the ideal Wevito vision.

### Habitat, Items, And Environment

```text
world/content
|-- already working
|   |-- habitat loadout manifest
|   |-- contact shadows
|   |-- depth/placement proof
|   |-- care/item/habitat mapping
|   `-- stable proof path
|
`-- still wanted
    |-- richer species interactions with habitat objects
    |-- enter/perch/hide/use behavior
    |-- stronger care-state visual response
    `-- final stale-reference cleanup
```

Priority: medium-high.

### Overlay UI And PET TASKS

```text
overlay/tool hub
|-- current shape
|   |-- PET TASKS command path
|   |-- report/preview language
|   |-- approval-gated posture
|   |-- three helper pets concept
|   `-- tool surfaces exist
|
`-- simplify next
    |-- one text bar entry point
    |-- clearer helper routing
    |-- simpler result cards
    |-- artifact buttons
    |-- less dev-dashboard feeling
    `-- no new dangerous execution by default
```

Priority: high for usability.

### Helper Tools

```text
helper tools
|-- report/preview foundation
|   |-- localDocs
|   |-- spriteAudit
|   |-- assetInventory
|   |-- petState
|   |-- codeReview
|   |-- codePatchPlan
|   |-- buildProof
|   |-- translateText
|   |-- audioAssist
|   `-- screenCapture
|
|-- proof next
|   |-- adapter-specific proof packets
|   |-- stable artifact references
|   |-- clear provider labels
|   `-- approval states visible to user
|
`-- still gated
    |-- screen recording
    |-- external audio booster control
    |-- live model calls
    |-- sprite generation/import
    `-- mutation/apply workflows
```

Priority: medium-high.

### Sprite Workflow V2

```text
Sprite Workflow V2
|-- desired product shape
|   |-- compact single selected row
|   |-- source/runtime/candidate/proof lanes
|   |-- validator findings
|   |-- provenance and hashes
|   |-- no-mutation preview mode
|   |-- backup-before-apply
|   |-- exact rollback
|   `-- post-apply proof packet
|
`-- current caution
    `-- do not rebuild the old confusing scrolling workflow
```

Priority: medium.

### Creative Learning Lab And AI Agents

```text
AI/helper-agent lane
|-- safe foundation
|   |-- exactly three helper pets
|   |-- roles/personality separate from permissions
|   |-- Core model adapter seam
|   |-- allowlist/consent tests
|   `-- live calls disabled
|
`-- next requirements
    |-- disabled-by-default model setting
    |-- first-call consent UI
    |-- fake-adapter proof
    |-- reviewed examples only
    |-- local preference snapshots
    |-- exportable learning packs
    `-- no uncontrolled self-training
```

Priority: medium after the baseline lock and tool simplification.

## Recommended Execution Order

```text
next concrete plan seed
|
+-- C-PHASE 54: Post-stable baseline lock and asset-prep safety
|   +-- record stable release/hash/checks in a short lock packet
|   +-- add/confirm guardrails around asset-prep regeneration
|   +-- make accidental runtime sprite churn harder
|   `-- keep this phase non-visual and deterministic
|
+-- C-PHASE 55: Visual/source-of-truth recovery plan
|   +-- reconcile authored coverage vs runtime coverage
|   +-- identify source boards needed for care/expression
|   +-- choose contact-sheet review order
|   `-- no broad mutation yet
|
+-- C-PHASE 56: Optional animation pilot expansion
|   +-- pick one small target lane
|   +-- produce source/candidate/proof artifacts
|   +-- apply with backup/hash/rollback
|   `-- scale only after the pilot is pleasant
|
+-- C-PHASE 57: Tool hub simplification pass
|   +-- one text bar
|   +-- clear helper routing
|   +-- simpler task/result cards
|   `-- no new risky execution
|
+-- C-PHASE 58: Adapter proof packets
|   +-- screenshot
|   +-- translation
|   +-- audio assist
|   +-- codeReview/codePatchPlan
|   `-- buildProof
|
+-- C-PHASE 59: Model capability flag and consent UI
|   +-- disabled by default
|   +-- fake adapter validation
|   +-- first-call copy
|   `-- still no live call until approved
|
+-- C-PHASE 60: Sprite Workflow V2 compact workbench
|   +-- report-only first
|   +-- source/runtime/candidate/proof lanes
|   +-- provenance manifest
|   `-- no hidden app scrolling maze
|
`-- C-PHASE 61: Creative Learning Lab seed
    +-- reviewed examples only
    +-- local preference snapshots
    +-- exportable learning packs
    `-- no uncontrolled training
```

## Current Source-Of-Truth Docs

Use these documents when creating the next concrete implementation plan:

- `docs/POST_STABLE_FULL_VISION_IMPLEMENTATION_PLAN_2026-05-12.md`
- `docs/CODEX_MEDIUM_POST_STABLE_PHASE_PROMPTS_2026-05-12.md`
- `docs/C_PHASE52_STABLE_RELEASE_PROMOTION_2026-05-12.md`
- `docs/C_PHASE53_PROJECT_PROGRESS_REPORT_2026-05-12.md`
- `docs/C_PHASE50_RC4_MANUAL_PLAYER_QA_2026-05-12.md`
- `docs/C_PHASE49_RC4_PACKAGE_AND_CLEAN_VALIDATION_2026-05-12.md`
- `docs/C_PHASE46_AI_HELPER_GATE_REVIEW_2026-05-12.md`
- `docs/C_PHASE45_TRANSLATION_AUDIO_PROVIDER_POLISH_2026-05-12.md`
- `docs/C_PHASE44_SCREEN_CAPTURE_UX_PROOF_2026-05-12.md`
- `docs/C_PHASE43_CARE_ITEM_HABITAT_MAPPING_2026-05-12.md`
- `docs/C_PHASE42_PET_TASKS_UX_CLARITY_2026-05-12.md`
- `docs/WEVITO_VISUAL_COMPLETION_TRACKER_2026-05-05.md`
- `docs/WEVITO_VISUAL_STATUS_DASHBOARD_2026-05-05.md`
- `docs/WEVITO_TOOL_AGENT_UI_MASTER_PLAN_2026-05-05.md`
- `docs/WEVITO_PET_HELPER_FUNCTIONS_ROADMAP_2026-05-05.md`
- `docs/WEVITO_OVERLAY_FIRST_IMPLEMENTATION_ROADMAP_2026-05-05.md`
- `docs/CODE_SIDE_DEEP_REVIEW_AND_AGENT_TOOLS_PLAN_2026-05-05.md`
- `SPRITE_PIPELINE_KIT/README.md`

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

5. If the dashboard becomes materially outdated again, update this living file and cite the newest phase reports rather than relying on older RC-era context.
