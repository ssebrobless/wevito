# Crow Runtime Edge Cleanup

## Goal

Clean the in-game crow runtime rows after user-visible reports of flattened/cropped crow frames and awkward motion.

## Scope

- Species: `crow`
- Runtime path: `sprites_runtime/crow/**`
- Families touched: `idle`, `walk`, `eat`, `happy`, `sad`, `sleep`, `sick`, `bathe`
- UI surfaces touched: Creative Learning Lab and Sprite Workflow V2 root layout scrollability.
- No source boards, prop anchors, manifests, generated art, or model/provider calls were touched.

## Implemented

- Added `tools/repair_crow_runtime_edges.py`.
- The tool preserves the existing crow pixels, removes only tiny disconnected alpha specks if present, and repacks each frame inside its existing canvas with a safe bottom margin.
- Added small controlled per-frame offsets so crow rows keep visible motion and do not trip the low-motion audit.
- Runtime backups are written under the apply artifact folder before replacement.
- Added root scroll viewers to Creative Learning Lab and Sprite Workflow V2 so bottom report/apply/export controls remain reachable on shorter usable desktop areas.

## Evidence

- Dry run: `vnext/artifacts/crow-runtime-edge-cleanup-20260514/dry-run/crow-runtime-edge-cleanup.md`
- Final apply: `vnext/artifacts/crow-runtime-edge-cleanup-20260514/apply-motion-v2/crow-runtime-edge-cleanup.md`
- Visual audit: `vnext/artifacts/crow-runtime-edge-cleanup-20260514/post-audit-motion-v2/sprite-visual-quality.md`
- Canvas audit: `vnext/artifacts/crow-runtime-edge-cleanup-20260514/runtime-canvas-motion-v2.md`
- Preview sheet: `vnext/artifacts/crow-runtime-edge-cleanup-20260514/runtime-previews-motion-v2/crow-preview.png`

## Validation

- Sprite visual quality audit: `0` findings.
- Runtime canvas audit: `2880` checked sequences, `10800` checked frames, `0` mixed-canvas rows, `0` missing rows, `0` invalid frames.
- Focused UI test coverage confirms PET TASKS, Creative Learning Lab, and Sprite Workflow V2 expose scrollable root/panel surfaces for dense controls.

## Notes

This pass is deterministic cleanup, not final high-art animation generation. It removes the obvious runtime edge/crop risk while preserving the current crow identity. A future full-strip art pass can still improve wing/body animation quality from approved source frames.
