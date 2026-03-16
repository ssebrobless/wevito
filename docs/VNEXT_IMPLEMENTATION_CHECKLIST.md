# Wevito vNext Implementation Checklist

Updated: 2026-03-15

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
- [x] Add persistent personality, habit, fitness, and condition state to vNext pets
- [x] Make long-term aging respond to habits, stress, and medical conditions
- [x] Add species-seeded innate conditions and personality biases
- [x] Expand the dev tools for personality, condition, and aging scenario testing
- [x] Add explicit animation-forcing controls for sprite QA
- [x] Add bulk color-set spawning for per-species animation review
- [x] Add habitat prop rendering from the shared item sprite library
- [x] Polish the main HUD header/layout for the current 392px shell width
- [x] Expand the webtools hotbar to five total tabs with the link bin as tab one
- [x] Keep `PASTE` and `DELETE` inside the link bin popup instead of the main HUD
- [x] Clean Gemini checkerboard/background remnants from shared environment, item, and status assets
- [x] Route shared asset loading through a cleaned runtime tree before falling back to raw shared assets
- [x] Remove tiny detached particle remnants from generated pet runtime frames
- [x] Make tint variants more readable in the live runtime
- [x] Align pet/environment sprite placement to integer pixels to reduce WPF softening
- [x] Add scenario-based launch overrides for targeted sprite/color/animation capture
- [x] Remove noisy loose overlay props from the habitat stage and rely on species environment sheets until prop assets are repaired
- [x] Increase live pet display scale with species-aware adjustments for readability
- [x] Improve opaque Gemini source-board extraction using luminance segmentation before runtime generation
- [x] Re-run representative 10-species audit after sprite extraction improvements
- [x] Re-run non-default matrix audits for baby blue idle and teen red walk across all 10 species

## Latest Validation Evidence

- Build and tests: `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug`
- Authored coverage summary: `C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\authored-coverage\summary.json`
- Shared asset cleanup summary: `C:\Users\fishe\Documents\projects\wevito\sprites_shared_runtime\cleanup-summary.json`
- Runtime pet cleanup summary: `C:\Users\fishe\Documents\projects\wevito\sprites_runtime\cleanup-summary.json`
- Screenshot harness summary: `C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\screenshots\20260312-233036\summary.json`
- Latest screenshot harness summary: `C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\screenshots\20260313-043531\summary.json`
- Latest targeted non-default scenario capture: `C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\screenshots\20260313-044326\summary.json`
- Latest snake-focused extraction/capture pass: `C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\screenshots\20260313-080548\summary.json`
- Flicker harness summary: `C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\flicker\20260312-225954\summary.json`
- Pinned interaction probe summary: `C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\probes\20260312-225954\summary.json`
- Sprite preview sheets from screenshot harness: `C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\screenshots\20260312-233036\sprite-previews\index.txt`
- Polished runtime-only preview sheets: `C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\sprite-previews-runtime-polished\index.txt`
- Latest representative 10-species audit summary: `C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\sprite-audit-runs\20260313-080629\summary.json`
- Latest baby-blue matrix audit summary: `C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\sprite-audit-runs\20260313-081558\summary.json`
- Latest teen-red matrix audit summary: `C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\sprite-audit-runs\20260313-081821\summary.json`

## Notes

