# C-PHASE 23 Blocker: drop_ball One-Row Pilot Cannot Safely Apply Yet

Updated: 2026-05-07

## Phase

C-PHASE 23 — Optional Animation `drop_ball` One-Row Pilot Through V2

## Intended Target

```text
goose / baby / female / blue / drop_ball
```

## Blocker

The visual candidate packet exists and its candidate SHA-256 hashes match the visual-side handoff, but the current `origin/main` runtime tree does not contain the required existing optional runtime rows:

```text
sprites_runtime/goose/baby/female/blue/drop_ball_00.png
sprites_runtime/goose/baby/female/blue/drop_ball_01.png
sprites_runtime/goose/baby/female/blue/drop_ball_02.png
sprites_runtime/goose/baby/female/blue/drop_ball_03.png
sprites_runtime/goose/baby/female/blue/hold_ball_00.png
sprites_runtime/goose/baby/female/blue/hold_ball_01.png
sprites_runtime/goose/baby/female/blue/hold_ball_02.png
sprites_runtime/goose/baby/female/blue/hold_ball_03.png
```

The C-PHASE 23 handoff requires:

- verify current runtime `drop_ball_00..03` hashes before backup
- protect accepted `hold_ball_00..03` endpoint hashes
- replace only existing `drop_ball_00..03`
- rollback to the exact original runtime files

Because the runtime files are absent in this branch, applying now would create new runtime files instead of replacing the audited current row. That would bypass the handoff's backup truth and make rollback/proof semantics ambiguous.

## Evidence

Candidate packet present:

```text
vnext/artifacts/animation-runs/20260505-goose-baby-female-blue-drop-ball-candidate/candidate-frames/
```

Candidate SHA-256 hashes matched the handoff:

```text
drop_ball_00.png  859ea85f5a36325f7cc0e15a1e945bc040feef8d1a7eb5178ee46ddf55b6760a
drop_ball_01.png  bfff0487cb5a219c84e74cd10b5f702b140434ba3b7b3fdf158e655f1912aa18
drop_ball_02.png  3876b02396cbd34d2ff8f9ba36c8804dd3551fe7c71a326da404a037d54828a6
drop_ball_03.png  b5e59670bb5326177c07238720f49fc8d6059a1cac008dcee4b96bb5b996e7a2
```

Runtime checks returned no matching files in:

```text
sprites_runtime/goose/baby/female/blue/
```

Historical lookup also found no tracked `origin/main` entries for:

```text
sprites_runtime/goose/baby/female/blue/drop_ball_*.png
sprites_runtime/goose/baby/female/blue/hold_ball_*.png
```

## Safe Next Options

1. Restore the previously accepted optional runtime rows from the branch, artifact, backup, or commit where `hold_ball_00..03` and `drop_ball_00..03` existed, then rerun C-PHASE 23 from a fresh branch.
2. Ask visual-side to revise the handoff so this is explicitly an "add optional row" pilot instead of a "replace existing row" pilot, including a new rollback policy for absent originals.
3. Defer C-PHASE 23 and proceed to a non-mutating phase, such as C-PHASE 24, until optional runtime row provenance is reconciled.

## Recommendation

Do not apply the `drop_ball` candidate yet. Reconcile why the accepted `hold_ball` endpoint and current `drop_ball` row are absent from `origin/main`, then rerun the phase with a valid current-runtime hash baseline.
