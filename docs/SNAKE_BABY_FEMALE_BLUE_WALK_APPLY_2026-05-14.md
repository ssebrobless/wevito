# Snake Baby Female Blue Walk Apply

Date: 2026-05-14

## Scope

Applied the accepted one-row pilot candidate for:

`snake / baby / female / blue / walk`

Only these runtime frames were replaced:

- `sprites_runtime/snake/baby/female/blue/walk_00.png`
- `sprites_runtime/snake/baby/female/blue/walk_01.png`
- `sprites_runtime/snake/baby/female/blue/walk_02.png`
- `sprites_runtime/snake/baby/female/blue/walk_03.png`
- `sprites_runtime/snake/baby/female/blue/walk_04.png`
- `sprites_runtime/snake/baby/female/blue/walk_05.png`

No source boards, prop anchors, content manifests, other colors, other rows, or other species were changed.

## Evidence

- Candidate packet: `vnext/artifacts/snake-one-row-candidate-20260514/`
- Apply report: `vnext/artifacts/snake-walk-apply-20260514/apply-report.json`
- Backup folder: `vnext/artifacts/snake-walk-apply-20260514/backup-before-apply/sprites_runtime/snake/baby/female/blue/`
- Post-apply contact sheet: `vnext/artifacts/snake-walk-apply-20260514/post-apply-contact-sheet.png`
- Runtime canvas report: `vnext/artifacts/snake-walk-apply-20260514/runtime-canvas.md`
- Sprite contract report: `vnext/artifacts/snake-walk-apply-20260514/sprite-contract.json`

## Rollback Drill

The apply script restored the six target frames from backup, verified each restored hash matched the pre-apply hash, then re-applied the candidate so the working tree ends in the improved pilot state.

## Validation

- `python .\tools\report_runtime_canvas_mismatches.py --output .\vnext\artifacts\snake-walk-apply-20260514\runtime-canvas.json --markdown .\vnext\artifacts\snake-walk-apply-20260514\runtime-canvas.md --fail-on-mismatch`
  - `mismatch_count=0`
  - `missing_count=0`
  - `invalid_count=0`
- `python .\tools\audit_sprite_contract.py --output .\vnext\artifacts\snake-walk-apply-20260514\sprite-contract.json`
  - `error_count=0`
- `dotnet build .\vnext\Wevito.VNext.sln`
  - passed
- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build`
  - passed `609 / 609`

## Remaining Work

This is only one pilot row. Snake still needs a broader visual repair pass across life stages, genders, colors, and action families if the pilot style is accepted in-game.
