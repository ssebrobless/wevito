# C-PHASE 199R3: Review Rejection Cleanup Plan Before C-200

Date: 2026-06-27
Branch context: `claude-implementation/c-phase-203c-outline-reimport`
Related PR: #314
Predecessor: `docs/C_PHASE199R2_OUTLINE_REIMPORT_REVIEW_2026-06-13.md`

## Goal

Record the third owner review gate rejection for C-199R2 and define the cleanup
phase required before any C-200 guarded matrix apply. No species is approved for
C-200 from the C-199R2 review sheets.

## Review Verdict

| Result | Value |
|---|---|
| Species approved for C-200 | 0 / 10 |
| C-200 matrix apply | BLOCKED |
| Required next phase | C-199R3 cleanup/review |
| New owner review gate required | YES |

## Owner Findings To Fix

| Species | Rejected rows / variants | Required cleanup |
|---|---|---|
| deer | Teen male, teen female | Clean between the legs. |
| crow | Adult female, adult male | Clean between the legs. |
| fox | Adult male | Fix head cut-off across affected sprites. |
| frog | Teen female | Restore eyes and remove faint PNG checker/opaque body pattern. |
| frog | Teen male | Clean between the legs. |
| goose | All goose rows | Clean between the legs across the species. |
| goose | Male goose rows | Fix cut-off feet. |
| goose | Blue baby female row | Restore the redacted/blank row. |
| pigeon | Teen male, teen female | Clean between and around the legs. |
| pigeon | Adult female | Restore eyes and remove checker-pattern body artifact. |
| pigeon | Adult male | Remove PNG checker pattern from wing. |
| raccoon | Teen male, teen female | Remove stray pixels around the body. |
| raccoon | Adult female | Remove faint PNG checker pattern over body. |
| rat | Teen female, teen male | Clean around and between tails and feet. |
| squirrel | Teen female, teen male | Remove shadows around feet. |
| snake | Whole review surface | Produce a clearer snake movement review plan: sizes are inconsistent, base/uncoiled movement is unclear, and the current sheet does not show whether the snake is ready. |

## Required Cleanup Phase Shape

C-199R3 should be a cleanup/review phase, not a C-200 apply phase.

1. Start from the C-199R2 branch/artifacts.
2. Preserve the C-199R2 backups and provenance.
3. Fix only the rejected review-sheet defects above.
4. Regenerate candidate frames and review sheets for the affected species/rows.
5. Re-run sprite contract and runtime canvas audits.
6. Re-run tint/pattern checks for the species with PNG checker artifacts.
7. Add a snake-specific review sheet or movement strip that shows scale and uncoiled motion clearly enough for approval.
8. Produce a new C-199R3 report with before/after evidence.
9. Stop for owner review again. Do not proceed to C-200 automatically.

## C-200 Gate

C-200 remains blocked until a future owner review explicitly approves one or more
species after C-199R3 cleanup. C-200's scope must be limited to species approved
in that future review gate. Species not approved remain excluded.

## Safety Boundaries

- No species is approved from C-199R2.
- No C-200 matrix apply may run from the rejected C-199R2 review sheets.
- No asset prep.
- No silent broad sprite mutation.
- Any cleanup mutation must have declared scope, dry-run, backup, sha256
  evidence, apply, post-proof, and rollback path.
- Ball remains a runtime overlay; do not bake ball pixels into sprite frames.
- Runtime sprite contract remains 28x24 RGBA.

## Next

C-199R3 cleanup/review prompt, then owner review gate, then C-200 only for
approved species.
