# Wevito Goose Hold-Ball Prompt Packet

Updated: 2026-05-04

This is Phase 7 of `docs/WEVITO_VISUAL_REMAINING_PHASE_PLAN_2026-05-04.md`.
It prepares the first future visual production packet for:

```text
goose / baby / female / blue / hold_ball
```

It does not authorize generation, import, runtime PNG edits, source-board edits,
runtime code changes, packaged sweeps, or Sprite Workflow App changes.

## Boundary

```text
Phase 7
  |
  +-- write final hold-ball prompt
  +-- define exact reference attachments
  +-- define draft manifest values
  +-- define required review artifacts
  +-- define rollback/apply requirements
  |
  +-- no generation
  +-- no import
  +-- no sprite edits
  +-- no runtime code changes
```

## Target

| Field | Value |
| --- | --- |
| Species | `goose` |
| Age | `baby` |
| Gender | `female` |
| Source color | `blue` |
| Family | `hold_ball` |
| Frame count | 4 |
| First future run id | `20260504-goose-baby-female-blue-hold-ball-pilot` |

Runtime target paths:

```text
C:\Users\fishe\Documents\projects\wevito\sprites_runtime\goose\baby\female\blue\hold_ball_00.png
C:\Users\fishe\Documents\projects\wevito\sprites_runtime\goose\baby\female\blue\hold_ball_01.png
C:\Users\fishe\Documents\projects\wevito\sprites_runtime\goose\baby\female\blue\hold_ball_02.png
C:\Users\fishe\Documents\projects\wevito\sprites_runtime\goose\baby\female\blue\hold_ball_03.png
```

## Current Evidence

The current `hold_ball` row is a placeholder endpoint, not an accepted final
grip pose.

```text
hold_ball_00..03
  -> exact hash match to idle_00..03
  -> canvas: 60x59
  -> visual read: idle clone, no ball contact pose
```

The first production goal is not a dramatic new action. It is a clean endpoint:

```text
baby goose
  -> same identity as source
  -> ball physically held at beak/front-body contact
  -> subtle 4-frame hold loop
```

## Anchor Note

From `sprites_runtime/_metadata/prop_anchors.json`:

```text
goose/baby/female/blue/ball/families/hold_ball
  anchor_norm:
    x: 0.865
    y: 0.400
  scale: 0.426
  z_index: 12
```

Visual interpretation:

```text
ball contact
  -> high/front side of baby goose
  -> beak/front-body hold
  -> should look supported, clenched, or pressed by the goose
  -> should not read as a pasted overlay
```

## Required Reference Attachments

Use these references in this order for a future provider or authoring handoff.

| Slot | Purpose | Path |
| ---: | --- | --- |
| 1 | Canonical baby goose source board | `C:\Users\fishe\Documents\projects\wevito\incoming_sprites\goose-baby.png` |
| 2 | Current idle-vs-hold duplicate proof | `C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\visual-review\20260504-goose-optional-family-review\goose-baby-female-blue-idle-vs-hold_ball-check.png` |
| 3 | Current optional-family context sheet | `C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\visual-review\20260504-goose-optional-family-review\goose-baby-female-blue-optional-family-contact-sheet.png` |
| 4 | Current idle frames | `C:\Users\fishe\Documents\projects\wevito\sprites_runtime\goose\baby\female\blue\idle_00.png` through `idle_03.png` |
| 5 | Current happy frames | `C:\Users\fishe\Documents\projects\wevito\sprites_runtime\goose\baby\female\blue\happy_00.png` through `happy_03.png` |
| 6 | Prop anchor metadata | `C:\Users\fishe\Documents\projects\wevito\sprites_runtime\_metadata\prop_anchors.json` |

If a layout guide image is generated later, it should be stored at:

```text
C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\animation-runs\20260504-goose-baby-female-blue-hold-ball-pilot\layout-guide-hold-ball-4.png
```

## Final Provider Prompt

