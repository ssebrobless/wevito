# Snake And Squirrel Readable Motion Repair - 2026-05-13

## Goal

Respond to in-game review that `snake` looks bad and `squirrel` has no readable animation, without using generation or changing source boards.

## Implemented

- Added `tools/repair_readable_motion_rows.py`.
- Applied deterministic runtime-only motion repairs for:
  - `snake`: `idle`, `walk`, and `happy` rows get a sine-wave row warp so the body reads more like slithering.
  - `squirrel`: `idle`, `walk`, and `happy` rows get stronger bounce/lean offsets so the animation is no longer visually frozen.
- Ran a follow-up snake canvas-padding pass because the wave motion exposed edge-touch risk in wider snake poses.

## Scope

- Runtime PNGs only.
- Species touched: `snake`, `squirrel`.
- Families touched: `idle`, `walk`, `happy`.
- Ages/genders/colors touched: all runtime variants for those species and families.

## Evidence

- Dry run: `vnext/artifacts/readable-motion-repair-20260513/dry-run/`
- Apply report and backups: `vnext/artifacts/readable-motion-repair-20260513/apply/`
- Padding dry run: `vnext/artifacts/readable-motion-repair-20260513/padding-dry-run/`
- Padding apply report: `vnext/artifacts/readable-motion-repair-20260513/padding-apply/`
- Post-padding audit: `vnext/artifacts/readable-motion-repair-20260513/post-padding-audit/sprite-visual-quality.md`
- Post-padding previews: `vnext/artifacts/readable-motion-repair-20260513/post-padding-previews/`

## Validation

- `python .\tools\report_sprite_visual_quality.py --output-root .\vnext\artifacts\readable-motion-repair-20260513\post-padding-audit` passed with `0` findings.
- `dotnet build .\vnext\Wevito.VNext.sln` passed.
- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build` passed: 583 / 583.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests` passed.

## Remaining Visual Truth

This is still a runtime repair, not a real replacement for authored animation. It improves motion readability, but `snake` and `squirrel` still need true authored animation/source-board work if the final bar is "beautiful," not merely "less static."
