# Wevito Animation Generation Contract

Updated: 2026-05-04

This contract defines how a Wevito animation generation or repair run is planned,
recorded, reviewed, and accepted. It is provider-neutral: the same manifest shape
applies whether the visual source came from Gemini web, OpenAI image generation,
manual pixel repair, or local deterministic synthesis.

This document is Phase 1 documentation only. It does not change runtime code,
sprite assets, Godot scripts, vNext C# code, or the Sprite Workflow App.

Related documents:

- [WEVITO_ANIMATION_QA_RUBRIC.md](WEVITO_ANIMATION_QA_RUBRIC.md)
- [wevito-animation-run.schema.json](wevito-animation-run.schema.json)
- [SPRITE_SOURCE_OF_TRUTH.md](SPRITE_SOURCE_OF_TRUTH.md)
- [AUTHORED_ANIMATION_WORKFLOW.md](AUTHORED_ANIMATION_WORKFLOW.md)

```text
Run request
  -> grounded visual job(s)
  -> source capture + hashes
  -> frame import / cleanup
  -> validation report
  -> contact sheet + preview video
  -> packaged runtime proof
  -> accept, repair, or reject
```

## Scope

The contract covers per-family animation work for Wevito's per-frame PNG runtime
tree. Every accepted frame must be a transparent `28x24` PNG in the expected
family sequence.

The current animation family surface is:

| Group | Families |
| --- | --- |
| Base | `idle`, `walk`, `eat`, `happy`, `sad`, `sleep`, `sick`, `bathe` |
| Optional | `drink`, `play_ball`, `hold_ball`, `pickup_ball`, `drop_ball`, `carry_ball_walk`, `carry_ball_run` |

The current variant axes are species, age, gender, and color. Generation runs
should usually ground work from the canonical blue source variant first, then
propagate or verify the six runtime colors through the normal Wevito pipeline.

## Manifest

Every generation or repair run must produce one manifest matching
`docs/wevito-animation-run.schema.json`. The manifest records the target variant,
grounding references, visual-provider jobs, import results, and required QA proof.

Required top-level fields:

| Field | Purpose |
| --- | --- |
| `run_id` | Stable run identifier used in artifact paths and reports. |
| `created_at` | ISO-8601 timestamp for manifest creation. |
| `target` | Species, age, gender, source color, family, frame count, and runtime output paths. |
| `references` | Canonical visual sources, verified runtime comparisons, prop anchor metadata, and layout guide. |
| `jobs` | Provider-neutral visual jobs, prompts, inputs, expected geometry, status, provenance, and hashes. |
| `import` | Candidate frame paths, cleanup operations, validation report, and backup directory. |
| `proofs` | Contact sheet, preview video, packaged runtime screenshot proof, and markdown summary. |

## Target Rules

`target` identifies exactly one animation family for exactly one species, age,
gender, and source color.

Required target fields:

| Field | Rule |
| --- | --- |
| `species` | Must be a known Wevito species. |
| `age` | `baby`, `teen`, or `adult`. |
| `gender` | `female` or `male`. |
| `source_color` | One of the six runtime colors: `blue`, `red`, `orange`, `yellow`, `indigo`, `violet`. |
| `family` | One base or optional family name. |
| `frame_count` | Expected frame count for the family. |
| `runtime_paths` | Ordered final runtime PNG paths, one per expected frame. |

`runtime_paths` are ordered and authoritative. A `drink` run with `frame_count: 4`
must list exactly four paths in frame order.

## Reference Rules

Visual generation must be grounded. Do not accept prompt-only family rows when a
canonical source board or verified runtime frame set exists.

Required reference fields:

| Field | Rule |
| --- | --- |
| `canonical_source_board` | Best available source image or board for the target identity. |
| `verified_runtime_strips` | Existing approved runtime frames, contact sheets, or strips used for comparison. |
| `prop_anchor_metadata` | Anchor/prop metadata or notes used to preserve prop placement. |
| `layout_guide` | The frame-count and slot-layout guide supplied to the visual job. |

Layout guides should make slot count, cell size, centering, and safe padding clear
to the provider. Guides are construction references only. Visible guide marks,
grid lines, labels, or colored backgrounds in output frames are errors.

## Job Rules

Each `jobs[]` item records one visual or manual work unit.

Required job fields:

