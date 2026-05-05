# Sprite Workflow App V1 Backlog

Updated: 2026-03-18

```text
╔══ V1 Goal ═══════════════════════════════════════════════════════════════╗
║ Ship a separate cross-platform desktop app that can:                    ║
║ 1. open a project profile                                               ║
║ 2. index sprite assets                                                  ║
║ 3. browse and filter them                                               ║
║ 4. play animations outside the game                                     ║
║ 5. record notes and review status                                       ║
║ 6. create structured requests                                           ║
║ 7. run a small set of project workflow actions                          ║
╚══════════════════════════════════════════════════════════════════════════╝
```

## 1. V1 Scope

V1 is the first usable release of the separate Sprite Workflow App.

V1 must support:
- project selection
- Wevito as the first project profile
- asset indexing from sprite folders
- browser/filter UI
- animation viewer
- review notes and statuses
- request creation
- basic workflow action runner

V1 does not need:
- full embedded pixel editor
- advanced AI provider integrations
- collaborative sync
- database storage
- automatic in-app browser automation

## 2. V1 Product Slice

```text
╔══ V1 Slice ═════════════════════════════════════════════╗
║ App Shell                                              ║
║ Project Picker                                         ║
║ Project Config Loader                                  ║
║ Asset Indexer                                          ║
║ Asset Browser                                          ║
║ Animation Viewer                                       ║
║ Review Notes + Status                                  ║
║ Request Creation                                       ║
║ Workflow Actions (script runner)                       ║
╚═════════════════════════════════════════════════════════╝
```

## 3. Repo Recommendation

Create a separate repo, not inside Wevito app code.

Suggested repo name:
- `sprite-workflow-app`

Suggested top-level structure:

```text
sprite-workflow-app/
├─ README.md
├─ docs/
│  ├─ PRODUCT_SPEC.md
│  ├─ V1_BACKLOG.md
│  └─ PROJECT_CONFIG_SCHEMA.md
├─ src/
│  ├─ SpriteWorkflow.App/
│  ├─ SpriteWorkflow.Core/
│  ├─ SpriteWorkflow.Infrastructure/
│  ├─ SpriteWorkflow.ProjectModel/
│  └─ SpriteWorkflow.Tests/
├─ sample-projects/
│  └─ wevito.project.json
└─ tools/
```

## 4. Solution Layout

### `SpriteWorkflow.App`

Avalonia desktop UI project.

Responsibilities:
- app shell
- navigation
- screens and view models
- UI commands

### `SpriteWorkflow.Core`

Pure app logic.

Responsibilities:
- asset indexing logic
- filtering
- playback state
- review/request rules
- workflow orchestration interfaces

### `SpriteWorkflow.Infrastructure`

File system and process integrations.

Responsibilities:
- file scanning
- JSON persistence
- image loading
- script execution
- project file IO

### `SpriteWorkflow.ProjectModel`

Project configuration and schema models.

Responsibilities:
- project config contracts
- animation family definitions
- action definitions
- config validation

### `SpriteWorkflow.Tests`

Unit tests for:
- config validation
- asset indexing
- filtering
- review/request persistence

## 5. Initial Domain Model

### Project models

- `ProjectConfig`
- `ProjectPaths`
- `VariantAxesConfig`
- `AnimationFamilyConfig`
- `WorkflowActionConfig`

### Asset models

- `AssetVariantKey`
- `AssetFamilyRecord`
- `FrameRecord`
- `AssetIndexSnapshot`
- `AssetCompleteness`

### Review models

- `ReviewRecord`
- `ReviewStatus`
- `IssueTag`

### Request models

- `RequestRecord`
- `RequestType`
- `RequestStatus`

### Workflow models

- `WorkflowRunRecord`
- `WorkflowRunStatus`
- `WorkflowActionResult`

## 6. Initial Service List

### Project services

- `IProjectConfigService`
- `IRecentProjectsService`
- `IProjectValidationService`

### Asset services

- `IAssetIndexService`
- `IAssetQueryService`
- `IImageFrameService`
- `IAnimationPlaybackService`

### Review/request services

- `IReviewRepository`
- `IRequestRepository`
- `IReviewService`
- `IRequestService`

### Workflow services

- `IWorkflowActionRunner`
- `IProcessRunner`
- `IRunHistoryRepository`

### App services

- `INavigationService`
- `INotificationService`
- `IAppStateStore`

## 7. UI Structure

### Shell

```text
╔══ Shell ═══════════════════════════════════════════════╗
║ Sidebar                                                ║
║  ├─ Dashboard                                          ║
║  ├─ Assets                                             ║
║  ├─ Review Queue                                       ║
║  ├─ Requests                                           ║
║  ├─ Workflow                                           ║
║  └─ Settings                                           ║
║                                                        ║
║ Main Panel                                             ║
║  └─ active screen                                      ║
╚════════════════════════════════════════════════════════╝
```

### First screens in build order

1. Project Picker
2. Dashboard
3. Assets screen
4. Viewer pane
5. Requests screen
6. Workflow screen
7. Settings screen

## 8. Project Config for Wevito

Create a sample `wevito.project.json` to prove the schema.

It should describe:
- root path
- runtime sprites root
- authored sprites root
- incoming handoff root
- artifact root
- variant axes
- animation families
- workflow action commands

V1 only needs to support one profile in active use, but the app structure must support many.

## 9. Persistence Strategy

Use JSON files in app-local storage or project-local metadata storage.

Suggested files:
- `projects.json`
- `reviews.json`
- `requests.json`
- `workflow-runs.json`
- `asset-index-cache.json`

