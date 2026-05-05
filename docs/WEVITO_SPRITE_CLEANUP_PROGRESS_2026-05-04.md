# Wevito Sprite Cleanup Progress

Updated: 2026-05-04

This is the running progress note for the current visual-side sprite cleanup
campaign. It records what was actually changed, what was classified as not safe
to scrub, and what remains in the queue.

## Current Shape

```text
cleanup campaign
  |
  +-- fixed
  |     +-- medicine icon
  |     +-- syringe
  |     +-- blanket mat
  |     +-- water bowl
  |     +-- habitat prop manual replacements
  |     +-- shared UI icons
  |     +-- shared status icons
  |     +-- shared item assets
  |     +-- shared portraits crop cleanup
  |     +-- shared environment boards
  |     +-- tight-cropped moon sprites
  |     +-- pet runtime tiny-speck cleanup
  |     +-- pet runtime raccoon drink bar cleanup
  |     +-- source shared asset durability sync
  |     +-- all-animal color variant coverage atlas
  |
  +-- classified, do not scrub automatically
  |     +-- rat drink_01 examples
  |     +-- pigeon adult examples
  |
  +-- deferred
        +-- optional-animation apply/proof after accepted expansion review
        +-- targeted animation/family polish only when a specific visual problem is identified
```

## Applied Fixes

| Asset | Fix | Audit result |
| --- | --- | --- |
| `sprites_shared_runtime/icons/medicine.png` | Removed detached components and added transparent breathing room. | One alpha component, no detached pixels, 2px minimum margin. |
| `sprites_shared_runtime/items/care/syringe.png` | Removed two detached pixels. | One alpha component, no detached pixels, 2px minimum margin. |
| `sprites_shared_runtime/items/toys_b/blanket_mat.png` | Restored from existing non-empty source at `sprites/items/toys_b/blanket_mat.png`. | Visible 96x54 asset, one alpha component, no detached pixels. |
| `sprites_shared_runtime/items/containers/water_bowl.png` | Applied safe cleaned source proposal. | Clean 123x51 bowl, one alpha component, no detached pixels. |
| `sprites_shared_runtime/items/toys_b/hay_bed.png` | Applied manual clean pixel-style replacement, then revised to remove detached straw pixels. | Clean 132x76 bed, one alpha component, no detached pixels. |
| `sprites_shared_runtime/items/toys_b/log_shelter.png` | Applied manual clean pixel-style replacement. | Clean 132x86 shelter, one alpha component, no detached pixels. |
| `sprites_shared_runtime/items/toys_b/nest_bed.png` | Applied manual clean pixel-style replacement. | Clean 124x82 nest, one alpha component, no detached pixels. |
| `sprites_shared_runtime/items/toys_b/moss_bed.png` | Applied manual clean pixel-style replacement. | Clean 124x72 moss bed, one alpha component, no detached pixels. |
| `sprites_shared_runtime/items/toys_b/rock_basking_spot.png` | Applied manual clean pixel-style replacement. | Clean 104x58 rock, one alpha component, no detached pixels. |
| `sprites_shared_runtime/items/containers/pond_dish.png` | Applied manual clean pixel-style replacement after automated cleanup was rejected. | Clean 126x72 pond dish, one alpha component, no detached pixels. |
| `sprites_shared_runtime/icons/*.png` | Applied clean hand-drawn pixel replacements for 21 shared UI icons. | All targeted icons audit with zero detached pixels and preserved UI dimensions. |
| `sprites_shared_runtime/status/*.png` | Applied clean centered replacements for eight large contaminated status icons. | All targeted status icons audit with zero detached pixels, zero low-alpha specks, and preserved canvas dimensions. |
| `sprites_shared_runtime/items/**` | Applied clean semantic replacements for 61 remaining flagged shared item/care/container/toy/utility assets, excluding the accepted ball overlay and already-cleaned assets. | All Phase 9 targets pass the targeted cleanup gate. |
| `sprites_shared_runtime/portraits/**` | Cleaned 420 flagged 48x48 portrait crops by removing tiny detached crop/noise components and refitting artwork into the original canvas. | All portraits pass the crop/no-low-alpha gate; remaining detached counts are reviewed as silhouette parts. |
| `sprites_shared_runtime/environment/*.png` | Rebuilt 12 flagged environment boards as clean connected habitat strips. | All Phase 11 targets pass the targeted cleanup gate. |
| `sprites_shared_runtime/celestial/moon_01.png`, `moon_02.png`, `moon_03.png`, `moon_06.png`, `moon_07.png` | Refit tight-cropped moon art into 24x24 canvases with 2px margin. | All moon crop flags cleared. |
| `sprites_runtime/**/*.png` safe tiny-noise set | Removed tiny detached specks from 258 pet runtime frames. | Zero safe tiny-noise candidates remain across 23,040 runtime frames. |
| `sprites_runtime/raccoon/*/male/*/drink_00..03.png` bar set | Removed thin detached top bars from 48 raccoon male drink frames. | Zero thin top-bar candidates remain. |
| `sprites_runtime/**/*.png` substantial component flags | Reviewed and classified the remaining 642 substantial detached-component frames. | Resolved as intentional body/action/prop silhouette exceptions; no additional automatic cleanup recommended. |
| `sprites/celestial`, `sprites/environment`, `sprites/icons`, `sprites/items`, `sprites/status` flagged source shared assets | Backed up dirty source-side shared assets and synced 94 matching files from already-cleaned `sprites_shared_runtime` counterparts. | Priority source shared flags dropped from 96 to 4 reviewed tight-but-clean source shapes; runtime files unchanged. |

