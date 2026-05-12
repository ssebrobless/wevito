# C-PHASE 56 Optional Animation Pilot Packet

Target: `goose / baby / female / blue / carry_ball_walk`

Status: `report_only_candidate_found_not_applied`

This packet prepares a review/proof surface for one optional animation row. It does not mutate `sprites_runtime`, source boards, prop anchors, content manifests, or generated/imported art.

## Files

```text
source-runtime-contact-sheet.png
fallback-reference-contact-sheet.png
target-manifest.json
decision-needed.md
candidate-frames/carry_ball_walk_00.png..carry_ball_walk_05.png
```

## Important Finding

The prior optional expansion review contains six `carry_ball_walk` candidate frames and their hashes match its manifest. However, that prior manifest references runtime source paths that do not exist on current `main`. Treat this as a reviewable candidate body-pose packet, not an apply-ready provenance packet.

## Ball Policy

The ball must remain a runtime overlay. Candidate PNGs are not applied here.
