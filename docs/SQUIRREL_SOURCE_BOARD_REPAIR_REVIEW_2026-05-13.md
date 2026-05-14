# Squirrel Source Board Repair Review - 2026-05-13

## Goal

Evaluate whether the original Gemini squirrel source boards can safely replace the current low-quality runtime squirrel sprites.

## Sources Reviewed

- `incoming_sprites/gemini_handoff/squirrel/baby/male/2-editable-board.png`
- `incoming_sprites/gemini_handoff/squirrel/baby/female/2-editable-board.png`
- `incoming_sprites/gemini_handoff/squirrel/teen/male/2-editable-board.png`
- `incoming_sprites/gemini_handoff/squirrel/teen/female/2-editable-board.png`
- `incoming_sprites/gemini_handoff/squirrel/adult/male/2-editable-board.png`
- `incoming_sprites/gemini_handoff/squirrel/adult/female/2-editable-board.png`

## Tooling Change

`tools/import_gemini_sprite_block.py` was updated to support the newer Gemini editable-board format used by the squirrel source boards:

- Detects blue-gray editable-board separators.
- Avoids treating squirrel shadow/body lines as row separators.
- Crops from the cleaned work region so label/background pixels do not bleed into imported frames.
- Preserves the old dark-grid import path as the first attempt.

This is a tooling fix only. It does not mutate runtime PNGs.

## Candidate Evidence

Candidate runtime frames were generated under:

- `vnext/artifacts/squirrel-source-board-repair-20260513/candidate-runtime/`

Candidate preview sheets:

- `vnext/artifacts/squirrel-source-board-repair-20260513/candidate-preview/squirrel-preview.png`
- `vnext/artifacts/squirrel-source-board-repair-20260513/candidate-preview/squirrel-colors.png`

Candidate audit:

- `vnext/artifacts/squirrel-source-board-repair-20260513/candidate-audit/sprite-visual-quality.md`
- `vnext/artifacts/squirrel-source-board-repair-20260513/candidate-audit/sprite-visual-quality.json`

Audit result:

- `rows_scanned`: 288
- `frames_scanned`: 1080
- `findings`: 288
- All findings are `static_duplicate_row`.

## Decision

Do not apply these squirrel candidate frames to `sprites_runtime`.

The candidate fixes some natural-color/source-board extraction problems, but it does not fix the core player-visible issue: the squirrel has no real animation. Every imported squirrel row is static, so applying this candidate would replace one bad squirrel presentation with another bad squirrel presentation.

It would also risk confusing the color-variant pipeline because the temporary candidate duplicates the same natural-color extraction into all six color folders for review only.

## Required Next Repair

Squirrel needs a true visual-animation repair pass, not a runtime padding/import-only patch.

The next safe squirrel repair should create or recover real motion rows for:

- `idle`
- `walk`
- `eat`
- `happy`
- `sad`
- `sleep`
- `sick`
- `bathe`

Required targets:

- squirrel / baby / male
- squirrel / baby / female
- squirrel / teen / male
- squirrel / teen / female
- squirrel / adult / male
- squirrel / adult / female

For each target, the repair packet must include:

- Source/candidate provenance.
- Contact sheet.
- Motion preview.
- Machine audit showing no `static_duplicate_row`.
- Backup/hash/rollback plan before any runtime apply.

## Status

Runtime squirrel PNGs were not modified in this review.
