# C-PHASE 199R3: Review-Pipeline Cleanup After Third Rejection

Date: 2026-06-27
Branch: `claude-implementation/c-phase-199r3-review-rejection-cleanup`
Parent: PR #314 / C-199R2 third review gate rejection

## Goal

Start the cleanup phase required before any C-200 matrix apply by fixing the
review-pipeline defect that hid already-authored optional ball families, then
rebuild local C-199R3 review artifacts for owner/art cleanup triage.

No species is approved by this phase.

## Review State

```
Third review gate
  |
  +-- Owner verdict: REJECTED
  |
  +-- Approved species: 0 / 10
  |
  +-- C-200 matrix apply: BLOCKED
  |
  +-- C-199R3 actions
      |
      +-- Review-pipeline fix: COMPLETE
      |   +-- Existing runtime optional families can now be included in
      |       candidate review trees.
      |   +-- Blue baby female goose no longer renders as a missing row in
      |       regenerated C-199R3 review artifacts.
      |
      +-- Art cleanup/regeneration: STILL REQUIRED
          +-- leg/feet/tail cleanup
          +-- fox adult male head/crop repair
          +-- frog/pigeon/raccoon checker-pattern body repair
          +-- snake movement/base-pose redesign and review visualization
```

## Implemented

| Area | Location | Result |
| --- | --- | --- |
| Existing optional-family visibility | `tools/generate_optional_ball_families.py` | Added opt-in `--include-existing-runtime-families`. Default behavior still skips existing runtime optional families; review rebuilds can now copy them into the artifact candidate tree. |
| Review-sheet labeling and snake evidence | `tools/build_ball_review_sheets.py` | Added `--phase-label`, `--species`, and `--all-frames` options. Default compact layout is preserved; full-frame sheets can now be generated for species like snake. |
| Local C-199R3 candidates | `vnext/artifacts/c-phase-199r3-cleanup-review/ball-candidates/` | Generated 10,770 synthesized candidate frames plus 30 copied existing runtime-family frames. Missing candidates: 0. Blank candidates: 0. |
| Local C-199R3 compact review sheets | `vnext/artifacts/c-phase-199r3-cleanup-review/review-sheets/` | Rebuilt 10 species sheets labeled C-199R3. |
| Local snake full-frame review sheet | `vnext/artifacts/c-phase-199r3-cleanup-review/review-sheets-full/snake.png` | Built a full-frame snake review sheet to expose all optional-family frames, not just compact samples. |

## Owner Findings Still Open

| Species | Open cleanup required before approval |
| --- | --- |
| deer | Teen male and teen female need cleanup between legs. |
| crow | Adult female and adult male need cleanup between legs. |
| fox | Adult male head/crop is cut off in many review-sheet sprites. |
| frog | Teen female has missing eyes and PNG-checker-like body artifact; teen male needs cleanup between legs. |
| goose | All rows need cleanup between legs; male goose has foot cutoff; blue baby female row visibility is fixed in C-199R3 artifacts but still requires owner review after cleanup. |
| pigeon | Teen male and teen female need cleanup between and around legs; adult female has missing eyes plus checker-pattern body artifact; adult male has checker-pattern wing artifact. |
| raccoon | Teen male and teen female have pixels around body; adult female has slight checker-pattern body artifact. |
| rat | Teen male and teen female need cleanup around and between tails and feet. |
| squirrel | Teen male and teen female have shadows around feet. |
| snake | Size and pose review remains confusing; needs movement/base-pose redesign or a dedicated motion-review pass before readiness can be judged. |

## Validation

| Command | Result |
| --- | --- |
| `python tools/generate_optional_ball_families.py --runtime-root sprites_runtime --out-root vnext/artifacts/c-phase-199r3-cleanup-review/ball-candidates --include-existing-runtime-families --manifest-name candidate-manifest.json` | PASS: 10,770 generated; 30 copied existing runtime-family frames; 0 skipped; 0 errors. |
| `python tools/build_ball_review_sheets.py --candidates-root vnext/artifacts/c-phase-199r3-cleanup-review/ball-candidates --out-dir vnext/artifacts/c-phase-199r3-cleanup-review/review-sheets --phase-label C-199R3` | PASS: 10 compact species sheets rebuilt. |
| `python tools/build_ball_review_sheets.py --candidates-root vnext/artifacts/c-phase-199r3-cleanup-review/ball-candidates --out-dir vnext/artifacts/c-phase-199r3-cleanup-review/review-sheets-full --phase-label C-199R3-full --species snake --all-frames` | PASS: full-frame snake sheet rebuilt. |
| Candidate completeness scan | PASS: missing candidates 0; blank candidates 0; `goose/baby/female/blue` has 30/30 candidate frames. |
| `python tools/procedural_cleanup_sweep.py --species crow goose raccoon rat squirrel --output vnext/artifacts/c-phase-199r3-cleanup-review/procedural-cleanup-dryrun.json` | PASS dry-run only: identified cleanup pixels, made no sprite changes. |

## Safety Boundaries

- No species approved.
- No C-200 matrix apply.
- No runtime sprite PNG edited in this pass.
- No authored-verified sprite PNG edited in this pass.
- No hosted model run.
- No apply runner, rollback runner, scheduler, judge, replay, snapshot, scoring, or eval surface invoked.
- Local review artifacts are for owner/art cleanup triage only.

## Next

Proceed with a focused art-cleanup/regeneration phase for the open owner
findings above. After regenerated review sheets are owner-approved, and only
then, resume C-200 matrix apply planning.
