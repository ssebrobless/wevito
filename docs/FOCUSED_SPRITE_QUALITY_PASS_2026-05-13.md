# Focused Sprite Quality Pass

## Goal

Respond to the latest in-game sprite review without hiding real art problems behind runtime hacks.

User-facing direction:

- Food and water should be dragged/used as action objects during `ACTIONS`, not permanently staged as habitat props.
- The focused stage should keep pets readable, side by side, and not force every species into the same oversized box.
- Broken sprite art should be identified as exact repair work, not declared fixed just because tests pass.

## Evidence Generated

- Runtime preview sheets: `vnext/artifacts/focused-sprite-quality-20260513/runtime-previews/`
- Authored-preferred preview sheets: `vnext/artifacts/focused-sprite-quality-20260513/authored-previews/`
- Machine audit: `vnext/artifacts/focused-sprite-quality-20260513/machine-audit/sprite-visual-quality.md`
- Machine audit JSON: `vnext/artifacts/focused-sprite-quality-20260513/machine-audit/sprite-visual-quality.json`

## Runtime Display Fix

Changed focused calm-lineup scaling so small/tiny sprites keep their authored base scale instead of being inflated to a 108 px minimum height.

Why:

- The previous focused-stage rule made compact animals look oversized and cramped.
- The app should not make squirrel, snake, frog, or pigeon appear broken by stretching them to a uniform visual height.
- Large sprites are still capped at 144 px tall so they do not swallow the stage.

Files changed:

- `vnext/src/Wevito.VNext.Shell/HomePanelWindow.xaml.cs`
- `vnext/tests/Wevito.VNext.Tests/HomePanelWindowRenderingTests.cs`

## Current Sprite Findings

The post-padding audit no longer finds high/critical edge crop findings for the currently reported problem species. The remaining issues are real art/animation quality issues:

- `squirrel`: idle rows across all ages/genders/colors have very low motion. This needs a true animation/source repair pass, not canvas padding.
- `snake`: rows are no longer side/top cropped, but the source style and pose consistency still need visual repair, especially baby/teen forms.
- `crow`: needs manual review for the reported flattened-head frame. The detector mostly sees expected bottom contact, so visual review is the right gate.
- `pigeon`: crop risk was reduced by transparent padding, but some frames still need manual visual review for silhouette quality.
- `fox`: crop risk was reduced by transparent padding; remaining concerns should be reviewed in-game after the display scale fix.

## Food And Water Direction

The focused stage should not treat food/water as permanent habitat decoration. The intended next UX is:

- `ACTIONS` opens a temporary interaction mode.
- Food/water/toy/care objects become draggable or explicitly usable action objects.
- Applying an object should make the target pet obvious before the action resolves.
- Closing `ACTIONS` returns living pets to their calm idle positions.

This report does not implement drag-and-drop action objects; it records the direction so the next UI phase does not reintroduce permanent food/water clutter.

## Next Repair Queue

1. `squirrel`: rebuild or replace idle/walk rows from source boards or approved candidates so movement is readable.
2. `snake`: review source boards and create a proper slither repair packet for baby/teen/adult variants.
3. `crow`: isolate the flattened-head frame from runtime previews and repair only that row/frame if confirmed.
4. `pigeon`: review the latest padded runtime frames in-game and repair true silhouette breaks.
5. `actions`: implement draggable food/water/tool objects with clear target-pet feedback.

