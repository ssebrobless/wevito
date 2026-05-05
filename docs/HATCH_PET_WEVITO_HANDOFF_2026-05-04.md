# Wevito Handoff: Hatch Pet Skill Review And Applicability

Date: 2026-05-04

Primary sources:

- OpenAI Hatch Pet skill: https://github.com/openai/skills/blob/main/skills/.curated/hatch-pet/SKILL.md
- Hatch Pet skill directory: https://github.com/openai/skills/tree/main/skills/.curated/hatch-pet
- Local Wevito root: `C:\Users\fishe\Documents\projects\wevito`

Purpose: give Claude enough context to understand the current state/direction of Wevito, what the other active thread appears to be doing, and which parts of OpenAI's `hatch-pet` skill are worth adapting into Wevito.

## Current Shape

```text
╔════════════════════════════════════════════════════════════════════════════╗
║ WEVITO RIGHT NOW                                                         ║
╠════════════════════════════════════════════════════════════════════════════╣
║ Product                                                                  ║
║   desktop companion / virtual pet overlay                                ║
║   ├─ focused HUD                                                         ║
║   ├─ passive roam band                                                   ║
║   ├─ pinned always-on-top mode                                           ║
║   └─ webtools popup, currently led by Link Bin                           ║
║                                                                           ║
║ Simulation                                                               ║
║   10 species × 3 ages × 2 genders × 6 colors                             ║
║   ├─ needs, actions, conditions, habits, personality                      ║
║   ├─ biological aging and death/ghost rendering                           ║
║   └─ species habitats, props, care icons, optional action animation       ║
║                                                                           ║
║ Asset pipeline                                                           ║
║   incoming_sprites ──▶ authored/verified ──▶ sprites_runtime             ║
║        │                  │                         │                    ║
║        │                  │                         └─ app loads this    ║
║        │                  └─ curated motion source                       ║
║        └─ canonical look / source truth                                  ║
║                                                                           ║
║ Active direction                                                         ║
║   finish high-confidence visual/action polish, especially optional        ║
║   action families and automated repair/QA, while preserving PC utility    ║
║   surfaces instead of turning Wevito into only a game.                    ║
╚════════════════════════════════════════════════════════════════════════════╝
```

## Current Repo State

The repo is dirty and another thread appears to be actively working in it. I did not modify app code; this document is the only intended new repo file from my pass.

Important status notes from `git status --porcelain`:

- Branch is `main`, ahead of `origin/main` by 1 commit.
- There is a lot of generated/staged noise under `.codex-cache`, `.vs`, and `artifacts/recovery`.
- Real project work is concentrated in:
  - `docs/VNEXT_IMPLEMENTATION_CHECKLIST.md`
  - `scripts/game_manager.gd`
  - `scripts/main_scene.gd`
  - `scripts/pet.gd`
  - `tools/*` sprite audit/repair/import/Gemini/optional-animation helpers
  - `vnext/src/Wevito.VNext.*`
  - `vnext/tests/Wevito.VNext.Tests/*`
  - `vnext/content/optional_animation_families.json`
  - `.sprite-workflow/*`

The active work looks like two overlapping lanes:

```text
current thread work
  |
  +-- optional animation production
  |     ├─ drink
  |     ├─ play_ball
  |     ├─ hold_ball
  |     ├─ pickup_ball
  |     ├─ drop_ball
  |     ├─ carry_ball_walk
  |     └─ carry_ball_run
  |
  +-- runtime/QA hardening
        ├─ prop anchors and mouth/beak alignment
        ├─ carried prop scaling
        ├─ hold_ball reseeded from safe idle poses
        ├─ visible-noise source-aware clearance
        └─ vNext repair automation infrastructure
```

The current queue section in `docs/VNEXT_IMPLEMENTATION_CHECKLIST.md` says the optional drink/ball lane is now broadly complete and packaged-validated:

- `2,520 / 2,520` optional drink/ball frame/color targets authored-complete.
- `0` optional drink/ball targets remain fallback-only.
- `360` drink/ball prop-anchor metadata rows active.
- Full blue packaged sweeps passed `60 / 60` for all seven optional families.
- Hold-ball broken authored poses were reseeded from clean idle rows, which is safer but less expressive than bespoke grip poses.
- Main remaining visual-polish issue: true species-specific mouth/paw/beak grip poses, beyond overlay anchors.

