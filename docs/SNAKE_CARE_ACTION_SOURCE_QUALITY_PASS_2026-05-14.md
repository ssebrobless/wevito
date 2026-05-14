# Snake Care/Action Source Quality Pass

## Goal

Use the dedicated snake Gemini care boards to repair only the care/action rows that are source-grounded and visually safer than the current runtime placeholders.

## Scope

Applied rows:

- `snake/*/*/*/eat_00..03.png`
- `snake/*/*/*/sleep_00..01.png`

Excluded rows:

- `snake/adult/female/*/sleep_00..01.png`
- `snake/*/*/*/happy_*`, `sad_*`, `sick_*`, `bathe_*`

## Source Decision

```
Snake source-quality pass
│
├─ Dedicated care boards
│  ├─ eat_00..03: safe across baby/teen/adult and male/female
│  ├─ sleep_00..01: safe for all except adult/female
│  └─ adult/female/sleep: blocked, source pose is upright/not sleeping
│
├─ Runtime full references
│  └─ rejected for this pass: many chopped/fractured fragments
│
└─ Remaining action families
   ├─ happy/sad/sick/bathe: no clean dedicated snake source rows found
   └─ next step: separate source/art pass before mutation
```

## Implemented

- Added `tools/repair_snake_care_runtime_rows.py`.
- Generated source-grounded candidates from `incoming_sprites/gemini_handoff_motion/snake/*/*/care/3-runtime-reference-blue.png`.
- Applied `204` changed runtime frames.
- Ran backup, rollback drill, and re-apply so the working tree ends with the repaired frames.
- Preserved the unsafe adult/female sleep row untouched rather than applying an incorrect pose.

## Evidence

- Candidate proof: `vnext/artifacts/snake-care-action-source-quality-20260514/snake-care-action-candidate-contact-sheet.png`
- Apply report: `vnext/artifacts/snake-care-action-source-quality-20260514/snake-care-action-apply-report.md`
- Apply JSON: `vnext/artifacts/snake-care-action-source-quality-20260514/snake-care-action-apply-report.json`
- Runtime contact sheet after apply: `vnext/artifacts/snake-care-action-source-quality-20260514/runtime-contact-sheets-after/snake.png`
- Runtime preview after apply: `vnext/artifacts/snake-care-action-source-quality-20260514/runtime-previews/snake-preview.png`

## Validation

- Runtime canvas audit after apply: `mismatch_count=0`, `missing_count=0`, `invalid_count=0`.
- Sprite contract audit after apply: `error_count=0`.
- Rollback drill: passed and re-applied candidates afterward.

## Remaining Snake Work

- `adult/female/sleep_00..01` needs a real sleeping-source replacement.
- `happy`, `sad`, `sick`, and `bathe` still need dedicated clean source rows or a reviewed redraw packet.
- Broad snake scaling beyond `eat/sleep` should wait until those source packets exist.