## Habitat Prop Classification

Recorded in:

```text
docs/WEVITO_HABITAT_PROP_CLEANUP_CLASSIFICATION_2026-05-04.md
```

Current habitat decision:

```text
fixed
  -> blanket_mat
  -> water_bowl
  -> hay_bed
  -> log_shelter
  -> nest_bed
  -> moss_bed
  -> rock_basking_spot
  -> pond_dish

still deferred
  -> none in this habitat packet
```

## Backup / Proof Artifacts

```text
vnext/artifacts/visual-review/20260504-shared-care-icon-cleanup-phase1/
  +-- backup-before-cleanup/
  +-- proposed/
  +-- qa/phase1-before-after-sheet.png
  +-- manifest.json
  +-- phase1-summary.md

vnext/artifacts/visual-review/20260504-blanket-mat-restore-phase2/
  +-- backup-before-restore/
  +-- source-copy/
  +-- qa/blanket-mat-before-after-sheet.png
  +-- manifest.json
  +-- phase2-summary.md

vnext/artifacts/visual-review/20260504-habitat-rest-container-cleanup-phase3/
  +-- proposed/
  +-- backup-before-apply/water_bowl.png
  +-- qa/habitat-rest-container-proposed-cleanup-sheet.png
  +-- qa/water-bowl-before-after-applied.png
  +-- phase3-apply-water-bowl.json
  +-- phase3-apply-summary.md

vnext/artifacts/visual-review/20260504-pet-runtime-noise-phase4/
  +-- pet-runtime-component-classification-sheet.png
  +-- classification.json

vnext/artifacts/visual-review/20260504-sprite-cleanup-final-audit/
  +-- targeted-final-audit.md
  +-- targeted-final-audit.json

vnext/artifacts/visual-review/20260504-habitat-prop-manual-cleanup-phase6/
  +-- backup-before-cleanup/
  +-- cleaned-copies/
  +-- qa/habitat-prop-manual-cleanup-before-after.png
  +-- manifest.json
  +-- phase6-summary.md
  +-- hay-bed-revision.json

vnext/artifacts/visual-review/20260504-shared-icon-cleanup-phase7/
  +-- backup-before-cleanup/
  +-- cleaned-copies/
  +-- qa/shared-icon-cleanup-before-after.png
  +-- manifest.json
  +-- phase7-summary.md

vnext/artifacts/visual-review/20260504-status-icon-cleanup-phase8/
  +-- backup-before-cleanup/
  +-- cleaned-copies/
  +-- qa/status-icon-cleanup-before-after.png
  +-- manifest.json
  +-- hungry-revision.json
  +-- phase8-summary.md

vnext/artifacts/visual-review/20260504-shared-item-cleanup-phase9/
  +-- backup-before-cleanup/
  +-- cleaned-copies/
  +-- qa/shared-item-cleanup-before-after-page-01.png
  +-- qa/shared-item-cleanup-before-after-page-02.png
  +-- qa/shared-item-cleanup-before-after-page-03.png
  +-- manifest.json
  +-- phase9-targeted-audit.json
  +-- phase9-targeted-audit.md

vnext/artifacts/visual-review/20260505-portrait-cleanup-phase10/
  +-- backup-before-cleanup/
  +-- cleaned-copies/
  +-- qa/portrait-cleanup-before-after-page-01.png
  +-- manifest.json
  +-- phase10-targeted-audit.json
  +-- phase10-targeted-audit.md

vnext/artifacts/visual-review/20260505-environment-board-cleanup-phase11/
  +-- backup-before-cleanup/
  +-- cleaned-copies/
  +-- qa/environment-board-cleanup-before-after.png
  +-- manifest.json
  +-- phase11-targeted-audit.json

vnext/artifacts/visual-review/20260505-celestial-crop-cleanup-phase12/
  +-- backup-before-cleanup/
  +-- cleaned-copies/
  +-- qa/celestial-crop-cleanup-before-after.png
  +-- manifest.json

vnext/artifacts/visual-review/20260505-shared-runtime-final-post-phase12-audit/
  +-- shared-runtime-final-post-phase12-audit.md
  +-- shared-runtime-final-post-phase12-audit.json
  +-- remaining-portrait-flags-review-sheet.png

vnext/artifacts/visual-review/20260505-pet-runtime-source-aware-cleanup/
  +-- pet-runtime-source-aware-audit.md
  +-- pet-runtime-source-aware-audit.json
  +-- backup-before-cleanup/
  +-- cleaned-copies/
  +-- pet-runtime-tiny-cleanup-manifest.json
  +-- pet-runtime-tiny-cleanup-summary.md
  +-- backup-before-bar-cleanup/
  +-- bar-cleaned-copies/
  +-- pet-runtime-bar-cleanup-manifest.json
  +-- pet-runtime-bar-cleanup-summary.md
  +-- pet-runtime-final-audit.md
  +-- pet-runtime-final-audit.json
  +-- qa/pet-runtime-final-substantial-detached-review-sheet.png
  +-- substantial-flag-classification/

vnext/artifacts/visual-review/20260505-source-shared-cleanup-audit/
  +-- source-shared-cleanup-audit.md
  +-- source-shared-cleanup-audit.json
  +-- qa/source-shared-all-flags.png
  +-- qa/source-shared-priority-flags.png

vnext/artifacts/visual-review/20260505-source-shared-runtime-sync-cleanup/
  +-- backup-before-sync/
  +-- runtime-source-copies/
  +-- qa/source-shared-sync-before-after.png
  +-- manifest.json
  +-- run-summary.md
  +-- post-sync-audit.md
  +-- post-sync-audit.json
  +-- qa/source-shared-sync-post-remaining-flags.png
```

