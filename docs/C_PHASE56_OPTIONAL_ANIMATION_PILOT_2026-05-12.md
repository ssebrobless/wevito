# C-PHASE 56 Optional Animation Pilot

Date: 2026-05-12

Branch: `claude-implementation/c-phase-56-optional-animation-pilot`

## Purpose

Prepare one optional animation pilot packet for:

```text
goose / baby / female / blue / carry_ball_walk
```

This phase is a report/proof packet only. It does not apply candidate frames, generate art, import art, or scale to any other optional target.

## Pilot Packet

Output folder:

```text
vnext/artifacts/c-phase-56-optional-animation-pilot/goose-baby-female-blue-carry-ball-walk/
```

Packet contents:

```text
source-runtime-contact-sheet.png
fallback-reference-contact-sheet.png
target-manifest.json
README.md
decision-needed.md
candidate-frames/carry_ball_walk_00.png
candidate-frames/carry_ball_walk_01.png
candidate-frames/carry_ball_walk_02.png
candidate-frames/carry_ball_walk_03.png
candidate-frames/carry_ball_walk_04.png
candidate-frames/carry_ball_walk_05.png
```

## Candidate Status

An exact prior candidate exists in:

```text
vnext/artifacts/animation-runs/20260505-goose-baby-female-blue-optional-expansion-review/candidate-frames/carry_ball_walk/
```

The six candidate PNGs were copied into the C-PHASE 56 packet for review only.

Verification result:

| Check | Result |
| --- | --- |
| Candidate frame count is 6 | PASS |
| Candidate SHA256 values match prior manifest | PASS |
| Candidate PNGs are RGBA | PASS |
| Current runtime target row exists | FAIL / intentionally missing |
| Current authored target row exists | FAIL / intentionally missing |
| Prior manifest source paths exist on current `main` | FAIL |

Important interpretation:

- The candidate frames are real and hash-verified against the prior optional expansion manifest.
- The current `main` branch does not contain `sprites_runtime/goose/baby/female/blue/carry_ball_walk_00.png..05.png`.
- The prior manifest's `source` paths for `carry_ball_walk` therefore do not exist on current `main`.
- This is a reviewable candidate body-pose packet, not an apply-ready provenance packet.

## Ball Overlay Contract

```text
ball policy: runtime overlay only
```

The ball must not be baked into `carry_ball_walk` PNGs. This phase did not apply candidate frames and did not edit any prop-anchor metadata.

## Contact Sheets

The packet includes two review sheets:

```text
source-runtime-contact-sheet.png
fallback-reference-contact-sheet.png
```

`source-runtime-contact-sheet.png` shows:

- missing authored optional source row,
- missing runtime current target row,
- copied candidate body-pose row.

`fallback-reference-contact-sheet.png` shows:

- current runtime walk fallback,
- current runtime idle fallback,
- copied candidate body-pose row.

## Decision Needed

No apply is approved in this phase.

Before any future mutation, choose one label:

```text
accept_candidate_for_apply_plan
revise_candidate_before_apply
hold_optional_animation_pilot
```

Recommended current decision:

```text
hold_optional_animation_pilot
```

Reason: the candidate frames are hash-valid, but the prior manifest source-path mismatch means visual-side review should confirm the body-pose row is still acceptable before code-side prepares a backup/hash/rollback/apply plan.

If `accept_candidate_for_apply_plan` is selected later, the next branch must:

```text
1. verify candidate hashes again
2. dry-run exact six-frame replacement/add scope
3. create backup-before-apply
4. apply only sprites_runtime/goose/baby/female/blue/carry_ball_walk_00.png..05.png
5. run post-proof
6. run rollback drill
7. re-apply so the branch ends post-apply
8. document all hashes and proof artifacts
```

## Required Validation

| Command | Result |
| --- | --- |
| `python .\tools\audit_optional_animation_readiness.py --output .\vnext\artifacts\c-phase-56-optional-animation-pilot\optional-readiness.json --markdown .\vnext\artifacts\c-phase-56-optional-animation-pilot\optional-readiness.md` | PASS |
| `python .\tools\report_runtime_canvas_mismatches.py --output .\vnext\artifacts\c-phase-56-optional-animation-pilot\runtime-canvas.json --markdown .\vnext\artifacts\c-phase-56-optional-animation-pilot\runtime-canvas.md --fail-on-mismatch` | PASS |
| `dotnet build .\vnext\Wevito.VNext.sln` | PASS |
| `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build` | PASS, 280 / 280 |

Optional readiness summary:

```text
passed=true
target_count=2520
authored_complete=0
runtime_prop_anchor_supported=0
fallback_only=2516
invalid_optional_art=0
error_count=0
```

Runtime canvas summary:

```text
checked_sequences=2880
checked_frames=10800
mismatch_count=0
canonical_mismatch_count=3852
missing_count=0
invalid_count=0
```

## Mutation Statement

No runtime PNGs, source boards, authored sprite folders, prop anchors, content manifests, generated art, imported art, or release assets were modified.

Changed tracked files:

```text
docs/C_PHASE56_OPTIONAL_ANIMATION_PILOT_2026-05-12.md
vnext/artifacts/c-phase-56-optional-animation-pilot/goose-baby-female-blue-carry-ball-walk/
```

The packet copies candidate frames into an artifact folder for review only. It does not copy them into `sprites_runtime`.

## Stop

This phase stops here. Do not scale to more optional targets and do not apply candidate frames without a separate explicit approval.
