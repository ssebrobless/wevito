# Wevito Post-Merge Pet Simulation Deep Audit

Date: 2026-05-14
Branch: `feature/pet-sim-deep-visual-audit`
Merged baseline: PR #127, merge commit `495eca977c45314478fe01dff86303c1c5184a5b`

## What Was Verified

```text
+----------------------+---------------------------------------------+
| area                 | result                                      |
+----------------------+---------------------------------------------+
| local main           | fast-forwarded to PR #127                   |
| artifact build       | republished with -SkipAssetPrep -SkipTests  |
| running processes    | shell + broker + dev controller relaunched  |
| dev-control pipe     | GetSnapshot success, 3 slots, 0 filled      |
| save state           | fresh egg-choice / empty active pet state   |
| contact sheets       | regenerated for all 10 species              |
| runtime canvas       | 0 mixed rows, 0 missing rows, 0 invalid PNG |
| sprite contract      | 30/30 boards, 17/17 inputs, 360/360 dirs    |
| optional animations  | 2516/2520 fallback-only                     |
+----------------------+---------------------------------------------+
```

## Artifact Paths

- `vnext/artifacts/visual-qa-cockpit/deep-audit-20260514-post-merge/contact-sheets/contact-sheets.md`
- `vnext/artifacts/visual-qa-cockpit/deep-audit-20260514-post-merge/contact-sheets/*.png`
- `vnext/artifacts/visual-qa-cockpit/deep-audit-20260514-post-merge/runtime-canvas.md`
- `vnext/artifacts/visual-qa-cockpit/deep-audit-20260514-post-merge/runtime-canvas.json`
- `vnext/artifacts/visual-qa-cockpit/deep-audit-20260514-post-merge/sprite-contract.json`
- `vnext/artifacts/visual-qa-cockpit/deep-audit-20260514-post-merge/optional-readiness.md`
- `vnext/artifacts/visual-qa-cockpit/deep-audit-20260514-post-merge/optional-readiness.json`

## Current Visual Findings

| Priority | Target | Finding | Required Follow-Up |
| --- | --- | --- | --- |
| P0 | snake | Still reads as thin, low-detail line segments with weak slither motion. Not a simple transparency bug. | Source-faithful runtime rebuild/redraw pass for snake rows, then in-game proof. |
| P0 | squirrel | Rows are heavily repeated and under-animated; motion does not read clearly as a living squirrel. | Dedicated motion pass for idle/walk/action rows. |
| P1 | pigeon | Contact sheet still supports user reports of broken/rough frames. | Frame-level isolate pass using cockpit force-animation/source lookup. |
| P1 | crow | Crow is tiny and user observed cropped/flattened head frames. | Frame-level isolate pass using cockpit force-animation/source lookup. |
| P1 | optional interaction families | Most optional actions are fallbacks, not authored animation. | Treat as future content production queue, not completed animation. |
| P1 | runtime layout | User screenshots show focus/passive/taskbar placement still needs practical visual proof. | Add overlay/frame-scrub/screenshot batch support, then verify live. |

## Egg Flow Status

The runtime now has the seven ROYGBIV egg prompt surface and the prompt layout no longer clips in tight focused stages. The current runtime sprite inventory does not include green pet variants, so the green egg remains disabled with an explicit tooltip until green runtime sprites are added or the design chooses a non-green fallback. This is a content gap, not a layout bug.

## Current Build Status

The local artifact build was refreshed from the merged baseline:

- Shell: `vnext/artifacts/shell/Wevito.VNext.Shell.exe`
- Dev Controller: `vnext/artifacts/dev-controller/Wevito.VNext.DevController.exe`

Running processes after refresh:

- `Wevito.VNext.Shell`
- `Wevito.VNext.Broker`
- `Wevito.VNext.DevController`

The dev-control smoke returned:

```text
success=True
slots=3
filled=0
```

## Next Best Work

```text
1. Add missing audit powers:
   - stable slot identity
   - exact frame scrubber
   - overlay toggles for bounds/ground/taskbar
   - batch screenshot/contact-sheet proof runner

2. Use those tools to isolate exact row/frame issues:
   - snake all ages/genders/colors
   - squirrel all ages/genders/colors
   - pigeon broken frames
   - crow cropped/flattened head frames

3. Only after exact rows are identified, start scoped sprite repair PRs:
   - one species or one repair family at a time
   - no hidden asset prep
   - backup/hash/rollback/post-proof for any PNG mutation
```

## Important Boundary

The current audit proves the build and content inventory are structurally healthy, but it does not prove the game is visually complete. Human-visible sprite quality remains the main blocker.