| Field | Rule |
| --- | --- |
| `job_id` | Stable job identifier within the run. |
| `provider` | `gemini-web`, `openai-imagegen`, `manual`, or `local-synth`. |
| `prompt_path` | Path to the exact prompt or instruction file used for the job. |
| `input_image_paths` | Ordered list of all visual grounding inputs. |
| `expected_geometry` | Expected output layout: cells, cell width, cell height, columns, rows. |
| `status` | `ready`, `blocked`, `sent`, `recorded`, `rejected`, or `applied`. |
| `source` | Selected source image path, SHA-256 hash, and capture timestamp. |
| `provenance_note` | Short human-readable note explaining where the source came from and why it was selected. |

`source` must point at the original selected visual output, not a later decoded,
cleaned, resized, or copied derivative. If a provider returns multiple candidates,
record only the selected candidate as the accepted source and keep rejected
candidates outside the accepted manifest.

## Import Rules

`import` records the deterministic processing after a visual source is selected.

Required import fields:

| Field | Rule |
| --- | --- |
| `candidate_frame_paths` | Ordered imported frames before final acceptance. |
| `cleanup_ops` | Cleanup operations applied to the candidate frames. |
| `validation_report_path` | Machine validation report for the import. |
| `apply_backup_dir` | Backup directory created before overwriting any accepted runtime frame. |

Cleanup must be minimal and auditable. If a repair needs identity-level redrawing,
record a new visual job instead of hiding the change as cleanup.

## Proof Rules

No run is accepted without all QA proof outputs.

Required proof fields:

| Field | Rule |
| --- | --- |
| `contact_sheet_path` | Full contact sheet showing every candidate frame in sequence. |
| `preview_video_path` | Looping preview video or GIF proving motion readability. |
| `packaged_screenshot_proof_path` | Screenshot proof from the packaged/runtime context. |
| `markdown_summary_path` | Human-readable run summary with accept/repair/reject decision. |

Contact sheets and previews are not optional convenience outputs. They are the
visual QA gate that catches identity drift, prop drift, slot bleed, and repeated
static frames that geometry checks cannot understand.

## Hash And Provenance Rules

- Hash every selected provider source image with SHA-256 before import.
- Hashes record provenance; they do not prove quality.
- The hash belongs to the original selected output, not a cropped or cleaned copy.
- Do not overwrite a source file after recording its hash.
- If a source must be re-exported, record it as a new job or new source path.
- Keep `provenance_note` short but specific: provider, source selection reason,
  and any manual acceptance caveat.

## QA Gates

The QA model has two result levels:

| Level | Meaning |
| --- | --- |
| Error | Blocks apply or requires rollback/repair if already applied. |
| Warning | Requires human review, but may be accepted with a documented reason. |

Errors and warnings must be keyed to the Wevito SpriteRepair vocabulary wherever
possible. The current C# enum names are documented in
`SpriteRepairContracts.cs`. Proposed Phase 3 names may appear in reports and docs
as planned vocabulary, but Phase 1 does not add enum values.

See [WEVITO_ANIMATION_QA_RUBRIC.md](WEVITO_ANIMATION_QA_RUBRIC.md) for the full
error and warning rubric.

## Worked Example Manifest

This example records a `goose / baby / female / drink` run for the blue source
variant. Paths are shaped to match the current Wevito repo layout and artifact
conventions.