- Focus-sensitive harnesses must be run sequentially. Parallel runs produce false failures because they compete for foreground state and publish output.
- Basket copy/open/delete is validated in the action probe. Clipboard writes were hardened in the shell to avoid transient `OpenClipboard` crashes.
- The pinned probe now resolves the real `CaptureButton` bounds through UI Automation instead of relying on stale hardcoded header coordinates.
- The WPF UI Automation tree is still inconsistent for some lower-row and header buttons in the action probe environment, so authored-runtime validation currently relies most strongly on unit tests, coverage validation, screenshot/flicker harnesses, and the pinned interaction probe.
- Shared non-pet assets now export into `C:\Users\fishe\Documents\projects\wevito\sprites_shared_runtime`. The cleanup pass aggressively removes baked Gemini checkerboards from `environment`, `items`, and `status`, while `icons`, `celestial`, and `portraits` are copied through unchanged because they already preserve transparency.
- Generated pet frames now go through a conservative detached-pixel scrub so tiny opaque islands that sit away from the main silhouette are removed without reshaping the body.
- The latest live scenario audit confirms the shell can launch directly into non-default species/color cases. Current weakest visible species from that path include the snake variants, which still need more art cleanup than the generator alone provides.
- The loose overlay habitat props are temporarily disabled in the live stage because several Gemini-derived prop PNGs still contain baked checker/matte contamination that looks worse than the dedicated species environment sheets.
- Opaque Gemini source boards now go through luminance-based subject isolation before pose extraction, which materially improved the weakest snake variants without changing the runtime animation contract.
- The broader audit loop now includes representative non-default age/gender/color cases instead of only the adult-violet scenario, which gives much better proof that live tint/variant rendering is actually working.
- Focused motion-family authoring packs are now in place under `incoming_sprites/gemini_handoff_motion`, with `locomotion` and `care` packs prepared across the roster so movement and rest/head-down interaction can be improved separately from the old full-board workflow.
- The runtime now supports variable frame sizes per animation family, which is currently used by `snake` walk frames so slither poses can stay wider than the neutral idle pose.

## Sprite Runtime Migration

- [x] Commit and push the validated vNext baseline before pet sprite migration
- [x] Inventory incoming animal pose boards and map each board to species plus age stage
- [x] Confirm incoming animal assets are dual-pose source boards, not direct animation sheets
- [x] Add a checked-in manifest for incoming animal pose boards
- [x] Build a generator that extracts the two source poses per board and synthesizes the full runtime animation set
- [x] Normalize generated pet runtime frames to canonical 72x64 transparent PNGs
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
- Runtime pet-frame loading now defaults to the cleaner generated `sprites_runtime` set. Authored overrides remain available as a staging lane behind `WEVITO_VNEXT_PREFER_AUTHORED=1`.
- The runtime generator now includes extra per-species motion shaping, including a real wave-based slither pass for reptiles so snake walk/idle no longer read as rigid translation.
- The home habitat stage now renders lightweight species-aware prop sprites plus one recommended-care prop, so environments read as small lived-in setups rather than plain backdrops.
- The focused HUD header is now split into title and control rows so the main shell reads cleanly at the real shipped width.
- The webtools hotbar now exposes five total slots on the main HUD. Only the link bin is active; the other four slots are intentionally empty placeholders for future PC tools.
- The main HUD only exposes the webtools show/hide toggle. Link-bin actions stay inside the popup as `PASTE` and `DELETE`.

## Canonical Sprite Contract

- [x] Lock the original pet source-of-truth contract into repo documentation
- [x] Confirm `incoming_sprites` contains the 30 canonical animal age sheets
- [x] Confirm each canonical animal age sheet is a dual-pose source board with male-left and female-right
- [x] Confirm the canonical runtime expansion remains 10 species x 3 ages x 2 genders x 6 colors
- [x] Confirm the canonical runtime animation contract remains 8 animations with the expected frame counts
- [x] Add a repeatable audit script that validates source boards and runtime outputs against that contract
- [ ] Audit every canonical animal age sheet visually for silhouette, transparency, and crop quality
- [ ] Score every species/age/gender for animation-readiness and replacement priority
- [ ] Replace synthesized motion sets with authored or guided frame animation while preserving canonical appearance
- [ ] Promote supporting source sheets from `incoming_sprites` into final polished runtime usage for props, icons, environments, and celestial assets

## Canonical Sprite Contract Notes

