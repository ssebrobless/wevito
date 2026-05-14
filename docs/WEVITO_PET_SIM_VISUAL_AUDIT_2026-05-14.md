# Wevito Pet Simulation Visual Audit

Date: 2026-05-14
Scope: current runtime pet sprites and visible pet-simulator behavior

## Current Evidence Shape

```text
+-------------------------------------------------------------+
| automated structure                                         |
| - runtime rows load: green                                  |
| - sprite contract coverage: green                           |
| - row-internal canvas mismatch: 0                           |
| - invalid/non-alpha PNGs: 0                                 |
+------------------------------+------------------------------+
                               |
                               v
+-------------------------------------------------------------+
| human visual quality                                         |
| - snake and squirrel still read as poor-quality in game      |
| - pigeon/crow have frame-level visual breakage concerns      |
| - optional interaction art is almost entirely fallback-only  |
| - canonical canvas drift still affects many runtime frames   |
+-------------------------------------------------------------+
```

## Evidence Artifacts

- `vnext/artifacts/visual-qa-cockpit/current-runtime-sprite-audit/sprite-visual-quality.md`
- `vnext/artifacts/visual-qa-cockpit/current-runtime-sprite-audit/sprite-visual-quality.json`
- `vnext/artifacts/visual-qa-cockpit/current-runtime-contact-sheets/contact-sheets.md`
- `vnext/artifacts/visual-qa-cockpit/current-runtime-contact-sheets/contact-sheets.json`
- `vnext/artifacts/visual-qa-cockpit/current-runtime-contact-sheets/*.png`
- `vnext/artifacts/visual-qa-cockpit/deep-audit-20260514/runtime-canvas.md`
- `vnext/artifacts/visual-qa-cockpit/deep-audit-20260514/runtime-canvas.json`
- `vnext/artifacts/visual-qa-cockpit/deep-audit-20260514/sprite-contract.json`
- `vnext/artifacts/visual-qa-cockpit/deep-audit-20260514/optional-readiness.md`
- `vnext/artifacts/visual-qa-cockpit/deep-audit-20260514/optional-readiness.json`
- `vnext/artifacts/visual-qa-cockpit/20260514-191414-slot-1-empty-p0-snake-weak-silhouette/issue.md`
- `vnext/artifacts/visual-qa-cockpit/20260514-191414-slot-2-empty-p0-squirrel-static-animation/issue.md`
- `vnext/artifacts/visual-qa-cockpit/20260514-191414-slot-3-empty-p1-pigeon-rough-silhouette/issue.md`
- `vnext/artifacts/visual-qa-cockpit/20260514-191414-slot-3-empty-p1-crow-possible-crop/issue.md`

## Automated Audit Results

| Check | Result | Interpretation |
| --- | ---: | --- |
| Runtime visual-quality scan | 2884 rows, 10818 frames, 0 findings | PNG rows are structurally loadable, but this scanner is not a human-quality judge. |
| Runtime contact sheets | 10 species sheets | Useful human review surface for species-level motion/style issues. |
| Runtime canvas mismatch report | 2880 sequences, 10800 frames, 0 mixed-canvas rows, 0 missing rows, 0 invalid frames | Current runtime rows are internally consistent. |
| Legacy canonical canvas diagnostic | 5220 frame mismatches | Many frames no longer match the older fixed-canvas expectations. This is diagnostic under the current natural-canvas policy, but it explains visual scale/framing risk. |
| Sprite contract audit | 30/30 source boards, 17/17 supporting inputs, 360/360 runtime variant dirs | Inventory coverage is present. |
| Runtime frame count | 10818 found vs 10800 expected | There are 18 extra runtime frames beyond the base contract, likely optional/pilot residue to keep tracked. |
| Optional animation readiness | passed, 2520 targets, 0 authored complete, 4 runtime-only complete, 2516 fallback-only | The audit passes because fallback is allowed, but visually the optional interaction system is mostly placeholder/fallback art. |
| Dev-control issue tagging | 4 report-only packets written | Snake, squirrel, pigeon, and crow are now captured as concrete cockpit issue packets without mutating sprites. |

## Important Interpretation

The structural scan passing does not mean the sprites are visually good. It only means the current runtime PNG rows are loadable and do not trip the existing automated edge/geometry/static-row checks. The contact sheets and user-visible runtime screenshots show several human-visible quality issues that require source-faithful redraws, targeted runtime replacement work, or renderer/layout fixes.

