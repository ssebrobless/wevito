# Snake Gemini Clean Source Apply

Generated: 2026-05-14

## Goal

Replace the worst snake runtime rows with the user-provided clean Gemini source board while preserving the guarded sprite workflow:

- source provenance is stored in-repo
- runtime mutations are limited to snake rows
- apply is backed up by hashes
- rollback drill is executed
- post-apply structural validation stays green

## Source

- User-provided source: `C:\Users\fishe\Downloads\Gemini_Generated_Image_e3suibe3suibe3su.png`
- Preserved provenance copy: `incoming_sprites/gemini_handoff/snake/clean-source-20260514/snake-clean-source-gemini-20260514.png`
- Import tool: `tools/import_snake_gemini_clean_source.py`

## Rows Applied

The clean blue source rows were extracted from the Gemini board and propagated through Wevito's six runtime color variants.

- `snake / baby / female / all colors / sad_00..01`
- `snake / baby / female / all colors / sick_00..03`
- `snake / teen / female / all colors / happy_00..03`
- `snake / adult / female / all colors / idle_00..03`
- `snake / adult / female / all colors / walk_00..05`
- `snake / adult / female / all colors / eat_00..03`
- `snake / adult / female / all colors / happy_00..03`
- `snake / adult / female / all colors / sick_00..03`
- `snake / adult / female / all colors / bathe_00..03`
- `snake / adult / male / all colors / walk_00..05`
- `snake / adult / male / all colors / sick_00..03`

Total changed runtime frames: `276`.

## Apply Proof

- Apply report: `vnext/artifacts/snake-gemini-clean-source-apply-20260514/snake-gemini-clean-source-apply-report.md`
- JSON report: `vnext/artifacts/snake-gemini-clean-source-apply-20260514/snake-gemini-clean-source-apply-report.json`
- Backup root: `vnext/artifacts/snake-gemini-clean-source-apply-20260514/backup-before-apply/20260515T025018Z`
- Rollback drill: passed, then candidate frames were re-applied so the working tree ends in post-apply state.

## Runtime Preview

- Runtime preview folder: `vnext/artifacts/snake-gemini-clean-source-apply-20260514/runtime-previews/`
- Snake preview: `vnext/artifacts/snake-gemini-clean-source-apply-20260514/runtime-previews/snake-preview.png`

## Validation

- `python .\tools\report_runtime_canvas_mismatches.py --output .\vnext\artifacts\snake-gemini-clean-source-apply-20260514\runtime-canvas.json --markdown .\vnext\artifacts\snake-gemini-clean-source-apply-20260514\runtime-canvas.md --fail-on-mismatch`
  - Result: `0` mixed-canvas rows, `0` missing rows, `0` invalid rows.
- `python .\tools\audit_sprite_contract.py --output .\vnext\artifacts\snake-gemini-clean-source-apply-20260514\sprite-contract.json`
  - Result: `0` errors.
- `dotnet build .\vnext\Wevito.VNext.sln`
  - Result: passed.
- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build`
  - Result: passed, `609 / 609`.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests`
  - Result: passed.

## Notes

`tools/generate_runtime_pose_sprites.py` was adjusted so future procedural snake walk rows use taller body targets. This does not change canvas geometry, but it prevents future regenerated snake walk frames from being crushed into tiny flat slither strips.

The generated source is substantially cleaner than the previous mushy snake runtime rows. The next check should be an in-app motion pass with the dev controller, because structural validation cannot judge whether the slither motion feels natural in the live overlay.
