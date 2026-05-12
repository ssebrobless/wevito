# C-PHASE 47 Visual-Side Integration Sync

Date: 2026-05-12

Branch: `claude-implementation/c-phase-47-visual-side-integration-sync`

## Decision

```text
visual/code integration state
|
+-- visual-side source-of-truth reviewed
|   +-- broad pet/shared cleanup complete
|   +-- color coverage/quality queue empty
|   +-- care/object art pool mostly clean
|   +-- habitat placement plan exists
|   `-- mutation/generation remains gated
|
+-- code-side phases since tracker
|   +-- RC3 manual QA packet prepared
|   +-- overlay stale-position recovery implemented
|   +-- PET TASKS wording clarified
|   +-- care/item/habitat mapping partially implemented
|   +-- screenshot/capture wording clarified
|   +-- translation/audio provider wording clarified
|   `-- AI helper live-call gate held closed
|
`-- current recommendation
    +-- no broad sprite mutation
    +-- no asset prep without explicit approval
    +-- no new visual generation/import
    +-- next release decision can proceed from current code baseline
    `-- visual side should review latest code-side screenshots/proofs, not start new cleanup
```

C-PHASE 47 reconciles the 2026-05-05 visual-side tracker with the newer code-side release-polish phases. The big picture is good: the visual lane's "code pending" items have moved forward in targeted ways, while the dangerous gates remain closed.

## Visual Docs Reviewed

- `docs/WEVITO_VISUAL_COMPLETION_TRACKER_2026-05-05.md`
- `docs/WEVITO_VISUAL_STATUS_DASHBOARD_2026-05-05.md`
- `docs/WEVITO_VISUAL_FINAL_DOC_SWEEP_2026-05-05.md`
- `docs/VISUAL_SIDE_CODE_READINESS_RECONCILIATION_2026-05-05.md`
- `docs/WEVITO_PET_RUNTIME_VISUAL_CLEANUP_FINAL_2026-05-05.md`
- `docs/WEVITO_SHARED_RUNTIME_VISUAL_CLEANUP_FINAL_2026-05-05.md`
- `docs/WEVITO_PET_TASKS_VISUAL_REFINEMENT_SPEC_2026-05-05.md`
- `docs/WEVITO_HABITAT_PLACEMENT_ANCHOR_CONTRACT_2026-05-05.md`

## Code-Side Docs Reviewed

- `docs/C_PHASE40_MANUAL_RC3_PLAYER_QA_PACKET_2026-05-12.md`
- `docs/C_PHASE41_OVERLAY_SAVE_POSITION_RECOVERY_2026-05-12.md`
- `docs/C_PHASE42_PET_TASKS_UX_CLARITY_2026-05-12.md`
- `docs/C_PHASE43_CARE_ITEM_HABITAT_MAPPING_2026-05-12.md`
- `docs/C_PHASE44_SCREEN_CAPTURE_UX_PROOF_2026-05-12.md`
- `docs/C_PHASE45_TRANSLATION_AUDIO_PROVIDER_POLISH_2026-05-12.md`
- `docs/C_PHASE46_AI_HELPER_GATE_REVIEW_2026-05-12.md`
- `docs/C_PHASE23_BASELINE_RESTORE_2026-05-07.md`
- `docs/C_PHASE23_APPLY_REPORT_2026-05-07.md`

## Reconciled Status

