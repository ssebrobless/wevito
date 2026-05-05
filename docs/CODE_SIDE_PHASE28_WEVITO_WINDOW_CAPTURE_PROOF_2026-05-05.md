# Code-Side Phase 28 Wevito Window Capture Proof - 2026-05-05

## Goal

Add the first real screenshot proof path while keeping capture narrow and safe.

This phase captures only a Wevito-owned window rectangle and writes local artifacts.

```text
Launch isolated vNext shell
   |
   v
Open PET TASKS proof surface
   |
   v
Capture Wevito PET TASKS window rectangle only
   |
   v
screenshot.png + manifest.json + run-summary.md
```

## Files Changed

- `C:\Users\fishe\.codex\worktrees\36d6\wevito\tools\capture-vnext-window.ps1`

Supporting prior phase files:

- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Contracts\PetAgentContracts.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Core\CapturePolicyEvaluator.cs`

## What The Script Does

- Launches `Wevito.VNext.Shell.exe`.
- Uses an isolated pinned scenario so the UI opens in a predictable state.
- Optionally opens PET TASKS with `-OpenPetTasks`.
- Finds the Wevito-owned window by process id and exact window title.
- Captures that window rectangle with local Windows screen-copy APIs.
- Writes:
  - `screenshot.png`
  - `manifest.json`
  - `run-summary.md`

## Safety Boundaries

- No full desktop capture.
- No selected arbitrary region capture.
- No foreground non-Wevito window capture.
- No screen recording.
- No external upload/share.
- No sprite/art mutation.
- No asset prep.
- No runtime/source PNG edits.

## Validation

Command:

```text
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\capture-vnext-window.ps1 -SkipBuild -OpenPetTasks
```

Result: passed.

Latest artifact folder:

```text
C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\pet-tasks\20260505-131621-capture-wevito-window
```

Artifacts:

- Screenshot: `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\pet-tasks\20260505-131621-capture-wevito-window\screenshot.png`
- Manifest: `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\pet-tasks\20260505-131621-capture-wevito-window\manifest.json`
- Summary: `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\pet-tasks\20260505-131621-capture-wevito-window\run-summary.md`

Manifest evidence:

- `windowTitle`: `Wevito PET TASKS`
- `preset`: `proofSurface`
- `targetKind`: `proofSurface`
- `outputKind`: `screenshotPng`
- `privacyLevel`: `wevitoOnly`
- `didUploadOrShare`: `false`
- `capturedAtUtc`: UTC timestamp

Screenshot dimensions:

- `520 x 420`
- File size: `31417` bytes

## Visual QA

The captured PET TASKS screenshot showed:

- popup title: `PET TASKS`,
- task textbox at the top,
- visible `PREPARE` button,
- capability/safety line,
- three helper cards,
- wellbeing snapshot,
- current-task panel,
- next-action cue,
- disabled approve/preview/cancel buttons until a task exists.

Visual result: pass for a first compact proof surface.

Remaining UI polish note:

- The popup is readable and much better organized, but the capability line is still dense. A later UI phase should consider splitting ready/locked capabilities into two tiny chips or rows.

## Audit

Pass.

- Correctness: capture script writes the expected local artifacts.
- Safety: capture scope stayed Wevito-only.
- Maintainability: script uses the same isolated/pinned launch pattern as PET TASKS probes.
- User proof value: the artifact is viewable and useful for verifying UI work.
- No broad capture/recording capability was introduced.

## Next Phase Recommendation

Proceed to Phase 29: asset inventory report tool.

Keep Phase 29 read-only:

- count assets,
- identify missing/stale groups,
- write markdown and JSON,
- no contact-sheet generation unless explicitly requested,
- no sprite mutation.