## Game Direction

Wevito has an older Godot project and a newer `vnext` native desktop runtime. The README still describes the original Godot rebuild, but the current development center is clearly vNext.

```text
legacy Godot lane
  project.godot
  scenes/main.tscn
  scripts/*.gd
    ├─ original virtual pet loop
    ├─ transparent always-on-top window
    ├─ pets, stats, actions, conditions
    └─ now also has optional action families and prop anchor metadata

vNext native desktop lane
  vnext/src/Wevito.VNext.*
    ├─ WPF shell windows
    ├─ broker process for desktop context/hotkeys/clipboard/drop targets
    ├─ companion state persistence
    ├─ richer pet simulation
    ├─ sprite asset resolution
    ├─ Link Bin + tool popup
    └─ repair automation/dev tooling
```

Current player-visible concept:

- A desktop pet/companion that lives over the PC rather than inside a normal game window.
- Focused mode shows a fuller home/HUD.
- Passive mode lets pets roam while another app is foregrounded.
- Pinned mode keeps the UI interactive over other apps.
- The pet system is richer than Tamagotchi basics: needs, action outcomes, comfort, fitness, medical conditions, personality, habits, aging, death, ghost rendering, habitat zones, and action props.
- The PC utility surface exists already through the Link Bin and a five-slot webtools hotbar, with four slots reserved for future tools.

Current engineering direction:

- Preserve the canonical uploaded pet look from `incoming_sprites`.
- Prefer verified authored frames from `sprites_authored_verified` when quality is high enough.
- Ship from `sprites_runtime`.
- Use audits, contact sheets, packaged screenshot proofs, and contract tests before accepting visual assets.
- Keep visual generation/import controlled; reject resized, clipped, boxed, matte-contaminated, or geometry-drifted candidates.

## Local Files Claude Should Read First

```text
C:\Users\fishe\Documents\projects\wevito\README.md
C:\Users\fishe\Documents\projects\wevito\docs\VNEXT_IMPLEMENTATION_CHECKLIST.md
C:\Users\fishe\Documents\projects\wevito\docs\SPRITE_SOURCE_OF_TRUTH.md
C:\Users\fishe\Documents\projects\wevito\docs\AUTHORED_ANIMATION_WORKFLOW.md
C:\Users\fishe\Documents\projects\wevito\vnext\content\optional_animation_families.json
C:\Users\fishe\Documents\projects\wevito\vnext\src\Wevito.VNext.Shell\ShellCoordinator.cs
C:\Users\fishe\Documents\projects\wevito\vnext\src\Wevito.VNext.Shell\ToolPopupWindow.xaml.cs
C:\Users\fishe\Documents\projects\wevito\vnext\src\Wevito.VNext.Shell\SpriteAssetService.cs
C:\Users\fishe\Documents\projects\wevito\vnext\src\Wevito.VNext.Core\PetSimulationEngine.cs
C:\Users\fishe\Documents\projects\wevito\vnext\src\Wevito.VNext.Core\SpriteRepair\
C:\Users\fishe\Documents\projects\wevito\vnext\tests\Wevito.VNext.Tests\
```

## Hatch Pet: What It Is

The OpenAI `hatch-pet` skill is not a runtime virtual-pet game. It is a Codex skill for producing Codex-compatible animated pet assets.

It can:

- Take a text concept, one or more reference images, or both.
- Infer pet name/description when missing.
- Generate a base pet image with `$imagegen`.
- Generate animation row strips from the base and references.
- Create row-specific prompts and layout guides.
- Enforce a fixed atlas geometry.
- Extract row strips into transparent frames.
- Validate geometry, transparency, frame counts, chroma-key artifacts, and visual QA outputs.
- Compose a final sprite atlas.
- Produce a contact sheet and preview videos.
- Package `pet.json` plus `spritesheet.webp` into a Codex custom pet folder.
- Queue targeted repairs for failed rows instead of regenerating the entire pet.

It does not:

- Implement a live companion AI.
- Add PC tools like clipboard capture, link opening, desktop automation, reminders, or file management.
- Directly match Wevito's runtime frame contract.

