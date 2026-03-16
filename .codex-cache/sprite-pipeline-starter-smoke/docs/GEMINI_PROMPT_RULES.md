# Sprite Smoke Test Gemini Prompt Rules

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
- prefer `locomotion_idle`, `locomotion_walk_a`, `locomotion_walk_b` for blur-prone species
- download with `Download full size image`
- import immediately after generation

## Species-Specific Notes Template

- quadrupeds:
  - clear stepping front/rear legs
  - grounded contact
  - stable torso and head rhythm
- birds:
  - preserve filled chest/body mass
  - tiny balancing wing motion only when needed
- reptiles:
  - preserve full readable body in each frame
  - avoid row-spanning continuous strips
- small animals:
  - avoid over-compressing detail
  - use smaller generation packs if sharpness drops
