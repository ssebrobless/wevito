# C-PHASE 130 Batch 007: Deer Baby Female

## Goal

Repair the queued `deer_baby_female` runtime rows while preserving source boards, prop anchors, content manifests, and unrelated sprite rows.

## Scope

- Row: `deer_baby_female`
- Species: `deer`
- Age: `baby`
- Gender: `female`
- Colors: `blue`, `indigo`, `orange`, `red`, `violet`, `yellow`
- Animation families: `drink`, `eat`, `groom`, `sad`, `sleep`
- Queue issues processed: 30

## Implemented

- Ran the C-PHASE 130 sprite repair batch runner against the queued row.
- Generated direct runtime frames for missing `drink` and `groom` rows across all six color variants.
- Processed existing `eat`, `sad`, and `sleep` crop/padding findings through the guarded candidate apply path.
- Updated `tools/repair_runtime_sprite_canvas_padding.py` with a single-row candidate mode so padding findings can flow through the same dry-run, backup, apply, post-proof, and rollback-aware pipeline as generated candidates.
- Updated the C-PHASE 128 repair queue status from `queued` to `done` for `deer_baby_female`.

## Evidence

- Batch artifact root: `vnext/artifacts/c-phase-130-batches/007-deer-baby-female`
- Focused contact sheet: `vnext/artifacts/c-phase-130-batches/007-deer-baby-female/contact-sheets/deer-baby-female-focused.png`
- Sprite contract audit: `vnext/artifacts/c-phase-130-batches/007-deer-baby-female/sprite_contract.json`
- Runtime canvas audit: `vnext/artifacts/c-phase-130-batches/007-deer-baby-female/runtime_canvas.json`
- Runtime canvas markdown: `vnext/artifacts/c-phase-130-batches/007-deer-baby-female/runtime_canvas.md`
- Cockpit sweep manifest: `vnext/artifacts/c-phase-130-batches/007-deer-baby-female/cockpit-sweep/visual_qa_manifest.json`

## Validation

- `python .\tools\repair_runtime_sprite_canvas_padding.py --repo-root . --row-id deer_baby_female --species deer --age baby --gender female --color blue --animation eat --out-dir .\sprites_authored\.candidates\smoke-deer-blue-eat`
  - Passed: candidate mode wrote four candidate frames without mutating runtime.
- `python .\tools\audit_sprite_contract.py --output .\vnext\artifacts\c-phase-130-batches\007-deer-baby-female\sprite_contract.json`
  - Passed: `error_count=0`
- `python .\tools\report_runtime_canvas_mismatches.py --output .\vnext\artifacts\c-phase-130-batches\007-deer-baby-female\runtime_canvas.json --markdown .\vnext\artifacts\c-phase-130-batches\007-deer-baby-female\runtime_canvas.md --fail-on-mismatch`
  - Passed: `mismatch_count=0`, `missing_count=0`, `invalid_count=0`
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\run-matrix-sweep.ps1 -ArtifactRoot .\vnext\artifacts\c-phase-130-batches\007-deer-baby-female\cockpit-sweep -Species deer -Age baby -Gender female`
  - Passed: wrote 6 matrix rows.

## Safety Boundaries

- No hosted AI calls.
- No asset-prep run.
- No source-board mutation.
- No prop-anchor edits.
- No content-manifest edits.
- No unrelated species, age, gender, or animation rows changed.
- Padding candidate mode writes only to `sprites_authored/.candidates` and lets the existing apply service own runtime mutation.

## Next

Continue C-PHASE 130 with the next queued row after this PR is merged.
