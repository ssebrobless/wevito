# C-PHASE 23 Apply Report

Generated: 2026-05-07

## Target

```text
goose / baby / female / blue / drop_ball
```

Only the runtime `drop_ball_00..03` files were changed in this apply commit. The restored `hold_ball_00..03` endpoint remained protected and unchanged.

## Candidate Source

```text
vnext/artifacts/animation-runs/20260505-goose-baby-female-blue-drop-ball-candidate/candidate-frames/
```

The candidate hashes matched the visual-side handoff before apply.

| Candidate | SHA-256 |
| --- | --- |
| `drop_ball_00.png` | `859ea85f5a36325f7cc0e15a1e945bc040feef8d1a7eb5178ee46ddf55b6760a` |
| `drop_ball_01.png` | `bfff0487cb5a219c84e74cd10b5f702b140434ba3b7b3fdf158e655f1912aa18` |
| `drop_ball_02.png` | `3876b02396cbd34d2ff8f9ba36c8804dd3551fe7c71a326da404a037d54828a6` |
| `drop_ball_03.png` | `b5e59670bb5326177c07238720f49fc8d6059a1cac008dcee4b96bb5b996e7a2` |

## V2 Pipeline Results

```text
candidate import: passed
dry-run apply: passed
dry-run replacements: 4
dry-run adds: 0
same-volume apply guard: satisfied
first apply: passed
post-apply proof: passed
auto-rollback during post-proof: false
rollback drill: passed
final re-apply: passed
final post-proof: passed
```

The unchanged V2 services generated transient local candidate/backup artifacts while running the pipeline:

| Artifact | Path |
| --- | --- |
| Imported V2 candidate folder | `C:\Users\fishe\.codex\worktrees\f0f4\wevito\sprites_authored\goose\baby\female\blue\.candidates\drop_ball-20260507-230000` |
| Dry-run manifest | `C:\Users\fishe\.codex\worktrees\f0f4\wevito\vnext\artifacts\c-phase-23-drop-ball-pilot-recovery\dry-run-apply.json` |
| First apply backup folder | `C:\Users\fishe\.codex\worktrees\f0f4\wevito\sprites_runtime\.backup\goose-baby-female-blue-drop_ball-20260507-230100` |
| First apply log | `C:\Users\fishe\.codex\worktrees\f0f4\wevito\sprites_runtime\.backup\goose-baby-female-blue-drop_ball-20260507-230100\apply.json` |
| Rollback log | `C:\Users\fishe\.codex\worktrees\f0f4\wevito\sprites_runtime\.backup\goose-baby-female-blue-drop_ball-20260507-230100\rollback.json` |
| Final re-apply backup folder | `C:\Users\fishe\.codex\worktrees\f0f4\wevito\sprites_runtime\.backup\goose-baby-female-blue-drop_ball-20260507-230500` |
| Final re-apply log | `C:\Users\fishe\.codex\worktrees\f0f4\wevito\sprites_runtime\.backup\goose-baby-female-blue-drop_ball-20260507-230500\apply.json` |

## Pre/Post Hash Audit

| File | Baseline before apply | Final after re-apply |
| --- | --- | --- |
| `drop_ball_00.png` | `1141da61a8b91ae60c2f31fc91e9e5903308e71805037f401cbade8d52a64381` | `859ea85f5a36325f7cc0e15a1e945bc040feef8d1a7eb5178ee46ddf55b6760a` |
| `drop_ball_01.png` | `20e9044d7a38200092bf4522c889ccbf51bd07c5300724896483235d57d6ace3` | `bfff0487cb5a219c84e74cd10b5f702b140434ba3b7b3fdf158e655f1912aa18` |
| `drop_ball_02.png` | `de50c4aa25716b36bc553cf26f38662d79f72179ce5cc74be292e293f5795029` | `3876b02396cbd34d2ff8f9ba36c8804dd3551fe7c71a326da404a037d54828a6` |
| `drop_ball_03.png` | `509cd3b9a9d1ef7deccd6c75df96c9c59f0374b2cce2cc1ca0921874df1d288a` | `b5e59670bb5326177c07238720f49fc8d6059a1cac008dcee4b96bb5b996e7a2` |

## Protected Hold Endpoint

These hashes were verified before apply, after apply, after rollback, and after final re-apply.

| Protected file | SHA-256 |
| --- | --- |
| `hold_ball_00.png` | `e2cac548eb4652ef77fe872af927e2e0e07d0cc42837bebaa5b595366ad1333a` |
| `hold_ball_01.png` | `1e847d80d35fd0cf6e5bf7a0a1aa8218c8db223957af08856019d7418d2779d2` |
| `hold_ball_02.png` | `8b6fb4322a6ee10a19e6e48e31a58a292d66f43e6bc7481ecc1cf4d89b4789dd` |
| `hold_ball_03.png` | `859ea85f5a36325f7cc0e15a1e945bc040feef8d1a7eb5178ee46ddf55b6760a` |

## Rollback Drill

`SpriteWorkflowRollbackService` restored `drop_ball_00..03` to the baseline hashes from `docs/C_PHASE23_BASELINE_RESTORE_2026-05-07.md`. After that verification, the same candidate row was re-applied through a second dry-run/apply pass so the working tree ends in the post-apply state.

## Additional Validation

```text
python .\tools\audit_sprite_contract.py
  error_count=0
  runtime_variant_dirs_found=360
  runtime_frames_found=10818

python .\tools\report_runtime_canvas_mismatches.py
  checked_sequences=2880
  checked_frames=10800
  mismatch_count=0
  missing_count=0
  invalid_count=0
```

The `runtime_frames_found=10818` count reflects the restored 18 optional reference PNGs from C-PHASE 23a.

## Ball Overlay Assertion

No ball pixels were baked into the `drop_ball` candidate by this code-side apply. The ball remains runtime-overlay driven by:

```text
sprites_runtime/_metadata/prop_anchors.json
sprites_shared_runtime/items/toys_a/ball.png
```