- The canonical sprite contract is documented in `C:\Users\fishe\Documents\projects\wevito\docs\SPRITE_SOURCE_OF_TRUTH.md`.
- The repeatable audit entry point is `C:\Users\fishe\Documents\projects\wevito\tools\audit_sprite_contract.py`.
- The new source-vs-runtime visual audit entry point is `C:\Users\fishe\Documents\projects\wevito\tools\audit_source_to_runtime_quality.py`.
- `incoming_sprites` remains the source of truth for what each pet should look like.
- `sprites_runtime` remains the shippable runtime output tree, not the canonical authoring source.
- Latest contract audit summary: `C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\sprite-audit\20260311-231225\summary.json`

## Authored Animation Lane

- [x] Add a documented authored-animation workflow that preserves the uploaded source sprites as canonical appearance
- [x] Add a species authoring-pack exporter that extracts canonical poses and exports editable labeled frame boards
- [x] Add current runtime reference boards and color strips to the species authoring pack
- [x] Add a dedicated `sprites_authored` override tree for improved animation frames
- [x] Update shell runtime loading to prefer authored animation frames over generated runtime frames when present
- [x] Update sprite preview rendering to visualize authored overrides
- [x] Export a rat pilot authoring pack
- [x] Add board import and color-propagation tools for authored animation round-trips
- [x] Validate a rat override round-trip through `sprites_authored`
- [x] Add rat pilot image-edit prompts based on the precise-edit workflow
- [x] Add a consolidated single-image Gemini upload pack per variant for safer automation
- [x] Add a live Chrome Gemini driver that uses the already-open logged-in browser session when available
- [x] Add full-roster authoring-pack export and Gemini handoff preparation
- [x] Restore a working logged-in Gemini app session after the live Chrome tab was closed during failed picker cleanup
- [x] Add a Gemini share-link fetch path and clipboard-image fallback for more reliable board retrieval
- [x] Add explicit Gemini `authuser` targeting for `wbogusz24@gmail.com`
- [x] Add automatic Gemini relaunch/recovery when the visible window disappears between variants
- [x] Add a `New chat` reset step before each automated Gemini generation to avoid stale-thread drift
- [x] Generate authored `idle` and `walk` board replacements for all 10 species, 3 ages, and 2 genders
- [x] Import authored board replacements into `sprites_authored` for all 60 base variants
- [x] Propagate all authored base variants across the full 6-color runtime matrix
- [x] Validate the fully authored sprite set through coverage plus screenshot harness
- [x] Expand the authored-animation lane across the full species roster

## Authored Animation Notes

- The workflow doc is `C:\Users\fishe\Documents\projects\wevito\docs\AUTHORED_ANIMATION_WORKFLOW.md`.
- The pack exporter is `C:\Users\fishe\Documents\projects\wevito\tools\export_species_authoring_pack.py`.
- The board importer is `C:\Users\fishe\Documents\projects\wevito\tools\import_authored_animation_board.py`.
- The color propagation tool is `C:\Users\fishe\Documents\projects\wevito\tools\propagate_authored_colors.py`.
- Verified authoring overrides now live under `C:\Users\fishe\Documents\projects\wevito\sprites_authored_verified`.
- The older `C:\Users\fishe\Documents\projects\wevito\sprites_authored` tree is now treated as legacy/staging history rather than the current authored source of truth.
- The rat pilot authoring pack is at `C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\authoring-packs\rat`.
- The latest full-roster authored coverage summary is `C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\authored-coverage\summary.json`.
- The latest full-roster authored screenshot harness summary is `C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\screenshots\20260312-194322\summary.json`.
- The rat pilot prompt pack is `C:\Users\fishe\Documents\projects\wevito\prompts\gemini\authored-animation-rat-pilot.md`.
- The live Gemini automation driver is `C:\Users\fishe\Documents\projects\wevito\tools\drive-live-gemini.ps1`.
- The batch Gemini runner is `C:\Users\fishe\Documents\projects\wevito\tools\batch-drive-live-gemini.ps1`.
- The batch import wrapper is `C:\Users\fishe\Documents\projects\wevito\tools\import-gemini-result.ps1`.
- Coverage reporting now lives at `C:\Users\fishe\Documents\projects\wevito\tools\report_authored_sprite_coverage.py`.
- The live Gemini workflow now relies on the `wbogusz24@gmail.com` session and prefers direct `Download full size image` retrieval. Clipboard fallback is still available, but public-share retrieval is no longer the intended path.
- `tools\drive-live-gemini.ps1` now defaults to attaching to the already-open logged-in Gemini Chrome window. Launching a fresh Chrome Gemini window now requires `-AllowLaunchFallback`.
- All 360 authored runtime variants are complete: 10 species x 3 ages x 2 genders x 6 colors.
- Authored overrides are now treated as a staging lane for future hand-polish, not the default live runtime source.
- Richer Gemini upload packs with optional approved-reference panels now round-trip correctly through `tools\extract_board_from_gemini_pack.py`.
- Verified authored locomotion coverage is currently complete for:
  - `snake` across all 3 ages and both sexes
  - `frog` across all 3 ages and both sexes
  - `crow` across all 3 ages and both sexes
  - `pigeon` across all 3 ages and both sexes
  - `rat` across all 3 ages and both sexes
  - `fox` across all 3 ages and both sexes
  - `deer` across all 3 ages and both sexes
