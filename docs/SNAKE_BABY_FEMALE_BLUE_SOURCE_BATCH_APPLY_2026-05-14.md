# Snake Baby Female Blue Source Batch Apply - 2026-05-14

## Scope
- Target: `snake / baby / female / blue`
- Applied families: `idle`, `walk`
- Runtime mutations: 10 PNG files only under `sprites_runtime/snake/baby/female/blue/`
- Care/action families were not applied because the available care extraction did not meet the visual quality bar.

## Source
- Idle frames: saved Gemini baby/female locomotion board.
- Walk frames: approved Gemini slither motion reference, recolored to the blue variant.
- Candidate manifest: `vnext/artifacts/snake-source-batch-20260514/locomotion-manifest.json`
- Review sheet: `vnext/artifacts/snake-source-batch-20260514/locomotion-source-candidate-contact-sheet.png`

## Safety
- Backup path: `C:\Users\fishe\.codex\worktrees\ab46\wevito\vnext\artifacts\snake-source-batch-20260514\backup-before-apply-20260514-181825`
- Dry-run scope count: 10 replacements, 0 adds.
- Rollback drill: passed, then candidates were re-applied so the working tree ends in post-apply state.

## Runtime Display Fix
- Live dev-control proof showed the repaired snake assets were loaded, but the focused home stage still hid pets below the visible stage.
- Root cause: calm lineup and starter egg prompt placement added the stage's window offset even though those elements render inside `HomePetCanvas`.
- Fix: `HomePanelWindow.ResolveCalmLineupPlacement` and `ComputeStarterEggPromptLayout` now return local stage coordinates.
- Added regression coverage with nonzero stage offsets so focused pets and egg selection cannot silently drift off-stage again.

## Runtime Proof
- Published shell from this branch with `-SkipAssetPrep` so repaired runtime sprites were preserved.
- Dev-control spawned `snake / baby / female / blue` and forced both `idle` and `walk`.
- Asset source confirmed:
  - `idle`: `vnext/artifacts/shell/sprites_runtime/snake/baby/female/blue/idle_00.png` with 4 frames.
  - `walk`: `vnext/artifacts/shell/sprites_runtime/snake/baby/female/blue/walk_00.png` with 6 frames.
- Proof packet:
  - `vnext/artifacts/snake-source-batch-20260514/dev-control-proof/snake-baby-female-blue-after-placement-proof.json`
  - `vnext/artifacts/snake-source-batch-20260514/dev-control-proof/desktop-proof-after-placement-idle.png`
  - `vnext/artifacts/snake-source-batch-20260514/dev-control-proof/desktop-proof-after-placement-walk.png`

## Per-Frame Hashes
| Frame | Pre SHA256 | Candidate/Post SHA256 |
|---|---|---|
| `idle_00.png` | `21fd4100b44d6a42bdc8a15cff7e014344c29fb6105d620704f8fd897c4f193e` | `cc6b0a9a395d20b28d69bfe57fe17358b596ce9cd5e2be3c110b9bc2b8caa689` |
| `idle_01.png` | `70273a2382d3d4da9005cf6184c954b900afcb872129c39396d6919213dd0050` | `29852eac3dac7ae44cdcb58dae927dd1720c575f354d4948f6fbbd9d575553cf` |
| `idle_02.png` | `94a71e43101d0d808dd2c5372403c2ef29a3eefb8f0ba8aa64da77a3b3d1ca84` | `1058c9be231d0820aaedb21bc96d241d1ac789d07b8d2bf111633f2c7fc3e2f4` |
| `idle_03.png` | `46784f92566343ec36b24e970e3c5d5ee4ec0bf4a2b466ab32dcd3e679a57a4c` | `49ff997695577d51e51101b6eb42b9b761449a30c9db1bbc5479eacc42f3b79a` |
| `walk_00.png` | `d21247d39e508937da48ce90b93b06a14bc26456a47e68ffb762e2274d2363a3` | `b10a916d239053d6b17b9e92e0417fa8b25139bdeb6e168262223e24fde2f44b` |
| `walk_01.png` | `b1f5e70fd92d4b78ec1cca5bb747b7d27d8008523637f33f665020b2e318c3f5` | `4288a3b75d956f5469b02151790c0c44183ce6c4afdc00fbcafa89a7ff596caa` |
| `walk_02.png` | `fde2d7099c1cf21e8d51b8b05b643ff3118783686102cbbbe5f4866df698fb2c` | `e8e0f96f972f420cb326ce5232a113048e9fd272cca95e9ce7581df27f809aae` |
| `walk_03.png` | `66a8a3babba35acabf70cfc0e79d386372b661e4d097a416465a1adc11858dbf` | `f06c50360f402742e68d27235bd0f054ab32b05e35115f6f8f8be3c46cf69795` |
| `walk_04.png` | `4f1c52ece17e045255d89c78e0a120942b8e6816317af8470de8e2d1499472b1` | `308e8819a0d959d07968023bb84556d8dbf963fdec81a61efc2283c55df012d7` |
| `walk_05.png` | `989eb5b318c19acba183fcd4dd78125110bf66eb1a9807849054c34dd7060370` | `1cabfdd8f7d88918a5b313446a249edf348e9d2f2b762d2c65181b0db3f69006` |

## Scale Decision
This is not yet scaled across all snake rows. It is a high-confidence locomotion pilot only. Scaling should wait until this row is accepted visually, because the available care/action source extraction did not yet meet the same quality bar.