## Deferred / Not Applied

| Asset or group | Reason |
| --- | --- |
| `rat / baby / female|male / blue / drink_01` | Large detached component appears to be body/interaction structure, not confirmed junk. |
| `pigeon / adult / blue examples` | Red/amber overlays correspond to feet, outline, anti-aliasing, or silhouette detail; not safe to scrub. |

## Current Audit Result

The targeted final audit marks these as fixed/restored:

```text
sprites_shared_runtime/icons/medicine.png
sprites_shared_runtime/items/care/syringe.png
sprites_shared_runtime/items/toys_b/blanket_mat.png
sprites_shared_runtime/items/containers/water_bowl.png
sprites_shared_runtime/items/toys_b/hay_bed.png
sprites_shared_runtime/items/toys_b/log_shelter.png
sprites_shared_runtime/items/toys_b/nest_bed.png
sprites_shared_runtime/items/toys_b/moss_bed.png
sprites_shared_runtime/items/toys_b/rock_basking_spot.png
sprites_shared_runtime/items/containers/pond_dish.png
sprites_shared_runtime/icons/*.png
sprites_shared_runtime/status/*.png
sprites_shared_runtime/items/** flagged Phase 9 set
sprites_shared_runtime/portraits/** flagged Phase 10 set
sprites_shared_runtime/environment/*.png
sprites_shared_runtime/celestial/moon_01.png
sprites_shared_runtime/celestial/moon_02.png
sprites_shared_runtime/celestial/moon_03.png
sprites_shared_runtime/celestial/moon_06.png
sprites_shared_runtime/celestial/moon_07.png
sprites_runtime/**/*.png safe tiny-noise set
sprites_runtime/raccoon/*/male/*/drink_00..03.png bar set
```

