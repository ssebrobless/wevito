# Snake Baby Female Blue Walk Candidate

Date: 2026-05-14

## Target

`snake / baby / female / blue / walk`

## Why This Exists

The live dev-control sweep and runtime contact sheets confirm that the current snake walk row reads as thin line segments rather than a clear pet. This packet is a one-row, non-applied prototype so the next step can be decided with visible evidence instead of another scanner-green procedural cleanup.

```text
+----------------------+------------------------------+
| row                  | status                       |
+----------------------+------------------------------+
| current runtime walk | tiny squiggle silhouettes    |
| candidate walk       | larger readable slither body |
+----------------------+------------------------------+
```

## Artifact Packet

- `vnext/artifacts/snake-one-row-candidate-20260514/manifest.json`
- `vnext/artifacts/snake-one-row-candidate-20260514/contact-sheet.png`
- `vnext/artifacts/snake-one-row-candidate-20260514/current-vs-candidate.png`
- `vnext/artifacts/snake-one-row-candidate-20260514/candidate-frames/walk_00.png`
- `vnext/artifacts/snake-one-row-candidate-20260514/candidate-frames/walk_01.png`
- `vnext/artifacts/snake-one-row-candidate-20260514/candidate-frames/walk_02.png`
- `vnext/artifacts/snake-one-row-candidate-20260514/candidate-frames/walk_03.png`
- `vnext/artifacts/snake-one-row-candidate-20260514/candidate-frames/walk_04.png`
- `vnext/artifacts/snake-one-row-candidate-20260514/candidate-frames/walk_05.png`

## Apply Boundary

This packet did not mutate `sprites_runtime`.

If approved, the only first apply scope should be:

- `sprites_runtime/snake/baby/female/blue/walk_00.png`
- `sprites_runtime/snake/baby/female/blue/walk_01.png`
- `sprites_runtime/snake/baby/female/blue/walk_02.png`
- `sprites_runtime/snake/baby/female/blue/walk_03.png`
- `sprites_runtime/snake/baby/female/blue/walk_04.png`
- `sprites_runtime/snake/baby/female/blue/walk_05.png`

Required apply procedure:

1. Capture current runtime hashes for the six target frames.
2. Copy the six current frames to a timestamped backup folder.
3. Verify candidate hashes from `manifest.json`.
4. Replace only the six target frames.
5. Generate a post-apply contact sheet and live dev-control proof.
6. Run rollback drill and re-apply only if rollback restores the exact pre-apply hashes.

## Recommendation

Use this as a one-row pilot only. It is visibly clearer than the current runtime row, but it is not a full species repair and should not be propagated to all snake rows until the user accepts the style and scale in-game.
