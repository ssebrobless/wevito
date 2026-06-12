# C-PHASE 203B (Stage 1): Gemini Regeneration Prep — packs staged, halted at round-trip

Date: 2026-06-12
Branch: `claude-implementation/c-phase-203b-gemini-regen-prep`
Plan: `docs/CLAUDE_C_PHASE196_PLUS_PET_SIM_COMPLETION_PLAN_2026-06-12.md` §10.2 (Amendment R2), anti-blur policy R2b
Prompt: `docs/CLAUDE_C_PHASE196_PLUS_CODEX_MEDIUM_PROMPTS_2026-06-12.md` §8 (as amended)
Status: **phase_blocked at owner round-trip by design** (C-202-style halt: prepared-packs-only)

## What landed

1. **Anti-blur small-board splits** (`tools/authored_motion_specs.py`): `care_eat` (4),
   `care_sleep` (2), `expression_happy` (4), `expression_sad` (2), `expression_sick` (4),
   `expression_bathe` (4). The composite `care` (6) and `expression` (14) boards remain
   for layout history but are refused at prep time.
2. **Frame-budget pin** (`tools/prepare_motion_gemini_handoff.py`): any family carrying
   more than 4 frames per board is refused unless `--allow-large-board` is passed
   deliberately. Verified: `--family expression` errors out.
3. **Enlarged cells**: boards with ≤4 cells render at 256×256 px cells (default was
   148×148) so each frame claims more of Gemini's fixed output resolution.
   `cellSize` is recorded in each pack's `pack-metadata.json`.
4. **Sharpness gate** (`tools/import_authored_family_board.py`): per imported frame,
   variance-of-Laplacian edge energy over body pixels is compared against the current
   runtime frame (`--sharpness-reference-dir`); any frame below `--sharpness-ratio`
   (default 0.55) of reference aborts the WHOLE import — nothing is written, the board
   is re-rolled. Calibration: Gaussian blur of 0.7 px already drops a runtime frame to
   0.17 of its own energy, so genuine Gemini mush cannot pass. Cell geometry honored
   from `--pack-metadata`. Per-frame metrics written next to imports.
5. **Handoff packs staged** under `incoming_sprites/gemini_handoff_motion/`:
   - C-202 lane (authored locomotion): goose, squirrel, raccoon ×
     {locomotion_idle, locomotion_walk_a, locomotion_walk_b} × 6 rows = 54 boards.
   - C-203B lane (regen, larger-issues bucket): deer, fox, frog, pigeon, snake ×
     {locomotion_idle, locomotion_walk_a, locomotion_walk_b, care_eat,
     expression_happy} × 6 rows = 150 boards.
   - 204 boards total, all ≤4 frames, all 256 px cells.

## Owner round-trip (the single driving session)

Per pack directory (`incoming_sprites/gemini_handoff_motion/<species>/<age>/<gender>/<family>/`):
upload `1-upload-pack.png`, paste `4-prompt.txt`, save the result to
`5-save-edited-board-here/<slug>-edited-board.png` — or drive the whole set with
`tools/batch-drive-live-gemini.ps1` against a logged-in browser session.

After boards return, Stage 2 (Codex/Claude side): `import_authored_family_board.py`
with `--pack-metadata` + `--sharpness-reference-dir` (gate ON), identity verification
vs canonical source sheets, `propagate_authored_colors.py` (closes BUG-007 for
fox/blue), guarded apply into `sprites_authored_verified` + `sprites_runtime` with the
standard backup/sha256/post-proof/rollback evidence, per-species batches.

## Stop-Gate Checklist (Stage 1)

- [x] No sprite frame mutated (prep writes only under `incoming_sprites/`).
- [x] Anti-blur pin active and verified (expression board refused).
- [x] Every staged board ≤ 4 frames at 256 px cells, `cellSize` in metadata.
- [x] Sharpness gate implemented + calibrated before any board returns.

## Validation

Full suite 1717/1717 (no production change); `git diff --check` PASS.
Sprite contract / canvas: N/A (no runtime mutation).
