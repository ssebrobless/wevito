# Wevito Full Project Status And Completion Roadmap

Generated: 2026-05-05

## Current Shape

```text
Wevito
├─ Game / overlay pet sim
│  ├─ Godot runtime
│  ├─ vNext desktop shell / broker
│  ├─ generated sprite runtime assets
│  └─ care, toy, interaction, save, debug, proof surfaces
│
├─ Visual production lane
│  ├─ sprite cleanup and QA
│  ├─ optional animation pilots
│  ├─ color / habitat / care visual planning
│  └─ contact sheets, manifests, review artifacts
│
└─ Tool / AI helper lane
   ├─ PET TASKS command bar
   ├─ report-only adapters
   ├─ approval-gated execution adapters
   ├─ future screenshot / proof / sprite workflow tools
   └─ future AI/ML learning and agent capabilities
```

## Current Implementation State

### Game Runtime

- vNext builds and tests successfully.
- Safe Debug publish works with `-SkipAssetPrep`.
- Godot/runtime sprite contract work is partially implemented and actively coordinated with visual-side cleanup.
- Runtime pets still behave as normal overlay pets; PET TASKS no longer triggers special task completion/review/failure animations.
- Sprite/assets are actively dirty from visual cleanup and must not be reverted.
- Runtime PNG mutation is visual-lane owned unless a specific apply/proof pilot is approved.

### PET TASKS / Tool Hub

- PET TASKS command bar exists.
- Task cards support prepare, preview, review, approval, execution, reports, feedback text, and trace logs.
- PET TASKS status is under the hood and does not drive pet animations.
- Current preview adapters include:
  - `localDocs`
  - `spriteAudit`
  - `assetInventory`
  - `codeReview`
  - `codePatchPlan`
  - `buildProof`
  - `petState`
  - `translateText`
  - `audioAssist`
  - `screenCapture`
- Current approval-gated execution adapters include:
  - `translateText` through DeepL when credentials exist
  - `audioAssist` for normal Windows volume/mute only
- Current locked or preview-only areas:
  - sprite generation/import
  - PNG mutation
  - prop anchor edits
  - build/proof command execution
  - browser automation
  - screen recording
  - full screenshot execution
  - audio boosters, drivers, APOs, external enhancer configuration

### Visual / Asset Lane

- Visual-side branch exists separately at `origin/codex/visual-side-qa-20260505`.
- Visual planning/review docs and candidate artifacts need to be merged into the same final integration branch.
- Visual cleanup has touched many runtime sprites, especially bird cleanup batches.
- Visual-side should continue owning broad sprite quality, animation review, contact sheets, and asset mutation.

### Validation Snapshot

- Latest code-side build: `dotnet build .\vnext\Wevito.VNext.sln` passed with `0` warnings and `0` errors.
- Latest full tests: `144 / 144` passed.
- Latest safe publish: `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests` passed.
- Latest PET TASKS live probe passed and confirmed target sprite hashes unchanged.

## Remaining Work To Become Fully Functional

### Phase A: Source Control Integration

```text
detached code worktree
   │
   ├── commit current code/runtime/docs/sprite state
   │
   ├── merge visual-side QA branch
   │
   ├── validate build/tests/probes
   │
   └── push one integration branch
```

- Create a normal branch from the detached worktree.
- Commit the current dirty worktree without deleting or reverting visual sprite changes.
- Merge `origin/codex/visual-side-qa-20260505`.
- Resolve conflicts if any.
- Run build/tests/safe publish.
- Push the single integration branch.
- Only update `main` after this branch is known good. Prefer PR/fast-forward over force push.

### Phase B: Game Core Completion

- Confirm the Godot and vNext runtime paths agree on current sprite naming, optional animation families, prop overlays, and runtime metadata.
- Finish pet interaction flow:
  - feeding
  - drinking
  - medicine/care
  - bathing/grooming
  - sleeping/rest
  - ball play, pickup, hold, drop, carry, fetch loop
- Finalize pet simulation:
  - needs/drives
  - emotions
  - personality development
  - relationships/affinity
  - health and body condition
  - aging
  - death and ghost state
- Finalize save/load persistence and migration safety.
- Finalize debug/dev forced scenarios for every meaningful state.
- Verify actual game controls match UI labels.
- Add repeatable in-game proof scenarios for every interaction and animation family.

### Phase C: Visual Asset Completion

- Finish full visual audit across every species, age, gender, and color.
- Remove all leftover fake PNG backgrounds, body holes, edge pixels, noise, and off-model frames.
- Stop boxing sprites into artificial generation bounds when motion needs more space.
- Rebuild or replace stiff/incomplete motion, especially snakes, frogs, birds, and any row with size popping.
- Confirm all animations in motion, not just still contact sheets.
- Confirm direction-facing behavior for locomotion.
- Confirm all optional animation families:
  - `pickup_ball`
  - `drop_ball`
  - `hold_ball`
  - `carry_ball_walk`
  - `carry_ball_run`
  - eating
  - drinking
  - playing
  - care/medicine
- Create/validate final color variants only after base animation quality is accepted.
- Define and implement ghost, fat, and skinny variants after the healthy base set is stable.

