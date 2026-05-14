# Wevito Pet Simulation Visual Audit

Date: 2026-05-14
Scope: current runtime pet sprites and visible pet-simulator behavior

## Evidence

```text
╔══════════════════════════════════════╗
║ Runtime sprite structural scan       ║
╠══════════════════════════════════════╣
║ rows scanned:   2884                ║
║ frames scanned: 10818               ║
║ findings:       0                   ║
╚══════════════════════════════════════╝

╔══════════════════════════════════════╗
║ Runtime contact sheets              ║
╠══════════════════════════════════════╣
║ species sheets: 10                  ║
║ rows per species: 288 typical       ║
║ frames per species: 1080 typical    ║
╚══════════════════════════════════════╝
```

Artifacts:

- `vnext/artifacts/visual-qa-cockpit/current-runtime-sprite-audit/sprite-visual-quality.md`
- `vnext/artifacts/visual-qa-cockpit/current-runtime-sprite-audit/sprite-visual-quality.json`
- `vnext/artifacts/visual-qa-cockpit/current-runtime-contact-sheets/contact-sheets.md`
- `vnext/artifacts/visual-qa-cockpit/current-runtime-contact-sheets/contact-sheets.json`
- `vnext/artifacts/visual-qa-cockpit/current-runtime-contact-sheets/*.png`

## Important Interpretation

The structural scan passing does not mean the sprites are visually good. It only means the current runtime PNG rows are loadable and do not trip the existing automated edge/geometry/static-row checks. The contact sheets show several human-visible quality issues that require either source-faithful redraws or targeted runtime replacement work.

## High-Priority Findings

| Priority | Species | Finding | Evidence | Recommended Next Move |
| --- | --- | --- | --- | --- |
| P0 | snake | Snake is readable as thin line segments in many rows, has weak silhouette, weak slither motion, and looks like a low-quality placeholder in-game. | `snake.png` contact sheet is much smaller than other species sheets and visually confirms tiny/line-like frames. | Dedicated snake redraw/rebuild pass from source references; do not treat as simple transparency cleanup. |
| P0 | squirrel | Many squirrel rows have little pose/action differentiation. It reads as mostly static upright/chipmunk-like sprites rather than a clean animated squirrel. | `squirrel.png` contact sheet shows repeated similar upright silhouettes across rows. | Dedicated squirrel motion/redraw pass for locomotion/action rows. |
| P1 | pigeon | Contact sheet shows visible square/background-like tiles in some early rows and several rows with rough silhouettes. | `pigeon.png` contact sheet. | Run frame-level crop/background audit and replace bad rows from source references. |
| P1 | crow | Crow is consistently tiny and may have cropped/flattened head frames that need frame-level proof. | `crow.png` contact sheet plus user-observed flattened head. | Use cockpit force-animation/source lookup to isolate the exact bad row/frame. |
| P1 | focused habitat view | User-visible screenshots show pets can be cramped, misaligned, or hidden in focused mode. | Manual screenshots from user. | Continue runtime layout pass after sprite cockpit proof, especially focused idle lineup and action/free-roam transitions. |
| P1 | passive/taskbar baseline | User-visible screenshots show pets do not always align to the top of the Windows taskbar area. | Manual screenshots from user. | Add visual QA overlay for taskbar baseline and align passive roam baseline against it. |

## Lower-Priority Findings

| Priority | Species | Finding | Recommended Next Move |
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