No goose hold-ball endpoint files were touched during cleanup. The accepted
`goose / baby / female / blue / hold_ball` row remains protected.

## Re-Audit Result

Re-audit artifacts:

```text
vnext/artifacts/visual-review/20260504-sprite-cleanup-reaudit/
  +-- sprite-cleanup-reaudit-sheet.png
  +-- sprite-cleanup-reaudit.md
  +-- sprite-cleanup-reaudit.json
```

Result:

```text
fixed assets passing targeted checks: 4 / 4
```

Verified fixed/restored assets:

| Asset | Current result |
| --- | --- |
| `sprites_shared_runtime/icons/medicine.png` | Visible, one component, zero detached pixels, safe margin. |
| `sprites_shared_runtime/items/care/syringe.png` | Visible, one component, zero detached pixels, safe margin. |
| `sprites_shared_runtime/items/toys_b/blanket_mat.png` | Visible, one component, zero detached pixels, safe margin. |
| `sprites_shared_runtime/items/containers/water_bowl.png` | Visible, one component, zero detached pixels, safe margin. |

Remaining re-audit flags are not currently confirmed visual errors:

| Asset/group | Re-audit status |
| --- | --- |
| rat `drink_01` examples | Components are part of body/interaction structure; do not scrub. |
| pigeon adult examples | Edge/anti-alias/feet/silhouette details; do not scrub automatically. |

## Final Shared Runtime Audit

Final shared-runtime audit artifacts:

```text
vnext/artifacts/visual-review/20260505-shared-runtime-final-post-phase12-audit/
  +-- shared-runtime-final-post-phase12-audit.md
  +-- shared-runtime-final-post-phase12-audit.json
  +-- remaining-portrait-flags-review-sheet.png
```

Result:

```text
checked shared runtime images: 554
remaining audit flags: 32
remaining visible dirt/crop errors after review: 0
```

Remaining flags are reviewed portrait silhouette/highlight cases, not confirmed
noise:

```text
remaining portrait flag classes
  |
  +-- substantial detached silhouette parts
  |     +-- tails
  |     +-- feet
  |     +-- antlers
  |
  +-- tiny pale highlight pixels
        +-- color/detail highlights inside otherwise clean portraits
```

The review sheet is:

```text
vnext/artifacts/visual-review/20260505-shared-runtime-final-post-phase12-audit/remaining-portrait-flags-review-sheet.png
```

## Final Pet Runtime Audit

Pet runtime cleanup report:

```text
docs/WEVITO_PET_RUNTIME_VISUAL_CLEANUP_FINAL_2026-05-05.md
```

Final pet-runtime artifacts:

```text
vnext/artifacts/visual-review/20260505-pet-runtime-source-aware-cleanup/
  +-- pet-runtime-final-audit.md
  +-- pet-runtime-final-audit.json
  +-- qa/pet-runtime-final-substantial-detached-review-sheet.png
```

Result:

```text
checked runtime frames: 23,040
unique runtime frames changed: 306
confirmed dirty pixels removed: 5,142
canvas dimensions changed: 0
safe tiny-noise candidates remaining: 0
thin top-bar candidates remaining: 0
substantial detached frames reviewed and resolved: 642
unresolved runtime cleanup flags: 0
```

The 642 substantial detached-component frames are not automatic cleanup targets.
Review shows they include body/action structure such as tails, feet, antlers,
crouch separation, and prop/action silhouettes.

Resolution doc:

```text
docs/WEVITO_PET_RUNTIME_SUBSTANTIAL_FLAGS_RESOLVED_2026-05-05.md
```

