# Squirrel Runtime Motion Cleanup - 2026-05-14

## Goal

Improve squirrel runtime motion readability without replacing the existing high-quality squirrel silhouettes.

## Scope

- Species: `squirrel`
- Runtime rows: all age / gender / color rows for `idle`, `walk`, `eat`, `happy`, `sad`, `sleep`, `sick`, and `bathe`
- Mutation: `sprites_runtime/squirrel/**`
- Tool: `tools/repair_squirrel_motion_rows.py`

## Implemented

- Strengthened the existing squirrel repair tool's motion offsets.
- Repacked each source silhouette inside its existing canvas with safe transparent margins.
- Preserved existing squirrel art style and local runtime silhouettes.
- Avoided procedural limb/body redraw because the source art is crisp and a full redraw would be higher visual risk.

## Evidence

- Repair report:
  - `vnext/artifacts/squirrel-motion-cleanup-20260514/apply-v2/squirrel-motion-cleanup.json`
  - Rows scanned: `288`
  - Rows changed: `288`
  - Frames changed: `936`
- Final visual-quality audit:
  - `vnext/artifacts/squirrel-motion-cleanup-20260514/post-audit-v2/sprite-visual-quality.json`
  - Findings: `0`
- Runtime canvas audit:
  - `vnext/artifacts/squirrel-motion-cleanup-20260514/runtime-canvas-v2.json`
  - Checked sequences: `2880`
  - Checked frames: `10800`
  - Mixed-canvas rows: `0`
  - Missing rows: `0`
  - Invalid/non-alpha rows: `0`
- Preview sheet:
  - `vnext/artifacts/squirrel-motion-cleanup-20260514/runtime-previews-v2/squirrel-preview.png`

## Safety Notes

- No generated art was created.
- No source boards were edited.
- No prop anchors or content manifests were edited.
- This is a runtime stabilization pass. A later art-authored squirrel animation pass can still add true limb animation.

## Validation

- `dotnet build .\vnext\Wevito.VNext.sln` passed.
- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build` passed: `591 / 591`.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests` passed.
