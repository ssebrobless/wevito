# Snake Editable Board Source Pass

## Goal

Continue the snake visual cleanup without fabricating new art. The previous
care/action pass repaired clean dedicated `eat` and `sleep` rows, but the live
preview still showed mixed snake designs and several action rows that looked
muddy or static. This pass uses the existing editable Gemini board packets as
the source of truth where the row is complete enough to apply.

## Source Inputs

- `incoming_sprites/gemini_handoff/snake/*/*/5-save-edited-board-here/*gemini-result*.png`
- Script: `tools/repair_snake_remaining_action_rows.py`
- Artifact root: `vnext/artifacts/snake-editable-board-source-20260514/`

The script locates the editable board section inside each saved Gemini result,
extracts already-generated frame cells, removes checkerboard/grid residue, keeps
the primary snake component, colorizes the source art into all six egg colors,
and fits each frame to the runtime canvas contract.

## Safety Rules

- Whole animation rows only; no partial-frame row application.
- Existing generated source only; no procedural redraw or newly generated art.
- Block any row where one or more frames extract as fragments.
- Backup before apply, rollback drill, then re-apply if the drill passes.
- Preserve runtime canvas validation with no missing or invalid PNG rows.

## Applied Rows

- `baby/female`: `idle`, `walk`, `eat`, `sleep`, `happy`, `bathe`
- `baby/male`: `idle`, `walk`, `eat`, `sleep`, `happy`, `sad`, `sick`, `bathe`
- `teen/female`: `idle`, `walk`, `eat`, `sleep`, `sad`, `sick`, `bathe`
- `teen/male`: `idle`, `walk`, `eat`, `sleep`, `happy`, `sad`, `sick`, `bathe`
- `adult/female`: `sleep`, `sad`
- `adult/male`: `idle`, `eat`, `sleep`, `happy`, `sad`, `bathe`

## Blocked Rows

- `baby/female/sad`: `sad_01` source extracts as a tail fragment.
- `baby/female/sick`: `sick_02` source extracts as a partial body fragment.
- `teen/female/happy`: `happy_00` source extracts as a partial body fragment.
- `adult/female/idle`: row contains grid fragments and one boxed partial source frame.
- `adult/female/walk`: row contains partial body fragments.
- `adult/female/eat`: `eat_02` source extracts as a partial body fragment.
- `adult/female/happy`: `happy_03` source extracts as a partial body fragment.
- `adult/female/sick`: `sick_00` source extracts as a partial body fragment.
- `adult/female/bathe`: `bathe_01` source extracts as a partial body fragment.
- `adult/male/walk`: row contains partial body fragments.
- `adult/male/sick`: `sick_01` and `sick_02` source extracts are line/body fragments.

These blocked rows still need a dedicated visual source packet before they
should be applied. The runtime remains valid, but those rows are not yet at the
final quality bar.

## Evidence

- Candidate sheet: `vnext/artifacts/snake-editable-board-source-20260514/snake-remaining-action-candidate-contact-sheet.png`
- Runtime preview: `vnext/artifacts/snake-editable-board-source-20260514/runtime-previews/snake-preview.png`
- Apply report: `vnext/artifacts/snake-editable-board-source-20260514/snake-remaining-action-apply-report.md`
- Runtime canvas report: `vnext/artifacts/snake-editable-board-source-20260514/runtime-canvas.md`
- Sprite contract report: `vnext/artifacts/snake-editable-board-source-20260514/sprite-contract.json`

## Validation

- `python .\tools\repair_snake_remaining_action_rows.py --output-root .\vnext\artifacts\snake-editable-board-source-20260514 --apply --rollback-drill`
- `python .\tools\render_runtime_sprite_previews.py --output-root .\vnext\artifacts\snake-editable-board-source-20260514\runtime-previews`
- `python .\tools\report_runtime_canvas_mismatches.py --output .\vnext\artifacts\snake-editable-board-source-20260514\runtime-canvas.json --markdown .\vnext\artifacts\snake-editable-board-source-20260514\runtime-canvas.md --fail-on-mismatch`
- `python .\tools\audit_sprite_contract.py --output .\vnext\artifacts\snake-editable-board-source-20260514\sprite-contract.json`

Runtime canvas result: `mismatch_count=0`, `missing_count=0`, `invalid_count=0`.
Sprite contract result: `error_count=0`.

## Next Visual Work

The next snake step should be a dedicated source packet for the blocked rows,
especially `adult/female` and `adult/male/walk`. Do not scale those rows from
the current editable board fragments.