Do not treat "scanner green" as "visual complete." We need both:

```text
mechanical valid  +  visually readable in-game  +  motion reads correctly
```

## High-Priority Findings

| Priority | Species/System | Finding | Evidence | Recommended Next Move |
| --- | --- | --- | --- | --- |
| P0 | snake | Snake is readable as thin line segments in many rows, has weak silhouette, weak slither motion, and looks like a low-quality placeholder in-game. | `snake.png` contact sheet, user screenshots, and runtime view. | Dedicated snake redraw/rebuild pass from source references; do not treat as simple transparency cleanup. |
| P0 | squirrel | Many squirrel rows have little pose/action differentiation. It reads as mostly static upright/chipmunk-like sprites rather than a clean animated squirrel. | `squirrel.png` contact sheet and user runtime screenshots. | Dedicated squirrel motion/redraw pass for locomotion/action rows. |
| P1 | pigeon | Contact sheet and user reports show visible breakage, rough silhouettes, and likely bad/cropped frames. | `pigeon.png` contact sheet and user screenshots. | Run frame-level crop/background audit and replace bad rows from source references. |
| P1 | crow | Crow is consistently tiny and may have cropped/flattened head frames that need frame-level proof. | `crow.png` contact sheet plus user-observed flattened head. | Use cockpit force-animation/source lookup to isolate the exact bad row/frame. |
| P1 | optional animations | 2516/2520 optional animation targets are fallback-only. | `optional-readiness.md`. | Treat optional families as mostly unimplemented content, not completed animation work. |
| P1 | focused habitat view | User-visible screenshots show pets can be cramped, misaligned, hidden, or not returned to idle positions. | Manual screenshots from user. | Continue runtime layout pass, especially focused idle lineup and action/free-roam transitions. |
| P1 | passive/taskbar baseline | User-visible screenshots show pets do not always align to the top of the Windows taskbar area. | Manual screenshots from user. | Add visual QA overlay for taskbar baseline and align passive roam baseline against it. |
| P1 | egg-selection flow | The egg prompt was cut off in the focused window before this pass. | User screenshot. | Fixed in cockpit branch by computing a bounded, scrollable seven-egg prompt layout. Keep in live QA regression set. |

## Lower-Priority Findings

| Priority | Species/System | Finding | Recommended Next Move |
| --- | --- | --- | --- |
| P2 | fox | Contact sheet is mostly readable, but user observed in-game visual breakage. Needs force-animation proof to isolate runtime row/frame. | Use cockpit to force fox rows and tag exact failures. |
| P2 | frog | Contact sheet is readable, but jump/action motion still appears conservative. | Review jump/play/action rows with force animation and decide if source redraw is needed. |
| P2 | goose | Sheet looks structurally consistent overall, but earlier goose rows had known optional-animation cleanup history. | Keep as regression sample in batch QA. |
| P2 | raccoon | Sheet is mostly readable after recent cleanup; still needs in-game size/baseline proof. | Use as a medium-quality comparison baseline. |
| P2 | rat | Sheet is mostly readable; needs runtime placement and action proof. | Use as a medium-quality comparison baseline. |
| P2 | deer | Sheet is mostly readable; needs runtime size and antler/sex differentiation proof. | Use as large-animal scale baseline. |

## Runtime Interaction Issues To Verify With Cockpit

- Egg flow should offer seven ROYGBIV eggs, not species-specific visible eggs.
- Deleting a dev-test pet should not create a ghost.
- Replacing a pet should be explicit and confirmed.
- Actions should clearly target the selected pet.
- Food and water should become draggable/action objects later, not confusing global buttons in the final player UX.
- Focused mode should show calm side-by-side idle pets unless Actions is open.
- Actions mode should temporarily pin Wevito and let pets roam/interact in a larger usable stage.
- Ghost pets should be visual-only remnants outside the environment, with ghost styling applied to the pet body instead of a floating emoji.

## Next Improvement Queue

```text
1. Finish Visual QA Cockpit stable-slot + frame-scrub + issue-tag tools.
2. Use cockpit to isolate exact bad pigeon/crow/fox rows.
3. Do dedicated source-faithful redraw passes:
   - snake
   - squirrel
   - pigeon
   - crow
4. Re-run contact sheets and structural scan.
5. Run focused/passive/runtime placement proof with overlays.
6. Convert remaining findings into scoped repair PRs.
```
