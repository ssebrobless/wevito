# Fox Runtime Motion Cleanup - 2026-05-14

## Goal

Clean the current fox runtime rows enough for in-game use while preserving the local no-generation safety boundary.

## Scope

- Species: `fox`
- Runtime rows: all age / gender / color rows for `idle`, `walk`, `eat`, `happy`, `sad`, `sleep`, `sick`, and `bathe`
- Source: existing local runtime idle silhouettes only
- Mutation: `sprites_runtime/fox/**`

## Implemented

- Added `tools/repair_fox_motion_rows.py`.
- Repacked fox action and motion rows from safe local idle silhouettes.
- Kept all generated frames inside their runtime canvases with transparent margins.
- Reworked dense adult-female action offsets after the first repack revealed static duplicate rows.
- Preserved a backup-before-apply folder for each apply run under the artifact root.

## Evidence

- Final visual-quality audit:
  - `vnext/artifacts/fox-motion-cleanup-20260514/post-audit-repack-v2/sprite-visual-quality.json`
  - Findings: `0`
- Final runtime canvas audit:
  - `vnext/artifacts/fox-motion-cleanup-20260514/runtime-canvas-repack-v2.json`
  - Checked sequences: `2880`
  - Checked frames: `10800`
  - Mixed-canvas rows: `0`
  - Missing rows: `0`
  - Invalid/non-alpha rows: `0`
- Final preview sheet:
  - `vnext/artifacts/fox-motion-cleanup-20260514/runtime-previews-repack-v2/fox-preview.png`

## Safety Notes

- No generated art was created.
- No source boards were edited.
- No prop anchors, content manifests, or sprite workflow candidate packets were edited.
- This is a deterministic runtime stabilization pass, not a replacement for a later hand-authored fox animation polish pass.

## Validation

- `dotnet build .\vnext\Wevito.VNext.sln` passed.
- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build` passed: `591 / 591`.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests` passed.