- Verified authored locomotion is also complete for:
  - `raccoon` adult male and adult female
  - `raccoon` teen male idle only
- Latest authored motion work was imported into `C:\Users\fishe\Documents\projects\wevito\sprites_authored_verified` from direct Gemini downloads using the `wbogusz24@gmail.com` live session.
- Blur-sensitive locomotion work now supports split Gemini pack families (`locomotion_idle`, `locomotion_walk_a`, `locomotion_walk_b`) so each generation carries fewer frames and preserves more pixel detail.
- The avian prompt now explicitly forbids hollow torsos/chests and requires solid internal shading, which fixed the pigeon body-fill failure in the split-pack workflow.
- `tools\report_authored_sprite_coverage.py` is now family-aware, so locomotion-family progress can be measured before `care` and `expression` are finished.
- The live shell now prefers verified authored `idle` and `walk` frames by default when they exist, while still using generated runtime frames for unfinished families. Set `WEVITO_VNEXT_DISABLE_AUTHORED_LOCOMOTION=1` to compare against the generated runtime-only lane, or `WEVITO_VNEXT_PREFER_AUTHORED=1` to prefer authored frames for any family that exists.
- Latest family-aware authored coverage summary: `C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\authored-coverage\summary.json`
  - `locomotion_idle`: `270 / 360` color variants complete
  - `locomotion_walk_a`: `264 / 360` color variants complete
  - `locomotion_walk_b`: `264 / 360` color variants complete
  - combined `locomotion`: `264 / 360` color variants complete

## Dev Tools

- [x] Keep current runtime sprites usable as placeholders while authored animation replacement is in progress
- [x] Expose visible color variants for the placeholder runtime through the current tinted sprite pipeline
- [x] Add a development-only dev tools surface in the testing build
- [x] Add dev controls for pet selection, add/remove pet, appearance swaps, and need/health manipulation
- [x] Add quick dev presets for hungry, thirsty, tired, dirty, lonely, sick, healthy, comforted, and recall
- [x] Add a dev hotkey and tray action to open the dev tools quickly
- [x] Capture the dev tools UI in the visual harness
- [x] Expand the dev tools for obesity, malnutrition, anxiety, depression, injury, elder-age, and personality archetype presets
- [x] Add direct fitness and biological-age controls to the dev tools
- [x] Add visible personality, habit, aging, and condition summaries to the dev tools
- [x] Add full pet-lab controls for forced animation playback and condition editing
- [x] Add clear-all and spawn-color-set controls for bulk sprite review

## Dev Tools Notes

