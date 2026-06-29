# C-PHASE 199R3: Procedural Owner-Cleanup Slice

Date: 2026-06-27
Branch: `claude-implementation/c-phase-199r3-procedural-owner-cleanup`
Stacked on: `claude-implementation/c-phase-199r3-review-rejection-cleanup`

## Goal

Apply the existing conservative procedural cleanup pass to the owner-rejected
species that already have a calibrated cleanup lane: crow, goose, raccoon, rat,
and squirrel.

This is a partial cleanup slice. It does not approve any species and does not
start C-200.

## Cleanup Shape

```
Owner third review rejection
  |
  +-- Review-pipeline defect
  |   +-- fixed in C-199R3 review-pipeline layer
  |
  +-- Procedural-lane pixel cleanup
  |   +-- crow
  |   +-- goose
  |   +-- raccoon
  |   +-- rat
  |   +-- squirrel
  |
  +-- Art/regeneration lane still open
      +-- deer
      +-- fox
      +-- frog
      +-- pigeon
      +-- snake
      +-- remaining owner review on all species after new sheets
```

## Implemented

| Area | Result |
| --- | --- |
| Runtime procedural cleanup | Ran `tools/procedural_cleanup_sweep.py` once against `sprites_runtime/{crow,goose,raccoon,rat,squirrel}`. Changed 5,216 runtime frames. |
| Authored mirror cleanup | Ran the same one-pass cleanup against `sprites_authored_verified/{crow,goose,raccoon,rat,squirrel}`. Changed 1,159 authored-verified frames. |
| Backups | Wrote local backups under `vnext/artifacts/c-phase-199r3-procedural-owner-cleanup/backup-runtime/` and `backup-authored-verified/`. |
| Review candidates | Regenerated ball candidates from cleaned runtime with existing runtime optional families included. |
| Review sheets | Rebuilt compact review sheets for all 10 species and a full-frame snake sheet. |

## Validation So Far

| Command | Result |
| --- | --- |
| `python tools/procedural_cleanup_sweep.py --species crow goose raccoon rat squirrel --apply --backup-root vnext/artifacts/c-phase-199r3-procedural-owner-cleanup/backup-runtime --output vnext/artifacts/c-phase-199r3-procedural-owner-cleanup/runtime-apply.json` | PASS: 5,216 runtime frames changed; 0 edge-touch frames; 0 tint-nonconform variants. |
| `python tools/procedural_cleanup_sweep.py --species crow goose raccoon rat squirrel --runtime-root sprites_authored_verified --apply --backup-root vnext/artifacts/c-phase-199r3-procedural-owner-cleanup/backup-authored-verified --output vnext/artifacts/c-phase-199r3-procedural-owner-cleanup/authored-verified-apply.json` | PASS: 1,159 authored-verified frames changed; 0 edge-touch frames; 0 tint-nonconform variants. |
| `python tools/generate_optional_ball_families.py --runtime-root sprites_runtime --out-root vnext/artifacts/c-phase-199r3-procedural-owner-cleanup/ball-candidates --include-existing-runtime-families --manifest-name candidate-manifest.json` | PASS: 10,770 generated; 30 copied existing runtime-family frames; 0 skipped; 0 errors. |
| Candidate completeness scan | PASS: 0 missing; 0 blank; `goose/baby/female/blue` remains 30/30 candidate frames. |
| Compact review sheets | PASS: 10 species sheets rebuilt under local artifacts. |
| Snake full-frame sheet | PASS: supplemental full-frame snake sheet rebuilt under local artifacts. |
| `python tools/audit_sprite_contract.py --runtime-root sprites_runtime --output vnext/artifacts/c-phase-199r3-procedural-owner-cleanup/sprite-contract.json` | PASS: `error_count=0`; runtime variant dirs 360/360. |
| `python tools/report_runtime_canvas_mismatches.py --runtime-root sprites_runtime --output vnext/artifacts/c-phase-199r3-procedural-owner-cleanup/runtime-canvas.json --markdown vnext/artifacts/c-phase-199r3-procedural-owner-cleanup/runtime-canvas.md` | PASS: `mismatch_count=0`; `missing_count=0`; `invalid_count=0`. Existing canonical mismatch count reported separately as 2,616. |
| `python -m py_compile tools/generate_optional_ball_families.py tools/build_ball_review_sheets.py tools/procedural_cleanup_sweep.py` | PASS. |
| `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests` | PASS. |
| `git diff --check` | PASS. |

## Still Open

| Species | Status after this slice |
| --- | --- |
| crow | Procedural cleanup applied; still requires owner review on regenerated sheet. |
| goose | Procedural cleanup applied; blue baby female row visibility fixed by review-pipeline layer; still requires owner review on regenerated sheet. |
| raccoon | Procedural cleanup applied; adult female checker-pattern concern may still require art cleanup if visible after owner review. |
| rat | Procedural cleanup applied; still requires owner review on regenerated sheet. |
| squirrel | Procedural cleanup applied; still requires owner review on regenerated sheet. |
| deer | Not repaired by this procedural lane; teen leg cleanup remains open. |
| fox | Not repaired by this procedural lane; adult male head/crop issue remains open. |
| frog | Not repaired by this procedural lane; teen female eyes/checker body and teen male leg cleanup remain open. |
| pigeon | Not repaired by this procedural lane; leg cleanup and checker-pattern body/wing concerns remain open. |
| snake | REJECTED in owner follow-up review on 2026-06-29. Most variants remain coiled only; the only attempted uncoiled/slithering poses are adult male variants, and those do not fit the base snake models. Needs a snake-specific art/regeneration pass before readiness can be judged. |

## Owner Follow-Up Review: Snake

2026-06-29 owner review of the regenerated sheets confirmed that snake remains
blocked. The issue is not only the review-sheet format: the underlying sprite
set does not yet provide coherent movement-ready snake variants.

```
Snake review result
  |
  +-- Most variants: coiled-only
  |
  +-- Adult male variants: attempted uncoiled/slithering poses
  |   |
  |   +-- rejected because they do not fit the base snake models
  |
  +-- Approval status: rejected
  |
  +-- Required next work: snake-specific art/regeneration pass
```

## Owner Follow-Up Review: Refreshed Sheets Still Rejected

2026-06-29 owner review of the regenerated C-199R3 procedural review sheets
confirmed that the same visible issues are still noticeable. The procedural
cleanup pass improved deterministic pixel hygiene, but it is not sufficient for
owner approval and should not be treated as readiness evidence.

```
C-199R3 refreshed sheets
  |
  +-- Review-pipeline completeness: fixed
  |
  +-- Procedural pixel cleanup: applied
  |
  +-- Owner visual verdict: still rejected
  |
  +-- Species approved: 0 / 10
  |
  +-- C-200 matrix apply: still blocked
  |
  +-- Required next work
      |
      +-- art cleanup/regeneration, not another sheet-only rebuild
```

## Safety Boundaries

- No species approved.
- No C-200 matrix apply.
- No hosted model run.
- No new sprite families added.
- No C# production code touched.
- Existing cleanup tool was applied once only, with backups.
- Runtime and authored-verified mirrors were cleaned together to avoid source/runtime drift.

## Next

Run the sprite contract/canvas checks and owner-review the regenerated sheets.
The remaining art/regeneration lane must address deer, fox, frog, pigeon, snake,
and any procedural-lane species still rejected by the owner after the refreshed
sheets.
