# Wevito vNext Implementation Checklist

Updated: 2026-03-11

## Core Delivery

- [x] Expand vNext content model for full species/action/environment/need/status/item surface
- [x] Expand persisted pet state for age/gender/color/animation/facing/needs/statuses
- [x] Load all 10 species into vNext content
- [x] Add runtime asset resolution for sprites, icons, environments, status icons, and celestial art
- [x] Replace placeholder pet rendering in home panel with sprite rendering
- [x] Replace placeholder pet rendering in roam band with sprite rendering
- [x] Render species-specific environment art in the home panel
- [x] Render day/night celestial art in the home panel
- [x] Add icon-backed primary action surface: Feed, Water, Rest, Play, Groom, Bath, Medicine, Doctor, Home
- [x] Add visible need/status presentation in the HUD
- [x] Add action effects that change pet state and animation, not only feedback text
- [x] Add state-based action enabling/disabling where appropriate
- [x] Keep focused/passive/pinned overlay behavior stable after UI expansion
- [x] Keep basket behavior working after HUD and content expansion

## Validation

- [x] Build vNext solution successfully
- [x] Run unit tests successfully
- [x] Add/verify content-load coverage for expanded content model
- [x] Add/verify asset coverage validation for enabled species/variants
- [x] Run screenshot harness against the updated UI
- [x] Run flicker harness against the updated UI
- [x] Verify focused mode renders correctly with real sprites
- [x] Verify passive mode renders correctly with roaming sprites
- [x] Verify pinned mode remains interactive without focus steal
- [x] Verify basket still copies/opens/deletes links correctly
- [x] Verify all primary actions execute without runtime errors

## Latest Validation Evidence

- Build and tests: `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug`
- Screenshot harness summary: `C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\screenshots\20260311-223523\summary.json`
- Flicker harness summary: `C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\flicker\20260311-222215\summary.json`
- Pinned interaction probe summary: `C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\probes\20260311-222215\summary.json`
- Action and tool probe summary: `C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\action-probes\20260311-222305\summary.json`
- Sprite preview sheets from screenshot harness: `C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\screenshots\20260311-223523\sprite-previews\index.txt`

## Notes

- Focus-sensitive harnesses must be run sequentially. Parallel runs produce false failures because they compete for foreground state and publish output.
- Basket copy/open/delete is validated in the action probe. Clipboard writes were hardened in the shell to avoid transient `OpenClipboard` crashes.

## Sprite Runtime Migration

- [x] Commit and push the validated vNext baseline before pet sprite migration
- [x] Inventory incoming animal pose boards and map each board to species plus age stage
- [x] Confirm incoming animal assets are dual-pose source boards, not direct animation sheets
- [x] Add a checked-in manifest for incoming animal pose boards
- [x] Build a generator that extracts the two source poses per board and synthesizes the full runtime animation set
- [x] Normalize generated pet runtime frames to canonical 28x24 transparent PNGs
- [x] Generate full runtime pet frames for all 10 species, 3 ages, 2 genders, and 6 colors
- [x] Create a dedicated `sprites_runtime` tree for pet runtime assets
- [x] Switch shell pet-frame resolution to `sprites_runtime` only with no pet fallback to `sprites`
- [x] Keep shared non-pet assets resolving from the shared sprite tree
- [x] Add automated runtime coverage validation for frame counts, canvas size, and alpha-preserving PNG output
- [x] Add generated sprite contact-sheet previews to the visual QA harness for direct frame inspection
- [x] Run screenshot harness with fully migrated pet sprites
- [x] Run flicker harness with fully migrated pet sprites
- [x] Run pinned interaction probe with fully migrated pet sprites
- [x] Run action and tool probe with fully migrated pet sprites

## Sprite Migration Notes

- The generated pet runtime is now authoritative at `C:\Users\fishe\Documents\projects\wevito\sprites_runtime`.
- Pet-frame runtime fallback to `sprites` has been removed. Shared non-pet assets still resolve from `sprites`.
- The visual harness now emits sprite contact sheets alongside runtime screenshots so extraction/generation mistakes are visible without launching the app manually.
- The screenshot harness refreshes live window bounds at each capture step so cropped artifacts match the real rendered positions instead of stale startup coordinates.
- In the latest action probe, `doctor` is present but disabled in the healthy default state. The probe records that as expected state gating rather than a missing control.
- The runtime generator now uses species motion profiles so birds, deer, frogs, snakes, and mammals do not all inherit the same transform recipe.
