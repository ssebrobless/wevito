# Wevito Code-Side Visual Phases Handoff Prompt

Updated: 2026-05-04

Use this as the single copy-paste prompt for the code-side Wevito thread after
visual Phases 1-10.

```text
You are working in the Wevito repo:
C:\Users\fishe\Documents\projects\wevito

This is a coordination update from the VISUAL-side thread. Please read the docs
listed below and reconcile them with current code-side state before planning any
new implementation or asset apply work.

Important lane boundary:
- The visual thread completed Phases 1-10 of the visual planning/review plan.
- Do not assume broad visual generation/import is approved.
- Do not normalize or rewrite runtime PNGs as a side effect of reading this.
- Do not touch the Sprite Workflow App unless explicitly assigned.

Most important docs to read first:
1. C:\Users\fishe\Documents\projects\wevito\docs\WEVITO_VISUAL_HANDOFF_2026-05-04.md
2. C:\Users\fishe\Documents\projects\wevito\docs\WEVITO_VISUAL_REMAINING_PHASE_PLAN_2026-05-04.md
3. C:\Users\fishe\Documents\projects\wevito\docs\WEVITO_PRODUCTION_GATE_CHECK_2026-05-04.md
4. C:\Users\fishe\Documents\projects\wevito\docs\WEVITO_GOOSE_HOLD_BALL_PHASE9_CANDIDATE_2026-05-04.md
5. C:\Users\fishe\Documents\projects\wevito\docs\WEVITO_POST_PILOT_EXPANSION_PLAN_2026-05-04.md

Key Phase 9 artifact folder:
C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\animation-runs\20260504-goose-baby-female-blue-hold-ball-pilot

Review these specific Phase 9 artifacts:
- C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\animation-runs\20260504-goose-baby-female-blue-hold-ball-pilot\qa\contact-sheet.png
- C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\animation-runs\20260504-goose-baby-female-blue-hold-ball-pilot\qa\preview.gif
- C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\animation-runs\20260504-goose-baby-female-blue-hold-ball-pilot\manifest.json
- C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\animation-runs\20260504-goose-baby-female-blue-hold-ball-pilot\validation.json
- C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\animation-runs\20260504-goose-baby-female-blue-hold-ball-pilot\run-summary.md

Critical runtime-contract finding:
The ball is NOT baked into existing optional-family PNGs. Godot renders the ball
as a separate carried-item overlay using:
C:\Users\fishe\Documents\projects\wevito\sprites_runtime\_metadata\prop_anchors.json
C:\Users\fishe\Documents\projects\wevito\sprites_shared_runtime\items\toys_a\ball.png

The Phase 9 candidate therefore contains body-pose frames only. Its QA contact
sheet and preview use a runtime-style ball overlay for review. Please preserve
that contract and avoid double-ball rendering.

Current visual-side decision state:
- Phase 8 production gate was closed for broad generation/import/mutation.
- Phase 9 created a non-applied manual candidate proof only.
- No sprites_runtime files were overwritten by the visual thread.
- No source boards were edited by the visual thread.
- The Phase 9 candidate needs human review before any apply/proof step.

The Phase 9 candidate decision labels are:
- accept_for_apply_probe
- revise_candidate
- reject_manual_candidate

If the user asks code-side to continue, safest code-side planning tasks are:
1. Verify the Phase 9 manifest/proof folder is compatible with code-side manifest/provenance expectations.
2. Confirm the exact non-destructive apply/proof procedure for a one-row candidate.
3. Confirm rollback would restore the four current runtime hold_ball frames exactly.
4. Confirm which runtime proof surface should be used after apply: Godot package, vNext, or both.
5. Confirm the carried-ball overlay contract so the body-pose candidate does not produce a double ball.
6. Keep the mixed-canvas base-animation issue separate from this optional hold_ball candidate.

Do not apply the candidate unless the user explicitly approves the apply/proof
step after reviewing the visual artifacts.

Relevant supporting docs:
- C:\Users\fishe\Documents\projects\wevito\docs\WEVITO_GOOSE_HOLD_BALL_PROMPT_PACKET_2026-05-04.md
- C:\Users\fishe\Documents\projects\wevito\docs\WEVITO_GOOSE_PROP_CONTACT_DECISION_PACKET_2026-05-04.md
- C:\Users\fishe\Documents\projects\wevito\docs\WEVITO_CANVAS_NORMALIZATION_POLICY_PACKET_2026-05-04.md
- C:\Users\fishe\Documents\projects\wevito\docs\WEVITO_COLOR_VARIANT_QA_EXPANSION_2026-05-04.md
- C:\Users\fishe\Documents\projects\wevito\docs\WEVITO_MEDICINE_CARE_REVIEW_EXPANSION_2026-05-04.md
- C:\Users\fishe\Documents\projects\wevito\docs\WEVITO_HABITAT_LOADOUT_REVIEW_EXPANSION_2026-05-04.md

Please produce your own concise next-step plan after reading these docs, and
include a single copy-paste prompt back to the visual/Codex thread if you need
the visual side to review, revise, generate, or hold.
```