- Dev tools are enabled in debug builds and can also be forced in other builds with `WEVITO_VNEXT_DEVTOOLS=1`.
- Global hotkey: `Ctrl+Shift+D`
- Latest dev-tools visual capture: `C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\screenshots\20260312-224339\summary.json`
- The dev popup now supports add/remove/clear pets, appearance swaps, bulk color spawning, forced animation overrides, direct condition editing, vitals editing, and long-run personality/aging presets.

## Current Polish Pass

- [x] Increase live pet scale and tint contrast so non-default colors read more clearly in the shell
- [x] Add broader scenario-driven sprite audits beyond the default startup trio
- [x] Improve opaque Gemini board extraction for weak species like snake
- [x] Tighten stage prop selection to prefer stage-safe habitat pieces instead of dirty item exports
- [x] Make clean-profile starter pets use more visibly distinct colors
- [x] Harden screenshot capture to stop stale Wevito processes before each run
- [x] Add scenario support for forcing tool-popup state during visual audits
- [x] Raise the generated runtime pet-frame contract to 72x64 so more source-board detail survives into the shell
- [x] Detect pose bounds from checkerboard-cleaned animal boards instead of the opaque source sheets
- [x] Replace dirty live stage container PNGs with clean in-shell dish rendering
- [x] Add audit-scenario support for explicit recent actions like `rest` and `water`
- [x] Validate the action-choice popup lane with focused scenario screenshots
- [x] Replace dirty live habitat prop rendering with clean species-specific vector habitat stages
- [x] Revalidate pinned desktop behavior after the habitat-stage rewrite
- [x] Make the link-bin popup controls and basket interactions feel intentional instead of stock WPF
- [x] Scale the clean habitat stage dressing with pet life stage
- [x] Restyle the main HUD buttons, webtool tabs, and action grid for a cleaner cohesive shell
- [x] Surface lightweight need/preference hints in recommendation cards and action choices
- [x] Harden non-default visual audits so basket/dev captures keep working on variant scenarios

## Current Polish Notes

- Latest default capture after the 72x64 sprite-runtime pass: `C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\screenshots\20260313-210341-462\summary.json`
- Latest crow-focused clean stage proof: `C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\screenshots\20260313-210129-804\summary.json`
- Latest fox-focused clean stage proof: `C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\screenshots\20260313-210143-155\summary.json`
- Latest snake-focused clean stage proof: `C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\screenshots\20260313-210156-444\summary.json`
- Latest explicit rest interaction proof: `C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\screenshots\20260313-210731-595\summary.json`
- Latest explicit water interaction proof: `C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\screenshots\20260313-211413-438\summary.json`
- Latest source-vs-runtime comparison sheets: `C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\debug\source-runtime-comparisons`
- The runtime pet-frame contract is now 72x64, which preserves more of the original incoming animal-board detail than the earlier 56x48 pass.
- Live habitat containers now render as clean in-shell dishes on the stage instead of relying on the dirtiest Gemini-derived container PNGs.
- Audit scenarios now support `lastActionId` and `lastActionSecondsAgo`, which makes it possible to validate bed/rest and dish/water interaction cues on demand.
- The runtime pose generator now extracts from the cleaned source board consistently and uses a denser downscale fallback when nearest-neighbor collapses too much silhouette detail.
- That runtime fix materially repaired the adult female pigeon body-loss issue and improved the adult audit boards for fox, rat, crow, frog, snake, goose, deer, raccoon, and squirrel.
- Latest source-vs-runtime adult audit boards:
  - `C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\source-runtime-audit\20260314-201549\source-runtime-audit-board.png`
  - `C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\source-runtime-audit\20260314-201818\source-runtime-audit-board.png`
  - `C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\source-runtime-audit\20260314-203435\source-runtime-audit-board.png`
  - `C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\source-runtime-audit\20260314-195524\source-runtime-audit-board.png`