That distinction matters: the big value for Wevito is the production discipline, QA loop, visual state taxonomy, and "pet as work companion" animation vocabulary.

## Hatch Pet Asset Contract

```text
Codex Hatch Pet final package
  |
  +-- ${CODEX_HOME}/pets/<pet-name>/
        |
        +-- pet.json
        `-- spritesheet.webp

spritesheet geometry
  |
  +-- 8 columns × 9 rows
  +-- 192 × 208 px cell
  +-- 1536 × 1872 px atlas
  +-- transparent background
  `-- unused cells fully transparent
```

Animation rows in Hatch Pet:

| Row | State | Frames | Purpose |
| --- | --- | ---: | --- |
| 0 | `idle` | 6 | neutral breathing/blinking |
| 1 | `running-right` | 8 | rightward locomotion |
| 2 | `running-left` | 8 | leftward locomotion |
| 3 | `waving` | 4 | greeting/attention |
| 4 | `jumping` | 5 | anticipation/lift/settle |
| 5 | `failed` | 8 | error/sad/deflated reaction |
| 6 | `waiting` | 6 | patient idle variant |
| 7 | `running` | 6 | generic/in-place run |
| 8 | `review` | 6 | focus/inspection/thinking |

Wevito's current runtime is different:

```text
Wevito runtime
  |
  +-- individual PNG frames, not one Codex atlas
  +-- path shape:
  |     sprites_runtime/<species>/<age>/<gender>/<color>/<animation>_<nn>.png
  |
  +-- required base animations:
  |     idle, walk, eat, happy, sad, sleep, sick, bathe
  |
  +-- optional action families:
        drink, play_ball, hold_ball, carry_ball_walk,
        carry_ball_run, pickup_ball, drop_ball
```

Conclusion: do not copy Hatch Pet's atlas format into Wevito as-is. Borrow the pipeline ideas and adapt them to Wevito's per-frame tree.

## Hatch Pet Workflow Inventory

```text
prepare_pet_run.py
  |
  +-- pet_request.json
  +-- imagegen-jobs.json
  +-- prompts/base-pet.md
  +-- prompts/<state>.md
  +-- references/canonical-base.png
  +-- references/layout-guides/<state>.png
  `-- decoded/<state>.png targets

$imagegen
  |
  +-- base job
  `-- row-strip jobs
       ├─ grounded by canonical base
       ├─ grounded by original references
       └─ grounded by layout guide

record_imagegen_result.py
  |
  +-- validates provenance
  +-- copies selected image to decoded/
  +-- records hashes, metadata, completion time
  `-- writes canonical base after base job

finalize_pet_run.py
  |
  +-- require all jobs complete
  +-- extract_strip_frames.py
  +-- inspect_frames.py
  +-- compose_atlas.py
  +-- validate_atlas.py
  +-- make_contact_sheet.py
  +-- render_animation_videos.py
  `-- package_custom_pet.py

queue_pet_repairs.py
  |
  +-- read QA failures
  +-- reopen only failed row jobs
  `-- preserve run history / archive failed outputs
