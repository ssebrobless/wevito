# Wevito x Hatch Pet — Adoption Plan (Claude, 2026-05-04)

Author: Claude (Opus 4.7), planning pass — no code touched.
Inputs: `docs/HATCH_PET_WEVITO_HANDOFF_2026-05-04.md`, README, vNext checklist,
sprite source-of-truth, authored-animation workflow, vNext source under
`vnext/src/Wevito.VNext.{Shell,Core,Contracts}`, `vnext/content/optional_animation_families.json`,
git status, OpenAI Hatch Pet skill files (`SKILL.md`, scripts/, references/).

> **Scope of this doc.** This is review + planning only. It does not modify
> runtime code, tools, or the parallel **Sprite Workflow App** that another
> thread is actively building. Treat it as a triage of the Hatch Pet skill
> against where Wevito is *now* and where it is *headed*.

---

## 1. Current State Of Wevito (verified, not just from the handoff)

```text
╔════════════════════════════════════════════════════════════════════════╗
║ WEVITO — TWO SURFACES, ONE PET                                         ║
╠════════════════════════════════════════════════════════════════════════╣
║ Runtime: vnext/  (WPF desktop companion, .NET, Wevito.VNext.*)         ║
║   ├─ HomePanel + RoamBand + ToolPopup windows (ShellCoordinator.cs)    ║
║   ├─ broker process (clipboard, hotkeys, drop targets)                 ║
║   ├─ rich PetSimulationEngine (needs/personality/aging/conditions)     ║
║   ├─ SpriteAssetService (authored ▸ runtime ▸ shared-runtime fallback) ║
║   └─ SpriteRepair/* (in-app deterministic repair queue)                ║
║                                                                        ║
║ Asset pipeline: tools/ + scripts/ (Python + PowerShell)                ║
║   incoming_sprites ▸ sprites_authored ▸ sprites_authored_verified      ║
║   ▸ sprites_runtime  (the runtime ships from this)                     ║
║                                                                        ║
║ Webtools: 5-slot hotbar, slot 1 = Link Bin (live), slots 2-5 reserved  ║
║                                                                        ║
║ Legacy lane: project.godot + scripts/*.gd (still touched for prop      ║
║   scaling, anchors, etc., but vNext is the center of gravity)          ║
╚════════════════════════════════════════════════════════════════════════╝
```

### 1a. Runtime is mature

- **Simulation:** 10 species × 3 ages × 2 genders × 6 colors. Needs, action
  outcomes, comfort, fitness, conditions, personality, habits, aging, death,
  ghost rendering, habitat zones, action props are all wired
  (`PetSimulationEngine.cs`).
- **Shell:** focused / passive / pinned modes; basket/Link Bin live; webtools
  hotbar exposes 5 tabs with 4 reserved slots. Dev-tools popup (`Ctrl+Shift+D`)
  has scenario forcing — useful for QA.
- **Repair architecture (already exists):** `vnext/src/Wevito.VNext.Core/SpriteRepair/`
  has `RepairAutomationRunner`, `RepairPipelineOrchestrator`, `RepairPlanner`,
  `LocalDeterministicRepairEngine`, `SpriteValidator`, `RepairQueueDiscoveryService`,
  `SpriteAssetIndex`, plus contracts in `Wevito.VNext.Contracts/SpriteRepairContracts.cs`
  with a real `SpriteIssueKind` taxonomy:
  `BodyHoles, WhiteBoxMatte, DetachedBorderJunk, PaleEdgePixels, DimensionMismatch,`
  `FullyTransparent, SequenceInconsistency, SilhouetteDrift, PaletteMismatch, GenericArtifact`.
  Strategies: `AlphaCleanup, DetachedComponentRemoval, HoleFill, DonorFrameSwap,`
  `DonorFamilySwap, PaletteRetint, SourceBoardReimport, MotionPreservingImport,`
  `CodexRepair, GeminiGeneration, ManualReview`. Validator dimensions match
  Hatch Pet's vocabulary closely.

### 1b. Optional-animation production is broadly *complete*

From `VNEXT_IMPLEMENTATION_CHECKLIST.md` (lines ~290-425):

| Family | Authored / target | Status |
|---|---|---|
| `drink` | 360/360 | complete |
| `play_ball` | 360/360 | complete |
| `hold_ball` | 360/360 | complete (reseeded from `idle_00..03`) |
| `pickup_ball` | 360/360 | complete (reseeded from `play_ball_00..03`) |
| `drop_ball` | 360/360 | complete (multi-species reseed-from-play_ball) |
| `carry_ball_walk` | 360/360 | complete (synthesized from `walk_00..05`) |
| `carry_ball_run` | 360/360 | complete (synthesized from `walk_00..05`) |
| **Total** | **2,520 / 2,520** | authored-complete, packaged-validated |

- All `60/60` blue packaged sweeps pass for every optional family.
- 360 prop-anchor metadata rows live; mouth/beak alignment forward-tuned.
- Runtime has clean shared-runtime care icons + universal action props.
- Latest fully-validated build:
  `builds/release/WevitoDesktopPet-v20260504-pickup-ball-reseed-win64.exe`
  (mirrored to `WevitoDesktopPet-latest-win64.exe`).

