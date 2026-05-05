# Code-Side Phase 41: Under-the-Hood PET TASKS Behavior

Generated: 2026-05-05

## Shape

```text
PET TASKS
   │
   ├── prepare / preview / run / blocked / done
   │          │
   │          └── updates task card, reports, feedback text, trace logs
   │
   └── no visible pet animation override

Pet overlay
   │
   └── continues normal pet-sim behavior:
       idle / walk / eat / happy / sad / sleep / sick / bathe
```

## Product Decision

The pets should not perform special completion/review/failure/helper animations when they complete tasks. PET TASKS work should be under the hood so the overlay still feels like a normal pet simulator.

## What Changed

- Removed visible pet animation pulses from PET TASKS command submission.
- Removed visible pet animation pulses from PET TASKS approve/cancel transitions.
- Removed visible pet animation pulses from preview completion/failure.
- Removed visible pet animation pulses from execution completion/failure.
- Removed visible pet animation pulses from clipboard/link basket helper events.
- Removed the ambient `Waiting` animation override while tool windows are open.
- Left normal pet-sim animation control intact for care/actions/dev scenarios.

## Compatibility Note

Legacy work-companion enum values may still exist in contracts for now, but Shell no longer drives them from PET TASKS/tool activity. Future cleanup can remove or migrate them only if saved-state compatibility is handled deliberately.

## Validation Plan

- `dotnet build .\vnext\Wevito.VNext.sln`: passed with `0` warnings and `0` errors.
- Full vNext tests: passed `144 / 144`.
- Safe Debug publish with `-SkipAssetPrep -SkipTests`: passed.
- Live `screenCapture` PET TASKS UI probe: passed.
- Probe confirmed task cards/reports still work.
- Probe confirmed target sprite row hashes were unchanged.

## Live Probe Artifacts

- Probe summary: `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\pet-task-probes\20260505-185830-292-cd2e5f5d\summary.json`
- Preview report: `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\pet-tasks\20260505-225844-screencapture-screencapture\run-summary.md`

## Next Safe Step

Continue tool implementation with this constraint: tool/task status must be expressed through the PET TASKS UI, feedback text, reports, and trace logs, not through special pet animations.