| Visual-side area | 2026-05-05 status | Current 2026-05-12 code-side status |
| --- | --- | --- |
| Overlay-first UI | Green planning / code pending | Partially advanced. C-PHASE 41 fixed stale saved pet/target positions and packaged automation stayed green. Manual multi-monitor feel remains player QA. |
| PET TASKS visual polish | Yellow / report-only | Advanced. C-PHASE 42 clarified PREPARE/PREVIEW/RUN APPROVED wording and preserved report-first behavior. |
| Care/medicine/object mapping | Green/yellow | Advanced. C-PHASE 43 maps existing item art into habitat recommendation/action preview surfaces without changing art. Compact action-button art can still improve later. |
| Habitat runtime placement | Green/yellow | Advanced by earlier habitat phases and C-PHASE 43. Five-species pilot/runtime proof exists, with visual review still useful before broad expansion. |
| Optional animations | Gated | One-row goose `drop_ball` pilot was applied through V2 with proof and rollback drill in C-PHASE 23. Expansion remains gated. |
| Screenshot/capture helpers | Planned/gated | Advanced. C-PHASE 44 clarified preview-vs-live capture wording. No screenshot or recording was executed in that phase. |
| Translation/audio helpers | Planned/gated | Advanced. C-PHASE 45 clarified DeepL-only execution path and normal Windows endpoint-volume boundary. No provider/audio execution was run. |
| AI/model helpers | Future/gated | Reviewed. C-PHASE 46 keeps the live-call gate closed until an explicit capability flag and consent UI exist. |
| Broad sprite cleanup | Green / queue empty | Remains closed. No new broad cleanup is recommended from code side. |
| Color repair queue | Empty | Remains empty. No recolor or all-color propagation should start as a side effect. |

## Asset Gate Verification

No sprite/runtime PNGs were changed in this phase.

Safe verification performed:

```text
protected optional row spot-check
|
+-- hold_ball_00..03 hashes match C-PHASE 23 / visual cleanup docs
+-- drop_ball_00..03 hashes match C-PHASE 23 final apply hashes
`-- ball remains runtime overlay by contract, not baked into body-pose docs
```

Runtime canvas reporter:

```text
python .\tools\report_runtime_canvas_mismatches.py

checked_sequences=2880
checked_frames=10800
mismatch_count=0
missing_count=0
invalid_count=0
canonical_mismatch_count=3852
```

Interpretation:

- `mismatch_count=0`, `missing_count=0`, and `invalid_count=0` mean active runtime rows are internally consistent.
- `canonical_mismatch_count=3852` is not the old mixed-row blocker. It means many frames do not match canonical dimensions, which is expected after earlier decisions to stop over-boxing sprites into one imaginary canvas.
- The command wrote only its default ignored report artifact under `vnext/artifacts/runtime-canvas-mismatches.json`; no tracked file changed.

## Current Gates

```text
closed gates
|
+-- runtime/source PNG mutation
+-- sprite generation/import
+-- prop-anchor edits
+-- all-color propagation
+-- asset-prep builds without explicit approval
+-- PET TASKS sprite apply/proof execution
+-- broad optional-animation expansion
+-- live model calls
+-- screen recording
`-- external audio booster control
```

These gates should remain closed unless a future phase explicitly opens one with backup, hash, rollback, proof, and user approval.

## What Is Ready For Visual-Side Review

The visual side can safely review these code-side outputs without starting mutation:

- RC3 public-zip screenshots and reports from C-PHASE 40.
- Overlay/save-position recovery behavior from C-PHASE 41.
- PET TASKS wording and report-card hierarchy from C-PHASE 42.
- Care/item/habitat mapping previews from C-PHASE 43.
- ScreenCapture preview report wording from C-PHASE 44.
- Translation/audio preview wording from C-PHASE 45.
- AI-helper gate review from C-PHASE 46.

Recommended visual-side posture:

