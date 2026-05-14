# Snake Scale Gate Audit

Date: 2026-05-14

## Decision

Do **not** scale the current `snake / baby / female / blue / walk` pilot across all snake ages, genders, colors, and actions yet.

```text
╔══════════════════════════════════════╦════════════════════════════════════╗
║ Gate                                 ║ Result                             ║
╠══════════════════════════════════════╬════════════════════════════════════╣
║ One-row pilot is clearer than before ║ Pass                               ║
║ Pilot is final species-wide style    ║ Fail                               ║
║ Full snake runtime is visually clean ║ Fail                               ║
║ Safe to propagate to every action    ║ Fail                               ║
╚══════════════════════════════════════╩════════════════════════════════════╝
```

## What Was Audited

- Current runtime snake contact sheet after the one-row pilot apply.
- Runtime canvas structure.
- Sprite contract structure.
- Live dev-control proof that the local build resolves snake walk from the published runtime folder.

## Evidence

- Current snake contact sheet: `vnext/artifacts/snake-scale-gate-20260514/contact-sheets/snake.png`
- Runtime canvas report: `vnext/artifacts/snake-scale-gate-20260514/runtime-canvas.md`
- Sprite contract report: `vnext/artifacts/snake-scale-gate-20260514/sprite-contract.json`
- Applied pilot report: `docs/SNAKE_BABY_FEMALE_BLUE_WALK_APPLY_2026-05-14.md`

## Findings

The applied pilot row is a clear improvement over the previous thin line-snake for `walk`. It reads as an actual snake silhouette at game scale.

The rest of the snake contact sheet still shows the core quality issue: most rows are still tiny line segments with weak body mass, weak action poses, and little age/gender distinction. This means the structural validators are green, but visual quality is not green.

The pilot should not be blindly copied to all snake actions because actions need distinct readable poses:

- `idle` should hold a calm coiled/resting snake silhouette.
- `walk/run` should show lateral slither extension.
- `eat/drink` should change head posture toward food/water.
- `play/hold/pickup/drop/carry` should preserve ball overlay contracts and not bake the ball into the snake.
- `sleep/rest` should read as low-energy/resting.
- `bath/groom/doctor/medicine` should stay visually compatible with care props.

## Next Safe Repair Path

```text
1. Build real snake row candidates in small batches.
   Target first batch: baby/female/blue core rows.

2. Use source-grounded visual generation or source-board recovery.
   Do not use local procedural drawing as the final species-wide replacement.

3. Review each row in contact sheet and live dev-control proof.
   Only accept rows that read clearly in the home panel and roam band.

4. Apply with backup/hash/rollback/post-proof.
   Never mutate more than the approved batch.

5. Expand after visual acceptance.
   Order: blue pilot rows → all colors for same age/gender → remaining genders → remaining ages.
```

## Recommended Immediate Next Batch

Create candidates for:

- `snake / baby / female / blue / idle`
- `snake / baby / female / blue / walk`
- `snake / baby / female / blue / run`
- `snake / baby / female / blue / rest`
- `snake / baby / female / blue / sleep`

This keeps the next repair small enough to review honestly while proving a consistent snake body style before scaling to the entire species.
