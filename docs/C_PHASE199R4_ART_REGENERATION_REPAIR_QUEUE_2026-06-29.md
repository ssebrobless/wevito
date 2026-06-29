# C-PHASE 199R4: Art-Regeneration Repair Queue

Date: 2026-06-29
Branch: `claude-implementation/c-phase-199r4-art-regeneration-repair-queue`
Stacked on: `claude-implementation/c-phase-199r3-procedural-owner-cleanup`

## Goal

Convert the repeated owner review rejections into a concrete art/source repair
queue before any C-200 matrix apply. C-199R3 proved that review-sheet
completeness and conservative procedural cleanup are not sufficient: the
remaining defects are owner-visible art/source defects.

No species is approved by this phase.

## Current Gate Shape

```
PR #314 / C-199R2
  |
  +-- Third owner review gate: rejected
  |
  +-- PR #315 / C-199R3 review-pipeline cleanup
  |   |
  |   +-- fixed hidden/missing review rows
  |   +-- did not approve species
  |
  +-- PR #316 / C-199R3 procedural cleanup
      |
      +-- applied conservative cleanup to crow/goose/raccoon/rat/squirrel
      +-- refreshed sheets still rejected by owner
      |
      +-- Approved species: 0 / 10
      +-- C-200 matrix apply: BLOCKED
      |
      +-- Next required phase: C-199R4 art/source repair
```

## Repair Queue

| Species | Owner-visible defect | Required remediation | Approval status |
| --- | --- | --- | --- |
| deer | Teen male and teen female still need cleanup between legs. | Targeted source-frame cleanup for teen male/female leg gaps, then regenerate runtime/authored mirrors and review sheets. | Rejected |
| crow | Adult female and adult male still need cleanup between legs. | Targeted source-frame cleanup for adult male/female leg gaps. Prior procedural cleanup was insufficient for approval. | Rejected |
| fox | Adult male head is cut off in many review-sheet sprites. | Full adult male source/canvas regeneration or crop repair; procedural cleanup is not applicable. | Rejected |
| frog | Teen female has missing eyes and PNG-checker-like body artifact; teen male still needs cleanup between legs. | Regenerate or manually repair teen female source body/eyes; targeted teen male leg cleanup. | Rejected |
| goose | All rows still need cleanup between legs; male goose has foot cutoff; blue baby female visibility is fixed but not approved. | Source-frame cleanup/regeneration across goose variants, with explicit foot/leg preservation checks. | Rejected |
| pigeon | Teen male/female need cleanup between and around legs; adult female has missing eyes and checker body artifact; adult male has checker wing artifact. | Regenerate or manually repair pigeon source rows, especially adult female/male body/wing artifacts and teen legs. | Rejected |
| raccoon | Teen male/female still have pixels around body; adult female has checker-pattern body artifact. | Additional targeted source cleanup and/or adult female regeneration; procedural cleanup alone is insufficient. | Rejected |
| rat | Teen male/female need cleanup around and between tails and feet. | Targeted source-frame cleanup for tails/feet before candidate regeneration. | Rejected |
| squirrel | Teen male/female still have shadows around feet. | Targeted source-frame cleanup for foot shadows before candidate regeneration. | Rejected |
| snake | Most variants remain coiled only; only adult male has attempted uncoiled/slithering poses, and those do not fit the base snake models. | Snake-specific art redesign/regeneration: coherent base pose plus movement/slither poses for all variants, with a dedicated full-frame motion review sheet. | Rejected |

## Remediation Lanes

```
C-199R4 work
  |
  +-- Lane A: targeted source cleanup
  |   |
  |   +-- deer legs
  |   +-- crow legs
  |   +-- goose legs/feet
  |   +-- rat tails/feet
  |   +-- squirrel foot shadows
  |   +-- raccoon outline/body pixels where source is salvageable
  |
  +-- Lane B: source regeneration/manual repaint
  |   |
  |   +-- fox adult male head/canvas
  |   +-- frog teen female eyes/checker artifact
  |   +-- pigeon adult female eyes/checker body
  |   +-- pigeon adult male wing checker artifact
  |   +-- snake base + movement pose redesign
  |
  +-- Lane C: proof and review
      |
      +-- reimport runtime + authored-verified mirrors
      +-- regenerate optional ball candidates
      +-- rebuild compact review sheets
      +-- rebuild snake full-frame motion sheet
      +-- owner review before C-200
```

## C-199R4 Acceptance Criteria

- Source/art defects are repaired before candidate review-sheet regeneration.
- Runtime and `sprites_authored_verified` mirrors remain in sync.
- Review artifacts include all ten compact species sheets.
- Snake receives a dedicated full-frame motion sheet that demonstrates coherent
  non-coiled movement poses across variants.
- Owner explicitly approves species before any species is carried into C-200.
- C-200 remains blocked until at least one species is owner-approved from the
  refreshed C-199R4 sheets.

## Safety Boundaries

- No C-200 matrix apply in this phase.
- No species approved by this document.
- No hosted model run recorded by this document.
- No sprite PNG mutation in this queue-only phase.
- No apply runner, rollback runner, scheduler, judge, replay, snapshot, scoring,
  or eval surface invoked.
- Future art/regeneration work must be committed in a separate implementation
  phase with before/after review evidence.

## Next

Start C-199R4 implementation with the highest-leverage repair lane:

1. Snake-specific art regeneration, because the current pose set is structurally
   unsuitable for movement review.
2. Fox/frog/pigeon regeneration/manual repaint, because the defects are source
   art defects rather than transparent-pixel cleanup defects.
3. Targeted leg/tail/foot cleanup for deer, crow, goose, rat, squirrel, and
   remaining raccoon issues.

After C-199R4 artifacts are rebuilt, stop for owner review. Do not proceed to
C-200 until owner approval is recorded.