### 1c. Real, named visual-polish gap

Bespoke per-species **grip/pickup/hold poses** — e.g. ball clenched in beak/
mouth/paw rather than overlaid via runtime anchor. `hold_ball` and `pickup_ball`
are currently *idle*-derived and *play_ball*-derived bodies + runtime ball
overlay. That's stable but not expressive. This is the open visual ceiling.

---

## 2. The Other Active Thread (do not overwrite)

`git status` (filtering caches/builds/sprite trees) shows two coordinated lanes:

```text
LANE A — Sprite Workflow App (separate Avalonia/.NET tool)
  +-- docs/SPRITE_WORKFLOW_APP_BUILD_SPEC.md       (NEW, untracked)
  +-- docs/SPRITE_WORKFLOW_APP_V1_BACKLOG.md       (NEW, untracked)
  +-- docs/EXPRESSION_QUALITY_AUDIT_2026-03-18.md  (NEW, untracked)
  +-- SPRITE_PIPELINE_KIT/FUTURE_SPRITE_ANIMATION_GUIDELINE.md (NEW)
  +-- .sprite-workflow/{candidates,requests,reviews,workspace-state}.json
  +-- .sprite-workflow/live-ops.jsonl
       => app reads/writes its workspace state here

LANE B — Sprite tooling + legacy Godot polish
  +-- tools/audit_source_to_runtime_quality.py
  +-- tools/audit_sprite_contract.py
  +-- tools/authored_motion_specs.py
  +-- tools/build-{desktop-bridge,release}.ps1
  +-- tools/generate_runtime_pose_sprites.py        (MM, working tree dirty)
  +-- tools/import_authored_family_board.py         (MM)
  +-- tools/prepare_motion_gemini_handoff.py        (MM)
  +-- tools/render_row_motion_strip.py              (NEW, AM)
  +-- tools/render_runtime_sprite_previews.py
  +-- tools/report_authored_sprite_coverage.py
  +-- scripts/{game_manager,main_scene,pet}.gd      (legacy Godot)
  +-- vnext/tests/Wevito.VNext.Tests/SpriteRuntimeCoverageTests.cs
  +-- export_presets.cfg
  +-- docs/VNEXT_IMPLEMENTATION_CHECKLIST.md
```

### 2a. What the active thread is almost certainly doing

```text
+ Building "Sprite Workflow App" (separate Avalonia tool, Wevito = first project profile)
+ Standing up its workspace state JSONs (.sprite-workflow/*)
+ Writing the V1 backlog and build spec
+ Producing an EXPRESSION_QUALITY_AUDIT for the next repair queue
+ Adding a row-motion-strip renderer (tools/render_row_motion_strip.py)
+ Tightening Gemini handoff prep, family-board import, and audit/coverage
  reporters in lockstep with the workflow app
+ Light Godot polish (prop scaling already shipped per checklist; remaining
  tweaks are likely follow-ons to that)
```

### 2b. Hands-off list for *this* thread

- `.sprite-workflow/**` — workspace state owned by Lane A.
- `docs/SPRITE_WORKFLOW_APP_*.md`, `docs/EXPRESSION_QUALITY_AUDIT_*.md`,
  `SPRITE_PIPELINE_KIT/FUTURE_SPRITE_ANIMATION_GUIDELINE.md`.
- The dirty tools (`generate_runtime_pose_sprites.py`,
  `import_authored_family_board.py`, `prepare_motion_gemini_handoff.py`,
  `render_row_motion_strip.py`, `audit_*`, `authored_motion_specs.py`).
- `scripts/*.gd`, `export_presets.cfg`.
- `docs/VNEXT_IMPLEMENTATION_CHECKLIST.md` (Lane A is appending to it).

This plan is structured so its *Phase 1-2* work can be done while Lane A
keeps moving, by writing **new, additive** files rather than editing those.

---

## 3. Hatch Pet Triage — Three Buckets

Hatch Pet (OpenAI skill) is a *production tool*, not a runtime. It produces
1536x1872 px atlases for the Codex app, with deterministic Python scripts
(`prepare_pet_run.py`, `pet_job_status.py`, `record_imagegen_result.py`,
`extract_strip_frames.py`, `inspect_frames.py`, `compose_atlas.py`,
`validate_atlas.py`, `make_contact_sheet.py`, `render_animation_videos.py`,
`package_custom_pet.py`, `derive_running_left_from_running_right.py`,
`queue_pet_repairs.py`, `generate_pet_images.py`).

