# C-PHASE 37 Release-Facing QA

Date: 2026-05-12

Branch: `claude-implementation/c-phase-37-release-facing-qa`

## Goal

Use `v0.1.0-desktop-rc2` as the release baseline, run player-facing packaged QA, and fix any release-facing issues found before moving back into broader visual/tool work.

## QA Shape

```text
RC2 public zip
├── full automation suite: PASS
├── drink scenario: PASS
└── fetch scenario: FAIL
    ├── fetch accepted and ended
    └── pet stayed visually in walk animation

Phase 37 fixed build
├── full automation suite: PASS
├── drink scenario: PASS
└── fetch scenario: PASS
```

## RC2 Baseline Results

Downloaded from GitHub release:

`v0.1.0-desktop-rc2`

Asset:

`WevitoDesktopPet-vcphase35-allresources1-win64.zip`

Baseline full automation result:

- Exit code: `0`
- Passed: `true`
- Report: `vnext/artifacts/c-phase-37-release-facing-qa/rc2-full-automation-report.json`

Baseline drink result:

- Exit code: `0`
- Passed: `true`
- Report: `vnext/artifacts/c-phase-37-release-facing-qa/rc2-force_low_hydration_drink-report.json`

Baseline fetch result:

- Exit code: `1`
- Passed: `false`
- Report: `vnext/artifacts/c-phase-37-release-facing-qa/rc2-force_fetch_sequence-report.json`

Failing checks:

```text
forced_fetch_sequence_completes: accepted=true active=false animation=walk
core_actions_update_pet_state: false
```

## Root Causes

### Fetch Visual Completion

The fetch sequence could finish with `fetch_stage == NONE` while the pet still displayed `walk`. This is player-facing because a completed fetch should settle into a completed/idle pose, not keep showing locomotion.

Fix in `scripts/pet.gd`:

- On fetch completion, clear wandering state.
- Clear resume-wander intent.
- Reset interaction/wander timers.
- Set the completion animation to `happy` when available, otherwise `idle`.

### Automation Scenario State Contamination

The fetch automation scenario sets affection to `100` so the fetch action is accepted. The shared `core_actions_update_pet_state` check later expects petting to increase affection, which cannot happen when affection is already capped.

Fix in `scripts/main_scene.gd`:

- Reset the active pet to mid-range stats after scenario-specific checks and before shared action-flow assertions.
- This keeps each shared action assertion meaningful across scenarios.

## Fixed Build Validation

Build:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-release.ps1 -Version cphase37-fetchfix3 -ExportTimeoutSeconds 300
```

Result:

- Build passed.
- Zip produced: `builds/release/WevitoDesktopPet-vcphase37-fetchfix3-win64.zip`

Fixed scenario reports:

| Run | Exit code | Passed | Report |
| --- | ---: | --- | --- |
| full automation | 0 | true | `vnext/artifacts/c-phase-37-release-facing-qa/cphase37-fixed-full-report.json` |
| drink scenario | 0 | true | `vnext/artifacts/c-phase-37-release-facing-qa/cphase37-fixed-drink-report.json` |
| fetch scenario | 0 | true | `vnext/artifacts/c-phase-37-release-facing-qa/cphase37-fetchfix3-force_fetch_sequence-report.json` |

## Remaining Manual QA Caveat

Desktop screenshot APIs can capture the transparent/layered overlay as a black rectangle even when the app-rendered viewport proof is healthy. For this overlay app, release QA should use:

- built-in Godot viewport screenshots for rendered proof,
- automation reports for control/state proof,
- human visual review for final UX confirmation.

## Release Recommendation

`v0.1.0-desktop-rc2` remains the current public prerelease, but this branch contains a release-facing fetch-completion fix.

Recommended next step after merge:

1. Tag and publish `v0.1.0-desktop-rc3`.
2. Upload `builds/release/WevitoDesktopPet-vcphase37-fetchfix3-win64.zip`.
3. Mark RC2 as superseded by RC3 because of the fetch visual-completion issue.
4. Continue release-facing manual QA from RC3.
