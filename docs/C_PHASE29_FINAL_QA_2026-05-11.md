# C-PHASE 29 Final QA Sweep

Date: 2026-05-11
Branch: `claude-implementation/c-phase-29-final-qa`

## Summary

C-PHASE 29 performed the release-candidate QA sweep after the C-PHASE 28 aging/death/ghost lifecycle merge. The runtime canvas contract is green, vNext tests are green, PET TASKS probes are green, pinned overlay behavior now works while another app owns foreground focus, and visual capture artifacts were regenerated successfully.

## Results

| Check | Result | Artifact |
| --- | --- | --- |
| vNext build | Pass | `vnext/artifacts/c-phase-29-final-qa/dotnet-build.log` |
| vNext tests | Pass, 278/278 | `vnext/artifacts/c-phase-29-final-qa/dotnet-test.log` and `dotnet-test-after-pinned-fix.log` |
| Debug publish | Pass with `-SkipAssetPrep -SkipTests` | `vnext/artifacts/c-phase-29-final-qa/build-vnext-debug-skip-assetprep.log` |
| Runtime canvas mismatch report | Pass, 0 mixed-canvas rows | `vnext/artifacts/c-phase-29-final-qa/runtime-canvas-mismatches.json` |
| Sprite contract audit | Pass, 0 errors | `vnext/artifacts/c-phase-29-final-qa/sprite-contract.json` |
| Optional animation readiness | Pass, 0 invalid optional art | `vnext/artifacts/c-phase-29-final-qa/optional-readiness.json` |
| PET TASKS family probes | Pass | `vnext/artifacts/c-phase-29-final-qa/pet-task-probe-logs/` |
| Action probe | Pass | `vnext/artifacts/c-phase-29-final-qa/probe-vnext-actions.log` |
| Pinned overlay probe | Pass | `vnext/artifacts/probes/20260511-234521/summary.json` |
| vNext visual capture | Pass | `vnext/artifacts/screenshots/20260511-234720-388/summary.json` |

## Fixes Made During QA

1. Pinned overlay foreground resilience:
   - The probe now launches Notepad as a controlled foreground app before each global hotkey. This validates that Wevito can operate while the user is working in another app.
   - The broker logs hotkey registration and falls back from `Ctrl+Shift` to `Ctrl+Alt` if a hotkey is already taken by the foreground application.
   - Overlay click hit-testing now prefers the most recently published/topmost overlay region, so the basket/tool popup receives clicks when it overlaps the home panel.

2. Sprite preview capture stability:
   - `tools/render_runtime_sprite_previews.py` now ignores hidden/system directories such as `.staging` and `_metadata` when enumerating species folders.
   - This keeps visual capture focused on real species rows without masking runtime sprite contract failures, which remain covered by `audit_sprite_contract.py` and `report_runtime_canvas_mismatches.py`.

## Important Observations

- Runtime canvas rows are clean: 2,880 sequences, 10,800 frames, 0 mixed-canvas rows, 0 missing rows, and 0 invalid rows.
- Optional animation readiness still reports most optional rows as fallback-only: 2,516 fallback-only targets, 0 invalid optional art. This is not a release blocker for C-PHASE 29, but it remains a visual/content completion item.
- The `screenCapture` PET TASKS probe currently requires approval before preview, which matches the later safety posture even though older C-PHASE 0 notes treated it as no-approval.

## Remaining Release Gate

C-PHASE 30 still needs the final release tag/package pass. Live model calls remain disabled unless the user explicitly opens that gate.