```

Script-by-script capabilities:

| File | Capability | Wevito relevance |
| --- | --- | --- |
| `prepare_pet_run.py` | Creates run folder, chooses name/description/chroma key, writes prompts, jobs, layout guides | Very high: adapt into `prepare_wevito_animation_run.py` for species/action families |
| `pet_job_status.py` | Shows ready/blocked/completed visual jobs from manifest dependencies | High: useful for Wevito optional-family queues |
| `record_imagegen_result.py` | Ingests selected generated image with provenance and grounding validation | Very high: Wevito needs stronger provenance around Gemini/Web outputs |
| `extract_strip_frames.py` | Removes chroma-key, component-splits or slot-splits row strips, fits frames to cells | High: Wevito already has importers; borrow component-first/slot-fallback manifest reporting |
| `inspect_frames.py` | Detects frame count, sparsity, edge pixels, chroma-adjacent artifacts, size outliers | High: overlaps Wevito audits, but the warnings/errors model is worth copying |
| `compose_atlas.py` | Composes 8x9 atlas | Low direct value, but composition discipline maps to Wevito contact sheets |
| `validate_atlas.py` | Validates atlas dimensions, used/unused cells, transparency | Medium conceptually; adapt to Wevito per-frame family validation |
| `make_contact_sheet.py` | Builds checkerboard contact sheet | High: Wevito already relies on boards; make this standard for every optional family |
| `render_animation_videos.py` | Produces per-state video previews | High: Wevito would benefit from action-family GIF/MP4 proofs, not only still boards |
| `package_custom_pet.py` | Writes Codex custom pet package | Low direct value; useful as model for future "installable Wevito pet pack" |
| `derive_running_left_from_running_right.py` | Mirrors right run only with explicit approval and provenance | Medium: use the same explicit "safe mirror" rule for symmetric Wevito actions |
| `queue_pet_repairs.py` | Requeues smallest failing row scope | Very high: matches Wevito's current repair automation direction |
| `generate_pet_images.py` | Secondary OpenAI API fallback when `$imagegen` unavailable | Low unless Wevito chooses OpenAI image gen as an alternate to Gemini |

## Hatch Pet Quality Rules Worth Keeping

The most useful Hatch Pet rules map directly onto problems Wevito has already been fighting.

```text
problem seen in Wevito             Hatch Pet rule to borrow
────────────────────────────────   ───────────────────────────────────────
Gemini checker/matte residue       explicit chroma-key + chroma-adjacent QA
tiny detached particles            forbid detached effects and stray pixels
cropped/boxed board outputs         layout guides + edge-pixel warnings
identity drift by generation        canonical base + identity lock prompts
oversized/undersized rows           per-row area outlier warnings
manual import uncertainty           manifest, hashes, provenance metadata
broad bad generations               repair smallest failing row only
technical pass but visual fail      contact sheet visual inspection required
```

Hatch Pet's prompt discipline is especially relevant:

- Use terse, sprite-specific prompts.
- Keep a canonical base reference attached for every row job.
- Attach a layout guide for frame count, spacing, centering, and safe padding.
- Reject outputs that include visible guide marks, text, UI, white/black backgrounds, checkerboards, shadows, glows, smears, speed lines, dust, or detached effects.
- Treat identity drift as a blocker even if deterministic geometry passes.
- Do not pretend a generated asset is complete by editing a manifest; record the actual selected source and hashes.

## What To Apply To Wevito

### 1. A Wevito Animation Run Manifest

Wevito currently has many helpful tools, but the generation/import story is spread across scripts, docs, and artifact reports. Hatch Pet's `imagegen-jobs.json` idea would make Wevito optional-family production easier to continue safely.

Suggested Wevito manifest:

```text
wevito-animation-run.json
  |
  +-- run_id
  +-- species / age / gender / source_color
  +-- target_family: drink | play_ball | hold_ball | ...
  +-- expected_frames
  +-- source references
  |     ├─ canonical incoming source board
  |     ├─ verified runtime/current family frames
  |     ├─ prop anchor metadata excerpt
  |     └─ optional layout guide
  +-- generation jobs
  |     ├─ prompt_path
  |     ├─ input_image_paths
  |     ├─ expected_output_geometry
  |     ├─ provider: gemini-web | openai-imagegen | manual
  |     └─ status/hash/provenance
  +-- import results
  |     ├─ candidate frame paths
  |     ├─ cleanup operations used
  |     ├─ validation report
  |     └─ apply backup path
  `-- packaged proof paths
```

This would make Claude's future work less dependent on reading a giant checklist and more dependent on structured state.

### 2. Layout Guides For Wevito Focused Boards

The active thread notes Gemini Web reflowed/upscaled focused boards. Hatch Pet handles this by generating a per-row layout guide and treating it as a construction reference.

Wevito adaptation:

- Generate a transparent or high-contrast guide image for each optional family and frame count.
- Include safe margins, exact cell slots, and no text.
- Upload it as a layout-only reference with the focused board.
- Add a validation rule that rejects outputs that copy guide pixels, but uses the guide to diagnose spacing/crop drift.

This will not fully solve Gemini Web resizing, but it gives the importer a stronger expected geometry contract.

### 3. Per-Family Contact Sheets And Preview Videos

