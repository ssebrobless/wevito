# Wevito Overlay Design Code-Side Handoff Prompt

Date: 2026-05-05

Purpose: single copy-paste prompt for the code-side thread after visual-side reconciled Claude Design with the existing Wevito overlay.

## Copy-Paste Prompt

```text
You are the code-side Wevito thread.

Repo:
C:\Users\fishe\Documents\projects\wevito

Visual-side reviewed the rendered Claude Design Phase 1B prototype and converted it into overlay-first implementation guidance. Please read these docs before planning UI/tool work:

0. C:\Users\fishe\Documents\projects\wevito\docs\WEVITO_VISUAL_UI_REMAINING_IMPLEMENTATION_PLAN_2026-05-05.md
1. C:\Users\fishe\Documents\projects\wevito\docs\WEVITO_CLAUDE_DESIGN_OVERLAY_INTEGRATION_PLAN_2026-05-05.md
2. C:\Users\fishe\Documents\projects\wevito\docs\WEVITO_CLAUDE_DESIGN_VISUAL_TRANSLATION_SPEC_2026-05-05.md
3. C:\Users\fishe\Documents\projects\wevito\docs\WEVITO_OVERLAY_FIRST_IMPLEMENTATION_ROADMAP_2026-05-05.md
4. C:\Users\fishe\Documents\projects\wevito\docs\WEVITO_PET_TASKS_VISUAL_REFINEMENT_SPEC_2026-05-05.md
5. C:\Users\fishe\Documents\projects\wevito\docs\WEVITO_SPRITE_WORKFLOW_V2_OVERLAY_COMPATIBILITY_SPEC_2026-05-05.md
6. C:\Users\fishe\Documents\projects\wevito\docs\WEVITO_PET_HELPER_FUNCTIONS_ROADMAP_2026-05-05.md
7. C:\Users\fishe\Documents\projects\wevito\docs\WEVITO_CLIPBOARD_SHELF_VISUAL_SPEC_2026-05-05.md
8. C:\Users\fishe\Documents\projects\wevito\docs\WEVITO_CARE_MEDICINE_OBJECT_REVIEW_PACKET_2026-05-05.md
9. C:\Users\fishe\Documents\projects\wevito\docs\WEVITO_COLOR_PALETTE_QUALITY_REVIEW_PASS1_2026-05-05.md
10. C:\Users\fishe\Documents\projects\wevito\docs\WEVITO_COLOR_PALETTE_QUALITY_REVIEW_PASS2_2026-05-05.md
11. C:\Users\fishe\Documents\projects\wevito\docs\WEVITO_CREATIVE_LEARNING_LAB_PLAN_2026-05-05.md
12. C:\Users\fishe\Documents\projects\wevito\docs\WEVITO_VISUAL_STATUS_DASHBOARD_2026-05-05.md
13. C:\Users\fishe\Documents\projects\wevito\docs\WEVITO_COLOR_PALETTE_WALK_MOTION_REVIEW_PASS3_2026-05-05.md
14. C:\Users\fishe\Documents\projects\wevito\docs\WEVITO_HABITAT_LOADOUT_MOCKUP_REVIEW_2026-05-05.md
15. C:\Users\fishe\Documents\projects\wevito\docs\WEVITO_CROW_DARK_BACKGROUND_CONTRAST_REVIEW_2026-05-05.md
16. C:\Users\fishe\Documents\projects\wevito\docs\WEVITO_REFINED_HABITAT_LOADOUT_REVIEW_2026-05-05.md
17. C:\Users\fishe\Documents\projects\wevito\docs\WEVITO_HABITAT_PLACEMENT_ANCHOR_CONTRACT_2026-05-05.md
18. C:\Users\fishe\Documents\projects\wevito\docs\WEVITO_COLOR_ATLAS_LAYOUT_FOLLOWUP_2026-05-05.md
19. C:\Users\fishe\Documents\projects\wevito\docs\WEVITO_CLAUDE_DESIGN_SCREENSHOT_INDEX_2026-05-05.md
20. C:\Users\fishe\Documents\projects\wevito\docs\WEVITO_VISUAL_FINAL_DOC_SWEEP_2026-05-05.md

Observed Claude Design screenshots are here:
C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\design\phase1b-claude-design\observed-render-20260505\

Compact screenshot index is here:
C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\design\phase1b-claude-design\screenshot-index-20260505\claude-design-phase1b-screenshot-index.png

Main visual-side decision:
Do NOT replace the existing living desktop-pet overlay with Claude's dashboard. The overlay remains Wevito's primary product surface. Claude's designs should become summoned tool surfaces:

- PET TASKS popup/drawer
- Sprite Workflow V2 separate workbench
- Creative Learning Lab later dashboard
- Implementation handoff/ownership model

Important boundaries:
- PET TASKS remains report-only unless you explicitly implement and validate a safe execution adapter later.
- Do not add sprite generation/import/runtime PNG mutation/prop-anchor edits/Godot proof launch to PET TASKS right now.
- Keep build validation on the safe path with -SkipAssetPrep while visual cleanup is active.
- Sprite Workflow V2 should be a separate workbench or separate app surface, not the normal desktop-pet overlay.
- The overlay must keep pass-through/unfocused/pinned behavior and roaming pets visible whenever practical.
- Ball/drink props remain runtime overlays unless a future explicit policy changes that.

Recommended code-side planning slices:

1. PET TASKS visual refinement only:
   - add clearer REPORT ONLY / NO EXECUTION state labels
   - improve prepared task-card hierarchy
   - add artifact Open/Copy Path/Open Folder affordances if safe
   - optionally add compact three-helper strip
   - keep PREPARE and PREVIEW semantics unchanged

2. spriteAudit/report-only improvements:
   - consider contact-sheet output for visual findings
   - continue writing only timestamped artifacts
   - no runtime/source PNG mutation

3. Sprite Workflow V2 coordination:
   - compare the current Sprite Workflow App work against the no-scroll layout in the visual docs
   - do not implement a broad refactor without coordinating with the other Sprite Workflow App thread

4. Pet helper functions:
   - localDocs, spriteAudit, proof-summary reading, link bin, and future Clipboard Shelf are the first safe lanes
   - apply/proof/generation/import stay manual/coordinated for now

Current visual-side phase priority:
1. preserve overlay-first UI contract
2. refine PET TASKS as report-only helper surface
3. coordinate Sprite Workflow V2 as separate workbench
4. add contact-sheet/report improvements before any mutation-oriented workflows
5. keep optional animation apply/proof manual/code-side
6. plan Clipboard Shelf and useful helper functions without passive clipboard capture
7. keep color work focused on quality review, not recreating folders
8. treat care/medicine/object work as content/UI mapping now, not broad repainting

Fresh visual-side findings:
- Clipboard Shelf should be manual-save only and should never passively scrape clipboard history.
- Care/medicine/object art pool is broad and mostly clean: 81 shared item/object PNGs exist, while `vnext/content/items.json` currently exposes 7 umbrella item records.
- Color index-sheet review is complete for all 10 species. No broad recolor is recommended. Current color repair queue is empty.
- Creative Learning Lab should be read-only/review-first if implemented; no training/export/unreviewed data promotion.
- Visual status dashboard is now the quickest summary of current visual-side state.
- Risk-focused walk-motion color review is complete. Current color repair queue remains empty.
- First habitat loadout mockups show the object pool is useful but too crowded when every recommended object is shown at once. Future habitat implementation should use primary / interaction / decor tiers, not draw full species loadouts at once.
- Crow dark-background contrast review is complete. Do not recolor crow yet; solve with staging/background contrast first.
- Refined habitat loadout review accepts the three-object model conceptually. Next habitat work is placement anchors/depth tiers, not more art creation.
- Habitat placement anchor contract is now drafted for goose, rat, crow, snake, and frog. Recommended code-side implementation should be staged: static primary anchors first, then active interaction objects, then occlusion/depth/age offsets.
- Tall-row color atlas follow-up is complete for squirrel adult male and deer adult female. The awkward old atlas display was a review-sheet layout issue, not a sprite defect. Future sheets should use dynamic row heights and print native frame dimensions.
- Claude Design screenshot index is complete. Borrow approved elements per artboard, but do not treat the prototype as code truth or replace the roaming overlay.
- Final visual doc sweep is complete. Prefer the current dashboard, remaining implementation plan, and this handoff prompt over older historical docs when statuses differ.

Please produce your own short code-side implementation plan from these docs, call out any conflicts with current code-side work, and return a single copy-paste prompt for the visual-side thread when your review is done.
```
