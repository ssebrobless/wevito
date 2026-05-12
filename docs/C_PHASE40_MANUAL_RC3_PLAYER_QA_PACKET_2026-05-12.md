# C-PHASE 40 Manual RC3 Player QA Packet

Date: 2026-05-12

Branch: `claude-implementation/c-phase-40-manual-rc3-player-qa`

## Goal

Use the public `v0.1.0-desktop-rc3` zip as the player-facing artifact and produce the manual QA packet needed before any stable release decision.

This phase did not mutate game code, sprites, source boards, runtime PNGs, prop anchors, release assets, or generated art.

## Artifact Under Review

```text
public RC3 zip
|
+-- release: v0.1.0-desktop-rc3
+-- asset: WevitoDesktopPet-vcphase37-fetchfix3-win64.zip
+-- sha256: ce33cb333c6f751c0661520c571ce72f5a925805528e3865ecee23056bb5a19e
+-- exe present: PASS
+-- bridge present: PASS
+-- runtime sprite files: 10818
`-- shared runtime files: 557
```

Evidence:

- `vnext/artifacts/c-phase-40-manual-rc3-player-qa/rc3-download-extract-summary.json`
- `vnext/artifacts/c-phase-40-manual-rc3-player-qa/download/WevitoDesktopPet-vcphase37-fetchfix3-win64.zip`
- `vnext/artifacts/c-phase-40-manual-rc3-player-qa/extract/WevitoDesktopPet.exe`

## Fresh Packaged Automation

```text
RC3 public zip automation
|
+-- full suite: PASS
+-- drink scenario: PASS
+-- fetch scenario: PASS
`-- goose viewport proof: PASS
```

| Run | Scenario | Exit code | Passed | Report |
| --- | --- | ---: | --- | --- |
| full | default full automation | 0 | true | `vnext/artifacts/c-phase-40-manual-rc3-player-qa/rc3-full-report.json` |
| drink | `force_low_hydration_drink` | 0 | true | `vnext/artifacts/c-phase-40-manual-rc3-player-qa/rc3-drink-report.json` |
| fetch | `force_fetch_sequence` | 0 | true | `vnext/artifacts/c-phase-40-manual-rc3-player-qa/rc3-fetch-report.json` |
| goose viewport | `c_phase_6_5_habitat_mirror_goose` | 0 | true | `vnext/artifacts/c-phase-40-manual-rc3-player-qa/rc3-goose-viewport-report.json` |

Screenshot proof:

- `vnext/artifacts/c-phase-40-manual-rc3-player-qa/rc3-player-qa-goose-viewport.png`

## What Is Machine-Proved

- The public RC3 zip still downloads, extracts, and hash-matches the expected SHA256.
- The executable and desktop bridge are present in the release zip.
- Runtime and shared runtime assets are bundled.
- The packaged executable can run the full automation suite successfully.
- The packaged executable can run the forced drink scenario successfully.
- The packaged executable can run the forced fetch scenario successfully.
- The packaged executable can render an app-generated goose habitat viewport screenshot.

## What Still Needs Human Player QA

```text
manual player QA
|
+-- launch feel
+-- overlay visibility
+-- click-through / focus behavior
+-- control discoverability
+-- visual quality in motion
+-- save/reload experience
+-- tool hub clarity
`-- release-page wording
```

These checks should not be marked complete from automation alone because the product is a visual desktop overlay and the final quality question is whether it feels understandable and pleasant to use.

## Player QA Checklist

### 1. Fresh User Launch

- Extract the RC3 zip to a fresh folder outside the repo.
- Launch `WevitoDesktopPet.exe`.
- Confirm the app opens without a terminal, Godot editor, or repo files.
- Confirm no black/empty overlay appears.
- Confirm first-run pet/egg/onboarding behavior is understandable.

Result: `needs human review`

### 2. Overlay Behavior

- Confirm the pet is visible over normal desktop work.
- Confirm the overlay does not constantly steal focus.
- Confirm pinned mode keeps controls available.
- Confirm unpinned/passive mode lets the user use other apps.
- Confirm the app can be recovered if it appears near a screen edge.

Result: `needs human review`

### 3. Pet Actions

- Feed a pet and confirm the UI/state response is visible.
- Trigger drink behavior and confirm it reads as drinking.
- Pet/groom/care for a pet and confirm the result is understandable.
- Trigger fetch and confirm the pet does not get stuck walking after completion.
- Confirm actions still work after closing and reopening the app.

Automation support:

- drink scenario: `PASS`
- fetch scenario: `PASS`
- full action suite: `PASS`

Result: `partially machine-proved, still needs human feel review`

### 4. Sprite And Animation Quality

- Watch animals move in normal play, not only still screenshots.
- Confirm no obvious fake PNG background boxes.
- Confirm no holes, border pixels, or noisy silhouettes.
- Confirm animals face the direction they move.
- Confirm motion does not look cramped, boxed, or shrink/grow inconsistent.
- Pay special attention to bird, snake, frog, pigeon, goose, and raccoon motion.

Result: `needs human visual review`

### 5. Habitat Presentation

- Confirm pet, shadow, and habitat props compose clearly.
- Confirm the pet is not buried behind props.
- Confirm the stage does not feel visually cramped.
- Confirm the goose viewport proof is representative enough for the current RC.

Automation support:

- goose viewport proof: `PASS`

Result: `partially machine-proved, needs broader human visual review`

### 6. Tool Hub / PET TASKS

- Confirm PET TASKS labels clearly distinguish report-only/previews from execution.
- Confirm helper tools do not imply live AI/model calls are active.
- Confirm result paths and artifact buttons are understandable.
- Confirm pet task work remains under the hood; pets should still act like regular pets.

Result: `needs human review`

### 7. Release Page And Help Text

- Confirm RC1 and RC2 are clearly superseded.
- Confirm RC3 is the current recommended prerelease.
- Confirm the help guide matches current behavior.
- Confirm known limitations are honest: prerelease, visual QA active, model calls disabled, broad sprite mutation gated.

Result: `needs human review before stable release`

## Decision Matrix

| Decision | Use When | Next Step |
| --- | --- | --- |
| `manual_qa_passed_promote_candidate` | Human QA finds no release-blocking issue. | Start C-PHASE 48 stable release decision. |
| `manual_qa_found_focused_fix` | One or more narrow player-facing issues are found. | Create focused fix phase before next RC. |
| `manual_qa_needs_visual_side_review` | Main concern is sprite/art/readability. | Send visual-side the artifact paths and checklist. |
| `manual_qa_hold_for_tools_polish` | Game is playable but tool hub is too confusing. | Start C-PHASE 42 before stable release. |

## Recommendation

Do not promote RC3 to stable from automation alone.

Recommended next step:

1. Have the user run the manual player QA checklist above against RC3.
2. If no release-blocking issue appears, move to a stable-release decision.
3. If an issue appears, create a narrow fix phase and publish RC4 instead of expanding broad feature work.

## Gates Still Closed

- No live model calls.
- No broad sprite generation/import.
- No runtime/source PNG mutation.
- No asset-prep regeneration.
- No screen recording by default.
- No stable release tag until human player QA is accepted.