Copy-paste this prompt only after production gates are approved.

```text
Create a Wevito hold_ball animation for:

species: goose
age: baby
gender: female
source color: blue
family: hold_ball
frame count: 4

Use the attached canonical goose baby source and current runtime references as
identity locks. Preserve the exact baby goose identity: head shape, beak, face
language, body proportions, outline weight, color family, and silhouette. The
result must still look like the same Wevito baby goose, not a redesigned goose.

Output exactly 4 transparent sprite frames in 4 clear slots, ordered left to
right. Each slot represents one hold_ball frame. Keep the goose centered with
safe padding. Do not crop, stretch, shrink, upscale, downscale, or box-fit the
goose.

The ball must be physically held at the goose's beak/front-body grip point. Use
this contact intent:

anchor_norm x: 0.865
anchor_norm y: 0.400
scale: 0.426
z_index/read: ball is in front of the goose body

The ball should look supported, pressed, or clenched by the beak/front body. It
must not look pasted on top of an idle pose. The ball must remain attached to
the same contact point across all 4 frames.

Make the motion subtle:

frame 00: settled hold pose; ball visibly connected to beak/front body
frame 01: tiny stabilizing head/body adjustment; ball remains attached
frame 02: small settle variation; same identity and ball scale
frame 03: loop bridge back to frame 00 without a pop

This is not a pickup, drop, throw, jump, dramatic play, or running action. It is
a calm held-ball endpoint.

Hard negatives:

No text, labels, UI, speech bubbles, speed lines, dust, shadows, glows, motion
arcs, checkerboard, white background, black background, guide marks, borders,
extra props, detached effects, cropped body parts, duplicated neighboring frame
slivers, or background/floor patches.

Do not change the species, age, face, beak, body size, color palette, outline
style, or pixel-art feel. Identity drift is a rejection.
```

## Output Expectations

Preferred provider output:

```text
one transparent 4-slot board
  -> 4 frames left to right
  -> no labels or guide marks in the image
  -> clean alpha
  -> each frame visually separated enough for extraction
```

Acceptable manual-authoring output:

```text
four transparent candidate PNGs
  -> hold_ball_00.png
  -> hold_ball_01.png
  -> hold_ball_02.png
  -> hold_ball_03.png
```

The final runtime frames must still be validated as `28x24` transparent PNGs
after import. Provider boards are not accepted runtime assets by themselves.

## Draft Manifest Values

This is a draft manifest shape for the first future run. It is intentionally
`ready`, not `applied`.

