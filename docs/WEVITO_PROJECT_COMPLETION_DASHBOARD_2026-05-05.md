# Wevito Project Completion Dashboard

Last updated: 2026-05-05

Purpose: living project dashboard for tracking Wevito's game, visual, tool, helper-agent, and release readiness work. Update this markdown file as phases complete, then regenerate the PDF reference copy.

## Current Overall Estimate

```text
Wevito full vision
├─ playable pet game foundation        ███████░░░  ~68%
├─ visual/sprite asset lane            ███████░░░  ~72%
├─ habitat/items/care integration      ████░░░░░░  ~45%
├─ PET TASKS/tool hub                  ████░░░░░░  ~40%
├─ advanced helper/AI/ML systems       ██░░░░░░░░  ~18%
└─ final QA/release readiness          ████░░░░░░  ~42%

Total project completion: ~55%
Remaining to complete full vision: ~45%
```

This estimate treats "everything done" as the expanded full Wevito vision: a working desktop pet game, clean sprites and animations, habitats, items, helper tools, screenshot/capture, translation, audio assist, coding helpers, Sprite Workflow V2, Creative Learning Lab, and pet-agent behavior.

If measuring only the basic playable pet-game foundation, Wevito is closer to 65-70%.

## Area Status

| Area | Status | Done | What Remains |
| --- | --- | ---: | --- |
| Git/build baseline | Clean and current | 90% | Keep `main` clean, rebuild when needed, avoid regenerating noisy local folders. |
| Core pet sim | Strong foundation | 68% | Tighten personality development, aging/death/ghost state, visible care outcomes, save/load proof. |
| Game interactions | Usable but uneven | 50% | Real staged fetch loop, visible drink/eat/care actions, forced scenario testing. |
| Sprites/visuals | Much better, not final | 72% | Final in-motion QA, optional animations, no artifacts, no boxing/shrink-grow, final proof sweeps. |
| Color variants | Mostly green | 85% | Only targeted review/fixes, not broad regeneration. |
| Care/medicine/items | Assets exist | 45% | Map items into gameplay/UI, confirm readability, remove stale expectations. |
| Habitat/environment | Planned well | 35% | Runtime anchors, depth, occlusion, shadows, enter/perch/hide/use zones. |
| Overlay UI | Direction is clear | 50% | Implement Claude/visual design direction in vNext without replacing pet overlay. |
| PET TASKS/tool hub | Functional seed exists | 40% | Simpler UI, artifact buttons, report-only polish, then approval-gated execution. |
| Screenshot/capture tools | Planned | 20% | Wevito-window screenshot first, then region capture, later short proof clips. |
| Translation/audio tools | Early implementation/plans | 30% | Confirm provider UX, DeepL/API handling, safe Windows volume only, external booster handoff. |
| Coding helpers | Planned/partial | 25% | Code review reports, patch plans, then tightly approved patch execution. |
| Sprite Workflow V2 | Designed, not built | 20% | One-screen workbench, provenance, contact sheets, dry-run/apply/rollback. |
| Creative Learning Lab | Concept planned | 15% | Reviewed examples, preference memory, dataset/export dashboard, no uncontrolled training. |
| Pet AI agents | Concept planned | 18% | Three named helper pets, roles, task cards, permissions, logs, no unsafe autonomy. |
| Final release QA | Not ready yet | 42% | Full tests, Godot proof, in-game sweeps, final docs/help, packaging. |

## Remaining Work By Category

### Game Core

```text
game core
├─ simulation
│  ├─ personality development
│  ├─ needs/drives/emotions
│  ├─ affection/relationships
│  ├─ health/body condition
│  ├─ aging/death/ghost state
│  └─ save/load migrations
│
├─ interactions
│  ├─ feeding
│  ├─ drinking
│  ├─ medicine/care
│  ├─ bathing/grooming
│  ├─ sleep/rest
│  └─ ball play/fetch sequence
│
└─ proof
   ├─ forced dev scenarios
   ├─ UI/control verification
   └─ in-game interaction sweeps
```

Priority: high.

Main reason: this is the actual game spine. Visuals and tools become easier to validate once pet behavior is deterministic and visibly connected to actions.

