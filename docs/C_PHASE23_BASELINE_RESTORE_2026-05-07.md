# C-PHASE 23 Baseline Restore

Generated: 2026-05-07

## Scope

Restored the pre-cleanup `goose / baby / female / blue` optional runtime row references from:

```text
vnext/artifacts/animation-runs/20260505-goose-baby-female-blue-drop-ball-candidate/references/
```

Into:

```text
sprites_runtime/goose/baby/female/blue/
```

No source boards, shared assets, prop anchors, code files, or other sprite rows were modified.

## Rationale

The original C-PHASE 23 apply was blocked because current `origin/main` no longer contained the runtime row bytes that the visual-side handoff was built against. The candidate packet preserves those bytes in `references/`, so this commit restores the missing baseline first, before applying the one-row `drop_ball` candidate through the V2 backup/apply/rollback pipeline.

## Restored Reference Hashes

| File | SHA-256 |
| --- | --- |
| `drop_ball_00.png` | `1141da61a8b91ae60c2f31fc91e9e5903308e71805037f401cbade8d52a64381` |
| `drop_ball_01.png` | `20e9044d7a38200092bf4522c889ccbf51bd07c5300724896483235d57d6ace3` |
| `drop_ball_02.png` | `de50c4aa25716b36bc553cf26f38662d79f72179ce5cc74be292e293f5795029` |
| `drop_ball_03.png` | `509cd3b9a9d1ef7deccd6c75df96c9c59f0374b2cce2cc1ca0921874df1d288a` |
| `hold_ball_00.png` | `e2cac548eb4652ef77fe872af927e2e0e07d0cc42837bebaa5b595366ad1333a` |
| `hold_ball_01.png` | `1e847d80d35fd0cf6e5bf7a0a1aa8218c8db223957af08856019d7418d2779d2` |
| `hold_ball_02.png` | `8b6fb4322a6ee10a19e6e48e31a58a292d66f43e6bc7481ecc1cf4d89b4789dd` |
| `hold_ball_03.png` | `859ea85f5a36325f7cc0e15a1e945bc040feef8d1a7eb5178ee46ddf55b6760a` |
| `pickup_ball_00.png` | `bfff0487cb5a219c84e74cd10b5f702b140434ba3b7b3fdf158e655f1912aa18` |
| `pickup_ball_01.png` | `717396e4afef73cbd6e825c387f7b4aeccd3b32387ffa15ccaf74d33fa0f3269` |
| `pickup_ball_02.png` | `3876b02396cbd34d2ff8f9ba36c8804dd3551fe7c71a326da404a037d54828a6` |
| `pickup_ball_03.png` | `b5e59670bb5326177c07238720f49fc8d6059a1cac008dcee4b96bb5b996e7a2` |
| `play_ball_00.png` | `bfff0487cb5a219c84e74cd10b5f702b140434ba3b7b3fdf158e655f1912aa18` |
| `play_ball_01.png` | `717396e4afef73cbd6e825c387f7b4aeccd3b32387ffa15ccaf74d33fa0f3269` |
| `play_ball_02.png` | `3876b02396cbd34d2ff8f9ba36c8804dd3551fe7c71a326da404a037d54828a6` |
| `play_ball_03.png` | `b5e59670bb5326177c07238720f49fc8d6059a1cac008dcee4b96bb5b996e7a2` |
| `play_ball_04.png` | `212bc4c9cd3dffdfeb2e10173c750b306d17f9b0a4742a80036b1b411c28a009` |
| `play_ball_05.png` | `3a2dea77b2225b6969f74857bfac23664ad43253de73a2a46500a974c83e2714` |

## Hash Assertions

The protected `hold_ball_00..03` endpoint hashes match `docs/WEVITO_OPTIONAL_EXPANSION_APPLY_PROOF_HANDOFF_2026-05-05.md`.

The restored `drop_ball_00..03` hashes match the handoff's `Current Runtime Backup Truth` table.

## Rollback Policy

This restore is intentionally isolated in commit `23a`. Reverting the commit safely re-deletes the restored optional runtime PNGs that were absent from the pre-recovery `origin/main` baseline. The following apply commit must use the V2 backup/apply/rollback path so `drop_ball_00..03` can return to these restored baseline hashes exactly.
