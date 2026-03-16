# Generic Sprite Smoke Gemini Prompt Rules

Updated: 2026-03-15

Use these rules in every sprite-edit prompt.

## Always Include

- preserve the exact uploaded character identity
- edit only the named frames
- leave every other frame unchanged
- keep the board layout and labels intact
- pixel art only
- no blur
- no anti-aliasing
- no checkerboard residue
- no matte halos
- no clipped silhouettes
- return image only

## Workflow Rules

- reuse the already-open logged-in Gemini tab/window
- generate one focused family at a time
- split dense families into smaller packs when sharpness drops
- download with `Download full size image`
- import immediately after generation

## Domain-Specific Notes Template

Adapt these notes to the kind of sprite work you are doing:

- characters / creatures:
  - preserve silhouette, anatomy, and readable motion arcs
  - keep contact points grounded
- props / devices:
  - preserve construction details and hinge/segment logic
  - keep open/close or active-state transitions mechanically consistent
- effects / VFX:
  - preserve timing shape and readable energy flow
  - avoid muddy edges or indistinct frame silhouettes
- UI mascots / icons:
  - preserve recognizability at target display size
  - avoid detail that collapses under downscale
