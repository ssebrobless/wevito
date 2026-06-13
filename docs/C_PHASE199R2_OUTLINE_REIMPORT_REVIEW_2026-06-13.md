# C-PHASE 199R2: Outline-segmented re-import + ball-candidate regen (third review gate)

Date: 2026-06-13
Branch: `claude-implementation/c-phase-203c-outline-reimport`
Supersedes: C-199R round 2 (PR #313, rejected — "silhouette cleanups + holes/missing chunks")

## Root cause of the round-2 rejection (now fixed)

The C-203B import cleaner (`clean_cell_border_flood`) segmented the Gemini boards
by **brightness**. The source art draws each animal as a colour fill inside a
thin dark outline on a light-gray checkerboard. For light/white bodies — the
pigeon worst of all — the body and the checker are the same brightness, so the
flood could not tell them apart: it leaked through gaps in the thin outline and
ate the body (enclosed belly holes, and whole frames reduced to faint slivers),
and left ragged semi-opaque halo specks around the silhouette. All three owner
complaints traced to this one cause.

## Fix: segment by the dark outline, not by colour

`import_authored_family_board.segment_cell_by_outline` (replaces the brightness
flood for 256px anti-blur cells):

1. dark pixels (the drawn outline) become a barrier, dilated by 2px to seal
   hairline gaps;
2. the outer ring is cleared so the cell-outline rectangle is dropped and the
   checker connects to the canvas border;
3. flood the EXTERIOR from the border through non-barrier space — checker plus
   open gaps (between-the-legs) drain to the border and stay transparent;
4. body = everything the flood can't reach; `binary_fill_holes` fills checker
   pockets fully enclosed by the outline (belly holes); keep-largest-component
   drops detached halo specks.

Colour plays no role, so a white pigeon on near-white checker is recovered as
reliably as a dark fox — while legitimate leg gaps stay open.

## Re-run (no Gemini — all 744 boards already on disk)

- `tools/reimport_motion_boards.py` re-imported 221 family boards through the new
  segmenter (5 families × deer/fox/frog/pigeon/snake + 3 locomotion families ×
  goose/raccoon/rat/squirrel). snake/adult/male walk_a stays deferred (bad
  Gemini art, unchanged). → `vnext/artifacts/c-phase-203c-imports-v2/`.
- `tools/apply_authored_family_imports.py` re-applied: 54 rows, **9,288 files**
  (runtime + authored, colorize all 6, no passthrough), full sha256 backup at
  `vnext/artifacts/c-phase-203c-apply-v2/backup/`. The defective working-tree v5
  apply was discarded first (`git checkout`), so backups capture true main.
- `tools/generate_optional_ball_families.py` regenerated **10,770** ball
  candidates from the re-cleaned required families →
  `vnext/artifacts/c-phase-199r2-ball-candidates/`.
- `tools/build_ball_review_sheets.py` rebuilt the 10 per-species sheets →
  `…/review-sheets/<species>.png`.

## Post-proof (all green)

| Check | Before (v5 apply) | After (outline re-import) |
|---|---|---|
| Enclosed holes (adult/female/red, all species) | 8,244px; pigeon/walk_01 2254, happy_00 destroyed | **816px, no frame >50px** |
| Sprite contract audit | — | **error_count=0** |
| Runtime canvas | — | **mismatch=0 missing=0 invalid=0** |
| Tint conformance (360 variants) | — | **0 nonconforming** (BUG-007 stays closed) |
| Full suite | — | **1717/1717** |
| build-vnext -SkipAssetPrep | — | compiles; suite green |

## AWAITING PER-SPECIES OWNER APPROVAL (third gate)

Review `vnext/artifacts/c-phase-199r2-ball-candidates/review-sheets/`. Check a
species to approve its C-200 matrix apply.

- [ ] crow   - [ ] deer   - [ ] fox   - [ ] frog   - [ ] goose
- [ ] pigeon - [ ] raccoon - [ ] rat  - [ ] snake  - [ ] squirrel

Note: crow/rat/goose/raccoon/squirrel required families were procedurally
cleaned in C-203A (not Gemini-regenerated); their candidates derive from that
art. snake/adult/male carry_ball_walk/run derive from the OLD walk row
(deferred).

## Next

Owner-approved species → C-200 guarded matrix apply (prop_anchors.json +
coverage pin) → C-204 fetch wiring → C-205 RC verdict.