- When runtime sprite PNGs change, live shell screenshots must be regenerated from a rebuilt/published app. `-SkipBuild` audit runs can show stale visuals even when the preview sheets already reflect the fixed runtime frames.
- Latest representative default capture: `C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\screenshots\20260313-141400-440\summary.json`
- Latest rat-focused habitat proof after stage-safe prop curation: `C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\screenshots\20260313-141710-888\summary.json`
- Latest rat-focused action-popup proof: `C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\screenshots\20260313-143144-452\summary.json`
- Latest rat-focused clean vector habitat proof: `C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\screenshots\20260313-144338-562\summary.json`
- Latest crow-focused clean vector habitat proof: `C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\screenshots\20260313-144713-611\summary.json`
- Latest default capture after the vector habitat rewrite: `C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\screenshots\20260313-150243-684\summary.json`
- Latest baby-rat focused proof after habitat age-scaling and basket-harness hardening: `C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\screenshots\20260313-151900-862\summary.json`
- Latest default capture after HUD and basket styling cleanup: `C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\screenshots\20260313-152601-700\summary.json`
- Latest rat action-popup proof after preference-hint polish: `C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\screenshots\20260313-153353-974\summary.json`
- Latest 10-species representative audit: `C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\sprite-audit-runs\20260313-080629\summary.json`
- Latest baby blue full-species matrix: `C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\sprite-audit-runs\20260313-081558\summary.json`
- Latest teen red full-species matrix: `C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\sprite-audit-runs\20260313-081821\summary.json`
- Latest refreshed adult blue full-species matrix after the sharper extraction/motion pass: `C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\sprite-audit-runs\20260314-021757\summary.json`
- Latest refreshed baby blue full-species matrix after the sharper extraction/motion pass: `C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\sprite-audit-runs\20260314-022021\summary.json`
- Latest refreshed teen red full-species matrix after the sharper extraction/motion pass: `C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\sprite-audit-runs\20260314-022241\summary.json`
- Audit boards can now be generated directly from any matrix summary with `tools/compose_sprite_audit_board.py`.
- Latest adult blue audit board: `C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\sprite-audit-runs\20260314-021757\adult-male-blue-walk-board.png`
- Latest baby blue audit board: `C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\sprite-audit-runs\20260314-022021\baby-male-blue-idle-board.png`
- Latest teen red audit board: `C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\sprite-audit-runs\20260314-022241\teen-female-red-walk-board.png`
- The half-sheet component finder now scores multiple bbox candidates for opaque Gemini animal boards instead of blindly trusting the first opaque isolation pass.
- That fix materially improved frog, deer, pigeon, raccoon, squirrel, goose, and crow extraction compared with the earlier whole-half crop failures.
- Latest targeted live proofs after the component-finder rewrite:
  - crow: `C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\sprite-audit-runs\20260314-033241\summary.json`
  - frog: `C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\sprite-audit-runs\20260314-033319\summary.json`
  - deer: `C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\sprite-audit-runs\20260314-033358\summary.json`
  - pigeon: `C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\sprite-audit-runs\20260314-033435\summary.json`
  - raccoon: `C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\sprite-audit-runs\20260314-033511\summary.json`
  - snake: `C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\sprite-audit-runs\20260314-033553\summary.json`
  - goose: `C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\sprite-audit-runs\20260314-033634\summary.json`
  - squirrel: `C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\sprite-audit-runs\20260314-033712\summary.json`
- Latest default screenshot harness after the sharper extraction/motion pass: `C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\screenshots\20260314-022558-880\summary.json`
- Latest pinned probe after the sharper extraction/motion pass: `C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\probes\20260314-022615\summary.json`
- Scenario JSON should currently use string facing directions like `"Left"` / `"Right"` to avoid numeric deserialization drift.
- Latest pinned desktop proof after the vector habitat rewrite: `C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\probes\20260313-145802\summary.json`
- The live habitat stage now uses clean species-specific vector habitat dressing instead of raw Gemini-derived prop collages, while recommended items and action options still use the shared asset library.
- The basket popup now supports direct link open on double-click, dynamic delete-count feedback, and themed controls that match the main shell.
- Recommendation cards and action-choice rows now expose lightweight “needed / fresh / favorite / cozy / preferred” style hints when the pet state or personality makes them relevant.