### Visuals And Animation

```text
visual lane
├─ keep
│  ├─ current color folder coverage
│  ├─ cleaned shared assets
│  ├─ runtime overlay prop contract
│  └─ visual-side review discipline
│
├─ finish
│  ├─ final in-motion sprite QA
│  ├─ optional ball animations
│  ├─ snake/frog/bird motion quality
│  ├─ size consistency
│  ├─ no fake PNG backgrounds
│  ├─ no holes/noise/edge residue
│  └─ final color variant acceptance
│
└─ defer until base quality is stable
   ├─ ghost variants
   ├─ fat/skinny variants
   └─ broad new generation
```

Priority: high, but coordinated with visual-side.

Main reason: sprite quality is the most visible product risk. The current direction is much safer than the earlier confusing workflow, but final animation proof still matters.

### Habitat, Items, And Environment

```text
world/content
├─ care/medicine mapping
├─ food/water/toy mapping
├─ egg lifecycle assets
├─ habitat object loadouts
├─ runtime interaction anchors
├─ depth bands and occlusion
├─ contact shadows
└─ stale asset reference cleanup
```

Priority: medium-high.

Main reason: this turns Wevito from "pets on a screen" into pets that live in a readable world.

### Overlay UI And Tool Hub

```text
overlay/tool hub
├─ overlay remains home
├─ summoned tool surfaces
├─ PET TASKS command bar
├─ three helper pets
├─ task cards
├─ result cards
├─ artifact buttons
├─ report-only labels first
└─ approval gates before execution
```

Priority: high for code-side.

Main reason: this is the bridge between the pet sim and the powerful helper-tool fantasy. It must stay simple.

### Helper Tools

```text
helper tools
├─ safe/report-only first
│  ├─ localDocs
│  ├─ spriteAudit
│  ├─ assetInventory
│  ├─ petState
│  ├─ codeReview
│  └─ codePatchPlan
│
├─ approval-gated next
│  ├─ buildProof
│  ├─ Wevito-window screenshot
│  ├─ selected-region screenshot
│  ├─ translation execution
│  └─ safe Windows volume control
│
└─ later / high-risk
   ├─ screen recording
   ├─ browser automation
   ├─ external audio boosters
   ├─ sprite generation/import
   └─ mutation/apply workflows
```

Priority: medium-high.

Main reason: tools make Wevito meaningfully useful beyond a game, but unsafe automation too early would create chaos.

### Sprite Workflow V2

```text
Sprite Workflow V2
├─ one selected row visible at a time
├─ source/runtime/candidate/proof strips
├─ validator findings
├─ provenance and hashes
├─ no-mutation preview mode
├─ one-row apply plan
├─ backup-before-apply
├─ exact rollback
└─ post-apply proof packet
```

Priority: medium.

Main reason: this should eventually replace the confusing external sprite workflow experience, but it should start read-only and compact.

### Creative Learning Lab And AI Agents

```text
AI/helper-agent lane
├─ exactly three active helper pets
├─ names/roles assigned by user
├─ task bar accepts natural language
├─ pet personality affects presentation, not permissions
├─ reviewed examples become learning data
├─ preference snapshots
├─ artifact-backed memory
├─ local/vector search later
└─ no uncontrolled self-training
```

Priority: medium-low until tool foundations are stable.

Main reason: this is exciting, but it only becomes trustworthy after artifacts, permissions, and rollback rules are solid.

## Recommended Execution Order

```text
next best sequence
├─ 1. finish Tool Hub/PET TASKS UI clarity
├─ 2. finish action/animation/prop contract
├─ 3. implement staged fetch/drink/care proof flows
├─ 4. map care/items/habitats into runtime UI
├─ 5. let visual-side finish targeted sprite/animation QA
├─ 6. add safe helper tools one at a time
├─ 7. build Sprite Workflow V2 read-only workbench
├─ 8. add controlled apply/proof/rollback gates
├─ 9. add Creative Learning Lab reviewed-data surface
└─ 10. run final in-game QA and release packaging
```

## Current Source-Of-Truth Docs

Use these documents when updating this dashboard:

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