```json
{
  "run_id": "01HZYWEVITO000000000001",
  "created_at": "2026-05-04T14:30:00-04:00",
  "target": {
    "species": "goose",
    "age": "baby",
    "gender": "female",
    "source_color": "blue",
    "family": "drink",
    "frame_count": 4,
    "runtime_paths": [
      "C:\\Users\\fishe\\Documents\\projects\\wevito\\sprites_runtime\\goose\\baby\\female\\blue\\drink_00.png",
      "C:\\Users\\fishe\\Documents\\projects\\wevito\\sprites_runtime\\goose\\baby\\female\\blue\\drink_01.png",
      "C:\\Users\\fishe\\Documents\\projects\\wevito\\sprites_runtime\\goose\\baby\\female\\blue\\drink_02.png",
      "C:\\Users\\fishe\\Documents\\projects\\wevito\\sprites_runtime\\goose\\baby\\female\\blue\\drink_03.png"
    ]
  },
  "references": {
    "canonical_source_board": "C:\\Users\\fishe\\Documents\\projects\\wevito\\incoming_sprites\\web_gemini_focused_boards\\goose\\baby\\female\\drink\\goose-baby-female-drink-focused-board.png",
    "verified_runtime_strips": [
      "C:\\Users\\fishe\\Documents\\projects\\wevito\\vnext\\artifacts\\workflow-runs\\20260503-optional-drink-packaged-runtime-rerun\\optional-goose-baby-female-drink-runtime-contact-sheet.png"
    ],
    "prop_anchor_metadata": "C:\\Users\\fishe\\Documents\\projects\\wevito\\sprites_runtime\\_metadata\\prop_anchors.json",
    "layout_guide": "C:\\Users\\fishe\\Documents\\projects\\wevito\\vnext\\artifacts\\animation-runs\\01HZYWEVITO000000000001\\layout-guide-drink-4.png"
  },
  "jobs": [
    {
      "job_id": "goose-baby-female-drink-gemini-001",
      "provider": "gemini-web",
      "prompt_path": "C:\\Users\\fishe\\Documents\\projects\\wevito\\vnext\\artifacts\\animation-runs\\01HZYWEVITO000000000001\\prompts\\drink.md",
      "input_image_paths": [
        "C:\\Users\\fishe\\Documents\\projects\\wevito\\incoming_sprites\\web_gemini_focused_boards\\goose\\baby\\female\\drink\\goose-baby-female-drink-focused-board.png",
        "C:\\Users\\fishe\\Documents\\projects\\wevito\\vnext\\artifacts\\animation-runs\\01HZYWEVITO000000000001\\layout-guide-drink-4.png"
      ],
      "expected_geometry": {
        "cells": 4,
        "cell_w": 28,
        "cell_h": 24,
        "columns": 4,
        "rows": 1
      },
      "status": "applied",
      "source": {
        "path": "C:\\Users\\fishe\\Documents\\projects\\wevito\\incoming_sprites\\web_gemini_focused_boards\\goose\\baby\\female\\drink\\selected-output.png",
        "sha256": "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef",
        "captured_at": "2026-05-04T14:42:00-04:00"
      },
      "provenance_note": "Gemini web output selected because identity, bowl contact, and 4-frame slot layout matched the blue source variant."
    }
  ],
  "import": {
    "candidate_frame_paths": [
      "C:\\Users\\fishe\\Documents\\projects\\wevito\\vnext\\artifacts\\animation-runs\\01HZYWEVITO000000000001\\candidate\\drink_00.png",
      "C:\\Users\\fishe\\Documents\\projects\\wevito\\vnext\\artifacts\\animation-runs\\01HZYWEVITO000000000001\\candidate\\drink_01.png",
      "C:\\Users\\fishe\\Documents\\projects\\wevito\\vnext\\artifacts\\animation-runs\\01HZYWEVITO000000000001\\candidate\\drink_02.png",
      "C:\\Users\\fishe\\Documents\\projects\\wevito\\vnext\\artifacts\\animation-runs\\01HZYWEVITO000000000001\\candidate\\drink_03.png"
    ],
    "cleanup_ops": [
      "matte_cleanup",
      "color_propagation"
    ],
    "validation_report_path": "C:\\Users\\fishe\\Documents\\projects\\wevito\\vnext\\artifacts\\animation-runs\\01HZYWEVITO000000000001\\validation.json",
    "apply_backup_dir": "C:\\Users\\fishe\\Documents\\projects\\wevito\\vnext\\artifacts\\animation-runs\\01HZYWEVITO000000000001\\backup-before-apply"
  },
  "proofs": {
    "contact_sheet_path": "C:\\Users\\fishe\\Documents\\projects\\wevito\\vnext\\artifacts\\animation-runs\\01HZYWEVITO000000000001\\qa\\contact-sheet.png",
    "preview_video_path": "C:\\Users\\fishe\\Documents\\projects\\wevito\\vnext\\artifacts\\animation-runs\\01HZYWEVITO000000000001\\qa\\preview.mp4",
    "packaged_screenshot_proof_path": "C:\\Users\\fishe\\Documents\\projects\\wevito\\vnext\\artifacts\\animation-runs\\01HZYWEVITO000000000001\\qa\\packaged-runtime-proof.png",
    "markdown_summary_path": "C:\\Users\\fishe\\Documents\\projects\\wevito\\vnext\\artifacts\\animation-runs\\01HZYWEVITO000000000001\\run-summary.md"
  }
}
```

## Acceptance Rule

A run is accepted only when the manifest is complete, the schema validates, all
error gates are clean, warnings have explicit review notes, the contact sheet and
preview are inspected, and the packaged runtime proof shows the intended animation
from the real runtime path.
