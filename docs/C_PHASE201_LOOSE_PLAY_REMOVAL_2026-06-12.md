# C-PHASE 201: Loose `play_*` Removal + Groom Registration + Stray-Family Pin

Date: 2026-06-12
Branch: `claude-implementation/c-phase-201-loose-play-removal-stray-pin`
Plan: `docs/CLAUDE_C_PHASE196_PLUS_PET_SIM_COMPLETION_PLAN_2026-06-12.md` §6.6
Prompt: `docs/CLAUDE_C_PHASE196_PLUS_CODEX_MEDIUM_PROMPTS_2026-06-12.md` §6 (as amended by the C-197 groom finding)
Owner decision: D1 = remove (confirmed at track go-ahead, 2026-06-12)

## Declared Mutation Scope — honored

DELETE-only: exactly the 504 files classified `uncontracted_dead` in the C-197
`baseline.json` loose-file inventory (`play_00..03.png` across 126 variants: frog 36,
goose 36, snake 36, rat 12, deer 6). Plus the contract registration of `groom` in
`tools/audit_sprite_contract.py` and the new pin test. No other sprite file touched.

## What Was Done

1. **Groom registered (step 0).** `"groom": 4` added to `EXPECTED_ANIMATIONS` —
   groom is rendered by `PetAnimationState.Groom` through the shell's primary
   animation-id path; it was present 360/360 and is now a required family, counted
   by the contract audit.
2. **Deletion list extracted from the C-197 baseline** (never a re-glob): 504
   entries; cross-checks passed (all exist, all `play_*`, count matches
   `uncontracted_dead_count`).
3. **Backup**: `play-files-backup.zip` (relative paths) + `backup-manifest.json`
   (sha256 per file).
4. **Delete + census proof**: runtime PNG census 14,214 → 13,710 (delta exactly 504).
5. **Stray-family pin, both layers**:
   - `tools/audit_sprite_contract.py`: strict per-variant check (always on) — any
     frame whose family is in neither `EXPECTED_ANIMATIONS` nor
     `OPTIONAL_EXPANDED_ANIMATIONS`, or that violates `<family>_<NN>.png` naming,
     is an error.
   - `vnext/tests/Wevito.VNext.Tests/SpriteRuntimeStrayFamilyTests.cs`: C# twin
     (2 facts) that fails in CI without Python.
6. **Post-proof**: contract audit 0 errors WITH the strict pin active; canvas
   report mismatch/missing/invalid all 0; full suite 1717/1717 (1715 + 2 new facts).
7. **`rollback.ps1`**: restores the zip, re-verifies all 504 hashes (and documents
   that the pin will then fail by design until re-removal).

## Why these files were dead

`PetAnimationState` has no `Play` member; the Play action maps to
`animationState=happy` + `optionalAnimationFamily=play_ball` (`actions.json`), so no
code path could ever select a `play` family. See the C-197 report for the full
analysis.

## Stop-Gate Checklist

- [x] Deletion list identical to the C-197 inventory (504/504; zero divergence).
- [x] No non-`play_*` file deleted; no file modified.
- [x] Post-proof green (contract 0 errors with pin active; canvas clean; suite green).
- [x] Strict check found no OTHER stray family after deletion (tree is exactly
      contracted: 9 required + 7 optional families).

## Artifacts (local root `vnext/artifacts/c-phase-201-loose-play-removal/`)

`backup-manifest.json`, `play-files-backup.zip`, `deletion-list.json`,
`sprite_contract.json` (0 errors), `runtime_canvas.json/.md`, `rollback.ps1`.

## Validation

Build PASS (0/0); filtered `Sprite|SpriteRuntimeStrayFamily` 79/79; full suite
1717/1717; `build-vnext -SkipAssetPrep -SkipTests` PASS; `git diff --check` PASS.

## Note for C-PHASE 200

The expected full-suite count is now **1717**. C-200's coverage-test update lands on
top of this; the C-199 candidate manifests are unaffected (candidates were generated
before this deletion from source rows that still exist — happy/idle/eat/walk).