## Optional Expansion Review Packet

After cleanup, the first post-hold endpoint optional expansion review packet was
prepared for:

```text
goose / baby / female / blue
  |
  +-- pickup_ball
  +-- drop_ball
  +-- carry_ball_walk
  +-- carry_ball_run
```

Artifacts:

```text
vnext/artifacts/animation-runs/20260505-goose-baby-female-blue-optional-expansion-review/
  +-- manifest.json
  +-- run-summary.md
  +-- candidate-frames/
  +-- qa/optional-expansion-candidate-review-sheet.png
  +-- qa/pickup_ball-candidate-preview.gif
  +-- qa/drop_ball-candidate-preview.gif
  +-- qa/carry_ball_walk-candidate-preview.gif
  +-- qa/carry_ball_run-candidate-preview.gif

vnext/artifacts/animation-runs/20260505-goose-baby-female-blue-drop-ball-candidate/
  +-- manifest.json
  +-- run-summary.md
  +-- candidate-frames/
  +-- qa/drop-ball-current-vs-candidate-contact-sheet.png
  +-- qa/drop-ball-candidate-preview.gif
```

Status:

```text
runtime mutation: no
pickup_ball: current row looks proof-ready
drop_ball: review-only candidate fixes current partial-slice row
carry_ball_walk: current row looks proof-ready
carry_ball_run: current row looks proof-ready
ball policy: runtime overlay only, not baked into frames
```

User decision recorded on 2026-05-05:

```text
accept_optional_expansion_review_for_apply_plan
```

Code-side handoff:

```text
docs/WEVITO_OPTIONAL_EXPANSION_APPLY_PROOF_HANDOFF_2026-05-05.md
```

The handoff authorizes planning for only one runtime mutation set:

```text
sprites_runtime/goose/baby/female/blue/drop_ball_00.png
sprites_runtime/goose/baby/female/blue/drop_ball_01.png
sprites_runtime/goose/baby/female/blue/drop_ball_02.png
sprites_runtime/goose/baby/female/blue/drop_ball_03.png
```

`pickup_ball`, `carry_ball_walk`, and `carry_ball_run` are proof-only current
runtime rows for this step. The ball remains a runtime overlay only.

## Goose Six-Color Preflight

Non-mutating preflight doc:

```text
docs/WEVITO_GOOSE_BABY_FEMALE_SIX_COLOR_PREFLIGHT_2026-05-05.md
```

Artifact packet:

```text
vnext/artifacts/visual-review/20260505-goose-baby-female-six-color-preflight/
  +-- manifest.json
  +-- run-summary.md
  +-- qa/goose-baby-female-six-color-core-runtime-sheet.png
  +-- qa/goose-baby-female-six-color-optional-runtime-sheet.png
  +-- qa/blue-drop-ball-current-vs-pending-candidate.png
```

Result:

```text
runtime/source mutation: no
six color folders present: yes
reviewed runtime rows present: yes
mixed-canvas warnings: 24
color propagation approved: no
```

The mixed-canvas warnings are planning context only. They should not be treated
as an instruction to shrink or crop goose motion.

## Goose Ball Overlay Preflight

Non-mutating overlay preflight doc:

```text
docs/WEVITO_GOOSE_BABY_FEMALE_BALL_OVERLAY_PREFLIGHT_2026-05-05.md
```

Artifact packet:

```text
vnext/artifacts/visual-review/20260505-goose-baby-female-ball-overlay-preflight/
  +-- manifest.json
  +-- run-summary.md
  +-- qa/goose-baby-female-six-color-pickup_ball-offline-ball-overlay.png
  +-- qa/goose-baby-female-six-color-drop_ball-offline-ball-overlay.png
  +-- qa/goose-baby-female-six-color-hold_ball-offline-ball-overlay.png
  +-- qa/goose-baby-female-six-color-carry_ball_walk-offline-ball-overlay.png
  +-- qa/goose-baby-female-six-color-carry_ball_run-offline-ball-overlay.png
  +-- qa/blue-drop-ball-current-vs-pending-candidate-offline-ball-overlay.png
```

Result:

