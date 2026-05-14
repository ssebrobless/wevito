# PET TASKS Popup And Snake Cleanup

## Goal

Fix two user-visible blockers from the May 13 playtest:

- PET TASKS report/apply controls were clipped at the bottom of the popup, making task cards hard to use.
- Snake runtime sprites were structurally valid but visually poor: opaque baked artifacts, mushy poses, and weak slither readability.

## PET TASKS UI Fix

- Root cause: `ToolPopupWindow` used a fixed-height popup while the PET TASKS surface was a long `StackPanel` with no internal scroll container.
- Fix: converted the PET TASKS panel into its own `ScrollViewer` with `VerticalScrollBarVisibility=Auto`.
- Result: report path, Open Report, Copy Path, Open Folder, queue selector, approval/preview/run buttons, and safety copy remain reachable inside the existing popup height.

## Snake Runtime Repair

- Added `tools/repair_snake_procedural_rows.py`.
- Scope: `sprites_runtime/snake/**` only.
- Families repaired: `idle`, `walk`, `eat`, `happy`, `sad`, `sleep`, `sick`, `bathe`.
- Rows scanned: `288`.
- Rows changed: `288`.
- Frames changed: `1080`.
- Method: deterministic local pixel redraw with transparent RGBA frames, age-scaled body sizes, color-variant palettes, male/female head variation, slither walk frames, subtle sleep breathing, and no external AI generation.

## Evidence

- Dry run: `vnext/artifacts/snake-procedural-cleanup-20260514/dry-run/snake-procedural-repair.md`
- Apply report: `vnext/artifacts/snake-procedural-cleanup-20260514/apply/snake-procedural-repair.md`
- Backup before apply: `vnext/artifacts/snake-procedural-cleanup-20260514/apply/backup-before-apply/`
- Visual audit: `vnext/artifacts/snake-procedural-cleanup-20260514/post-audit/sprite-visual-quality.md`
- Runtime canvas audit: `vnext/artifacts/snake-procedural-cleanup-20260514/runtime-canvas.md`
- Runtime previews: `vnext/artifacts/snake-procedural-cleanup-20260514/runtime-previews/`

## Audit Results

- `report_sprite_visual_quality.py`: `0` findings after the final snake redraw.
- `report_runtime_canvas_mismatches.py --fail-on-mismatch`: `0` mixed-canvas rows, `0` missing frames, `0` invalid frames.

## Learning Note

This repair should be treated as reviewed evidence for future Wevito learning, not automatic training. The useful lesson is:

- Structural PNG audits can pass while a species still looks bad in motion.
- Opaque baked artifacts require source/runtime replacement or redraw; transparency cleanup alone will not fix them.
- Future autonomous learning should record this as a labeled before/after repair example only after human review.