- `extract_gender_pose` now prefers the checkerboard-cleaned crop whenever it produces a usable alpha silhouette instead of immediately re-isolating from the opaque crop.
- Border-palette cleanup now samples matte-like corner colors instead of trusting the full crop edge, which fixed the light-variant rat female edge-eating bug and made the runtime rows safer when sprites touch the crop boundary.
- Latest full regenerated preview set after the shared extraction cleanup pass: `C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\sprite-previews-manual-latest`
- Latest live adult-male blue walk board after the full regeneration pass: `C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\sprite-audit-runs\20260314-061853\adult-male-blue-walk-live-board.png`
- Latest live adult-male blue walk sweep after the full regeneration pass:
  - rat: `C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\sprite-audit-runs\20260314-061853\summary.json`
  - crow: `C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\sprite-audit-runs\20260314-061906\summary.json`
  - fox: `C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\sprite-audit-runs\20260314-061919\summary.json`
  - snake: `C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\sprite-audit-runs\20260314-061932\summary.json`
  - deer: `C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\sprite-audit-runs\20260314-061945\summary.json`
  - frog: `C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\sprite-audit-runs\20260314-061958\summary.json`
  - pigeon: `C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\sprite-audit-runs\20260314-062011\summary.json`
  - raccoon: `C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\sprite-audit-runs\20260314-062024\summary.json`
  - squirrel: `C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\sprite-audit-runs\20260314-062037\summary.json`
  - goose: `C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\sprite-audit-runs\20260314-062050\summary.json`
- The live habitat stage now has a dedicated clean backdrop layer, so species habitats read as cohesive scenes instead of props floating on a transparent panel.
- Stage-safe species templates now favor clean-drawn moss beds, nest beds, leaf piles, basking rocks, perches, and pond dishes instead of falling back to the roughest extracted shared prop sprites.
- Latest post-backdrop full-species adult-male blue walk sweep: `C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\sprite-audit-runs\20260314-142137\summary.json`
- Latest post-backdrop full-species adult-male blue walk board: `C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\sprite-audit-runs\20260314-142137\adult-male-blue-walk-live-board.png`
- Action-option rows now prefer clean UI icons by action/category instead of showing the dirtiest shared item thumbnails directly in the popup.
- Interaction placement is now anchor-aware, so rest/play/water moments line pets up differently for beds, perches, shelters, and bowls instead of using one generic offset.
- Latest explicit post-alignment rest proof: `C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\screenshots\20260314-143242-338\summary.json`
- Latest explicit post-alignment water proof: `C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\screenshots\20260314-143244-563\summary.json`

## Personality And Aging

- [x] Carry over the original long-run personality vocabulary into vNext
- [x] Add evolving personality traits for food-love, cuddle need, cleanliness, activity, cheerfulness, social need, playfulness, and stubbornness
- [x] Add persistent habit history for nutrition, hydration, exercise, hygiene, affection, rest, medical care, and stress
- [x] Add persistent fitness and biological-age tracking
- [x] Drive acquired conditions from neglect, overfeeding, low fitness, low comfort, and exhaustion
- [x] Keep innate species conditions present after load and reconfiguration
- [x] Make medicine and doctor actions affect persistent condition state
- [x] Add tests for personality/action effects, condition creation, medical recovery, and aging-rate differences

## Personality And Aging Notes

- The long-term behavior now lives primarily in `C:\Users\fishe\Documents\projects\wevito\vnext\src\Wevito.VNext.Core\PetSimulationEngine.cs`.
- Species temperament seeds and innate conditions now live in `C:\Users\fishe\Documents\projects\wevito\vnext\content\species.json`.
- Condition reference content now lives in `C:\Users\fishe\Documents\projects\wevito\vnext\content\conditions.json`.
- Unit coverage for the new systems is in `C:\Users\fishe\Documents\projects\wevito\vnext\tests\Wevito.VNext.Tests\PetSimulationEngineTests.cs`.
