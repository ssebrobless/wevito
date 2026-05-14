# Snake Source Board Review - 2026-05-13

## Goal

Check whether the original snake source boards can safely repair the player-visible snake quality problems.

## Evidence Produced

Source thumbnail board:

- `vnext/artifacts/snake-source-board-review-20260513/snake-source-thumbnails.png`

Editable-board candidate preview:

- `vnext/artifacts/snake-source-board-review-20260513/candidate-preview/snake-preview.png`
- `vnext/artifacts/snake-source-board-review-20260513/candidate-preview/snake-colors.png`

Editable-board candidate audit:

- `vnext/artifacts/snake-source-board-review-20260513/candidate-audit/sprite-visual-quality.md`
- `vnext/artifacts/snake-source-board-review-20260513/candidate-audit/sprite-visual-quality.json`

Current runtime audit:

- `vnext/artifacts/snake-source-board-review-20260513/current-runtime-audit/sprite-visual-quality.md`
- `vnext/artifacts/snake-source-board-review-20260513/current-runtime-audit/sprite-visual-quality.json`

Ground-aware whole-runtime audit:

- `vnext/artifacts/sprite-visual-quality-audit-20260513-ground-aware/sprite-visual-quality.md`
- `vnext/artifacts/sprite-visual-quality-audit-20260513-ground-aware/sprite-visual-quality.json`

## Findings

- The editable snake boards import, but the imported candidate is not an acceptable runtime replacement.
- The editable-board candidate has `48` findings, all `static_duplicate_row`.
- The current snake runtime has actual motion in more rows than the editable-board candidate.
- The saved Gemini-result boards are not in the importer-supported 5x6 editable-board format and were not applied.
- No snake runtime PNGs were changed.

## Audit Tooling Adjustment

`tools/report_sprite_visual_quality.py` now ignores bottom-only edge contact as a normal grounded-pet condition.

This makes the audit useful for the actual player-visible problems:

- top/side crop
- static rows
- low-motion rows
- mixed geometry

The ground-aware whole-runtime audit reduced broad false-positive noise from `2720` findings to `158` findings.

## Decision

Do not replace current snake runtime sprites with the editable-board candidate.

The current runtime snake still needs polish, but the editable-board candidate would remove more motion than it repairs. The right next step is either:

- a true snake animation source recovery from the saved Gemini-result/upload-pack boards, with a dedicated extractor for that layout, or
- a fresh art repair candidate that preserves full slither extension and does not squeeze the snake into a small box.

## Status

Runtime snake PNGs were not modified in this review.