### Phase D: Habitat / Environment / Item Completion

- Finalize required habitat objects by species and environment.
- Implement interaction zones:
  - enter
  - perch
  - hide
  - sleep
  - drink
  - eat
  - play
- Implement depth sorting, occlusion, contact shadows, and ground alignment.
- Finalize food, water, medicine, care, UI, egg, toy, and status icons.
- Remove stale asset expectations for abandoned features.
- Ensure new assets are named descriptively and applied where they belong.

### Phase E: Tool Hub Functional Completion

- Turn `screenCapture` from preview-only into approval-gated Wevito-window screenshot execution.
- Add selected-region screenshot execution with explicit selection/approval.
- Keep full-desktop screenshot high-risk and explicit.
- Keep screen recording blocked until privacy, duration, storage, and visible recording indicator exist.
- Implement proof capture for Wevito/Godot/vNext surfaces without mutating visual assets.
- Implement buildProof execution only after a fake-runner/stub phase and explicit approval.
- Expand translation providers only if needed:
  - DeepL is currently first-class.
  - Google/Azure/LibreTranslate are provider-readiness only unless implemented later.
- Keep audioAssist limited to normal Windows endpoint volume/mute unless the user manually configures external tools.
- Keep tool actions visible in PET TASKS UI, reports, and logs, not pet animation states.

### Phase F: Sprite Workflow Tool Rebuild

- Design a simplified sprite workflow inside Wevito rather than reviving the confusing external app.
- Single entry point:
  - user asks for audit, candidate, contact sheet, or apply plan
  - tool returns one clear result packet
- Required workflow pieces:
  - canonical source/reference manifest
  - provenance and hashes
  - source-to-runtime mapping
  - contact sheets and GIF previews
  - no-mutation preview mode
  - one-row apply/proof/rollback
  - strict scope boundaries
  - visual-side review packet handoff
- Avoid broad automated visual mutation until the manifest/provenance/apply workflow is production-safe.

### Phase G: Creative Learning Lab

- Define what the pet helpers are allowed to learn:
  - user preferences
  - accepted/rejected visual candidates
  - preferred sprite style rules
  - safe tool-routing patterns
  - recurring project vocabulary
- Store learning as reviewed examples and preference snapshots, not uncontrolled model fine-tuning.
- Add feedback labels:
  - accepted
  - rejected
  - needs revision
  - prefer A
  - prefer B
- Add auditability:
  - what example was learned
  - which pet/helper used it
  - where it affected later suggestions
- Keep the animals' sim personalities simpler than Papilionem; AI agent capability can be strong under the hood without making the animal personalities overcomplicated.

### Phase H: AI Agent Capabilities

- Keep exactly three helper pets active at a time unless the design changes.
- Give each helper a role, allowed tool families, and current task card.
- Add safe task execution layers:
  - local docs/reporting
  - code review
  - patch planning
  - screenshot/proof capture
  - translation
  - audio status/control
  - sprite audit
  - build proof
- Add stronger agent planning only after tool permissions, artifact roots, and rollback rules are stable.
- Add coding capability carefully:
  - start with code review and patch plans
  - then approved patch execution on small scopes
  - require validation and rollback notes
  - never silently mutate broad code or assets

### Phase I: ML / Advanced Intelligence

- Review Papilionem ML ideas for:
  - memory stores
  - preference learning
  - behavior selection
  - emotional state modeling
  - creature/personality architecture
- Adapt only the useful lightweight pieces into Wevito.
- Do not import Papilionem complexity wholesale.
- Prefer simple, explainable learning loops before any advanced model integration.
- Consider local vector search only for docs/preferences/examples, not autonomous uncontrolled behavior.

### Phase J: UX / UI Polish

- Keep the PET TASKS UI compact and understandable.
- Add one text bar near tools for assigning tasks directly to named pets.
- Avoid separate complex tool screens unless a tool truly needs one.
- Provide clear action states:
  - preview
  - awaiting approval
  - running
  - reviewing
  - done
  - blocked
- Make every report openable and understandable.
- Keep game/pet overlay visually calm while tool work happens under the hood.

### Phase K: Release Readiness

- Run full code validation.
- Run Godot/package validation.
- Run all visual proof sweeps.
- Run in-game forced scenario sweeps for all pets, ages, genders, colors, and key interactions.
- Confirm no stale generated assets, stale feature references, or hidden broken animations.
- Package the game and tools.
- Write a final user-facing controls/help guide.
- Tag a release only after both code and visual lanes agree.

## Immediate Next Steps

1. Create a normal integration branch from the current detached state.
2. Commit the current worktree.
3. Merge `origin/codex/visual-side-qa-20260505` into that branch.
4. Validate.
5. Push the integration branch.
6. Decide whether to PR/merge into `main`.

## Important Git Notes

- Current worktree was detached at local `main`.
- Local `HEAD` is ahead of `origin/main` by one commit.
- Visual-side work exists on `origin/codex/visual-side-qa-20260505`.
- Do not force-push `main`.
- Do not discard dirty sprite/runtime changes.
- Prefer a single pushed integration branch first, then merge into `main` once verified.
