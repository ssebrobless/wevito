# C-PHASE 203A: Procedural Cleanup Sweep — crow, goose, raccoon, rat, squirrel

Date: 2026-06-12
Branch: `claude-implementation/c-phase-203a-procedural-cleanup`
Plan: `docs/CLAUDE_C_PHASE196_PLUS_PET_SIM_COMPLETION_PLAN_2026-06-12.md` §10.2 (Amendment R2)
Prompt: `docs/CLAUDE_C_PHASE196_PLUS_CODEX_MEDIUM_PROMPTS_2026-06-12.md` §8 (as amended)
Queue: `vnext/artifacts/c-phase-203-quality-triage/triage-queue.json` — owner approved 2026-06-12 ("proceed")

## Declared Mutation Scope — honored

MODIFY-only (no add, no delete), exactly `sprites_runtime/{crow,goose,raccoon,rat,squirrel}/**/*.png`.
6,204 of 6,870 frames changed; every changed frame backed up with sha256 before/after.
New tool `tools/procedural_cleanup_sweep.py`. No production C# touched.

## What the sweep does

1. **Fringe cleanup** — removes near-invisible halo pixels (alpha < 24). Found 0:
   the runtime tree had no low-alpha fringe.
2. **Dark-rim trim** — the owner-reported "silhouette cleanup / cleaning between
   the legs" defect is a ragged crawl of near-black, weakly-connected boundary
   pixels (fully opaque, attached to the body — invisible to component analysis).
   Trim rule: opaque AND on the silhouette boundary AND luma < threshold AND ≤ N
   of 8 neighbors. Per-species parameters calibrated on before/after strips
   (`inspect/`): squirrel/rat/raccoon 56-luma/4-neighbors/2-passes;
   goose 44/3/1 **with the bottom 28% of the body bbox protected** (goose
   legs/feet are legitimately dark and 1 px thin — the unprotected filter
   amputated them); crow 44/3/1 (intrinsically dark art, gentle pass only).
   323,493 px trimmed.
3. **Speck removal** — disconnected alpha components ≤ 8 px (many created when
   the rim trim severs dark webs): 1,189 components / 1,848 px removed.
4. **Tint conformance + fix (BUG-007)** — per variant, circular-mean hue of
   saturated pixels in REQUIRED-family frames vs the variant's expected tint hue
   (from `COLOR_VARIANTS`), tolerance ±40°, saturation floor 0.15. Measured on
   required families only: the original BUG-007 variant (goose/baby/female/blue)
   passes a whole-variant mean because its blue pilot optional frames outvote
   the cream required frames. 8 nonconforming variants found — goose blue ×6
   (all rows) + raccoon adult blue ×2 (previously unknown) — all repaired with
   the pipeline's own `colorize()`, required families only. Post-apply census:
   0 nonconforming. fox/blue remains for C-203B.
5. **Report-only findings** — edge-touch: 0 frames (C-197's edge-touch flags
   were source-image measurements; runtime frames are already repacked with
   margins). Within-row size outliers: 1 (`goose/baby/female/blue/drop_ball_00`,
   a C-198 synthesized frame that C-199R regenerates anyway).

## Process note (recorded honestly)

The first apply ran with whole-variant tint measurement and missed the pilot
variant; a second apply with the corrected measurement accidentally re-ran the
dark-rim erosion on the already-cleaned tree (the trim is intentionally
non-convergent — each run exposes the next ring of shading). The tree was fully
restored from backup (all 6,194 frames hash-verified to original sha256) and the
corrected tool applied ONCE from pristine state. **The sweep is a run-once tool:
never apply it twice without restoring first.** This warning is in the tool
docstring's lineage and here.

## Stop-Gate Checklist

- [x] Mutation scope honored (5 species' runtime PNGs only; modify-only).
- [x] Backup + sha256 for every changed frame (`backup/`, hashes in `apply.json`).
- [x] Post-proof green: contract audit 0 errors; canvas mismatch/missing/invalid
      all 0; full suite 1717/1717.
- [x] BUG-007 conformance: 0 nonconforming variants among the 5 species post-apply.
- [x] Goose legs/feet preserved (bottom-protect verified on strips).
- [x] No batch outside the owner-approved queue rows.

## Artifacts (local root `vnext/artifacts/c-phase-203a-procedural-cleanup/`)

`apply.json` (per-frame findings + before/after sha256), `dry-run-1/2.json`,
`post-check.json`, `sprite_contract.json` (0 errors), `runtime_canvas.json/.md`,
`backup/` (6,204 original frames), `rollback.ps1` (restore + hash re-verify),
`before-after-sheets/<species>-before-after.png` (36 variants × walk/idle),
`inspect/` (calibration strips, goose-blue-tintfix.png, pilot-blue-tintfix.png).

## Validation

Build PASS (0 errors); full suite 1717/1717; contract audit error_count=0;
canvas mismatch=0 missing=0 invalid=0; `build-vnext -SkipAssetPrep -SkipTests`
PASS; `git diff --check` PASS.

## Next

C-203B (Gemini regeneration: deer, fox, frog, pigeon, snake + C-202 locomotion
boards, single owner round-trip session, anti-blur policy binding) → C-199R
candidate regeneration + second owner review → C-200.
