# Sprite Workflow App Build Spec

Updated: 2026-03-18

```text
╔══ Sprite Workflow App ═══════════════════════════════════════════════════╗
║ Product: separate cross-platform desktop app                            ║
║ First project profile: Wevito                                           ║
║ Purpose: generate, review, edit, repair, and manage sprite workflows    ║
╠═══════════════════════════════════════════════════════════════════════════╣
║ Project Picker                                                           ║
║   ▼                                                                       ║
║ Asset Browser ──▶ Animation Viewer ──▶ Review / Notes / Status           ║
║   │                                  │                                    ║
║   ├──────────────▶ Request System ◀──┤                                    ║
║   │                                  │                                    ║
║   └──────────────▶ Workflow Actions ─┴─▶ Import / Propagate / Verify     ║
║                                      │                                    ║
║                                      └─▶ Embedded Pixel Editor           ║
╚═══════════════════════════════════════════════════════════════════════════╝
```

## 1. Overview

The Sprite Workflow App is a reusable desktop application for sprite and animation pipeline work across projects. It is not a Wevito feature. It is a separate tool that can load multiple project profiles, each with its own folder layout, manifests, animation families, scripts, and AI workflow settings.

Wevito is the first configured project profile and the initial proving ground.

The app must support the full asset lifecycle:

1. browse existing assets
2. review animations outside the game
3. write notes and mark issues
4. request new work or repairs
5. run project-specific workflow actions
6. repair assets manually in-app
7. verify imported results before approval

## 2. Product Goals

### Primary goals

- Provide a single reusable desktop tool for sprite and animation workflows across projects.
- Make visual review a first-class gate before import and gameplay testing.
- Reduce bad batch imports by requiring structured review and status tracking.
- Support fast manual repairs for small issues without round-tripping to AI.
- Capture requests, notes, constraints, and repair instructions in a structured format that can be reused in future AI prompts.

### Secondary goals

- Replace ad hoc script juggling with a project-aware UI.
- Centralize history for generated results, review decisions, and repairs.
- Make it easy to compare authored, runtime, reference, and edited assets.

### Non-goals for first release

- Full Photoshop-class image editing.
- Full animation rigging or timeline authoring.
- Built-in model inference; AI generation can remain provider-driven and script-backed at first.
- Cloud sync or multi-user collaboration.

## 3. Recommended Technical Stack

### App framework

- `C# + .NET 8+`
- `Avalonia UI`

### Why this stack

- Cross-platform on Windows and macOS.
- Strong desktop app ergonomics.
- Good fit for image-heavy tooling.
- Easy to share logic patterns with existing .NET tooling if needed.
- Suitable for local file orchestration and external process execution.

### Supporting libraries

- `Avalonia`
- `SkiaSharp` or Avalonia image primitives for pixel rendering
- `System.Text.Json` for config/state persistence
- optional image helpers for palette operations and diff views

## 4. Product Scope

### Core modules

```text
╔══ Core Modules ═════════════════════════════════════════╗
║ Project System                                          ║
║ Asset Indexer                                           ║
║ Asset Browser                                           ║
║ Animation Viewer                                        ║
║ Review System                                           ║
║ Request System                                          ║
║ Workflow Actions                                        ║
║ Repair Queue                                            ║
║ Sprite Editor                                           ║
║ AI Handoff Builder                                      ║
╚══════════════════════════════════════════════════════════╝
```

### Project-aware behavior

The app core stays reusable. Each project provides:

- folder paths
- species / variant axes
- family definitions
- script/action mappings
- prompt templates
- provider preferences

## 5. User Personas

### Primary user

- artist/designer using AI and manual edits to build sprite sets

### Secondary user

- technical pipeline operator who prepares, imports, propagates, and audits assets

### Tertiary user

- reviewer who marks assets approved or flags them for repair

## 6. Core User Flows

### A. Review existing sprites

1. open app
2. choose project
3. browse species / age / gender / color / family
4. play animation in viewer
5. compare authored vs runtime vs reference
6. add notes
7. mark `approved`, `needs_review`, or `to_be_repaired`

### B. Create a new request

1. open project
2. click `Make Request`
3. select request type
4. fill structured fields
5. save request
6. optionally generate AI-ready handoff text

### C. Repair an existing row

1. open affected variant/family
2. inspect animation
3. mark issue and write notes
4. choose manual repair or AI repair
5. if manual, edit frames in-app
6. if AI, generate structured repair request
7. re-import result
8. re-review before approval

### D. Run a workflow step

1. choose project asset or queued task
2. click a project-defined action
3. watch logs and outputs in-app
4. review result
5. approve or reject

