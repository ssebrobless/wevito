# Wevito Optional Expansion Apply/Proof Handoff

Updated: 2026-05-05

This handoff records the visual-side decision after the first post-hold endpoint
review packet for `goose / baby / female / blue`.

It does not apply sprites, generate art, rewrite source boards, change runtime
code, or authorize broad optional-animation expansion.

## Current Shape

```text
accepted hold endpoint
  |
  +-- proofed row: goose / baby / female / blue / hold_ball
  +-- protected runtime files: hold_ball_00..03
  |
  +-- optional expansion review
        |
        +-- pickup_ball      -> proof current runtime row only
        +-- drop_ball        -> apply/proof one replacement row
        +-- carry_ball_walk  -> proof current runtime row only
        +-- carry_ball_run   -> proof current runtime row only
```

User decision:

```text
accept_optional_expansion_review_for_apply_plan
```

## Source Review Packets

Unified review packet:

```text
C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\animation-runs\20260505-goose-baby-female-blue-optional-expansion-review\
  +-- manifest.json
  +-- run-summary.md
  +-- candidate-frames\
  +-- qa\optional-expansion-candidate-review-sheet.png
  +-- qa\pickup_ball-candidate-preview.gif
  +-- qa\drop_ball-candidate-preview.gif
  +-- qa\carry_ball_walk-candidate-preview.gif
  +-- qa\carry_ball_run-candidate-preview.gif
```

Drop-only candidate packet:

```text
C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\animation-runs\20260505-goose-baby-female-blue-drop-ball-candidate\
  +-- manifest.json
  +-- run-summary.md
  +-- candidate-frames\drop_ball_00.png
  +-- candidate-frames\drop_ball_01.png
  +-- candidate-frames\drop_ball_02.png
  +-- candidate-frames\drop_ball_03.png
  +-- qa\drop-ball-current-vs-candidate-contact-sheet.png
  +-- qa\drop-ball-candidate-preview.gif
```

## Authorized Mutation Scope For Code-Side Planning

Only these runtime files should be considered for the one-row apply/proof step:

```text
C:\Users\fishe\Documents\projects\wevito\sprites_runtime\goose\baby\female\blue\drop_ball_00.png
C:\Users\fishe\Documents\projects\wevito\sprites_runtime\goose\baby\female\blue\drop_ball_01.png
C:\Users\fishe\Documents\projects\wevito\sprites_runtime\goose\baby\female\blue\drop_ball_02.png
C:\Users\fishe\Documents\projects\wevito\sprites_runtime\goose\baby\female\blue\drop_ball_03.png
```

No other runtime PNGs are part of this apply step.

Do not mutate:

- `hold_ball_00..03`; these are the accepted endpoint and must stay protected.
- `pickup_ball_*`; proof current runtime only.
- `carry_ball_walk_*`; proof current runtime only.
- `carry_ball_run_*`; proof current runtime only.
- `sprites_shared_runtime\items\toys_a\ball.png`.
- `sprites_runtime\_metadata\prop_anchors.json`.
- source boards, Godot scripts, vNext source, generated imports, or all-color rows.

## Ball Policy

The ball remains a runtime overlay only.

```text
pet frame PNG
  + runtime overlay metadata
  + shared ball asset
  = proof render
```

Do not bake the ball into any candidate PNG. During proof, watch for:

- double ball
- missing ball
- ball drifting away from beak/body contact
- wrong z-order

Relevant overlay inputs:

```text
C:\Users\fishe\Documents\projects\wevito\sprites_runtime\_metadata\prop_anchors.json
C:\Users\fishe\Documents\projects\wevito\sprites_shared_runtime\items\toys_a\ball.png
metadata key: goose/baby/female/blue
metadata family: drop_ball
```

## Current Runtime Backup Truth

Before apply, code-side should back up the current runtime `drop_ball_00..03`
files and verify these hashes.

| Runtime file | Current SHA-256 before apply |
| --- | --- |
| `drop_ball_00.png` | `1141da61a8b91ae60c2f31fc91e9e5903308e71805037f401cbade8d52a64381` |
| `drop_ball_01.png` | `20e9044d7a38200092bf4522c889ccbf51bd07c5300724896483235d57d6ace3` |
| `drop_ball_02.png` | `de50c4aa25716b36bc553cf26f38662d79f72179ce5cc74be292e293f5795029` |
| `drop_ball_03.png` | `509cd3b9a9d1ef7deccd6c75df96c9c59f0374b2cce2cc1ca0921874df1d288a` |