```text
HATCH PET FEATURE                           BUCKET            WHERE TO LAND IT
=========================================================================================
Manifested generation jobs (imagegen-jobs.json)  ADAPT NOW    Sprite Workflow App + tools/
Provenance + hash on selected source             ADAPT NOW    Sprite Workflow App + tools/
Layout guides per row/family                     ADAPT NOW    tools/ (gen) + Workflow App
Contact sheet as standard QA output              ADAPT NOW    tools/finalize_*
Per-state preview videos / GIFs                  ADAPT NOW    tools/finalize_*
Targeted repair queue (smallest scope first)     ALREADY HAVE  vNext SpriteRepair (align taxonomy)
Forbidden-effect prompt rules                    ADAPT NOW    Gemini handoff prompts
Identity-lock prompt language                    ADAPT NOW    Gemini handoff prompts
Symmetric-mirror as explicit, provenance-tagged  ADAPT NOW    tools/ derive_*  helper
QA rubric with errors vs. warnings               ADAPT NOW    docs + audit JSON shape
Subagent row-generation policy (parent owns      DEFER        Workflow App (multi-provider)
  manifest, subagent only returns source path)
Codex 8x9 atlas geometry                         SKIP         keep Wevito per-frame tree
pet.json + spritesheet.webp custom-pet package   FUTURE       optional creator pipeline
$imagegen as sole provider                       SKIP         Wevito uses Gemini Web (+manual)
"No local synthesis" hard rule                   PARTIAL      keep deterministic synthesis
                                                                where intentional + labeled
"running-left", "waving", "review", "waiting",   ADAPT NOW    new Wevito work-companion
  "failed", "jumping" state vocabulary                          state set (runtime side)
```

### 3a. Directly useful now (lift mostly as-is)

1. **Per-run JSON manifest** with provenance/hash recording. Adopt the
   `imagegen-jobs.json` shape, but generalize the `provider` field
   (`gemini-web | openai-imagegen | manual | local-synth`).
2. **Layout-guide images** for each animation family/row. Wevito has been
   bitten by Gemini Web reflowing focused boards; a guide is the single
   highest-value workflow change.
3. **Contact sheet + preview video** as required outputs of any "apply"
   step, so `hold_ball`-style technical-pass-but-visual-fail is caught at
   author-time, not packaged-runtime time.
4. **Hatch Pet's prompt rule list** (forbid speed lines, dust, glow,
   speech bubbles, white/black backgrounds, checkerboard, detached effects;
   require terse sprite-specific language; require canonical base reference
   on every row job). Drop these into the Gemini handoff prompt template.
5. **Repair scope rule:** smallest failing scope first (single frame ▸ row ▸
   family ▸ identity-level regen). Wevito's `RepairAutomationRunner` already
   prioritizes by `SpriteIssueKind`; the alignment to do is in *vocabulary*,
   not architecture.
6. **Errors vs. warnings split** in `qa/review.json`. Wevito has many JSON
   audits already, but they don't share a common `errors[]` / `warnings[]`
   schema — easy win.

### 3b. Useful with adaptation

7. **Job status command** (`pet_job_status.py`): Wevito has many separate
   reports today; one "what's ready / blocked / running / failed" command
   per family run would simplify the optional-family lane. The Sprite
   Workflow App may already plan this — check before adding a Python helper.
8. **`derive_*_from_*` helper** with explicit "safe-mirror" decision file.
   Wevito has been doing this implicitly for several reseed/synthesize
   passes (carry_walk from walk, hold_ball from idle, pickup_ball from
   play_ball). Capture that as a first-class operation with a stored
   decision note instead of a checklist paragraph, so the provenance is
   queryable.
9. **`compose_atlas` + `validate_atlas`** structurally equivalent to "all
   target paths exist with correct dimensions and transparent fill" —
   Wevito's `audit_sprite_contract.py` does this. Adapt the *report shape*
   (one JSON, structured rows) rather than copying the script.
10. **Subagent-per-row generation pattern** is a fit for the Sprite
    Workflow App's *future* multi-provider story but not for today's
    Gemini-Web/manual loop. Park.

### 3c. Not worth copying

- Codex 8x9 atlas (1536x1872 PNG/WebP). Wevito's runtime is per-frame PNGs
  at 28x24 with 8 base + 7 optional families. The atlas is a Codex serving
  detail, not a quality contract.
- `pet.json` package. Becomes interesting only if Wevito ships an
  installable third-party pet pack (not on the roadmap).
- `$imagegen` as the only provider, and the matching ban on local
  deterministic synthesis. Wevito's reseed/synthesize/retint passes are
  intentional, validated, and documented; converting them to "must come
  from generation" would burn quality without raising it.
- `generate_pet_images.py` (OpenAI direct fallback). Wevito's provider
  decision is independent.

---

## 4. Phased Plan

```text
PHASE 0  (this thread, no code) ─ documents only, ~ already complete with this file
PHASE 1  contracts + manifest schema (additive, no live behavior change)
PHASE 2  layout-guide generator + finalize_run helper (tools/)
PHASE 3  align vNext SpriteRepair issue taxonomy with Hatch Pet vocabulary
PHASE 4  work-companion runtime states + tool reactions
PHASE 5  webtools slot 2 prototype (Clipboard Shelf or Focus Timer)
PHASE 6  optional: bespoke grip-pose authoring batches (the real visual ceiling)
PHASE 7  optional: "Hatch a Wevito" creator pipeline (export pet pack)
```

### Phase 1 — Contracts + Manifest Schema (low risk, **first PR**)