Wevito already has packaged visual boards. Hatch Pet makes contact sheet and video preview part of finalization.

Wevito should standardize:

```text
after any optional family apply
  |
  +-- per-target contact sheet
  +-- all-color propagated sheet
  +-- packaged runtime screenshot proof
  +-- short animation preview per species/age/gender
  +-- validation JSON
  `-- markdown summary with pass/fail and manual notes
```

This would help catch issues like the `hold_ball` broken authored poses earlier, before they pass a contract but fail visual intuition.

### 4. Repair Automation Should Borrow Hatch Pet's Scope Rule

Hatch Pet's repair policy is simple and good:

```text
repair scope
  |
  +-- single bad frame
  +-- one row/family
  `-- full regeneration only when identity/layout is broadly broken
```

Wevito's new vNext repair classes already move this way:

- `SpriteRepairContracts.cs`
- `RepairPlanner.cs`
- `RepairPipelineOrchestrator.cs`
- `RepairAutomationRunner.cs`
- `SpriteValidator.cs`
- `LocalDeterministicRepairEngine.cs`
- `Wevito.VNext.AutomationRunner`

Recommended next step: align the repair automation's issue taxonomy with Hatch Pet's QA vocabulary:

- matte/background residue
- detached component
- body hole
- edge/crop risk
- size outlier
- palette drift
- silhouette drift
- identity drift/manual review

### 5. Add Work-Companion Animation States

Hatch Pet includes `review`, `waiting`, `failed`, `waving`, `jumping`, and `running` states. These are not Wevito's current base animation names, but they are valuable because they map to real PC helper states.

Suggested Wevito state mapping:

| Work/PC event | Pet state | Current fallback | Future authored family |
| --- | --- | --- | --- |
| Link captured successfully | attention/success | `happy` | `waving` or `jumping` |
| Invalid clipboard URL | error | `sad` or `sick` | `failed` |
| Opening a saved link | working | `walk` | `running` |
| Tool popup waiting for user choice | standby | `idle` | `waiting` |
| Repair automation scanning queue | inspection | `idle`/`walk` | `review` |
| Repair applied | success | `happy` | `jumping` |
| Repair escalated | needs help | `sad` | `failed` |
| Focus session active | quiet support | `sleep`/`idle` | `waiting` |
| Build/test command running | busy | `walk` | `running` |

This helps make Wevito feel like a useful desktop companion rather than a pet whose actions are only self-contained game verbs.

### 6. PC Tool Slots That Fit The Project Direction

Current Wevito has a five-slot webtools hotbar with Link Bin live and four empty slots reserved. Hatch Pet itself does not supply tools, but its state vocabulary can make tools feel alive and readable.

Concrete tool ideas that fit Wevito:

```text
webtools hotbar
  |
  +-- 1. Link Bin       existing
  |     paste/capture/open/delete URLs
  |
  +-- 2. Clipboard Shelf
  |     capture text snippets, image paths, file paths, and URLs;
  |     pet reacts with success/error states
  |
  +-- 3. Focus Timer
  |     starts a focus session, pins/unpins HUD, shows quiet waiting state,
  |     gives break reminders
  |
  +-- 4. Drop Shelf
  |     drag files/URLs into the pet; store in a staging list or project inbox;
  |     later open folder/link or copy paths
  |
  +-- 5. Work Monitor
        dev/user mode split:
        ├─ user: build/test/task command status
        └─ dev: sprite repair queue scan/run/escalations
```

Guardrails:

- Any action that opens links, edits files, deletes items, runs commands, or changes system state should stay explicit and visible.
- Pet reactions should report tool state, not hide it behind cute behavior.
- The "game" and "assistant" loops should share the same pet, but not compete for control.

### 7. Future "Hatch A Wevito Pet" Feature

Hatch Pet's package model could inspire a Wevito creator pipeline:

```text
user concept/reference
  |
  +-- generate canonical base/source board
  +-- generate required animations
  +-- generate optional family subset
  +-- validate against Wevito frame tree
  +-- package as:
        wevito_pets/<pet-id>/
          ├─ pet.json
          ├─ sprites_runtime/...
          ├─ source_refs/...
          ├─ qa/...
          └─ license/provenance.json
```

