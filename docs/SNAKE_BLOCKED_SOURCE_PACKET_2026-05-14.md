# Snake Blocked Source Packet

```
snake cleanup state
│
├─ runtime structure
│  ├─ canvas contract: must remain green
│  └─ current rows: mostly usable after the editable-board pass
│
├─ remaining visual risk
│  ├─ 11 rows still have unsafe source provenance
│  ├─ some alternate boards are clean but wrong pose family
│  └─ fragment rows must not be applied as "repairs"
│
└─ next safe action
   ├─ create/review exact source rows listed below
   ├─ apply whole rows only after source approval
   └─ use backup/hash/rollback/post-proof before merging
```

## Summary

This is a report-only source-quality pass for the snake rows that could not be safely repaired by the prior editable-board extraction.

No runtime PNGs were changed in this pass. The packet exists to prevent us from accidentally replacing currently usable runtime frames with older source rows that are either incomplete, fragmented, or visually the wrong pose family.

## Evidence Packet

- JSON: `vnext/artifacts/snake-blocked-source-packet-20260514/snake-blocked-source-packet.json`
- Markdown: `vnext/artifacts/snake-blocked-source-packet-20260514/snake-blocked-source-packet.md`
- Contact sheet: `vnext/artifacts/snake-blocked-source-packet-20260514/snake-blocked-source-contact-sheet.png`
- Generator: `tools/report_snake_blocked_source_packet.py`

## Rows Still Needing Clean Source

| Row | Why blocked | Needed source |
| --- | --- | --- |
| `baby/female/sad` | `sad_01` extracts as a tail fragment | complete two-frame sad row |
| `baby/female/sick` | `sick_02` extracts as a partial body fragment | complete four-frame sick row |
| `teen/female/happy` | `happy_00` extracts as a partial body fragment | complete four-frame happy row |
| `adult/female/idle` | editable-board row has grid fragments/boxed partial source | review current runtime first; replace only with clean low-profile idle source |
| `adult/female/walk` | editable-board row has partial body fragments | review current runtime first; replace only with clean slither source |
| `adult/female/eat` | `eat_02` extracts as a partial body fragment | new full eat row or explicit approval for coiled care-board source |
| `adult/female/happy` | `happy_03` extracts as a partial body fragment | complete four-frame happy row |
| `adult/female/sick` | `sick_00` extracts as a partial body fragment | complete four-frame sick row |
| `adult/female/bathe` | `bathe_01` extracts as a partial body fragment | complete four-frame bathe row |
| `adult/male/walk` | editable-board row has partial body fragments | review current runtime first; replace only with clean slither source |
| `adult/male/sick` | `sick_01` and `sick_02` extract as fragments | complete four-frame sick row |

## Apply Policy For The Next Snake Pass

- Do not locally draw or procedurally invent replacement snake art.
- Do not use coiled locomotion/care boards to overwrite low-profile slither rows unless the visual decision is explicit.
- Apply only complete source-grounded rows, not individual cherry-picked frames.
- Apply across all six colors only after the source row is clean in blue.
- Keep the normal mutation gate: dry-run scope, backup hashes, apply, post-proof, rollback drill, then re-apply final state.

## Validation

This pass is report-only, so validation focuses on proving it did not mutate runtime art:

- Generated the packet with `python .\tools\report_snake_blocked_source_packet.py --output-root .\vnext\artifacts\snake-blocked-source-packet-20260514`.
- Runtime sprite validation should be run before merge to confirm the report-only pass preserved the current green runtime state.