```text
runtime/source mutation: no
ball baked into PNGs: no
metadata source: sprites_runtime/_metadata/prop_anchors.json
ball source: sprites_shared_runtime/items/toys_a/ball.png
proof type: offline visual approximation, not packaged Godot proof
```

The overlay packet is for visual planning only. Code-side still owns the real
Godot apply/proof/rollback for blue `drop_ball`.

## Source Shared Asset Durability Sync

Source cleanup doc:

```text
docs/WEVITO_SOURCE_SHARED_ASSET_CLEANUP_2026-05-05.md
```

Result:

```text
source shared PNGs audited: 559
priority non-portrait source flags before sync: 96
source shared files synced from cleaned runtime counterparts: 94
remaining post-sync flags: 4
runtime files changed: 0
protected goose hold/drop rows changed: 0
```

Remaining post-sync flags are tight-but-clean source shapes:

```text
sprites/icons/exercise.png
sprites/icons/memoriam.png
sprites/icons/water.png
sprites/egg/egg_04.png
```

They are not confirmed dirt and should not be redrawn without a specific UI
problem.

## All-Animal Color Variant Coverage

Coverage doc:

```text
docs/WEVITO_ALL_ANIMAL_COLOR_VARIANT_COVERAGE_2026-05-05.md
```

Artifact packet:

```text
vnext/artifacts/visual-review/20260505-all-animal-color-variant-coverage/
  +-- color-variant-coverage.json
  +-- color-variant-coverage.md
  +-- qa/
```

Result:

```text
runtime/source mutation: no
expected runtime color folders: 360
actual runtime color folders: 360
missing runtime color folders: 0
frame count errors: 0
portrait variants in sprites_shared_runtime: 420 / 420
portrait variants in sprites: 420 / 420
QA sheets generated: 130
```

Conclusion: the six color variants already exist structurally for all animals.
The next color-variant task is visual quality review, not creating missing
folders.

Follow-up legacy folder fill:

```text
vnext/artifacts/visual-review/20260505-legacy-sprites-rat-orange-male-fill/
  +-- manifest.json
  +-- run-summary.md
```

Result:

```text
sprites_runtime:            360 / 360 color folders
sprites_authored:           360 / 360 color folders
sprites_authored_verified:  360 / 360 color folders
sprites:                    360 / 360 color folders
```

The only missing legacy folders were:

```text
sprites/rat/baby/male/orange
sprites/rat/teen/male/orange
sprites/rat/adult/male/orange
```

They were filled from the matching `sprites_authored` rows. Runtime files were
not touched.

## Code-Side Readiness Reconciliation

Latest reconciliation doc:

```text
docs/VISUAL_SIDE_CODE_READINESS_RECONCILIATION_2026-05-05.md
```

Current code-side status from the handoff:

```text
PET TASKS: live report-only helper surface
spriteAudit: non-mutating markdown/JSON report mode
PET TASKS contact sheets: deferred
vNext tests: 79 / 79 pass in code-side worktree
action/tool probe: passed in code-side worktree
safe build rule: use -SkipAssetPrep
```

Do not use PET TASKS for mutation/import/generation/proof/apply work. The
manual `goose / baby / female / blue / drop_ball` apply/proof remains
code-side-owned and outside PET TASKS.

## Next Cleanup Queue

```text
next safest work
  |
  +-- code-side apply/proof plan for accepted expansion review
  |     +-- only if user chooses accept_optional_expansion_review_for_apply_plan
  |
  +-- targeted animation/family polish
        +-- only when a specific visual problem is identified
        +-- compare adjacent frames before mutation
        +-- do not broad-scrub body/animation frames
  |
  +-- no-edit proof only
        +-- if a clean source can be identified
        +-- otherwise mark remap/replace instead of destructive cleanup
```

Do not run broad automatic cleanup across shared props or pet runtime frames.
The first attempted rest/container proposal proved that the broad cleaner can
damage some assets.

## Cleanup Campaign Status

```text
confirmed dirty assets from first packet
  -> fixed/restored

ambiguous high-count audit flags
  -> classified, not blindly fixed

remaining visual debt
  -> optional-animation apply/proof after accepted expansion review
  -> targeted animation/family polish only when specific visual problems are identified
```
