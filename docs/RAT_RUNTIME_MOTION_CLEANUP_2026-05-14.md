# Rat Runtime Motion Cleanup

## Goal

Clean the rat runtime animation rows that still read as muddy duplicate/afterimage stacks in the desktop overlay.

## Scope

- Species: `rat`
- Runtime path: `sprites_runtime/rat/**`
- Tool: `tools/repair_rat_motion_rows.py`
- Evidence root: `vnext/artifacts/rat-motion-cleanup-20260514/`

## Method

The repair is deterministic and local-only. It does not generate art, import external images, repaint pixels, recolor variants, edit source boards, or edit prop anchors.

Each runtime row is rebuilt from that row's own `idle_*.png` silhouettes:

- crop the visible idle silhouette
- re-center it with a stable bottom anchor
- apply small whole-sprite offsets per animation family
- write backups before replacing runtime frames

This removes obvious muddy/ghosted rat action frames while preserving the shipped rat silhouette and palette family. It is a runtime readability cleanup, not a final hand-authored limb-animation pass.

## Results

- Rows scanned: `288`
- Rows changed: `288`
- Frames changed: `1068`
- Structural visual findings after repair: `0`
- Runtime canvas mismatches after repair: `0`
- Missing runtime rows after repair: `0`
- Invalid/non-alpha PNGs after repair: `0`

## Evidence

- Apply report: `vnext/artifacts/rat-motion-cleanup-20260514/rat-motion-cleanup.md`
- Apply JSON: `vnext/artifacts/rat-motion-cleanup-20260514/rat-motion-cleanup.json`
- Runtime preview sheets: `vnext/artifacts/rat-motion-cleanup-20260514/runtime-previews/`
- Post-audit report: `vnext/artifacts/rat-motion-cleanup-20260514/post-audit/sprite-visual-quality.md`
- Runtime canvas report: `vnext/artifacts/rat-motion-cleanup-20260514/runtime-canvas.md`

## Validation

Run before merge:

```powershell
python .\tools\report_sprite_visual_quality.py --output-root .\vnext\artifacts\rat-motion-cleanup-20260514\post-audit
python .\tools\render_runtime_sprite_previews.py --output-root .\vnext\artifacts\rat-motion-cleanup-20260514\runtime-previews
python .\tools\report_runtime_canvas_mismatches.py --output .\vnext\artifacts\rat-motion-cleanup-20260514\runtime-canvas.json --markdown .\vnext\artifacts\rat-motion-cleanup-20260514\runtime-canvas.md --fail-on-mismatch
dotnet build .\vnext\Wevito.VNext.sln
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
```

## Remaining Quality Note

Rat rows are now clean and readable at runtime scale, but they are still conservative whole-sprite motion rows. A later art-authored pass can improve true limb motion if higher animation fidelity is required.
