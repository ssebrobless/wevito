# Focused / Actions / Roam UX Fix

## What Changed

- Focused and pinned home views now render living pets in a calm side-by-side idle lineup unless an Actions surface is open.
- Opening `ACTIONS` auto-pins the overlay while the action surface is active.
- Closing Actions releases the temporary auto-pin and returns pets to the calm lineup.
- While Actions is open, living pets use the home stage as their movement/play area instead of being forced back to their idle spots.
- Passive roam motion now targets the top of the Windows taskbar area with a small overlap instead of the physical bottom of the screen.
- The overlay style cache now includes taskbar visibility, so the main Wevito window can restore its visible taskbar tab correctly.

## Pigeon Finding

A read-only scan of `sprites_runtime/pigeon/**.png` found many pigeon frames with non-transparent pixels touching one or more canvas edges. This matches the reported flattened/cropped pigeon-frame symptom.

This patch does not mutate sprite PNGs. The pigeon issue should be handled as a visual asset repair pass with backup/hash/proof, not as a hidden runtime rewrite.

## Validation

- Focused rendering tests cover calm-lineup scale and placement.
- Shell presentation tests cover Actions-surface detection and taskbar-top roam motion bounds.