**Goal:** lay down a written contract Lane A and future Claude/Codex
threads can both build against, without changing any code.

| Deliverable | File | Notes |
|---|---|---|
| Generation contract doc | `docs/WEVITO_ANIMATION_GENERATION_CONTRACT.md` (new) | provider-neutral, defines manifest, provenance, QA gates |
| Manifest schema | `docs/wevito-animation-run.schema.json` (new) | JSON Schema for the manifest below |
| QA rubric (mirrors Hatch Pet's `qa-rubric.md`) | `docs/WEVITO_ANIMATION_QA_RUBRIC.md` (new) | errors vs warnings, identity drift as blocker |
| State-mapping cheat sheet | `docs/WEVITO_WORK_COMPANION_STATES.md` (new) | maps PC events ▸ pet states (see §6) |

Manifest shape (provider-neutral):

```text
wevito-animation-run.json
├─ run_id                       ULID
├─ created_at                   ISO 8601
├─ target
│   ├─ species / age / gender / source_color
│   ├─ family       drink | play_ball | hold_ball | ...
│   ├─ frame_count  4 | 6
│   └─ runtime_paths[]
├─ references
│   ├─ canonical_source_board   (incoming_sprites/...)
│   ├─ verified_runtime_strips[]
│   ├─ prop_anchor_metadata     (sprites_runtime/_metadata/prop_anchors.json subset)
│   └─ layout_guide             (tools/.../layout_guide-<family>-<frames>.png)
├─ jobs[]
│   ├─ job_id
│   ├─ provider                 gemini-web | openai-imagegen | manual | local-synth
│   ├─ prompt_path
│   ├─ input_image_paths[]
│   ├─ expected_geometry        {cells, cell_w, cell_h, columns, rows}
│   ├─ status                   ready | blocked | sent | recorded | rejected | applied
│   ├─ source                   { path, sha256, captured_at }
│   └─ provenance_note
├─ import
│   ├─ candidate_frame_paths[]
│   ├─ cleanup_ops[]            chroma_strip | edge_trim | guard_dilate | ...
│   ├─ validation_report_path
│   └─ apply_backup_dir
└─ proofs
    ├─ contact_sheet_path
    ├─ preview_video_path
    ├─ packaged_screenshot_proof_path
    └─ markdown_summary_path
```

**Why this is the right first PR:** zero runtime behavior change; it's a
written contract Lane A's Sprite Workflow App can target; Phase 2 tools
can target it; future Codex threads can read it before touching the lane.

### Phase 2 — Layout Guide + Finalize Helper (low risk)

| Deliverable | New file | Outputs |
|---|---|---|
| Layout-guide generator | `tools/generate_layout_guide.py` | per family/frame_count: transparent PNG with marked cell borders + safe margins, alongside an `_invisible.png` variant that is uniform color (used as "construction reference; ignore visually" for Gemini) |
| Run preparer | `tools/prepare_wevito_animation_run.py` | writes `wevito-animation-run.json` for one (species, age, gender, family) target; reuses existing prep prompts but emits manifest first |
| Run finalizer | `tools/finalize_wevito_animation_run.py` | requires manifest + recorded sources; produces contact sheet PNG, short MP4/GIF preview, validation JSON, markdown summary; updates manifest in place |
| Result recorder | `tools/record_wevito_animation_result.py` | hashes + records selected source path + provider + provenance note; mirrors Hatch Pet's `record_imagegen_result.py` semantics on Wevito's tree |

> **Coordination point with Lane A:** the Sprite Workflow App is meant to
> drive imports/reviews. These tools should be callable from that app
> (subprocess) and from a human at the shell. Writing them as plain CLIs
> with a stable JSON contract is the safest bet — Lane A can wrap them in
> the app UI later. Confirm with Lane A's owner before adding them.

### Phase 3 — Align vNext Repair Taxonomy

The architecture is already in place. The only remaining alignment is
*vocabulary* (and a small expansion for matte-residue subtypes that
Wevito currently lumps under `WhiteBoxMatte`).

Suggested additions to `SpriteRepairContracts.cs#SpriteIssueKind`:

```text
existing                            add to fully cover Hatch Pet rubric
=================================== =====================================
BodyHoles                            ChromaAdjacentResidue   (gemini matte)
WhiteBoxMatte                        EdgePixelTouch          (frame-edge clip risk)
DetachedBorderJunk                   SizeOutlier             (frame-area outlier)
PaleEdgePixels                       SparseFrame             (near-empty frame)
DimensionMismatch                    IdentityDrift           (manual review only)
FullyTransparent
SequenceInconsistency
SilhouetteDrift
PaletteMismatch
GenericArtifact
```

Touch points:
- `vnext/src/Wevito.VNext.Contracts/SpriteRepairContracts.cs` — add enum members.
- `vnext/src/Wevito.VNext.Core/SpriteRepair/SpriteValidator.cs` — add detectors
  for size-outlier and sparse-frame; chroma-adjacent residue may already be
  partly captured by `WhiteBoxMatte` — split if it makes the planner clearer.
- `vnext/src/Wevito.VNext.Core/SpriteRepair/RepairPlanner.cs` — add planning
  branches; `IdentityDrift` ▸ `ManualReview` only.
- `vnext/src/Wevito.VNext.Core/SpriteRepair/RepairQueueDiscoveryService.cs` —
  weight new kinds in `ComputePriority`.
- `vnext/tests/Wevito.VNext.Tests/SpriteValidatorTests.cs` and
  `RepairPlannerTests.cs` — add unit cases.
- *Avoid* `SpriteRuntimeCoverageTests.cs` while Lane A is editing it.

> **Why this matters for Hatch Pet alignment:** Hatch Pet's
> `qa-rubric.md` calls out chroma-adjacent residue, edge-pixel touches,
> sparse frames, and size outliers as their own categories. Aligning the
> vocabulary makes future audit JSON portable between Wevito's runtime
> queue and the Sprite Workflow App.

### Phase 4 — Work-Companion Runtime States

The runtime today renders animations from `PetAnimationState`. Hatch Pet's
state vocabulary (`waving`, `jumping`, `failed`, `waiting`, `review`)
maps cleanly onto desktop-companion semantics:

```text
PC EVENT                        FALLBACK NOW    NEW STATE          AUTHORED FAMILY (later)
================================ ============== ================== =======================
Link captured into Link Bin       happy           Wave/Greet         waving (4 frames)
Clipboard URL invalid             sad             Failed             failed (8) or sad-pulse
Opening saved link                walk            Run                running-in-place (6)
Tool popup waiting for choice     idle            Waiting            waiting (6)
Repair queue scanning             idle            Review             review (6)
Repair successfully applied       happy           Jumping            jumping (5)
Repair escalated to manual        sad             Failed             failed (8)
Build/test running (future tool)  walk            Run                running (6)
Focus session active (future)     sleep/idle      Waiting            waiting (6) — quiet
```

Implementation outline (Phase 4a, runtime hookup *only* — no new art):

1. Add new enum members to `Wevito.VNext.Contracts/Models.cs#PetAnimationState`
   (`Waving`, `Jumping`, `Failed`, `Waiting`, `Review`).
2. In `SpriteAssetService.ResolveAnimationFrames`, map any unknown new state
   to the existing fallback (`happy`, `idle`, etc.) so nothing breaks
   visually if frames are missing.
3. In `ShellCoordinator`, raise the new states from real events:
   - basket capture success → `Jumping` for 800ms
   - basket capture fail → `Failed` for 800ms
   - tool popup open → `Waiting` while open
   - `RepairAutomationRunner` actively running → `Review` on the dev pet

This **does not require new sprite art**. Phase 4b later authors the new
families using the same Phase 1/2 manifest pipeline.

### Phase 5 — Webtools Slot 2 Prototype

Recommended first new tool: **Clipboard Shelf** (lowest novelty, highest
synergy with Link Bin + work-companion states).

```text
Clipboard Shelf
  ├─ paste captures any clipboard payload (URL, text, file path, image path)
  ├─ types are visually distinct (URL chip, file chip, image chip)
  ├─ clicking a row copies it back to the clipboard or opens it
  ├─ pet reacts:
  │    new capture       → Jumping
  │    duplicate ignored → Waiting (quick)
  │    invalid payload   → Failed (quick)
  └─ shares the popup chrome of Link Bin (consistent UX)
```

Touch points (sketch, not implementation):

- `vnext/src/Wevito.VNext.Contracts/Models.cs` — add `ClipboardShelfItem`
  record + `CompanionState.ClipboardShelf`.
- `vnext/src/Wevito.VNext.Core/` — add `ClipboardShelfService` (mirrors
  `BasketService` pattern, capacity-bounded, persisted).
- `vnext/src/Wevito.VNext.Shell/ToolPopupWindow.{xaml,xaml.cs}` — add a
  shelf panel parallel to `BasketPanel`, with type-aware row template.
- `vnext/src/Wevito.VNext.Shell/HomePanelWindow` — wire slot 2 click to
  `ToggleClipboardShelfRequested`.
- `vnext/tests/Wevito.VNext.Tests/` — `ClipboardShelfServiceTests`.

Slot 3-5 candidates (in priority order):
- **Focus Timer** — pin/unpin HUD, Pomodoro cycles, pet uses `Waiting`/`Sleep`.
- **Drop Shelf** — drag files/URLs/images into the pet, queues a small
  staging list, opens folder/copy path, pet reacts on hover/drop.
- **Work Monitor** — split user/dev mode; user mode reflects build/test
  command status (later); dev mode shows `RepairAutomationRunner` queue
  count + last action.

### Phase 6 — Bespoke Grip-Pose Authoring (real visual ceiling)

The remaining honest-to-eyes gap is per-species `hold_ball`, `pickup_ball`,
and `drop_ball` *grip* poses. The Phase 1 manifest + Phase 2 layout-guide
machinery is the right vehicle for this lane, with Hatch Pet's prompt
rules baked in:

```text
prompt rules to add to grip-pose Gemini handoff
  - identity lock: "preserve head/face/markings/palette/silhouette"
  - frame-count lock: "exactly 4 frames in 4 cells, no extra cells"
  - background: "transparent or pure chroma key only; no checkerboard"
  - effects: "no speed lines, dust, shadows, sparkles, smears, motion arcs"
  - prop: "ball clenched in beak/mouth/paw of the species; no overlay halo"
  - boundary: "do not crop body; do not box-fit"
```

Sequencing:
- 1 species pilot (suggest **goose** — already most-iterated; web-Gemini
  prep folder already exists).
- Use Phase 2 finalize helper to produce contact sheet + preview MP4
  before importing.
- If pilot succeeds, expand to 2-3 species per batch.

### Phase 7 — Optional "Hatch A Wevito" Creator Pipeline

Park this until Phase 1-5 land. Concept sketch only:

```text
user concept / reference image
  ▸ generate canonical source board (Phase 2 prepare/finalize)
  ▸ generate base 8 animations
  ▸ generate optional family subset
  ▸ validate against Wevito frame tree
  ▸ package as wevito_pets/<pet-id>/
        pet.json (id/displayName/description/runtime_root)
        sprites_runtime/...
        source_refs/...
        qa/...
        license/provenance.json
  ▸ Sprite Workflow App imports the pack
```

This is where Hatch Pet's `package_custom_pet.py` becomes a useful model.

---

## 5. Files Likely Touched (cross-phase summary)

```text
NEW (additive, safe to land while Lane A works)
  docs/WEVITO_ANIMATION_GENERATION_CONTRACT.md
  docs/WEVITO_ANIMATION_QA_RUBRIC.md
  docs/WEVITO_WORK_COMPANION_STATES.md
  docs/wevito-animation-run.schema.json
  tools/generate_layout_guide.py
  tools/prepare_wevito_animation_run.py
  tools/finalize_wevito_animation_run.py
  tools/record_wevito_animation_result.py

EXTEND (after Lane A signs off / coordinates)
  vnext/src/Wevito.VNext.Contracts/SpriteRepairContracts.cs       Phase 3
  vnext/src/Wevito.VNext.Contracts/Models.cs                       Phase 4 (PetAnimationState)
  vnext/src/Wevito.VNext.Core/SpriteRepair/SpriteValidator.cs      Phase 3
  vnext/src/Wevito.VNext.Core/SpriteRepair/RepairPlanner.cs        Phase 3
  vnext/src/Wevito.VNext.Core/SpriteRepair/RepairQueueDiscoveryService.cs  Phase 3
  vnext/src/Wevito.VNext.Shell/ShellCoordinator.cs                 Phase 4-5
  vnext/src/Wevito.VNext.Shell/SpriteAssetService.cs               Phase 4 (state fallback map)
  vnext/src/Wevito.VNext.Shell/ToolPopupWindow.{xaml,xaml.cs}      Phase 5
  vnext/src/Wevito.VNext.Shell/HomePanelWindow.{xaml,xaml.cs}      Phase 5
  vnext/src/Wevito.VNext.Core/  (new ClipboardShelfService.cs)     Phase 5
  vnext/tests/Wevito.VNext.Tests/                                  Phases 3-5

DO NOT TOUCH while Lane A is dirty
  .sprite-workflow/**
  docs/SPRITE_WORKFLOW_APP_*.md
  docs/EXPRESSION_QUALITY_AUDIT_*.md
  SPRITE_PIPELINE_KIT/FUTURE_SPRITE_ANIMATION_GUIDELINE.md
  scripts/*.gd
  export_presets.cfg
  tools/{generate_runtime_pose_sprites,import_authored_family_board,
         prepare_motion_gemini_handoff,render_row_motion_strip,
         audit_*,authored_motion_specs,render_runtime_sprite_previews,
         report_authored_sprite_coverage}.py
  vnext/tests/Wevito.VNext.Tests/SpriteRuntimeCoverageTests.cs
  docs/VNEXT_IMPLEMENTATION_CHECKLIST.md   (Lane A is appending)
```

---

## 6. Specific Concerns From The Brief

### 6a. Sprite generation/import manifests

Adopt a `wevito-animation-run.json` per (species, age, gender, family) run
(see Phase 1 schema). Coordinate with the Sprite Workflow App so its
review/request JSONs cross-reference the same run_id. The workflow app
should **own UI**; the manifest should be the cross-tool contract.

### 6b. Provenance + hash tracking

For every selected source image (Gemini Web download, copy-image, manual
edit), record `sha256`, absolute path, capture timestamp, and provider.
This matches Hatch Pet's `record_imagegen_result.py` and closes the
"don't pretend a generated asset is complete by editing a manifest" gap.
The Wevito repair pipeline already has `RepairResult` shape; this is the
upstream complement.

### 6c. Layout guides for Gemini/OpenAI rows

Wevito's biggest live pain: Gemini Web reflowed `772x258` boards to
`1771x592` on download. A layout-guide PNG attached as a third reference
(separate from base + verified-runtime strips) gives a stronger expected
geometry. The guide should be:

```text
- transparent or single contrast color, no text
- exactly the target board dimensions
- cell borders marked with thin lines that the importer expects to see
  removed by the validator (and cells that contain pixel-overlapping the
  guide colors must be rejected)
- safe-margin marked with a translucent inset
- saved alongside the prompt under tools/.../layout_guides/
```

### 6d. Contact sheets + preview videos

Two hard outputs of `tools/finalize_wevito_animation_run.py`:

1. **Contact sheet PNG** — checkerboard background, all cells of the new
   family laid out with each color variant, plus a same-position
   *previous frames* row for diffing.
2. **Preview MP4/GIF** — frame loop of the applied family at runtime
   speed, on a checkerboard background; one per (species, age, gender)
   per family per run.

These are the kind of artifacts that catch the `hold_ball` "broken
authored poses pass alpha but fail visual intuition" class earlier.

### 6e. Targeted repair queues

Already exists in `vnext/src/Wevito.VNext.Core/SpriteRepair/`. Phase 3 is
about *vocabulary alignment* with Hatch Pet's QA rubric, plus exposing
the repair queue's snapshot JSON in a shape the Sprite Workflow App can
consume. Hatch Pet's "smallest failing scope" rule is a one-liner to add
to the repair-pipeline doc / planner comments, since the planner already
behaves this way.

### 6f. Optional action families

`vnext/content/optional_animation_families.json` is the Wevito source of
truth. Hatch Pet adds nothing to the *taxonomy*. The Hatch Pet
contribution here is the **per-family quality-gate text** — `qualityGate`
strings already exist in this file, but they're terse; they should grow
into per-family Hatch-Pet-style rubric blocks (see Phase 1
`WEVITO_ANIMATION_QA_RUBRIC.md`).

### 6g. PC helper / tool functions beyond "just a game"

This is the headline opportunity. Wevito already has the broker process,
clipboard hooks, pinned mode, and a 5-slot hotbar. The pet's animation
states are currently *self-contained game verbs*. Phase 4 + Phase 5 turn
the pet into a **work-state mirror**:

```text
GAME LOOP            ASSISTANT LOOP
============         ====================
hunger/thirst        link captured (Wave)
play_ball            invalid clipboard (Failed)
sleep                focus session (Waiting)
sad                  build broken (Failed, longer)
happy                build green (Jumping)
                     repair queue scanning (Review)
```

Both loops drive the same `PetAnimationState`. The pet doesn't act *for*
the user; it *reflects* the system state, which is the right line.

### 6h. Future webtools slots after Link Bin

Order of value:

1. Clipboard Shelf (Phase 5).
2. Focus Timer (uses pin + Waiting/Sleep states the pet already supports).
3. Drop Shelf (drag-and-drop captures; reuses broker drop targets).
4. Work Monitor (build/test in user mode; SpriteRepair queue in dev mode).

### 6i. Work-companion states

`waving`, `jumping`, `failed`, `waiting`, `review`, `running` — all
should be added (Phase 4) and immediately fall back to the existing 8
base animations when their authored frames don't exist. Authoring is
deferred to Phase 4b/Phase 6 where the Phase 1-2 pipeline carries the
weight.

---

## 7. Risks, Blockers, Validation

### 7a. Risks

| Risk | Mitigation |
|---|---|
| Lane A's Sprite Workflow App may already own a manifest schema | Phase 1 doc only; coordinate via the doc *before* Phase 2 tools. The contract is meant to be the meeting point, not Wevito's. |
| `SpriteIssueKind` enum changes break tests in flight | Phase 3 is gated behind Lane A's `SpriteRuntimeCoverageTests.cs` edits landing first. |
| `PetAnimationState` enum changes break persisted state | New states must be additive at the end of the enum and have idle/happy/sad fallbacks in `SpriteAssetService`. Cover with a serialization round-trip test. |
| Gemini Web reflow keeps damaging boards faster than guides catch it | Phase 2 guide is an additional defense, not a fix. Maintain the existing salvage importer. Reject by default; require explicit `--right` salvage. |
| Phase 5's clipboard shelf duplicates Lane A workflow app intent | The app is a *dev/QA tool* on the desktop; the shelf is an *end-user* runtime feature in Wevito itself. Different surfaces. |
| Authoring grip-pose batches before tooling is ready repeats reseed history | Block Phase 6 on Phase 1-2 finalize helper landing. |

### 7b. Blockers (today, this hour)

- Lane A is mid-edit on shared docs; do **not** modify
  `VNEXT_IMPLEMENTATION_CHECKLIST.md` or any of the new
  `SPRITE_WORKFLOW_APP_*.md` / `EXPRESSION_QUALITY_AUDIT_*.md`.
- Anything under `tools/` that is dirty (`MM` flag in `git status`) is
  Lane A's; new Phase 2 tools should be **new files** with new names.
- The repo already shipped optional families to 100% authored-complete on
  packaged blue sweeps. Don't re-trigger sweeps just to verify the plan.

### 7c. Validation expectations per phase

```text
PHASE 1 — docs only
  ✓ markdown lints / typo-checks
  ✓ JSON schema validates against a hand-written sample run
  ✓ no .cs / .py / .gd file changes

PHASE 2 — tools (new files)
  ✓ python -m compileall succeeds for new tools
  ✓ generate_layout_guide.py outputs match expected dimensions for each frame_count
  ✓ prepare_wevito_animation_run.py round-trips manifest through schema
  ✓ finalize_wevito_animation_run.py produces contact sheet + preview on a fixture
  ✓ no edits to existing tools

PHASE 3 — vNext repair taxonomy
  ✓ tools\build-vnext.ps1 -Configuration Debug succeeds
  ✓ all existing tests still pass (esp SpriteValidatorTests, RepairPlannerTests)
  ✓ new tests cover SizeOutlier + SparseFrame + ChromaAdjacentResidue detectors
  ✓ packaged sprite audit still 0 errors / 0 warnings
  ✓ no API change visible to ToolPopupWindow

PHASE 4 — runtime work-companion states
  ✓ build + tests
  ✓ focused-mode screenshot harness still passes
  ✓ pinned-mode probe still passes
  ✓ a new "state fallback" test asserts each new state resolves to an existing frame set

PHASE 5 — webtools slot 2 (Clipboard Shelf)
  ✓ clipboard hook regression covered by broker tests
  ✓ persistence round-trip (CompanionState carry over)
  ✓ shelf popup capture in screenshot harness
  ✓ no regression in basket popup

PHASE 6 — grip-pose authoring (per family)
  ✓ candidate validation 0 invalid
  ✓ packaged blue sweep 60/60
  ✓ contact sheet visually reviewed (mandatory, not just JSON)
  ✓ readiness audit unchanged or better
```

---

## 8. Recommended First Low-Risk Task

**Land Phase 1 only — the contract docs + the manifest schema.**

That is:

```text
+ docs/WEVITO_ANIMATION_GENERATION_CONTRACT.md       NEW
+ docs/WEVITO_ANIMATION_QA_RUBRIC.md                 NEW
+ docs/WEVITO_WORK_COMPANION_STATES.md               NEW
+ docs/wevito-animation-run.schema.json              NEW
```

No code, no behavior, no edits to Lane A's files. This is the safest
checkpoint to reach alignment with the Sprite Workflow App's owner
before Phase 2 tooling shows up.

---

## 9. Hatch Pet Lessons In One Diagram (Wevito-flavored)

```text
GOOD WEVITO ASSET PIPELINE (Hatch-Pet-aligned)
  |
  +-- source truth
  |     +-- canonical incoming_sprites/<species>-<age>.png
  |     +-- prop_anchor_metadata
  |
  +-- generation work
  |     +-- run manifest (provider-neutral)
  |     +-- terse, identity-locked prompts
  |     +-- layout-guide attachment
  |     +-- forbid speed lines / shadows / dust / text / borders
  |     +-- canonical base + verified runtime strip as references
  |
  +-- ingest
  |     +-- record selected source
  |     +-- sha256 + provider + timestamp
  |     +-- one true `decoded/` path
  |
  +-- deterministic processing
  |     +-- chroma/matte cleanup (where intentional + labeled)
  |     +-- component / slot extraction
  |     +-- frame fitting to runtime contract (28x24)
  |     +-- color propagation across 6 colors
  |     +-- backup before apply
  |
  +-- QA
  |     +-- geometry + transparency
  |     +-- chroma-adjacent + edge-pixel detection
  |     +-- silhouette + palette stability
  |     +-- contact sheet (mandatory)
  |     +-- preview MP4/GIF (mandatory)
  |     +-- packaged runtime proof
  |
  +-- repair
  |     +-- smallest failing scope first
  |     +-- vocabulary aligned with Hatch Pet rubric
  |     +-- runtime queue + Sprite Workflow App share schema
  |     +-- IdentityDrift -> ManualReview only
  |
  +-- runtime reaction
        +-- pet animation states bound to system events
        +-- waving/jumping/failed/waiting/review join the base 8
        +-- webtools slots 2-5 surface useful PC features
        +-- the pet *reflects* state instead of just *playing*
```

---

## 10. Bottom Line

Wevito has already paid the architectural price Hatch Pet preaches —
SpriteRepair is real, ValidationDimension is real, the asset
authored ▸ runtime ▸ shared-runtime fallback chain is real, the optional
animation families are 100% authored-complete and packaged-validated.

The big remaining gains from Hatch Pet are:

1. **A shared, written contract** (manifest + QA rubric) so the Sprite
   Workflow App, the Gemini handoff tools, and the vNext repair queue
   speak the same vocabulary.
2. **Layout guides** to harden the Gemini Web import path that has been
   the recurring source of pain.
3. **Mandatory contact sheets + preview videos** so technical-pass-but-
   visual-fail rows (`hold_ball`-class) stop slipping past the alpha gate.
4. **Work-companion runtime states** that turn the pet into a useful
   desktop assistant rather than a self-contained game.
5. **Webtools slot 2** as the first concrete demonstration of #4.

Phase 1 is four docs and a schema — that's the right first PR.