V1 persistence rules:
- human-readable
- easy to back up
- no DB migration burden

## 10. Asset Indexing Rules

The indexer must:
- scan authored and runtime roots
- build per-variant/per-family completeness
- detect frame presence
- identify source type:
  - authored
  - runtime only
  - missing
- expose rows for filtering and viewer use

The first version can assume fixed frame naming patterns per family from project config.

## 11. Viewer Requirements

V1 viewer must:
- play selected animation family
- loop frames
- support speed control
- support zoom
- switch species/age/gender/color/family
- compare authored vs runtime when available

Nice-to-have if easy:
- side-by-side multi-species compare for trusted animals

## 12. Review System Requirements

Each asset row must support:
- status
- issue tags
- freeform notes
- updated timestamp

V1 statuses:
- `approved`
- `needs_review`
- `to_be_repaired`
- `do_not_use`

V1 issue tags:
- `anthropomorphic_drift`
- `bad_anatomy`
- `outline_broken`
- `placeholder_artifact`
- `off_model`
- `missing_frames`

## 13. Request System Requirements

V1 request types:
- `new_species`
- `new_variant`
- `new_animation_family`
- `repair_existing`
- `polish_existing`

Each request should capture:
- target asset scope
- request type
- goal
- must preserve
- must avoid
- references
- notes
- priority
- status

The request detail view should generate a structured AI handoff summary.

## 14. Workflow Actions Requirements

V1 action runner should support project-config-defined commands like:
- `prepare_handoff`
- `import_result`
- `propagate_colors`
- `rerender_previews`
- `refresh_coverage`

Each action run should show:
- start/end time
- success/failure
- stdout
- stderr

V1 does not need advanced job graphs or dependency scheduling.

## 15. Milestones

### Milestone 1: Foundation

Goal:
- open app
- choose project
- validate config

Deliverables:
- repo scaffold
- solution/projects
- app shell
- project picker
- sample Wevito config

Acceptance:
- app opens
- Wevito profile loads
- invalid config errors are readable

### Milestone 2: Asset Intelligence

Goal:
- index assets and show useful rows

Deliverables:
- asset models
- file scanner
- completeness logic
- asset browser list
- filters

Acceptance:
- user can filter by species, age, gender, color, family
- authored/runtime availability is visible

### Milestone 3: Viewer

Goal:
- inspect animations outside gameplay

Deliverables:
- animation viewer panel
- frame playback
- authored/runtime compare
- speed and zoom controls

Acceptance:
- crow/deer/fox/rat can be reviewed fully in viewer

### Milestone 4: Review System

Goal:
- mark quality issues and trust state

Deliverables:
- review records
- notes box
- issue tags
- review statuses
- review queue filter

Acceptance:
- user can flag bad rows like frog expression and retrieve them later

### Milestone 5: Requests

Goal:
- create structured new work and repair requests

Deliverables:
- request list
- request editor
- AI-ready request summary

Acceptance:
- user can create a repair request from an asset with notes carried into it

### Milestone 6: Workflow Actions

Goal:
- trigger pipeline steps from app

Deliverables:
- process runner
- workflow action UI
- log panel
- history records

Acceptance:
- at least one Wevito script can be run from the app and inspected safely

## 16. Task Backlog

### Foundation tasks

- Create new repo and solution
- Add Avalonia app project
- Add core/infrastructure/project-model/test projects
- Add basic navigation shell
- Add recent project storage
- Add Wevito sample project config

### Project system tasks

- Define `ProjectConfig` schema
- Define config validator
- Add open-project command
- Add project summary state model

### Asset indexing tasks

- Define frame naming map structure
- Implement authored/runtime scan
- Implement completeness calculation
- Implement asset cache format
- Add reindex command

### Browser tasks

- Build asset list view model
- Add filters
- Add search box
- Add status/source badges
- Add selection -> viewer binding

### Viewer tasks

- Load PNG frames
- Implement timer-based playback
- Add frame stepping
- Add speed control
- Add zoom
- Add authored/runtime compare toggle

### Review tasks

- Define `ReviewRecord`
- Implement review repository
- Add notes editor
- Add issue tag selector
- Add status buttons
- Add review queue filter

### Request tasks

- Define `RequestRecord`
- Implement request repository
- Build request editor
- Link request creation from selected asset
- Add AI-ready request summary block

### Workflow tasks

- Define workflow action config model
- Implement process runner
- Capture stdout/stderr
- Build workflow action panel
- Save run history

### QA tasks

- Unit test config validation
- Unit test index completeness logic
- Unit test filter logic
- Unit test review persistence
- Unit test request serialization

## 17. Build Order Recommendation

Build in this order:

1. repo + shell
2. project config loader
3. asset indexer
4. asset browser
5. viewer
6. review notes/status
7. request system
8. workflow action runner

Do not start the embedded pixel editor before the review/request/workflow loop is stable.

## 18. First Demo Goal

The first meaningful demo should show:
- Wevito selected as a project
- trusted animals loaded in viewer
- asset filters working
- a bad row marked `to_be_repaired`
- a repair request created from that row
- one workflow action executed from inside the app

## 19. Stretch Goals After V1

- embedded sprite editor
- onion skin
- palette tool
- dropper
- compare against imported Gemini result
- queue runner
- provider adapters
- before/after repair history

## 20. Immediate Next Step

Create the separate repo and scaffold:

1. `SpriteWorkflow.App`
2. `SpriteWorkflow.Core`
3. `SpriteWorkflow.Infrastructure`
4. `SpriteWorkflow.ProjectModel`
5. `SpriteWorkflow.Tests`

Then implement the Wevito project config and asset indexer first.

