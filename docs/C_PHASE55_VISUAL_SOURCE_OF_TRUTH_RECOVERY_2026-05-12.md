# C-PHASE 55 Visual Source-Of-Truth Recovery

Date: 2026-05-12

Branch: `claude-implementation/c-phase-55-visual-source-of-truth`

## Purpose

Produce a deterministic, report-only visual source-of-truth map before any future sprite mutation, generation, import, or optional animation expansion.

## Source-Of-Truth Shape

```text
visual source of truth
|
+-- runtimeRequired
|   +-- sprites_runtime
|   +-- required families
|   `-- current runtime contract audit
|
+-- authoredVerified
|   +-- sprites_authored_verified
|   +-- locomotion/care/expression coverage
|   `-- missing authored-source queues
|
+-- sourceBoards
|   +-- incoming_sprites
|   +-- incoming_animal_pose_manifest.json
|   `-- supporting source inputs
|
+-- optionalFamilies
|   +-- optional_animation_families.json
|   +-- runtime optional files
|   +-- authored optional files
|   `-- prop anchor metadata presence
|
`-- priorityQueues
    +-- missing_authored_care
    +-- missing_authored_expression
    +-- optional_fallback_only
    +-- runtime_only_optional
    `-- needs_visual_review
```

## New Tool

Created:

```text
tools/report_visual_source_of_truth.py
```

The script is read-only. It composes existing audit logic from:

```text
tools/audit_sprite_contract.py
tools/report_authored_sprite_coverage.py
tools/audit_optional_animation_readiness.py
tools/report_runtime_canvas_mismatches.py
```

It writes:

```text
vnext/artifacts/c-phase-55-visual-source-of-truth/visual-source-of-truth.json
vnext/artifacts/c-phase-55-visual-source-of-truth/visual-source-of-truth.md
```

## Report Outputs

Generated artifact root:

```text
vnext/artifacts/c-phase-55-visual-source-of-truth/
```

Files generated during validation:

```text
visual-source-of-truth.json
visual-source-of-truth.md
runtime-canvas.json
runtime-canvas.md
sprite-contract.json
```

## High-Level Findings

| Area | Result |
| --- | ---: |
| Runtime variant dirs | 360 / 360 |
| Runtime frames | 10818 / 10800 |
| Runtime contract errors | 0 |
| Source boards | 30 / 30 |
| Supporting source inputs | 17 / 17 |
| Authored complete variants | 0 / 360 |
| Authored incomplete variants | 360 |
| Optional targets | 2520 |
| Optional fallback-only | 2516 |
| Runtime-only optional | 4 |
| Invalid optional art | 0 |

Interpretation:

- Runtime assets are structurally healthy.
- Incoming source boards and supporting inputs are present.
- The authored-verified source lane is the main long-term recovery gap.
- Optional animation art is safe but mostly fallback-only.
- Four optional rows are runtime-only and need source/provenance reconciliation before broad expansion.

## Priority Queue Counts

| Queue | Count | Meaning |
| --- | ---: | --- |
| `missing_authored_care` | 360 | Every variant is missing authored care frames in `sprites_authored_verified`. |
| `missing_authored_expression` | 360 | Every variant is missing authored expression frames in `sprites_authored_verified`. |
| `optional_fallback_only` | 2516 | Optional targets rely on runtime fallbacks rather than authored optional frames. |
| `runtime_only_optional` | 4 | Optional runtime rows exist without matching authored optional source. |
| `needs_visual_review` | 364 | Authored incomplete variants plus runtime-only optional rows. |

## Authored Family Coverage

| Family | Complete | Incomplete | Main missing frames |
| --- | ---: | ---: | --- |
| `locomotion` | 264 | 96 | `walk_00..05` for 96 variants, plus idle gaps for 90 variants |
| `locomotion_idle` | 270 | 90 | `idle_00..03` |
| `locomotion_walk_a` | 264 | 96 | `walk_00..02` |
| `locomotion_walk_b` | 264 | 96 | `walk_03..05` |
| `care` | 0 | 360 | `eat_00..03`, `sleep_00..01` |
| `expression` | 0 | 360 | `happy`, `sad`, `sick`, `bathe` frames |

Recommended first authored-source recovery focus:

```text
1. care authored coverage
2. expression authored coverage
3. remaining locomotion gaps
4. runtime-only optional provenance reconciliation
5. optional fallback-only production queue
```

## Optional Family Counts

| Family | Status |
| --- | --- |
| `carry_ball_run` | 360 fallback-only |
| `carry_ball_walk` | 360 fallback-only |
| `drink` | 360 fallback-only |
| `drop_ball` | 359 fallback-only, 1 runtime-only complete |
| `hold_ball` | 359 fallback-only, 1 runtime-only complete |
| `pickup_ball` | 359 fallback-only, 1 runtime-only complete |
| `play_ball` | 359 fallback-only, 1 runtime-only complete |

Runtime-only optional targets:

```text
goose|baby|female|blue|drop_ball
goose|baby|female|blue|hold_ball
goose|baby|female|blue|pickup_ball
goose|baby|female|blue|play_ball
```

These should not be treated as a scalable source-of-truth pattern yet. They are useful pilot evidence, but future expansion should reconcile authored source, runtime rows, manifests, hashes, and proof packets before applying more optional art.

## Recommended First Visual Review Queue

```text
first visual review queue
|
+-- authored care recovery
|   `-- all species / ages / genders / colors
|
+-- authored expression recovery
|   `-- all species / ages / genders / colors
|
+-- authored locomotion gap recovery
|   `-- 96 variants with missing walk frames, 90 with missing idle frames
|
+-- goose baby female blue optional provenance
|   `-- drop_ball, hold_ball, pickup_ball, play_ball
|
`-- optional action pilot continuation
    `-- use C-PHASE 56 only after this report is reviewed
```

For C-PHASE 56, the report supports starting with the plan's scoped pilot target instead of broad mutation:

```text
goose / baby / female / blue / carry_ball_walk
```

## Validation

| Command | Result |
| --- | --- |
| `python .\tools\report_visual_source_of_truth.py --output-root .\vnext\artifacts\c-phase-55-visual-source-of-truth` | PASS |
| `python .\tools\report_runtime_canvas_mismatches.py --output .\vnext\artifacts\c-phase-55-visual-source-of-truth\runtime-canvas.json --markdown .\vnext\artifacts\c-phase-55-visual-source-of-truth\runtime-canvas.md --fail-on-mismatch` | PASS |
| `python .\tools\audit_sprite_contract.py --output .\vnext\artifacts\c-phase-55-visual-source-of-truth\sprite-contract.json` | PASS |
| `dotnet build .\vnext\Wevito.VNext.sln` | PASS |
| `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build` | PASS, 280 / 280 |

## Mutation Statement

No PNGs, source boards, runtime folders, prop anchors, content manifests, generated art, imported art, or release assets were modified.

Changed files:

```text
tools/report_visual_source_of_truth.py
docs/C_PHASE55_VISUAL_SOURCE_OF_TRUTH_RECOVERY_2026-05-12.md
```

Generated ignored artifacts:

```text
vnext/artifacts/c-phase-55-visual-source-of-truth/
```

## Next Phase

C-PHASE 56 can use this report's `optional_fallback_only` queue, but it must remain a separate branch and should start with one scoped pilot target. No visual mutation is authorized by this report alone.
