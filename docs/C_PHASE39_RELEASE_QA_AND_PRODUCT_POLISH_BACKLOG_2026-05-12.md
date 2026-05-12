# C-PHASE 39 Release QA And Product Polish Backlog

Date: 2026-05-12

Branch: `claude-implementation/c-phase-39-release-qa-backlog`

## Goal

Turn the validated `v0.1.0-desktop-rc3` package into a release-facing manual QA plan and the next code-side product-polish backlog.

This phase is documentation/planning only. It does not mutate sprites, runtime PNGs, source boards, prop anchors, Godot scenes/scripts, vNext code, or release assets.

## Current Trusted Baseline

```text
validated release baseline
|
+-- GitHub prerelease: v0.1.0-desktop-rc3
+-- ZIP: WevitoDesktopPet-vcphase37-fetchfix3-win64.zip
+-- SHA256: ce33cb333c6f751c0661520c571ce72f5a925805528e3865ecee23056bb5a19e
+-- clean download: PASS
+-- bundle structure: PASS
+-- full automation: PASS
+-- drink scenario: PASS
+-- fetch scenario: PASS
`-- app-rendered goose viewport proof: PASS
```

Primary evidence:

- `docs/C_PHASE38_RC3_CLEAN_VALIDATION_2026-05-12.md`
- `docs/C_PHASE37_RELEASE_FACING_QA_2026-05-12.md`
- `vnext/artifacts/c-phase-38-rc3-clean-validation/rc3-full-report.json`
- `vnext/artifacts/c-phase-38-rc3-clean-validation/rc3-drink-report.json`
- `vnext/artifacts/c-phase-38-rc3-clean-validation/rc3-fetch-report.json`
- `vnext/artifacts/c-phase-38-rc3-clean-validation/rc3-godot-viewport-goose.png`

## Release QA Shape

```text
RC3 release QA
|
+-- automated proof already green
|   +-- clean release download
|   +-- launch + full automation
|   +-- drink forced scenario
|   +-- fetch forced scenario
|   `-- app-rendered habitat screenshot
|
+-- manual/player QA still needed
|   +-- install/extract/launch
|   +-- overlay behavior
|   +-- controls and care actions
|   +-- visual quality in motion
|   +-- save/reload/reset
|   +-- tool hub readability
|   `-- release notes/help text
|
`-- gates still closed
    +-- live model calls
    +-- broad sprite mutation
    +-- asset-prep regeneration
    +-- stable release tag
    `-- external automation claims
```

## Manual QA Checklist

### Install, Extract, And Launch

- Download `v0.1.0-desktop-rc3` from GitHub on a clean path.
- Confirm Windows can extract the zip without blocked-file errors.
- Launch `WevitoDesktopPet.exe`.
- Confirm the app opens without requiring development tools, terminals, Godot editor, or local repo files.
- Confirm `WevitoDesktopBridge.exe` is present and not visibly disruptive.
- Confirm the app does not open as a black/empty overlay.

### Overlay And Window Behavior

- Confirm the pet appears on the desktop and remains readable over normal apps.
- Confirm pinned/unpinned behavior works as intended.
- Confirm click-through/pass-through behavior does not trap the user.
- Confirm the overlay can be recovered if it appears near a monitor edge.
- Confirm old save positions do not strand the pet permanently off-screen.
- Confirm the overlay does not steal focus repeatedly while the user works elsewhere.

### Core Pet Controls

- Confirm visible UI/control affordances match actual behavior.
- Confirm feeding updates hunger and produces a visible/understandable response.
- Confirm drinking updates hydration and uses the correct water/drink visual.
- Confirm petting/grooming/care actions update state without stale capped-stat failures.
- Confirm rest/sleep can trigger and recover cleanly.
- Confirm ball/fetch starts, resolves, and returns to idle/happy rather than lingering in walk.
- Confirm actions still work after app restart.

### Visual And Animation QA

- Confirm no obvious fake PNG backgrounds remain visible during normal play.
- Confirm no holes, body gaps, border pixels, or chroma residue are obvious on dark and light backgrounds.
- Confirm animals face the correct movement direction.
- Confirm walk/run/fetch/drink/rest motion reads as an action, not as static frame swapping.
- Confirm size changes do not look like shrink/grow artifacts.
- Confirm babies, teens, and adults read as age-appropriate sizes.
- Confirm birds, snake, frog, and other previously high-risk species are checked in motion, not still-frame only.
- Confirm the ball remains a runtime overlay and is not doubled or baked into pet PNGs.

### Habitat And World Presentation

- Confirm pilot habitat species show stable pet placement, contact shadow, and readable props.
- Confirm stage depth does not bury pets behind props incorrectly.
- Confirm perch/enter/hide style placements are understandable where implemented.
- Confirm habitat visuals support the overlay rather than making the window feel crowded.

### Save, Reset, And Recovery

- Confirm save data persists core pet state across restart.
- Confirm reset/new-pet paths do not corrupt existing runtime assets.
- Confirm old save data from pre-RC3 does not break startup.
- Confirm debug/automation flags are not visible to normal users unless intentionally invoked.

### Tool Hub And PET TASKS

- Confirm PET TASKS surfaces are clearly labeled as report-only where applicable.
- Confirm localDocs, spriteAudit, assetInventory, petState, codeReview, and codePatchPlan do not imply unsafe execution.
- Confirm buildProof, screenCapture, translateText, and audioAssist remain approval-gated where designed.
- Confirm task result cards expose useful artifact paths/buttons without making the UI feel like a developer dashboard.
- Confirm the pet sim remains visually normal; task completion should be under the hood, not special pet-task animations.

### Release Text And User Guidance

- Confirm the help guide matches the current RC3 behavior.
- Confirm known limitations are explicit: prerelease, gated AI/model calls, visual QA still active, no guarantee of broad sprite mutation.
- Confirm the release page marks RC1/RC2 as superseded and points users to RC3.

## Product Polish Backlog

```text
next code-side product work
|
+-- C-PHASE 40: manual RC3 player QA packet
+-- C-PHASE 41: overlay/save-position recovery polish
+-- C-PHASE 42: Tool Hub/PET TASKS UX clarity
+-- C-PHASE 43: care/item/habitat runtime mapping polish
+-- C-PHASE 44: screenshot/capture UX proof
+-- C-PHASE 45: translation/audio-assist provider polish
+-- C-PHASE 46: AI helper gate review
+-- C-PHASE 47: visual-side integration sync
`-- C-PHASE 48: stable release decision
```

