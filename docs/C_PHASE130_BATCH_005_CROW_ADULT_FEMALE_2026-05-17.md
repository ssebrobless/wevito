# C-PHASE 130 Batch 005: Crow Adult Female

## Goal

Repair the queued `crow_adult_female` runtime rows without changing source boards, prop anchors, content manifests, or unrelated sprite families.

## Scope

- Row: `crow_adult_female`
- Species: `crow`
- Age: `adult`
- Gender: `female`
- Colors: `blue`, `indigo`, `orange`, `red`, `violet`, `yellow`
- Animation families: `drink`, `groom`
- Runtime frames created or replaced: 48

## Implemented

- Ran the C-PHASE 130 sprite repair batch runner against the queued row.
- Generated direct runtime frames for all six color variants for `drink_00..03` and `groom_00..03`.
- Updated the C-PHASE 128 repair queue status from `queued` to `done` for `crow_adult_female`.
- Preserved the existing runtime canvas contract and source-of-truth boundaries.

## Evidence

- Batch artifact root: `vnext/artifacts/c-phase-130-batches/005-crow-adult-female`
- Focused contact sheet: `vnext/artifacts/c-phase-130-batches/005-crow-adult-female/contact-sheets/crow-adult-female-focused.png`
- Sprite contract audit: `vnext/artifacts/c-phase-130-batches/005-crow-adult-female/sprite_contract.json`
- Runtime canvas audit: `vnext/artifacts/c-phase-130-batches/005-crow-adult-female/runtime_canvas.json`
- Runtime canvas markdown: `vnext/artifacts/c-phase-130-batches/005-crow-adult-female/runtime_canvas.md`
- Cockpit sweep manifest: `vnext/artifacts/c-phase-130-batches/005-crow-adult-female/cockpit-sweep/visual_qa_manifest.json`

## Validation

- `python .\tools\audit_sprite_contract.py --output .\vnext\artifacts\c-phase-130-batches\005-crow-adult-female\sprite_contract.json`
  - Passed: `error_count=0`
- `python .\tools\report_runtime_canvas_mismatches.py --output .\vnext\artifacts\c-phase-130-batches\005-crow-adult-female\runtime_canvas.json --markdown .\vnext\artifacts\c-phase-130-batches\005-crow-adult-female\runtime_canvas.md --fail-on-mismatch`
  - Passed: `mismatch_count=0`, `missing_count=0`, `invalid_count=0`
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\run-matrix-sweep.ps1 -ArtifactRoot .\vnext\artifacts\c-phase-130-batches\005-crow-adult-female\cockpit-sweep -Species crow -Age adult -Gender female`
  - Passed: wrote 6 matrix rows.

## Safety Boundaries

- No hosted AI calls.
- No asset-prep run.
- No source-board mutation.
- No prop-anchor edits.
- No content-manifest edits.
- No unrelated species, age, gender, or animation rows changed.

## Next

Continue C-PHASE 130 with the next queued row after this PR is merged.