```json
{
  "run_id": "20260504-goose-baby-female-blue-hold-ball-pilot",
  "created_at": "2026-05-04T00:00:00-04:00",
  "target": {
    "species": "goose",
    "age": "baby",
    "gender": "female",
    "source_color": "blue",
    "family": "hold_ball",
    "frame_count": 4,
    "runtime_paths": [
      "C:\\Users\\fishe\\Documents\\projects\\wevito\\sprites_runtime\\goose\\baby\\female\\blue\\hold_ball_00.png",
      "C:\\Users\\fishe\\Documents\\projects\\wevito\\sprites_runtime\\goose\\baby\\female\\blue\\hold_ball_01.png",
      "C:\\Users\\fishe\\Documents\\projects\\wevito\\sprites_runtime\\goose\\baby\\female\\blue\\hold_ball_02.png",
      "C:\\Users\\fishe\\Documents\\projects\\wevito\\sprites_runtime\\goose\\baby\\female\\blue\\hold_ball_03.png"
    ]
  },
  "references": {
    "canonical_source_board": "C:\\Users\\fishe\\Documents\\projects\\wevito\\incoming_sprites\\goose-baby.png",
    "verified_runtime_strips": [
      "C:\\Users\\fishe\\Documents\\projects\\wevito\\vnext\\artifacts\\visual-review\\20260504-goose-optional-family-review\\goose-baby-female-blue-idle-vs-hold_ball-check.png",
      "C:\\Users\\fishe\\Documents\\projects\\wevito\\vnext\\artifacts\\visual-review\\20260504-goose-optional-family-review\\goose-baby-female-blue-optional-family-contact-sheet.png"
    ],
    "prop_anchor_metadata": "C:\\Users\\fishe\\Documents\\projects\\wevito\\sprites_runtime\\_metadata\\prop_anchors.json",
    "layout_guide": "C:\\Users\\fishe\\Documents\\projects\\wevito\\vnext\\artifacts\\animation-runs\\20260504-goose-baby-female-blue-hold-ball-pilot\\layout-guide-hold-ball-4.png"
  },
  "jobs": [
    {
      "job_id": "goose-baby-female-blue-hold-ball-gemini-001",
      "provider": "gemini-web",
      "prompt_path": "C:\\Users\\fishe\\Documents\\projects\\wevito\\vnext\\artifacts\\animation-runs\\20260504-goose-baby-female-blue-hold-ball-pilot\\prompts\\hold-ball.md",
      "input_image_paths": [
        "C:\\Users\\fishe\\Documents\\projects\\wevito\\incoming_sprites\\goose-baby.png",
        "C:\\Users\\fishe\\Documents\\projects\\wevito\\vnext\\artifacts\\visual-review\\20260504-goose-optional-family-review\\goose-baby-female-blue-idle-vs-hold_ball-check.png",
        "C:\\Users\\fishe\\Documents\\projects\\wevito\\vnext\\artifacts\\visual-review\\20260504-goose-optional-family-review\\goose-baby-female-blue-optional-family-contact-sheet.png",
        "C:\\Users\\fishe\\Documents\\projects\\wevito\\vnext\\artifacts\\animation-runs\\20260504-goose-baby-female-blue-hold-ball-pilot\\layout-guide-hold-ball-4.png"
      ],
      "expected_geometry": {
        "cells": 4,
        "cell_w": 28,
        "cell_h": 24,
        "columns": 4,
        "rows": 1
      },
      "status": "ready",
      "source": {
        "path": "C:\\Users\\fishe\\Documents\\projects\\wevito\\vnext\\artifacts\\animation-runs\\20260504-goose-baby-female-blue-hold-ball-pilot\\source\\selected-output.png",
        "sha256": "0000000000000000000000000000000000000000000000000000000000000000",
        "captured_at": "2026-05-04T00:00:00-04:00"
      },
      "provenance_note": "Draft Gemini web job. Replace source path, hash, and captured_at after a candidate is selected; do not apply from this placeholder manifest."
    }
  ],
  "import": {
    "candidate_frame_paths": [
      "C:\\Users\\fishe\\Documents\\projects\\wevito\\vnext\\artifacts\\animation-runs\\20260504-goose-baby-female-blue-hold-ball-pilot\\candidate\\hold_ball_00.png",
      "C:\\Users\\fishe\\Documents\\projects\\wevito\\vnext\\artifacts\\animation-runs\\20260504-goose-baby-female-blue-hold-ball-pilot\\candidate\\hold_ball_01.png",
      "C:\\Users\\fishe\\Documents\\projects\\wevito\\vnext\\artifacts\\animation-runs\\20260504-goose-baby-female-blue-hold-ball-pilot\\candidate\\hold_ball_02.png",
      "C:\\Users\\fishe\\Documents\\projects\\wevito\\vnext\\artifacts\\animation-runs\\20260504-goose-baby-female-blue-hold-ball-pilot\\candidate\\hold_ball_03.png"
    ],
    "cleanup_ops": [
      "none"
    ],
    "validation_report_path": "C:\\Users\\fishe\\Documents\\projects\\wevito\\vnext\\artifacts\\animation-runs\\20260504-goose-baby-female-blue-hold-ball-pilot\\validation.json",
    "apply_backup_dir": "C:\\Users\\fishe\\Documents\\projects\\wevito\\vnext\\artifacts\\animation-runs\\20260504-goose-baby-female-blue-hold-ball-pilot\\backup-before-apply"
  },
  "proofs": {
    "contact_sheet_path": "C:\\Users\\fishe\\Documents\\projects\\wevito\\vnext\\artifacts\\animation-runs\\20260504-goose-baby-female-blue-hold-ball-pilot\\qa\\contact-sheet.png",
    "preview_video_path": "C:\\Users\\fishe\\Documents\\projects\\wevito\\vnext\\artifacts\\animation-runs\\20260504-goose-baby-female-blue-hold-ball-pilot\\qa\\preview.gif",
    "packaged_screenshot_proof_path": "C:\\Users\\fishe\\Documents\\projects\\wevito\\vnext\\artifacts\\animation-runs\\20260504-goose-baby-female-blue-hold-ball-pilot\\qa\\packaged-runtime-proof.png",
    "markdown_summary_path": "C:\\Users\\fishe\\Documents\\projects\\wevito\\vnext\\artifacts\\animation-runs\\20260504-goose-baby-female-blue-hold-ball-pilot\\run-summary.md"
  }
}
```

