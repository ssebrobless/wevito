# Wevito Sprite Source Of Truth

Updated: 2026-03-11

```text
╔══════════════════════ Canonical Sprite Pipeline ══════════════════════╗
║ Source art                                                           ║
║   incoming_sprites/                                                  ║
║   ├─ 10 animal species                                               ║
║   ├─ 3 ages per species: baby / teen / adult                         ║
║   ├─ 1 source board per species+age                                  ║
║   ├─ male pose on the left                                           ║
║   └─ female pose on the right                                        ║
║                                                                       ║
║ Local tooling                                                         ║
║   ├─ extracts male/female canonical poses                            ║
║   ├─ preserves transparent backgrounds                               ║
║   ├─ generates runtime-ready animation frames                        ║
║   ├─ expands each pet into 6 color variants                          ║
║   └─ validates frame counts / canvas / alpha                         ║
║                                                                       ║
║ Runtime output                                                        ║
║   sprites_runtime/<species>/<age>/<gender>/<color>/<anim>_<nn>.png   ║
║                                                                       ║
║ Long-run target                                                       ║
║   authored animation frames should replace synthesized motion         ║
║   while preserving the canonical look from incoming_sprites           ║
╚═══════════════════════════════════════════════════════════════════════╝
```

## Core Rules

- `incoming_sprites/` is the canonical source of pet appearance.
- The 30 animal age boards define the intended look of every pet species, age stage, and sex.
- Each animal age board is a dual-pose source sheet:
  - male on the left
  - female on the right
- Local tooling is responsible for generating transparent runtime frames from those source sheets.
- The runtime pet contract remains:
  - canvas: `28x24`
  - animations:
    - `idle_00..03`
    - `walk_00..05`
    - `eat_00..03`
    - `happy_00..03`
    - `sad_00..01`
    - `sleep_00..01`
    - `sick_00..03`
    - `bathe_00..03`
- Every species / age / gender combination expands into 6 runtime color variants:
  - `red`
  - `orange`
  - `yellow`
  - `blue`
  - `indigo`
  - `violet`

## Animal Coverage

```text
species
├─ rat
├─ crow
├─ fox
├─ snake
├─ deer
├─ frog
├─ pigeon
├─ raccoon
├─ squirrel
└─ goose

age stages
├─ baby
├─ teen
└─ adult

sexes
├─ male
└─ female
```

## Supporting Source Sheets

`incoming_sprites/` also contains the non-pet source boards that should feed the final polished runtime:

- environments
- food sets
- water and feeding containers
- medicine and care items
- toys and enrichment
- utility and shelter props
- status effects
- large action buttons
- small UI icons
- sun / moon
- egg hatch lifecycle

These are source art inputs, not throwaway references. They should be promoted into the runtime asset pipeline as the shell presentation is polished.

## Distinction Between Source Boards

```text
╔══════════════════════ Two Board Families ══════════════════════╗
║ 1. incoming animal age sheets                                 ║
║    purpose: canonical pet appearance                          ║
║    shape: male-left / female-right                            ║
║                                                                ║
║ 2. rigid animation boards from prompt pack                    ║
║    purpose: future fully-authored animation sources           ║
║    shape: one 5x6 board for one variant                       ║
╚════════════════════════════════════════════════════════════════╝
```

The current runtime generator may synthesize motion from the age sheets as an interim step, but the long-run product goal is better than synthesized transforms:

- preserve the canonical look from `incoming_sprites`
- replace generic synthesized motion with authored or guided frame animation
- keep color expansion as a tooling responsibility

## Long-Run Work Still Required

- Audit every incoming animal board visually against the expected male-left / female-right contract.
- Score every species / age / sex for:
  - silhouette clarity
  - transparency cleanliness
  - crop quality
  - animation-readiness
- Replace synthesized motion species-by-species with authored or guided runtime animation sets.
- Promote the supporting prop/UI/environment source boards into the final shell art pipeline.
- Keep the visual harness as the final review surface for:
  - contact sheets
  - focused / passive / pinned runtime captures
  - transition/flicker validation

## Enforcement

- Use `tools/incoming_animal_pose_manifest.json` as the declared inventory of the 30 canonical pet source boards.
- Use `tools/audit_sprite_contract.py` to validate that:
  - all expected source boards exist
  - all expected runtime species / ages / genders / colors / animations exist
  - the runtime tree still matches the contract above
