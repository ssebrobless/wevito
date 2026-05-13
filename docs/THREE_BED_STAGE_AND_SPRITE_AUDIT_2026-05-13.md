# Three-Bed Stage And Sprite Audit

## Goal

Respond to the May 13 runtime visual report:

- focused Wevito window was too cramped and pets were hard to see;
- the universal home still drew a house, food bowl, and water bowl;
- pets did not visibly walk back to their focused home positions;
- squirrel, snake, pigeon, crow, and other rows still need sprite-quality review.

## Runtime Presentation Changes

- Replaced the universal habitat loadout with exactly three shared `moss_bed` slots for every species.
- Removed the visual house, food bowl, and water bowl from the focused habitat stage manifest.
- Kept action/care item recommendations in the HUD/action cards; this change only simplifies the stage props.
- Focused/pinned home layout now assigns living pets to the three bed slots instead of placing them near the old shelter floor.
- Returning pets keep their simulated position until they settle at home, so they can visibly walk back instead of snapping into the calm lineup.
- Focused/pinned returning pets now use `Walk` while recalling to their bed, then settle into the calm idle lineup.

## Non-Mutating Sprite Audit

New tool:

`tools/report_sprite_visual_quality.py`

Artifacts:

- `vnext/artifacts/sprite-visual-quality-audit-20260513/sprite-visual-quality.json`
- `vnext/artifacts/sprite-visual-quality-audit-20260513/sprite-visual-quality.md`
- `vnext/artifacts/sprite-visual-quality-audit-20260513/runtime-canvas.json`
- `vnext/artifacts/sprite-visual-quality-audit-20260513/runtime-canvas.md`
- `vnext/artifacts/sprite-visual-quality-audit-20260513/sprite-contract.json`

The audit is read-only. It scans runtime PNG rows for:

- `mixed_geometry`
- `edge_touch`
- `empty_frame`
- `sparse_frame`
- `static_duplicate_row`
- `low_motion_row`

## Current Audit Summary

- Rows scanned: `2884`
- Frames scanned: `10818`
- Findings: `2726`
- Existing runtime contract audit: `0` missing rows, `0` invalid rows, `0` strict row mismatches, but `3852` canonical geometry differences.
- Existing sprite contract audit: `0` errors, `360 / 360` runtime variant dirs, `10818` runtime frames found vs `10800` expected.

Highest priority automated findings:

- `goose / baby / female / blue / drop_ball`, `pickup_ball`, and `play_ball` have mixed frame geometry in the optional rows.
- `snake` has widespread left/right edge touches, matching the in-game impression that the body is cramped or cut against the canvas.
- `pigeon` has widespread top/left/right/bottom edge touches, matching prior reports of cropped/broken frames.
- `squirrel` has widespread bottom edge touches plus `36` low-motion rows, matching the report that squirrel has weak or missing animation.
- `crow` currently reports mostly bottom-edge contacts in this automated pass; the user's flattened-head observation should still be manually reviewed because the current detector only catches alpha touching the image edge, not poor silhouette shape slightly below the edge.

## Safety Notes

- No sprite PNGs were edited.
- No source boards were edited.
- No generation/import/apply flow was started.
- This pass only changes runtime staging rules, tests, and report-only audit tooling/artifacts.

## Validation

- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "Habitat|HomePanelWindowRendering|FocusedPetReturning"`
- `python .\tools\report_sprite_visual_quality.py --output-root .\vnext\artifacts\sprite-visual-quality-audit-20260513`
- `python .\tools\report_runtime_canvas_mismatches.py --output .\vnext\artifacts\sprite-visual-quality-audit-20260513\runtime-canvas.json --markdown .\vnext\artifacts\sprite-visual-quality-audit-20260513\runtime-canvas.md`
- `python .\tools\audit_sprite_contract.py --output .\vnext\artifacts\sprite-visual-quality-audit-20260513\sprite-contract.json`

## Next Recommended Repair Queue

1. Fix runtime proof surface first by confirming the three-bed focused stage shows three visible pets at bed assignments.
2. Repair or regenerate `snake` animation rows with body extension allowed and enough transparent padding.
3. Repair `squirrel` animation rows, prioritizing locomotion and idle so the animal visibly animates.
4. Repair `pigeon` rows with top/side crop artifacts.
5. Manually inspect crow rows for silhouette shape/crop artifacts not caught by edge-touch detection.
6. Re-run the same audit and add contact sheets for the top failing rows before any broad sprite apply.
