# Code-Side Phase 40: Screen Capture Preview

Generated: 2026-05-05

## Shape

```text
PET TASKS request
   │
   ├── screenshot / screen capture ──▶ screenCapture preview
   │                                      │
   │                                      ├── markdown report
   │                                      └── JSON report
   │
   └── recording / GIF ───────────────▶ blocked in preview policy

No screenshot. No recording. No sprite mutation.
```

## What Changed

- Added `TaskKind.ScreenCapture`.
- Added screen capture action/capability contracts.
- Added `ScreenCapturePreviewAdapter`.
- Routed screenshot/screen-capture requests through PET TASKS.
- Added read-only shell policy for `screenCapture`.
- Added `screenCapture preview` to the PET TASKS capability line.
- Updated the PET TASKS probe to assert the new capability text.

## Current Behavior

- `take a screenshot of the Wevito window` creates a preview report only.
- No screenshot is captured yet.
- No screen recording is started.
- Future screenshot execution is explicitly approval-gated.
- Full-desktop screenshots are marked high risk.
- Screen recording/GIF capture remains blocked until a separate privacy/duration gate exists.

## Validation

- Focused screen capture/parser/dispatcher/contract tests: passed `42 / 42`.
- `dotnet build .\vnext\Wevito.VNext.sln`: passed with `0` warnings and `0` errors.
- Full vNext tests: passed `144 / 144`.
- Safe Debug publish with `-SkipAssetPrep -SkipTests`: passed.
- Live `screenCapture` PET TASKS UI probe: passed.
- Probe confirmed no screenshot was captured and target sprite row hashes were unchanged.

## Live Probe Artifacts

- Probe summary: `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\pet-task-probes\20260505-144510-522-75880ffe\summary.json`
- Preview report: `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\pet-tasks\20260505-184524-screencapture-screencapture\run-summary.md`

## Next Safe Step

Add an approval-gated execution adapter for Wevito-window screenshots only. Keep full-desktop screenshots and recording behind later, stricter gates.