## 7. Information Architecture

```text
╔══ App Navigation ═══════════════════════════════════════╗
║ Start                                                   ║
║  └─ Project Picker                                      ║
║                                                         ║
║ Project Workspace                                       ║
║  ├─ Dashboard                                           ║
║  ├─ Assets                                              ║
║  ├─ Viewer                                              ║
║  ├─ Review Queue                                        ║
║  ├─ Requests                                            ║
║  ├─ Workflow                                            ║
║  ├─ Editor                                              ║
║  └─ Settings                                            ║
╚═════════════════════════════════════════════════════════╝
```

## 8. Screen-by-Screen Spec

### 8.1 Project Picker

Purpose:
- choose an existing project profile
- create a new project profile
- view recent projects

Required features:
- recent projects list
- create/edit/remove project config
- validate paths before opening

### 8.2 Project Dashboard

Purpose:
- summarize project health and active work

Required features:
- coverage summary
- recent requests
- repair queue counts
- last workflow runs
- trust/suspect summary

### 8.3 Asset Browser

Purpose:
- navigate all project assets

Required features:
- filters for species, age, gender, color, family, source, status
- row/grid view
- badges for completeness and review status
- quick jump to viewer, notes, request, and workflow actions

### 8.4 Animation Viewer

Purpose:
- inspect animations outside the game

Required features:
- animation playback
- frame stepping
- speed control
- zoom and nearest-neighbor scaling
- compare modes:
  - authored vs runtime
  - current vs reference
  - multi-species compare
- safe/trusted-only toggle

### 8.5 Review Workspace

Purpose:
- store judgments and issues against assets

Required features:
- per-frame notes
- per-row notes
- issue tags
- review status buttons
- timestamp and reviewer metadata

### 8.6 Request Workspace

Purpose:
- define new asset work and repair work

Required features:
- `Make Request` entry point from dashboard and assets
- request list with filters
- request detail view
- generate AI-ready request summary

### 8.7 Workflow Panel

Purpose:
- run project-defined processes from the app

Required features:
- action buttons driven by config
- action log view
- stdout/stderr display
- stop/cancel where safe
- last run status

### 8.8 Sprite Editor

Purpose:
- fast frame touchups and repair work

Required features:
- pixel grid view
- brush
- eraser
- selection tool
- move selection
- dropper
- palette and color wheel
- undo/redo
- onion skin
- side-by-side reference compare

## 9. Data Model

### 9.1 ProjectConfig

Fields:
- `project_id`
- `display_name`
- `root_path`
- `runtime_sprite_root`
- `authored_sprite_root`
- `incoming_handoff_root`
- `artifact_root`
- `manifest_paths`
- `variant_axes`
- `families`
- `workflow_actions`
- `prompt_templates`
- `ai_adapters`

### 9.2 AssetRecord

Fields:
- `asset_id`
- `species`
- `age`
- `gender`
- `color`
- `family`
- `frame_names`
- `source_type`
- `completeness`
- `current_status`
- `path_info`

### 9.3 ReviewRecord

Fields:
- `review_id`
- `target_asset_id`
- optional `frame_name`
- `status`
- `tags`
- `notes`
- `created_at`
- `updated_at`

### 9.4 RequestRecord

Fields:
- `request_id`
- `request_type`
- `target_scope`
- `priority`
- `goal`
- `must_preserve`
- `must_avoid`
- `references`
- `notes`
- `status`
- `generated_prompt_text`

### 9.5 WorkflowRunRecord

Fields:
- `run_id`
- `action_id`
- `target_scope`
- `started_at`
- `ended_at`
- `status`
- `stdout_path`
- `stderr_path`

## 10. Status System

The review status model must be simple and visible everywhere.

Allowed statuses:
- `approved`
- `needs_review`
- `to_be_repaired`
- `replaced`
- `do_not_use`

Suggested issue tags:
- `anthropomorphic_drift`
- `bad_anatomy`
- `outline_broken`
- `placeholder_artifact`
- `off_model`
- `palette_issue`
- `bad_import`
- `missing_frames`

## 11. Request System Spec

### Request types

- `new_species`
- `new_variant`
- `new_animation_family`
- `repair_existing`
- `polish_existing`

### Request authoring rules

Every request should answer:

- what is being requested
- what asset it targets
- what should change
- what must stay the same
- what should be avoided
- what references matter

### AI-ready output

Each request should generate a structured handoff block that can be copied into a prompt or passed to a provider adapter.

## 12. Embedded Editor Spec

### Essential tools

