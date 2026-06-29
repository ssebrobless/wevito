# C-PHASE 199R4: Snake Art Regeneration Pack

Date: 2026-06-29
Branch: `claude-implementation/c-phase-199r4-snake-art-regeneration-pack`
Stacked on: `claude-implementation/c-phase-199r4-art-regeneration-repair-queue`

## Goal

Convert the owner rejection for snake into a source-art regeneration handoff
before any C-200 matrix apply. This phase does not approve snake, does not
mutate runtime sprite PNGs, and does not import the currently saved snake motion
boards as a fix.

The current saved snake source and motion boards are not safe to apply: most
variants remain coiled-only, and the attempted adult male uncoiled/slithering
poses do not match the base snake model well enough for owner approval.

## Evidence

Local diagnostic artifact:

`vnext/artifacts/c-phase-199r4-snake-art-regeneration-pack/diagnostics/snake-source-motion-diagnostic.png`

The diagnostic sheet compares the current saved source boards and saved motion
boards across the snake variants. It supports the owner review finding:

```
Snake review state
  |
  +-- baby female / baby male
  |   +-- source boards remain coiled/upward
  |   +-- no convincing uncoiled movement family
  |
  +-- teen female / teen male
  |   +-- source boards remain coiled/upward
  |   +-- motion boards do not establish readable slither movement
  |
  +-- adult female
  |   +-- source boards remain coiled/upward
  |   +-- no solid movement proof from the saved handoff boards
  |
  +-- adult male
      +-- attempted uncoiled/slither poses exist
      +-- poses are fragmented and do not fit the base snake model
```

## Owner Rejection Recorded

Snake remains rejected. The review concern is not a review-sheet pipeline issue
and not a simple transparent-pixel cleanup issue. The visual problem is source
pose design: the variants do not yet have a coherent base-to-movement snake
pose language.

## Required Regeneration Output

The next art pass must regenerate snake source art before any reimport. Minimum
expected handoff:

| Variant | Required output | Notes |
| --- | --- | --- |
| baby female | Base pose plus readable movement/slither family. | Must not be coiled-only. |
| baby male | Base pose plus readable movement/slither family. | Must preserve baby scale. |
| teen female | Base pose plus readable movement/slither family. | Must preserve variant identity. |
| teen male | Base pose plus readable movement/slither family. | Must preserve variant identity. |
| adult female | Base pose plus readable movement/slither family. | Must not rely on the coiled pose only. |
| adult male | Regenerated base-compatible movement family. | Current attempted uncoiled poses are rejected. |

## Art Acceptance Criteria

- Each snake variant has a readable base pose and a coherent movement/slither
  family.
- Movement frames must fit the same body mass, scale, color, and silhouette
  language as that variant's base model.
- No variant may be represented only by the default coiled pose.
- Adult male movement must be regenerated or substantially repainted so it no
  longer looks detached from the base model.
- No faint PNG checker pattern, redaction, cutoff, or transparent garbage pixels
  may appear in regenerated frames.
- A dedicated full-frame snake motion review sheet must be produced before any
  snake approval is recorded.

## Do Not Import

Do not import the current saved snake motion boards as the repair. They are
useful as rejection evidence only. Importing them would preserve the same owner
review failure: coiled-only variants and adult male movement that does not fit
the base snake models.

## Safety Boundaries

- No C-200 matrix apply in this phase.
- No snake approval recorded by this document.
- No species approval recorded by this document.
- No runtime sprite PNG mutation in this handoff phase.
- No `sprites_authored_verified` PNG mutation in this handoff phase.
- No hosted model run recorded by this document.
- No apply runner, rollback runner, scheduler, judge, replay, snapshot, scoring,
  or eval surface invoked.

## Next

Run a snake-specific source-art regeneration pass using this handoff as the
acceptance target. After regenerated source boards exist, rebuild snake runtime
and authored-verified mirrors, generate compact and full-frame motion review
sheets, and stop for owner review.

C-200 remains blocked until owner approval is recorded from refreshed review
artifacts.