### C-PHASE 40 - Manual RC3 Player QA Packet

Goal: execute or prepare a player-facing QA pass from the public RC3 zip.

Scope:

- Use the exact public RC3 zip, not a local rebuild.
- Capture manual notes for install, launch, overlay, pet controls, visual motion, save/reload, and tool hub readability.
- Produce a single report with pass/fail/defer items and screenshot references where helpful.

Stop gate:

- If a player-facing issue is found, branch into a focused fix phase before stable release.

### C-PHASE 41 - Overlay And Save-Position Recovery Polish

Goal: make the app resilient when saves contain old or off-screen pet/window positions.

Scope:

- Audit save migration and monitor-bounds handling.
- Add a safe recenter/recover path if needed.
- Avoid breaking intentional multi-monitor roaming.

Validation:

- Synthetic old-save fixtures.
- Manual launch with out-of-bounds position data.

### C-PHASE 42 - Tool Hub / PET TASKS UX Clarity

Goal: make helper tools feel simple and safe.

Scope:

- Strengthen `REPORT ONLY`, `PREVIEW`, `APPROVAL REQUIRED`, and `EXECUTION DISABLED` labels.
- Improve result-card hierarchy and artifact buttons.
- Keep pets visually acting like pets; no special task-completion animations.

Stop gate:

- Do not enable new execution capabilities in this phase.

### C-PHASE 43 - Care, Item, And Habitat Runtime Mapping Polish

Goal: connect the existing clean art pool to more understandable gameplay surfaces.

Scope:

- Review care/medicine/food/water/toy mappings.
- Confirm habitat manifest use remains single-source.
- Improve small UI labels/tooltips if needed.

Stop gate:

- Do not generate or replace item art unless separately approved.

### C-PHASE 44 - Screenshot And Capture UX Proof

Goal: turn screenshot/screen-capture capability from a technical adapter into a user-understandable tool.

Scope:

- Confirm app/window screenshot behavior.
- Label offline approximation vs packaged proof.
- Keep screen recording deferred unless approved.

Stop gate:

- Recording is privacy-sensitive and should remain a separate approval gate.

### C-PHASE 45 - Translation And Audio Assist Provider Polish

Goal: make translation and audio assist understandable, safe, and honest.

Scope:

- Confirm what powers translation in the current implementation.
- Make fallback/provider status visible to users.
- Keep audio boost safety conservative and avoid hidden system-wide volume surprises.

Stop gate:

- Do not install or control third-party audio boosters automatically.

### C-PHASE 46 - AI Helper Gate Review

Goal: decide whether the default-disabled pet model adapter is ready for a first live-call approval.

Scope:

- Re-review capability flag, allowlist, credential lookup, consent text, and lethal-trifecta separation.
- Keep exactly three helper pets.
- Keep personality separate from permissions.

Stop gate:

- No live model calls without explicit user approval.

### C-PHASE 47 - Visual-Side Integration Sync

Goal: reconcile code-side release status with visual-side sprite/asset cleanup progress.

Scope:

- Read the latest visual completion tracker and visual-side handoff.
- Confirm whether any visual-side assets are ready for code-side apply/proof.
- Keep sprite mutation behind hash/backup/rollback/proof gates.

Stop gate:

- Do not run asset prep or mutate sprites as a side effect.

### C-PHASE 48 - Stable Release Decision

Goal: decide whether to promote RC3 or a later RC into a stable release.

Inputs:

- RC3 clean validation.
- Manual QA packet.
- Any focused fixes after manual QA.
- Updated help/release notes.

Decision labels:

- `promote_current_rc`
- `publish_next_rc`
- `hold_release_for_fixes`

## Gates That Remain Closed

| Gate | Current State | Why |
| --- | --- | --- |
| Live model calls | Closed | Capability exists but must stay default-disabled until explicit review/approval. |
| Broad sprite mutation | Closed | Visual-side cleanup must stay coordinated through manifest/proof/rollback. |
| Asset prep without explicit approval | Closed | It can regenerate runtime asset folders and disturb validated visual baselines. |
| Stable release tag | Closed | RC3 is validated by automation, but player-facing manual QA is still needed. |
| Screen recording | Deferred | Higher privacy risk than screenshots and not required for RC3 release. |
| External audio booster control | Deferred | Needs user-visible safety/consent and should not silently alter system audio. |

## Recommended Next Step

Start `C-PHASE 40 - Manual RC3 Player QA Packet`.

Use the public RC3 zip and this checklist as the source of truth. If the QA pass finds a real player-facing problem, fix that narrow issue before expanding tools, AI helpers, sprite workflows, or visual mutation work.