```text
review only
|
+-- inspect screenshots/proofs/docs
+-- call out UI/wording confusion
+-- request targeted code-side polish if needed
`-- do not generate/import/apply sprites yet
```

## What Is Not Ready For Visual-Side Mutation

Do not start these yet:

- New sprite generation/import.
- Broad runtime/source PNG cleanup.
- All-color optional animation propagation.
- Pickup/drop/carry expansion beyond the already applied goose `drop_ball` pilot.
- Prop-anchor edits.
- Asset-prep regeneration.
- PET TASKS apply/proof execution.

## Remaining Visual-Code Risks

### P1 - Manual player QA is still the release-facing truth gate

Automation is green, but the final visual product needs human review for overlay feel, motion readability, control discoverability, and whether the pet remains the product center.

### P1 - Asset-prep remains dangerous near validated sprite baselines

Continue using safe validation commands that avoid asset prep unless a phase explicitly approves regeneration.

### P2 - PET TASKS is clearer, but still needs user-facing screenshot review

C-PHASE 42 improved wording, but visual-side should still inspect whether the popup feels simple rather than developer-console-like.

### P2 - Habitat and care mapping are partially integrated, not final art direction proof

C-PHASE 43 connected mapping metadata and small-icon-safe art selection. It did not fully redesign every compact action button or inventory grouping.

### P2 - AI helper capability remains dormant

The code has a model seam, but no explicit Shell capability flag or consent UI yet. This is intentionally blocked before any first call.

## Recommended Next Step

Proceed to C-PHASE 48 stable release decision using this state:

```text
C-PHASE 48 inputs
|
+-- RC3 clean validation
+-- C-PHASE 40 manual QA packet
+-- C-PHASE 41 overlay recovery fix
+-- C-PHASE 42-45 tool UX/provider clarity
+-- C-PHASE 46 AI gate HOLD decision
`-- C-PHASE 47 visual-code sync
```

Decision labels for C-PHASE 48 should stay:

```text
promote_current_rc
publish_next_rc
hold_release_for_fixes
```

Given C-PHASE 41 changed player-facing behavior after RC3, the likely stable-release recommendation is **publish_next_rc**, not promote the exact RC3 zip unchanged.

## Copy-Paste Prompt For Visual Side

```text
You are the visual-side Wevito thread.

Repo:
C:\Users\fishe\Documents\projects\wevito

Code-side completed the C-PHASE 47 visual integration sync and saved the report here:

C:\Users\fishe\Documents\projects\wevito\docs\C_PHASE47_VISUAL_SIDE_INTEGRATION_SYNC_2026-05-12.md

Please read it alongside:

C:\Users\fishe\Documents\projects\wevito\docs\C_PHASE40_MANUAL_RC3_PLAYER_QA_PACKET_2026-05-12.md
C:\Users\fishe\Documents\projects\wevito\docs\C_PHASE41_OVERLAY_SAVE_POSITION_RECOVERY_2026-05-12.md
C:\Users\fishe\Documents\projects\wevito\docs\C_PHASE42_PET_TASKS_UX_CLARITY_2026-05-12.md
C:\Users\fishe\Documents\projects\wevito\docs\C_PHASE43_CARE_ITEM_HABITAT_MAPPING_2026-05-12.md
C:\Users\fishe\Documents\projects\wevito\docs\C_PHASE44_SCREEN_CAPTURE_UX_PROOF_2026-05-12.md
C:\Users\fishe\Documents\projects\wevito\docs\C_PHASE45_TRANSLATION_AUDIO_PROVIDER_POLISH_2026-05-12.md
C:\Users\fishe\Documents\projects\wevito\docs\C_PHASE46_AI_HELPER_GATE_REVIEW_2026-05-12.md

Current code-side interpretation:
- Broad visual cleanup remains complete; do not restart broad sprite cleanup.
- Color repair queue remains empty.
- Care/item/habitat mapping has advanced in code, but visual review of screenshots/UX is still useful.
- The goose baby female blue drop_ball one-row pilot was already applied through the V2 proof/rollback path in C-PHASE 23.
- Optional-animation expansion, sprite generation/import, all-color propagation, prop-anchor edits, and asset-prep regeneration remain gated.
- PET TASKS should remain report-first and should not become a sprite mutation executor.
- Live model calls remain blocked until a future capability-flag/consent UI phase.

Please stay review-only for now. Review the latest code-side docs/proofs and return:
1. any visual/UI concerns that should block C-PHASE 48 stable-release decision,
2. any screenshot/proof surfaces you want code-side to capture before release,
3. whether visual-side agrees that the next release decision should likely be publish_next_rc rather than promote_current_rc because C-PHASE 41 changed runtime behavior after RC3.

Do not mutate sprites, source boards, runtime PNGs, prop anchors, or generated/import artifacts unless the user explicitly opens a scoped apply/proof phase.
```
