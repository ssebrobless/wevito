# Snake Runtime Procedural Cleanup - 2026-05-14

## Goal

Replace the visually muddy snake runtime rows with cleaner, readable slither silhouettes while staying inside the local deterministic repair boundary.

## Scope

- Species: `snake`
- Runtime rows: all age / gender / color rows for `idle`, `walk`, `eat`, `happy`, `sad`, `sleep`, `sick`, and `bathe`
- Mutation: `sprites_runtime/snake/**`
- Tool: `tools/repair_snake_procedural_rows.py`

## Implemented

- Tuned the existing snake procedural repair tool instead of hand-editing individual PNGs.
- Increased horizontal breathing room for snake poses.
- Replaced the chunky zig-zag body with a slimmer, smoother serpentine body.
- Reduced highlight noise so the body reads as one animal instead of broken opaque chunks.
- Preserved tongue/head details for eating and expression readability.

## Evidence

- Repair report:
  - `vnext/artifacts/snake-procedural-cleanup-20260514/apply-v2/snake-procedural-repair.json`
  - Rows changed: `288`
  - Frames changed: `1080`
- Final visual-quality audit:
  - `vnext/artifacts/snake-procedural-cleanup-20260514/post-audit-v2/sprite-visual-quality.json`
  - Findings: `0`
- Runtime canvas audit:
  - `vnext/artifacts/snake-procedural-cleanup-20260514/runtime-canvas-v2.json`
  - Checked sequences: `2880`
  - Checked frames: `10800`
  - Mixed-canvas rows: `0`
  - Missing rows: `0`
  - Invalid/non-alpha rows: `0`
- Preview sheet:
  - `vnext/artifacts/snake-procedural-cleanup-20260514/runtime-previews-v2/snake-preview.png`

## Safety Notes

- No generated art was created.
- No source boards were edited.
- No prop anchors or content manifests were edited.
- This is a deterministic runtime repair pass. A later art-directed snake repaint can still replace these rows if desired.

## Validation

- `dotnet build .\vnext\Wevito.VNext.sln` passed.
- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build` passed: `591 / 591`.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests` passed.
