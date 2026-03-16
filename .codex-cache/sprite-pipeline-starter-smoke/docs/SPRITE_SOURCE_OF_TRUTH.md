# Sprite Smoke Test Sprite Source Of Truth

Updated: 2026-03-15

```text
╔════════════════════ Source Contract ════════════════════╗
║ canonical source art  │ incoming_sprites                ║
║ runtime output        │ sprites_runtime               ║
║ verified authored     │ sprites_authored_verified              ║
║ project root          │ C:\Users\fishe\Documents\projects\wevito\.codex-cache\sprite-pipeline-starter-smoke                ║
╚═════════════════════════════════════════════════════════╝
```

## Source Of Truth

- Canonical source art lives in `incoming_sprites`
- Generated runtime output lives in `sprites_runtime`
- Verified authored/imported overrides live in `sprites_authored_verified`

## Required Decisions

- What is the canonical input board format?
- Which files are true source and which are previews/exports?
- Are genders/ages/colors separate source boards or derived outputs?
- Which animations exist in the runtime contract?
- Which frame sizes are fixed and which can vary by family?

## Species Roster

- `fox`
- `crow`
- `snake`

## Runtime Contract

Fill this in before generation starts:

- frame families:
  - `locomotion_idle`
  - `locomotion_walk_a`
  - `locomotion_walk_b`
  - `care`
  - `expression`
- output file naming:
  - `<animation>_<nn>.png`
- color policy:
  - base motion first
  - color propagation after approval

## Non-Negotiables

- Preserve the canonical look.
- AI edits should refine, not redesign.
- Direct-download assets only if using Gemini.
- Validate every imported family before moving on.
