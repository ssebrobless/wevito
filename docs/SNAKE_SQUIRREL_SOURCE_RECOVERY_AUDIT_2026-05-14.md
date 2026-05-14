# Snake And Squirrel Source Recovery Audit

Date: 2026-05-14
Branch: `fix/snake-squirrel-source-audit`

## Why This Audit Exists

The post-merge pet simulation audit identified `snake` and `squirrel` as the two highest-priority visual blockers. Both species had prior deterministic cleanup passes, but the in-game result still does not satisfy the visual quality bar.

```text
+--------------------+----------------------+---------------------------+
| species            | current runtime issue | best next repair posture  |
+--------------------+----------------------+---------------------------+
| snake              | bad silhouette/motion | source recovery or redraw |
| squirrel           | weak motion variety   | source-faithful animation |
+--------------------+----------------------+---------------------------+
```

## Source Files Reviewed

### Snake

- `incoming_sprites/snake baby.png`
- `incoming_sprites/snake-teen.png`
- `incoming_sprites/snake-adult.png`
- `incoming_sprites/gemini_handoff_motion/snake/baby/female/locomotion/5-save-edited-board-here/snake-baby-female-locomotion-gemini-result.png`
- `incoming_sprites/gemini_handoff_motion/snake/baby/female/locomotion/5-save-edited-board-here/snake-baby-female-locomotion-extracted-board-import.png`
- `incoming_sprites/gemini_handoff_motion/snake/adult/male/locomotion/5-save-edited-board-here/snake-adult-male-locomotion-extracted-board-import.png`
- `tools/repair_snake_procedural_rows.py`
- `docs/SNAKE_RUNTIME_PROCEDURAL_CLEANUP_2026-05-14.md`

### Squirrel

- `incoming_sprites/squirrel-baby.png`
- `incoming_sprites/squirrel-teen.png`
- `incoming_sprites/squirrel-adult.png`
- `tools/repair_squirrel_motion_rows.py`
- `docs/SQUIRREL_RUNTIME_MOTION_CLEANUP_2026-05-14.md`

## Findings

### Snake

The single snake reference images are visually stronger than current runtime frames: larger, clearer, and more readable. However, the saved motion boards are not clean enough to blindly import as runtime frames:

- Some boards repeat almost the same coiled idle pose rather than showing a full slither.
- Some boards include cropped or partially visible strips.
- Some extracted board imports produce tiny/incorrect frame crops when passed through the current importer.
- Prior procedural cleanup replaced muddy frames with simpler deterministic snake silhouettes, but the result still reads as too thin and placeholder-like in game.

Conclusion: snake should not be fixed by rerunning `tools/repair_snake_procedural_rows.py`. That script already represents the failed deterministic compromise. The next repair should either:

- recover clean frames from the best source boards with a corrected board-specific crop map, or
- create a new art-directed snake motion packet using the original high-resolution snake references as identity anchors.

### Squirrel

The single squirrel references are visually strong. Current runtime squirrel silhouettes are crisp, but the animation reads too static because many action rows reuse similar upright poses with only small movement offsets.

Conclusion: squirrel does not need procedural body redraw. It needs real pose variation:

- walk/run body lean and foot/tail timing,
- eat/drink head lowering,
- play/happy bounce,
- sleep/rest posture,
- action-specific frames that do not look like the same idle pose shifted by a few pixels.

## Recommended Repair Order

```text
1. Snake source-recovery prototype
   - target one row first: snake / baby / female / blue / walk
   - compare current runtime vs source board vs recovered candidate
   - prove in cockpit with exact frame pinning

2. Snake full runtime replacement plan
   - only after prototype is visually accepted
   - expand by age/gender/color using identity-preserving palette variants

3. Squirrel motion prototype
   - target one row first: squirrel / baby / female / blue / walk
   - preserve current silhouette style but add true pose changes

4. Pigeon/crow frame isolation
   - use exact frame proof from PR #129 to identify bad frame ids
```

## Hard Boundaries For The Next Repair

- Do not mutate `sprites_runtime` until the one-row candidate is visually proven.
- Do not rerun old procedural snake cleanup as the final fix.
- Do not claim scanner-green means visual-green.
- Preserve the stronger original source identity where possible.
- Any runtime PNG mutation needs backup, hash report, rollback, post-proof, and refreshed contact sheet.

## Current Recommendation

Start with `snake / baby / female / blue / walk` as a one-row source-recovery prototype. If that cannot be recovered cleanly from existing boards, pause and create a new prompt/candidate packet grounded in the original snake reference images instead of forcing the current procedural runtime art further.