- brush
- eraser
- selection
- move
- dropper
- palette picker
- color wheel
- zoom
- undo/redo

### High-value support tools

- onion skin
- previous/next frame compare
- outline cleanup workflow
- transparent background toggle
- reference overlay

### Editing boundaries

The in-app editor is for touchups and repair, not full concept creation.

## 13. Workflow Actions Spec

Each project config can define callable actions like:

- `prepare_handoff`
- `run_generation`
- `import_result`
- `propagate_colors`
- `rerender_previews`
- `refresh_coverage`
- `open_artifact_folder`

Each action definition should include:
- display name
- command
- arguments
- working directory
- expected outputs
- optional safety rules

## 14. AI Integration Strategy

The app should be AI-provider agnostic.

### Adapter types

- `manual_export`
- `manual_result_import`
- `Gemini`
- future `OpenAI`
- future local provider

### First release

The first release can focus on:
- generating handoff packs
- organizing prompts and notes
- storing results
- invoking existing project scripts

Browser automation should be treated as replaceable infrastructure, not core product identity.

## 15. Project Config Format

Suggested format: `project.json`

Example shape:

```json
{
  "project_id": "wevito",
  "display_name": "Wevito",
  "root_path": "C:/Users/fishe/Documents/projects/wevito",
  "runtime_sprite_root": "sprites_runtime",
  "authored_sprite_root": "sprites_authored_verified",
  "incoming_handoff_root": "incoming_sprites/gemini_handoff_motion",
  "artifact_root": "vnext/artifacts",
  "variant_axes": {
    "species": ["crow", "deer", "fox", "frog", "goose", "pigeon", "raccoon", "rat", "snake", "squirrel"],
    "age": ["adult", "teen", "baby"],
    "gender": ["female", "male"],
    "color": ["red", "orange", "yellow", "blue", "indigo", "violet"]
  },
  "families": {
    "locomotion": ["idle", "walk"],
    "care": ["eat", "sleep"],
    "expression": ["happy", "sad", "sick", "bathe"]
  }
}
```

## 16. Persistence

Use local JSON files in the app data area or project workspace.

Recommended files:
- `project.json`
- `reviews.json`
- `requests.json`
- `workflow-runs.json`
- `asset-cache.json`
- `appsettings.json`

## 17. Milestones

### Milestone 1: Foundation

- app shell
- project picker
- project config validation
- recent projects

### Milestone 2: Asset Intelligence

- asset indexer
- completeness detection
- browser filters

### Milestone 3: Viewer + Review

- animation viewer
- compare modes
- notes
- statuses

### Milestone 4: Requests + Queue

- request authoring
- repair queue
- trusted/suspect filters

### Milestone 5: Workflow Integration

- action runner
- logs
- rerender/coverage actions

### Milestone 6: Editor

- pixel editor
- touchup tools
- onion skin

### Milestone 7: AI Workflow Support

- AI-ready request output
- prompt history
- result history

## 18. Acceptance Criteria

### v1 acceptance

- can open Wevito as a project profile
- can browse assets by species/variant/family
- can play animations outside the game
- can write notes and mark `to_be_repaired`
- can create structured requests
- can run at least one workflow action from the app

### v2 acceptance

- can compare authored vs runtime vs reference
- can maintain a repair queue
- can generate AI-ready request text from review notes

### v3 acceptance

- can manually fix frames in-app
- can save repairs safely
- can review before/after changes

## 19. Risks

### Risk: project-specific sprawl

Mitigation:
- keep project adapters in config
- keep app logic generic

### Risk: weak review discipline

Mitigation:
- make status and notes visible everywhere
- gate imports through viewer/review

### Risk: AI drift during emotion-heavy families

Mitigation:
- preserve notes and hard constraints
- mark high-risk outputs before import

### Risk: editor complexity explosion

Mitigation:
- keep editor scoped to repair/touchup
- defer advanced art features

## 20. Wevito First-Use Plan

Use Wevito as the first full project profile.

Initial trusted implementation set:
- `crow`
- `deer`
- `fox`
- `rat`

Initial review priority:
- broken or suspect `expression` rows
- especially `frog`, `goose`, and `pigeon`

The app should be used first to:

1. load Wevito
2. browse trusted animals
3. review suspect expression rows
4. mark repair targets
5. generate structured repair requests

## 21. Immediate Next Steps

1. Create a separate repo for the app.
2. Scaffold the Avalonia shell.
3. Implement `ProjectConfig` and project picker.
4. Implement the asset indexer against Wevito.
5. Port the current standalone viewer concept into the new app as the first production feature.

