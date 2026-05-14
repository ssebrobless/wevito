# Squirrel Motion Cleanup

## Goal

Respond to the May 13 playtest finding that squirrel rows looked static and some action frames carried ghosted duplicate silhouettes.

## Scope

- Runtime sprite mutation only under `sprites_runtime/squirrel/**`.
- No source boards, prop anchors, content manifests, hosted AI, browser generation, or external image tools.
- Added `tools/repair_squirrel_motion_rows.py` so the repair can be repeated or rolled forward deterministically.

## Method

The repair keeps each squirrel row's existing local idle silhouette and reseeds runtime families with small transparent-canvas offsets:

- `idle`: subtle breathing bob.
- `walk`: six-frame left/right plus vertical bob motion.
- `eat`, `happy`, `sad`, `sleep`, `sick`, `bathe`: family-specific small vertical motion.

This intentionally favors a clean in-game baseline over pretending to be final hand-authored limb animation.

## Evidence

- Dry run: `vnext/artifacts/squirrel-motion-cleanup-20260514/dry-run/squirrel-motion-cleanup.md`
- Apply report: `vnext/artifacts/squirrel-motion-cleanup-20260514/apply/squirrel-motion-cleanup.md`
- Backup before apply: `vnext/artifacts/squirrel-motion-cleanup-20260514/apply/backup-before-apply/`
- Visual audit: `vnext/artifacts/squirrel-motion-cleanup-20260514/post-audit/sprite-visual-quality.md`
- Runtime canvas audit: `vnext/artifacts/squirrel-motion-cleanup-20260514/runtime-canvas.md`
- Runtime previews: `vnext/artifacts/squirrel-motion-cleanup-20260514/runtime-previews/`

## Results

- Rows scanned: `288`.
- Rows changed: `288`.
- Frames changed on apply: `1002`.
- Visual quality audit: `0` findings.
- Runtime canvas audit: `0` mixed-canvas rows, `0` missing frames, `0` invalid frames.

## Residual

The squirrel is now cleaner and more readable, but it is still a conservative runtime cleanup rather than a final bespoke squirrel animation pass. If the project later reopens high-art generation, squirrel should be reviewed for more expressive leg/tail motion.