Before use, replace placeholder source fields with the selected candidate's
actual file path, SHA-256 hash, and capture timestamp.

## Required QA Outputs

The first production pilot is not reviewable without:

| Output | Required path pattern |
| --- | --- |
| Candidate contact sheet | `vnext/artifacts/animation-runs/<run_id>/qa/contact-sheet.png` |
| Candidate preview GIF/video | `vnext/artifacts/animation-runs/<run_id>/qa/preview.gif` or `.mp4` |
| Validation report | `vnext/artifacts/animation-runs/<run_id>/validation.json` |
| Packaged/runtime proof | `vnext/artifacts/animation-runs/<run_id>/qa/packaged-runtime-proof.png` |
| Run summary | `vnext/artifacts/animation-runs/<run_id>/run-summary.md` |
| Manifest | `vnext/artifacts/animation-runs/<run_id>/manifest.json` |

## Review Checklist

```text
GOOSE HOLD_BALL PILOT REVIEW

Contract
  [ ] exactly 4 frames
  [ ] transparent candidate frames after import
  [ ] final runtime frames validate as 28x24 PNGs
  [ ] no copied guide marks, labels, or backgrounds

Identity
  [ ] still goose
  [ ] still baby
  [ ] still blue source identity
  [ ] same beak/head/face language
  [ ] same Wevito outline and pixel style

Grip
  [ ] ball sits at beak/front-body contact
  [ ] ball does not float
  [ ] ball does not teleport across the four frames
  [ ] ball scale remains consistent

Motion
  [ ] subtle held endpoint, not pickup/drop/play
  [ ] visible enough variation to avoid idle clone
  [ ] frame 03 loops cleanly to frame 00

Proof
  [ ] manifest validates against docs/wevito-animation-run.schema.json
  [ ] contact sheet reviewed
  [ ] preview reviewed
  [ ] packaged/runtime proof reviewed
  [ ] rollback backup exists before apply
```

## Rollback And Apply Requirements

Do not apply candidate frames unless:

- current `hold_ball_00..03` runtime frames are backed up
- the backup path is recorded in the manifest
- candidate frames pass validation
- contact sheet and preview are accepted
- packaged/runtime proof is accepted
- rollback can restore the exact previous runtime files

Suggested backup path:

```text
C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\animation-runs\20260504-goose-baby-female-blue-hold-ball-pilot\backup-before-apply\
```

## Phase 7 Status

```text
Phase 7: complete
prompt packet target: goose / baby / female / blue / hold_ball
provider prompt prepared: yes
reference attachment list prepared: yes
draft manifest values prepared: yes
QA outputs required: contact sheet, preview, validation, packaged proof, run summary
asset mutation approved: no
generation approved: no
next visual phase: Phase 8 production gate check
```
