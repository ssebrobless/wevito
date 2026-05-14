# PET TASKS Popup And Pigeon Cleanup

## Goal

Fix the usability issue where the PET TASKS report/apply controls could be visually blocked by the home panel, then continue the runtime sprite cleanup sweep with the next visibly broken species: pigeon.

## Scope

- Changed the WPF shell layout rule for `ToolPopupWindow`.
- Rebuilt `sprites_runtime/pigeon/**` runtime motion rows only.
- Added a deterministic pigeon cleanup tool and evidence artifacts.
- Did not touch source boards, `sprites_authored*`, prop anchors, generation/import code, or hosted/model AI paths.

## Implemented

- `ShellCoordinator.ResolveToolPopupRect(...)` now opens the tool popup beside the home panel when there is not enough room above it. This keeps PET TASKS report controls reachable instead of placing the popup behind the main Wevito panel.
- Added layout tests that cover the tall-home overlap case and the normal above-home case.
- Added `tools/repair_pigeon_motion_rows.py`.
- Rebuilt pigeon `idle`, `walk`, `eat`, `happy`, `sad`, `sleep`, `sick`, and `bathe` rows for all ages, genders, and colors from each row's safest local idle silhouette.
- Preserved existing runtime canvas sizes and per-row color/style identity; this is a deterministic cleanup pass, not new art generation.

## Evidence

- Dry run: `vnext/artifacts/pigeon-motion-cleanup-20260514/dry-run/pigeon-motion-cleanup.md`
- Apply report: `vnext/artifacts/pigeon-motion-cleanup-20260514/apply/pigeon-motion-cleanup.md`
- Sprite quality audit: `vnext/artifacts/pigeon-motion-cleanup-20260514/post-audit/sprite-visual-quality.md`
- Runtime canvas audit: `vnext/artifacts/pigeon-motion-cleanup-20260514/runtime-canvas.md`
- Runtime previews: `vnext/artifacts/pigeon-motion-cleanup-20260514/runtime-previews/pigeon-preview.png`

## Results

- Pigeon dry-run scan: `288` rows, `1080` target frames.
- Pigeon final apply: `288` rows scanned, `246` rows changed, `858` frames changed after the happy-row edge-touch correction.
- Sprite visual quality audit: `0` findings.
- Runtime canvas audit: `2880` sequences, `10800` frames, `0` mixed-canvas rows, `0` missing rows, `0` invalid rows.

## Notes

Wevito can use these artifacts as reviewed evidence later, but this pass does not perform hidden learning, hidden training, model calls, or autonomous mutation. The next visual pass should continue species-by-species from contact sheets and in-game observation.