## Candidate Truth

Apply candidates should come from:

```text
C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\animation-runs\20260505-goose-baby-female-blue-drop-ball-candidate\candidate-frames\
```

| Candidate file | Source frame | Size | Candidate SHA-256 |
| --- | --- | ---: | --- |
| `drop_ball_00.png` | `sprites_runtime\goose\baby\female\blue\hold_ball_03.png` | `60x59` | `859ea85f5a36325f7cc0e15a1e945bc040feef8d1a7eb5178ee46ddf55b6760a` |
| `drop_ball_01.png` | `sprites_runtime\goose\baby\female\blue\play_ball_00.png` | `101x125` | `bfff0487cb5a219c84e74cd10b5f702b140434ba3b7b3fdf158e655f1912aa18` |
| `drop_ball_02.png` | `sprites_runtime\goose\baby\female\blue\play_ball_02.png` | `101x123` | `3876b02396cbd34d2ff8f9ba36c8804dd3551fe7c71a326da404a037d54828a6` |
| `drop_ball_03.png` | `sprites_runtime\goose\baby\female\blue\pickup_ball_03.png` | `102x125` | `b5e59670bb5326177c07238720f49fc8d6059a1cac008dcee4b96bb5b996e7a2` |

The mixed dimensions are inherited from existing runtime full-body frames. Do
not crop or shrink the candidate to force a fixed box during this proof.

## Protected Hold Endpoint Hashes

After any apply/proof operation, these hashes should remain unchanged:

| Protected file | SHA-256 |
| --- | --- |
| `hold_ball_00.png` | `e2cac548eb4652ef77fe872af927e2e0e07d0cc42837bebaa5b595366ad1333a` |
| `hold_ball_01.png` | `1e847d80d35fd0cf6e5bf7a0a1aa8218c8db223957af08856019d7418d2779d2` |
| `hold_ball_02.png` | `8b6fb4322a6ee10a19e6e48e31a58a292d66f43e6bc7481ecc1cf4d89b4789dd` |
| `hold_ball_03.png` | `859ea85f5a36325f7cc0e15a1e945bc040feef8d1a7eb5178ee46ddf55b6760a` |

## Requested Code-Side Proof Plan

Code-side should prepare and execute the smallest safe Godot proof plan:

1. Re-read this handoff plus both manifests above.
2. Confirm the user has approved this one-row apply/proof step.
3. Back up current runtime `drop_ball_00..03` into the run artifact folder.
4. Replace only runtime `drop_ball_00..03` with the four candidate frames.
5. Verify runtime `drop_ball_00..03` now match the candidate hashes.
6. Verify protected `hold_ball_00..03` still match the endpoint hashes.
7. Run a Godot packaged proof/contact sheet using runtime overlay metadata.
8. Include the full optional sequence proof surface:
   `pickup_ball`, `drop_ball`, `hold_ball`, `carry_ball_walk`, `carry_ball_run`.
9. Record proof summary, backup paths, hashes, and rollback command/process.
10. Return one decision request to the user:
    `accept_applied_optional_expansion`, `revise_or_rollback_drop`, or
    `hold_before_more_expansion`.

## Stop Rules

Stop and do not apply if:

- any candidate hash differs from this handoff or the manifest
- any current runtime drop hash differs before backup, unless explained by a
  newer user-approved code-side change
- any file outside `drop_ball_00..03` needs mutation
- the proof surface would bake the ball into frames
- the proof shows double-ball, missing-ball, or obviously broken z-order
- the accepted `hold_ball_00..03` endpoint changes
- rollback cannot restore the original four drop frames exactly

Do not expand to pickup/drop/carry for other colors, genders, ages, species, or
new generated candidates until this one-row applied proof has a decision.

## Visual-Side Status

```text
visual review packet: complete
user decision: accepted for code-side apply/proof planning
visual-side mutation after decision: none
next owner: code-side one-row apply/proof/rollback plan
```