This should be a later feature. The current project has enough complexity finishing the built-in species and PC utility surface first.

## What Not To Copy Directly

- Do not switch Wevito to the Codex 8x9 atlas. It does not match Wevito's runtime tree or current authored pipeline.
- Do not add Hatch Pet's `pet.json` loader directly unless the product decision is to support external pet packs.
- Do not force `$imagegen` as the only provider if the project continues to use Gemini Web. Instead, copy the provider-neutral manifest/provenance model.
- Do not adopt the Hatch Pet ban on deterministic local transforms wholesale. Wevito already uses deterministic synthesis, color propagation, prop overlays, and safe reseeding as intentional runtime strategies. The better rule is: generated new art needs provenance and visual QA; deterministic repair/synthesis is allowed when clearly labeled and validated.
- Do not treat deterministic validation as enough. Hatch Pet is correct that contact sheets/manual visual inspection remain necessary.

## Recommended Claude Plan

```text
Phase 1: document/contract cleanup
  |
  +-- create docs/WEVITO_ANIMATION_GENERATION_CONTRACT.md
  +-- define Wevito's manifest fields, provenance rules, QA gates
  +-- map Hatch Pet states to Wevito utility/action states

Phase 2: manifest-first optional family pipeline
  |
  +-- add tools/prepare_wevito_animation_run.py
  +-- add layout-guide output for optional families
  +-- emit prompts + run manifest + expected frame schema

Phase 3: finalize/QA standardization
  |
  +-- add tools/finalize_wevito_animation_run.py
  +-- produce contact sheets, optional preview videos/GIFs, validation JSON
  +-- write one markdown summary per run

Phase 4: repair automation alignment
  |
  +-- connect vNext repair queue issue taxonomy to existing Python audits
  +-- keep local deterministic repairs first
  +-- escalate Codex/Gemini/manual only with explicit provider notes

Phase 5: PC tool expansion
  |
  +-- keep Link Bin as slot 1
  +-- prototype Clipboard Shelf or Focus Timer as slot 2
  +-- use review/waiting/failed/success animation states for tool feedback
```

Fastest useful first PR:

1. Add `docs/WEVITO_ANIMATION_GENERATION_CONTRACT.md`.
2. Add a small provider-neutral manifest schema for one optional family run.
3. Add a layout-guide generator for Wevito's current optional frame counts.
4. Do not touch live runtime behavior yet.

This is low risk and gives future generation/import work a firmer runway.

## Hatch Pet Lessons In One Diagram

```text
good pet asset pipeline
  |
  +-- source truth
  |     └─ canonical base/reference, not a vague prompt
  |
  +-- generation work
  |     ├─ small row/family jobs
  |     ├─ layout guides
  |     ├─ grounded references
  |     └─ no local fake-completion
  |
  +-- ingest
  |     ├─ selected source path
  |     ├─ hash/provenance
  |     └─ exact output path
  |
  +-- deterministic processing
  |     ├─ background removal
  |     ├─ component/slot extraction
  |     ├─ frame fitting
  |     └─ package assembly
  |
  +-- QA
  |     ├─ geometry
  |     ├─ transparency
  |     ├─ edge/chroma artifacts
  |     ├─ size outliers
  |     ├─ contact sheet
  |     └─ videos/runtime proof
  |
  `-- repair
        ├─ smallest failing scope
        ├─ preserve identity
        └─ never call it done without visual review
```

## Bottom Line

Hatch Pet is valuable for Wevito, but mostly as a production architecture, not as a drop-in feature.

Best things to lift:

- Manifested generation jobs.
- Canonical reference/base image discipline.
- Layout guides per animation row/family.
- Strict artifact/transparent-background prompt rules.
- Provenance and hash recording.
- Contact-sheet/video QA as standard outputs.
- Targeted repair queues.
- Work-companion states like `review`, `waiting`, `failed`, `waving`, and `jumping`.

Best Wevito-specific next direction:

- Keep finishing the vNext desktop companion and PC utility surfaces.
- Add a manifest-first Wevito animation generation contract.
- Turn the empty webtools slots into useful PC tools, with pet animations reflecting real tool states.
- Use Hatch Pet's discipline to make future asset generation less fragile, especially for bespoke grip/action poses.
