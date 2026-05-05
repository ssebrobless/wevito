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
- [x] Historically normalize generated pet runtime frames to canonical 72x64 transparent PNGs before the natural per-sequence canvas policy replaced the fixed-frame contract
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
- [x] Historically raise the generated runtime pet-frame contract to 72x64 so more source-board detail survived into the shell before natural per-sequence canvases were approved
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

- Historical default capture after the 72x64 sprite-runtime pass: `C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\screenshots\20260313-210341-462\summary.json`
- Latest crow-focused clean stage proof: `C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\screenshots\20260313-210129-804\summary.json`
- Latest fox-focused clean stage proof: `C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\screenshots\20260313-210143-155\summary.json`
- Latest snake-focused clean stage proof: `C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\screenshots\20260313-210156-444\summary.json`
- Latest explicit rest interaction proof: `C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\screenshots\20260313-210731-595\summary.json`
- Latest explicit water interaction proof: `C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\screenshots\20260313-211413-438\summary.json`
- Latest source-vs-runtime comparison sheets: `C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\debug\source-runtime-comparisons`
- Historical note: the runtime pet-frame contract was raised from 56x48 to 72x64 at that point; the current 2026-05-04 contract is natural per-sequence canvases with stable frame sizes inside each exact animation row.
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

## 2026-05-04 Code-Side Runtime Gate Notes

- Deterministic code-lane Phase 1 work in the Codex worktree added build/test skip and timeout knobs, a non-mutating canvas mismatch reporter, and an updated popup-aware vNext action/tool probe.
- New phase report: `C:\Users\fishe\.codex\worktrees\36d6\wevito\docs\CODE_SIDE_PHASE1_RUNTIME_GATE_PROGRESS_2026-05-04.md`.
- New reporter: `C:\Users\fishe\.codex\worktrees\36d6\wevito\tools\report_runtime_canvas_mismatches.py`.
- Latest canvas mismatch report: `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\runtime-canvas-mismatches-20260504-worktree.md`.
- Latest passing action/tool probe summary: `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\action-probes\20260504-175949\summary.json`.
- The probe now starts isolated sessions from a pinned audit scenario so Chrome or other foreground apps cannot hide the shell during control validation.
- Basket Delete now enables for marked rows, selected rows, or a single saved row, matching the existing handler intent and making Link Bin deletion testable.
- `build-vnext.ps1` now warns if `-SkipAssetPrep` is used without `-SkipTests`, because tests will validate whichever runtime assets are currently present.
- Validation passed: code-only `build-vnext`, popup action/tool probe, and focused `SpriteValidatorTests`, `RepairPlannerTests`, and `PetSimulationEngineTests` filter in this worktree.
- Historical pre-closure blocker: full sprite runtime coverage previously failed because `generation-summary.json` was absent and the old fixed-canvas assertion found a width mismatch (`expected 72`, `actual 89`); the non-mutating reporter found `546` mixed-canvas sequences before Phase 4A.
- Phase 4A safe canvas apply completed in this worktree: `972` PNG files received transparent padding only, with backups under `C:\Users\fishe\.codex\worktrees\36d6\wevito\artifacts\recovery\runtime-canvas-normalization-20260504-phase4a-safe-padding`.
- Phase 4A reduced mixed-canvas rows from `546` to `222`; all remaining rows are `review_alignment` and were intentionally skipped.
- Phase 4A report: `C:\Users\fishe\.codex\worktrees\36d6\wevito\docs\RUNTIME_CANVAS_NORMALIZATION_PHASE4A_SAFE_APPLY_2026-05-04.md`.
- Post-Phase 4A canvas contract report: `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\runtime-canvas-contract-20260504-after-phase4a.md`.
- Historical Phase 4A status: full sprite runtime coverage remained red after Phase 4A for expected policy reasons: missing generation summary and old fixed-canvas assertions. Do not treat that as evidence that the safe padding pass failed.
- Phase 4B/4C sequence-stable canvas closure completed: `1704` total runtime PNGs were transparently padded across Phase 4A, Phase 4B, and Phase 4C, with backups for every changed PNG.
- Phase 4B/4C closure report: `C:\Users\fishe\.codex\worktrees\36d6\wevito\docs\RUNTIME_CANVAS_NORMALIZATION_PHASE4BC_CLOSURE_2026-05-04.md`.
- Final sequence-stable canvas report: `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\runtime-canvas-contract-20260504-final-sequence-stable.md`.
- Final sequence-stable result: `2880` sequences checked, `10800` frames checked, `0` mixed-canvas sequences, `0` missing/count mismatches, and `0` invalid PNGs.
- `SpriteRuntimeCoverageTests` now validates the approved natural per-sequence canvas contract instead of forcing old fixed `72x64` canvases.
- Validation passed after closure: filtered `SpriteRuntimeCoverageTests` passed `2 / 2`; `build-vnext.ps1 -Configuration Debug -SkipAssetPrep -StepTimeoutSeconds 180` passed vNext tests `26 / 26` and published broker/shell artifacts.

## 2026-05-04 Code-Side Work-Companion Runtime Bridge

- [x] Add runtime enum support for `Waving`, `Jumping`, `Failed`, `Waiting`, and `Review`.
- [x] Add safe sprite fallback routing for unauthored work states: `Waving`/`Jumping` -> `happy`, `Failed` -> `sad`, `Waiting`/`Review` -> `idle`.
- [x] Add shell pulses for visible tool/action state changes without adding new sprite assets.
- [x] Preserve explicit/dev animation overrides when ambient popup `Waiting` is active.
- [x] Add trace-once protection for fallback/missing sprite logs.
- [x] Add a simulation regression test proving active work-companion animation overrides survive normal tick refresh.
- Latest Phase 2 report is appended in `C:\Users\fishe\.codex\worktrees\36d6\wevito\docs\CODE_SIDE_PHASE1_RUNTIME_GATE_PROGRESS_2026-05-04.md`.
- Latest Phase 2 validation:
  - `dotnet test .\vnext\Wevito.VNext.sln -c Debug --filter "FullyQualifiedName~PetSimulationEngineTests"` passed `17 / 17`.
  - `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests -StepTimeoutSeconds 180` passed.
  - `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\probe-vnext-actions.ps1 -Configuration Debug -SkipBuild -StartupDelayMs 2500` passed with summary `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\action-probes\20260504-182214\summary.json`.
- Coordination note: the main project already contains the broader visual-thread Phase 3/4 repair taxonomy and repair automation work. This worktree intentionally added only the deterministic runtime bridge to avoid copying broad visual-lane changes.

## 2026-05-04 Runtime Asset Contract Diagnostics

- [x] Extend the non-mutating canvas reporter to include legacy fixed-canvas diagnostics alongside the active sequence-stable contract.
- [x] Report whether `sprites_runtime\generation-summary.json` exists as diagnostic metadata.
- [x] Keep reporter output read-only so it can guide the visual lane without resizing or deleting runtime PNGs.
- Historical pre-closure report: `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\runtime-canvas-contract-20260504-worktree.md`.
- Historical pre-closure report results: `2880` sequences checked, `10800` frames checked, `546` mixed-canvas sequences, `3852` legacy fixed-canvas diagnostic mismatches, `0` missing/count mismatches, `0` invalid PNGs, and missing `generation-summary.json`.
- Current contract report: `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\runtime-canvas-contract-20260504-final-sequence-stable.md`.
- Current contract result: `2880` sequences checked, `10800` frames checked, `0` mixed-canvas sequences, `0` missing/count mismatches, and `0` invalid PNGs.
- The active runtime contract is sequence-stable natural canvases. Legacy fixed-canvas mismatches remain diagnostic only unless `--fail-on-canonical-mismatch` is explicitly requested.

## 2026-05-04 Code-Side Phase 2 Merge Safety

- [x] Inventory the dirty worktree without staging, reverting, deleting, or moving files.
- [x] Separate current canvas/runtime contract closure from vNext runtime bridge work, local-only recovery backups, older Godot gameplay edits, and Gemini/browser automation helpers.
- [x] Record recommended commit buckets so the next staging pass can avoid a broad unsafe `git add .`.
- Phase 2 report: `C:\Users\fishe\.codex\worktrees\36d6\wevito\docs\CODE_SIDE_PHASE2_DIRTY_WORKTREE_MERGE_SAFETY_2026-05-04.md`.
- Current dirty-worktree inventory: `1718` tracked modified files, including `1704` runtime PNGs; `1713` untracked files, including `1704` recovery PNG backups.
- Recommended staging split: vNext runtime bridge/reliability code, sequence-stable canvas tooling/tests/docs, runtime PNG payload, local-only recovery backups, and separate-review Godot/Gemini changes.
- No files were staged or reverted during Phase 2.

## 2026-05-04 Code-Side Phase 3 Validation Sweep

- [x] Run focused vNext validation for sprite runtime coverage and simulation bridge behavior.
- [x] Run the non-mutating runtime canvas reporter in fail-on-mismatch mode.
- [x] Run the full vNext test project.
- [x] Run `build-vnext.ps1` through tests, broker publish, and shell publish.
- [x] Run the popup-aware action/tool probe against the built shell.
- Phase 3 report: `C:\Users\fishe\.codex\worktrees\36d6\wevito\docs\CODE_SIDE_PHASE3_VALIDATION_SWEEP_2026-05-04.md`.
- Runtime canvas validation artifact: `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\runtime-canvas-contract-20260504-phase3-validation.md`.
- Action/tool probe artifact: `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\action-probes\20260504-205959\summary.json`.
- Phase 3 validation result: focused tests passed `19 / 19`; full vNext tests passed `26 / 26`; build/publish passed; action/tool probe passed with all action traces, settings traces, basket traces, save trace, and shell-alive checks green.

## 2026-05-04 Code-Side Phase 4 Runtime Contract Hardening

- [x] Inspect runtime contract seams across the C# runtime coverage test, Python canvas reporter, and build script.
- [x] Align the Python reporter with the C# test so non-alpha PNG color types are reported as invalid runtime frames.
- [x] Keep legacy fixed-canvas mismatches diagnostic-only unless `--fail-on-canonical-mismatch` is requested.
- [x] Re-run reporter, focused sprite runtime coverage tests, and full vNext tests after the hardening change.
- Phase 4 report: `C:\Users\fishe\.codex\worktrees\36d6\wevito\docs\CODE_SIDE_PHASE4_RUNTIME_CONTRACT_HARDENING_2026-05-04.md`.
- Phase 4 runtime canvas artifact: `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\runtime-canvas-contract-20260504-phase4-hardening.md`.
- Phase 4 validation result: reporter passed with `0` active failures, focused `SpriteRuntimeCoverageTests` passed `2 / 2`, and full vNext tests passed `26 / 26`.

## 2026-05-04 Code-Side Phase 5 Visual Readiness Check

- [x] Read visual-side planning/gate docs from the main project path because they are not present in this forked worktree yet.
- [x] Inspect code seams for sprite runtime loading, action/tool probes, dev/audit scenarios, habitat loadouts, care item mapping, body-condition state, and optional animation support.
- [x] Classify what can safely continue on the visual side versus what must remain paused.
- Phase 5 report: `C:\Users\fishe\.codex\worktrees\36d6\wevito\docs\CODE_SIDE_PHASE5_VISUAL_READINESS_CHECK_2026-05-04.md`.
- Key Phase 5 decision: no-edit visual QA and contact sheets can continue; visual generation, sprite import, runtime/source PNG mutation, and broad runtime/content implementation should remain paused.
- Stale visual-gate note: the main visual production gate still references old mixed-canvas/full-test failures. Code-side Phase 3/4 evidence now shows `0` mixed-canvas sequences and full vNext tests passing `26 / 26` in this worktree.
- Main code-side gaps before production mutation: manifest/provenance/apply workflow, vNext optional animation addressing, body-condition renderer, ghost renderer, habitat object zone/depth/occlusion content contract, and first-class care item content mapping.

## 2026-05-04 Code-Side Phase 6 Commit And Packaging Readiness

- [x] Refresh dirty-worktree inventory after Phases 3-5.
- [x] Update recommended staging/commit buckets after the reporter hardening change and new phase docs.
- [x] Identify vNext debug publish readiness and Godot release packaging blockers.
- Phase 6 report: `C:\Users\fishe\.codex\worktrees\36d6\wevito\docs\CODE_SIDE_PHASE6_COMMIT_PACKAGING_READINESS_2026-05-04.md`.
- Current inventory after Phase 6: `1718` tracked modified files and `1717` untracked files, including `1704` tracked runtime PNG changes and `1704` untracked recovery PNG backups.
- vNext shell debug publish is ready at `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\shell\Wevito.VNext.Shell.exe`.
- Godot release packaging is not ready from this worktree until the separate-review Godot script changes, runtime PNG payload, and asset-prep policy are explicitly resolved.
- No files were staged, committed, pushed, or packaged during Phase 6.

## 2026-05-04 Code-Side Phase 7 Cross-Thread Handoff

- [x] Consolidate the code-side runtime, validation, dirty-worktree, visual-readiness, and packaging-readiness results into a single visual-thread handoff.
- [x] Record what the visual side can safely continue now versus what remains paused.
- [x] Call out stale visual production-gate assumptions that should be updated from the latest code-side evidence.
- Phase 7 handoff: `C:\Users\fishe\.codex\worktrees\36d6\wevito\docs\CODE_SIDE_TO_VISUAL_HANDOFF_2026-05-04.md`.
- Key Phase 7 decision: no-edit visual QA/contact sheets can continue, but visual generation, sprite import, runtime/source PNG mutation, broad asset rewrites, and one-row production pilots should wait for explicit coordination.
- Current biggest blocker: the canvas contract is no longer the blocker; the missing production-safe manifest/provenance/apply workflow is now the main blocker before new visual mutation work.
- No files were staged, committed, pushed, or packaged during Phase 7.

## 2026-05-05 Code-Side Contract Schema Foundation

- [x] Add pet-helper, task-intent, task-card, tool-policy, reviewed-example, and learning-feedback contract models.
- [x] Add sprite-row, candidate, and apply/proof manifest contract models.
- [x] Preserve the boundary that pet names/helper roles route tasks but do not grant permissions.
- [x] Preserve the runtime prop-overlay contract for optional animation candidates.
- [x] Add focused contract serialization/default tests.
- Phase report: `C:\Users\fishe\.codex\worktrees\36d6\wevito\docs\CODE_SIDE_PHASE2_CONTRACT_SCHEMA_PROGRESS_2026-05-05.md`.
- Validation result: focused contract tests passed `7 / 7`; `dotnet build vnext\Wevito.VNext.sln` passed; full vNext tests passed `33 / 33`.
- No runtime PNGs, source boards, Godot scripts, sprite generation, browser automation, or production apply paths were changed.

## 2026-05-05 Code-Side Command Parser And Policy Foundation

- [x] Add deterministic pet command parsing for `@PetName`, `PetName, ...`, selected-pet fallback, and best-helper routing.
- [x] Add draft task-card creation with `Draft`, `WaitingForApproval`, and `Blocked` outcomes.
- [x] Add a dry-run `ToolPolicyEvaluator` that can return `Allowed`, `ApprovalRequired`, or `Blocked` without executing tools.
- [x] Block risky command classes before policy execution.
- [x] Add focused tests for parser routing, risky-command blocking, build approval, and tool-policy decisions.
- Phase report: `C:\Users\fishe\.codex\worktrees\36d6\wevito\docs\CODE_SIDE_PHASE3_COMMAND_POLICY_PROGRESS_2026-05-05.md`.
- Validation result: focused parser/contract/policy tests passed `20 / 20`; `dotnet build vnext\Wevito.VNext.sln` passed; full vNext tests passed `46 / 46`.
- No tools execute yet. The next safe code-side phase is a draft-only command-bar view model or adapter before touching dirty shell UI files.

## 2026-05-05 Code-Side Command Bar Adapter

- [x] Add `PetCommandBarState` as the Shell-facing state shape for future UI binding.
- [x] Add `PetCommandBarService` to cap helper pets at 3, parse input, create a draft task card, evaluate dry-run policy, and return visible state.
- [x] Keep all submit behavior draft-only: no Tool Broker execution, no browser automation, no filesystem mutation.
- [x] Add focused tests for helper caps, safe read-only task cards, approval-required build cards, missing-policy blocks, and risky-command blocks.
- Phase report: `C:\Users\fishe\.codex\worktrees\36d6\wevito\docs\CODE_SIDE_PHASE4_COMMAND_BAR_ADAPTER_PROGRESS_2026-05-05.md`.
- Validation result: focused command-bar/parser/policy/contract tests passed `25 / 25`; `dotnet build vnext\Wevito.VNext.sln` passed; full vNext tests passed `51 / 51`.
- Shell UI files were intentionally not edited in this phase because `ToolPopupWindow.xaml` and `ToolPopupWindow.xaml.cs` already have unrelated dirty work and should be handled in a focused UI integration pass.

## 2026-05-05 Code-Side Pet Command UI Integration

- [x] Add a visible `PET TASKS` tab to the HUD `TOOLS` strip.
- [x] Add a compact `helpers` popup panel with helper roster, task input, draft status, task kind/tool family, policy result, and latest timeline entry.
- [x] Wire `PET TASKS` to `PetCommandBarService.SubmitDraft` through `ShellCoordinator`.
- [x] Keep the UI draft-only: no Tool Broker command, browser automation, sprite generation/import, filesystem mutation, or task execution.
- [x] Add `open-helpers` hotkey/tool routing support for future deterministic probes.
- Phase report: `C:\Users\fishe\.codex\worktrees\36d6\wevito\docs\CODE_SIDE_PHASE5_PET_COMMAND_UI_PROGRESS_2026-05-05.md`.
- Validation result: `dotnet build vnext\Wevito.VNext.sln` passed; full vNext tests passed `51 / 51`.
- Next safe step: persist draft task cards into a non-executing local queue/store before designing any approval or tool-execution bridge.

## 2026-05-05 Code-Side Task Draft Queue

- [x] Add backward-compatible `CompanionState.TaskCards` storage for queued helper-pet task cards.
- [x] Add `PetTaskCardQueueService` with duplicate replacement, newest-first ordering, and default cap of `25` cards.
- [x] Persist `PET TASKS` submissions through the existing app-state JSON path without adding a database migration.
- [x] Hydrate older saved states with an empty task-card queue.
- [x] Add focused queue and repository round-trip tests.
- Phase report: `C:\Users\fishe\.codex\worktrees\36d6\wevito\docs\CODE_SIDE_PHASE6_TASK_DRAFT_QUEUE_PROGRESS_2026-05-05.md`.
- Validation result: `dotnet build vnext\Wevito.VNext.sln` passed; full vNext tests passed `54 / 54`.
- Next safe step: add a clearer approval/review surface for queued `Draft`, `WaitingForApproval`, and `Blocked` cards before any execution bridge is considered.

## 2026-05-05 Code-Side Task Queue Visibility

- [x] Add `PetCommandBarState.QueuedTaskCards` for Shell display of saved helper-pet task cards.
- [x] Pass saved task cards from `CompanionState.TaskCards` into the `PET TASKS` popup state.
- [x] Show queue totals and grouped status counts for `Draft`, `WaitingForApproval`, and `Blocked`.
- [x] Show the latest three queued cards without adding approval or execution buttons.
- [x] Add contract serialization coverage for queued command-bar state.
- Phase report: `C:\Users\fishe\.codex\worktrees\36d6\wevito\docs\CODE_SIDE_PHASE7_TASK_QUEUE_VISIBILITY_PROGRESS_2026-05-05.md`.
- Validation result: `dotnet build vnext\Wevito.VNext.sln` passed; full vNext tests passed `55 / 55`.
- Next safe step: design the approval/execution bridge on paper before adding any adapter that can run tools.

## 2026-05-05 Code-Side Pet Agent Approval Bridge Design

- [x] Document the approval/execution bridge before implementation.
- [x] Define allowed task-card status transitions from `Draft` through `Done`, `Blocked`, `Failed`, and `Cancelled`.
- [x] Define adapter gate requirements: policy match, dry-run preview, root-path enforcement, audit logs, result summary, cancellation, and no silent mutation.
- [x] Sequence first adapters from read-only docs/audits/proofs toward build proof, with visual generation/import/apply explicitly deferred.
- [x] Identify required UI controls before execution: selected card detail, approve, cancel, blocked reason, dry-run preview, and audit/proof path.
- Phase report: `C:\Users\fishe\.codex\worktrees\36d6\wevito\docs\CODE_SIDE_PHASE8_PET_AGENT_APPROVAL_BRIDGE_DESIGN_2026-05-05.md`.
- Validation result: design-only phase after the latest green build/test pass: `dotnet build vnext\Wevito.VNext.sln` passed; full vNext tests passed `55 / 55`.
- Next safe implementation step: add approval-state editing only, with no adapter execution.

## 2026-05-05 Code-Side Task Approval State

- [x] Add local task-card status transitions without execution.
- [x] Allow `WaitingForApproval` -> `Approved`.
- [x] Allow `Draft`, `WaitingForApproval`, and `Approved` -> `Cancelled`.
- [x] Block plain `Draft` -> `Approved` so safe read-only drafts do not imply execution readiness.
- [x] Add `PET TASKS` queue selector plus `APPROVE` and `CANCEL` buttons.
- [x] Persist approved/cancelled state into `CompanionState.TaskCards`.
- [x] Add focused transition tests.
- Phase report: `C:\Users\fishe\.codex\worktrees\36d6\wevito\docs\CODE_SIDE_PHASE9_TASK_APPROVAL_STATE_PROGRESS_2026-05-05.md`.
- Validation result: `dotnet build vnext\Wevito.VNext.sln` passed; full vNext tests passed `58 / 58`.
- Next safe step: cross-thread coordination checkpoint before implementing any real adapter execution.

## 2026-05-05 Code-Side Cross-Thread Adapter Coordination

- [x] Record current `PET TASKS` implementation state and non-execution boundary.
- [x] Split next adapter work into green/yellow/red lanes.
- [x] Ask visual-side to confirm proof/sprite-audit artifact expectations before code-side implements adapters.
- [x] Recommend keeping the goose `drop_ball` one-row apply/proof outside `PET TASKS` because it mutates runtime PNGs and needs hand-controlled rollback.
- [x] Recommend first pet-agent adapter be no-mutation `spriteAudit` or `localDocs`, not visual apply/proof.
- Phase report: `C:\Users\fishe\.codex\worktrees\36d6\wevito\docs\CODE_SIDE_PHASE10_CROSS_THREAD_ADAPTER_COORDINATION_2026-05-05.md`.
- Validation result: docs-only phase after latest green validation: `dotnet build vnext\Wevito.VNext.sln` passed; full vNext tests passed `58 / 58`.
- Next safe step after visual-side acknowledgement: implement adapter contracts/plumbing for a no-mutation `spriteAudit` or `localDocs` dry-run adapter.

## 2026-05-05 Code-Side Local Docs Preview Adapter

- [x] Add adapter request/result contracts for audited previews.
- [x] Add `TaskAdapterRunMode` and `TaskAdapterResultStatus`.
- [x] Add Core-only `LocalDocsPreviewAdapter`.
- [x] Enforce dry-run preview mode, `localDocs` family match, read-only policy, approved-root requirements, and outside-root blocking.
- [x] Keep adapter non-mutating with `DidMutate = false` and empty `WrittenPaths`.
- [x] Add tests for approved docs preview, outside-root blocking, missing approved roots, execute-mode blocking, and result serialization.
- Phase report: `C:\Users\fishe\.codex\worktrees\36d6\wevito\docs\CODE_SIDE_PHASE11_LOCAL_DOCS_PREVIEW_ADAPTER_2026-05-05.md`.
- Validation result: `dotnet build vnext\Wevito.VNext.sln` passed; full vNext tests passed `63 / 63`.
- Next safe step: add a dispatcher dry-run preview service that can select preview adapters by tool family, still without Shell execution wiring.

## 2026-05-05 Code-Side Sprite Audit Preview Adapter

- [x] Read visual-side PET TASKS adapter expectations from `C:\Users\fishe\Documents\projects\wevito\docs\VISUAL_SIDE_PET_TASKS_ADAPTER_EXPECTATIONS_2026-05-05.md`.
- [x] Add `SpriteAuditFrameFinding` and `SpriteAuditReport` contracts.
- [x] Add Core-only `SpriteAuditPreviewAdapter`.
- [x] Enforce dry-run report mode, `spriteAudit` family match, read-only policy, approved-root requirements, outside-root blocking, and `pet-tasks` artifact-root requirement.
- [x] Write markdown and JSON reports under a new pet-task artifact folder.
- [x] Preserve PNG hashes and avoid runtime/source/prop/candidate mutation.
- [x] Block existing visual packet artifact roots such as animation candidate/proof folders.
- [x] Add focused tests for report output, blocked paths, blocked unsafe artifact roots, execute-mode blocking, and no PNG mutation.
- Phase report: `C:\Users\fishe\.codex\worktrees\36d6\wevito\docs\CODE_SIDE_PHASE12_SPRITE_AUDIT_PREVIEW_ADAPTER_2026-05-05.md`.
- Validation result: `dotnet build vnext\Wevito.VNext.sln` passed; full vNext tests passed `67 / 67`.
- Known limitation: contact-sheet generation is deferred; current minimum output is markdown plus JSON.
- Next safe step: add a dry-run adapter dispatcher that can select `localDocs` or `spriteAudit` preview adapters by `ToolFamily`, still without Shell execution wiring.

## 2026-05-05 Code-Side Adapter Preview Dispatcher

- [x] Add Core-only `PetTaskAdapterPreviewDispatcher`.
- [x] Route `localDocs` dry-run preview requests to `LocalDocsPreviewAdapter`.
- [x] Route `spriteAudit` dry-run preview/report requests to `SpriteAuditPreviewAdapter`.
- [x] Block unknown tool families.
- [x] Block mismatched intent/policy tool families before routing.
- [x] Keep dispatcher disconnected from Shell execution, Godot proofing, sprite import, and runtime/source PNG mutation.
- [x] Add focused dispatcher tests.
- Phase report: `C:\Users\fishe\.codex\worktrees\36d6\wevito\docs\CODE_SIDE_PHASE13_ADAPTER_PREVIEW_DISPATCHER_2026-05-05.md`.
- Validation result: `dotnet build vnext\Wevito.VNext.sln` passed; full vNext tests passed `71 / 71`.
- Next safe step: add a non-executing Shell preview bridge for selected cards only, with explicit UI gating and no `Execute` run mode.

## 2026-05-05 Code-Side PET TASKS Preview Bridge

- [x] Add `PREVIEW` button to the `PET TASKS` selected-card controls.
- [x] Enable dry-run preview only for `Draft` and `Approved` task cards.
- [x] Block `WaitingForApproval` cards from preview until they are approved or cancelled.
- [x] Route preview requests through `PetTaskAdapterPreviewDispatcher`.
- [x] Inject safe approved roots for `localDocs` and `spriteAudit` at preview time.
- [x] Write adapter artifacts only under `vnext\artifacts\pet-tasks\<timestamp-tool-kind>\`.
- [x] Persist preview status, result summary, audit path, and timeline entries back to the task card.
- [x] Keep `Execute` mode, Godot proofing, sprite import, visual generation, prop-anchor edits, and runtime/source PNG mutation unreachable.
- Phase report: `C:\Users\fishe\.codex\worktrees\36d6\wevito\docs\CODE_SIDE_PHASE14_PET_TASKS_PREVIEW_BRIDGE_2026-05-05.md`.
- Validation result: `dotnet build vnext\Wevito.VNext.sln` passed with `0` warnings; full vNext tests passed `73 / 73`.
- Next safe step: add target extraction for local paths and obvious sprite row phrases so preview reports are useful without broad scans.

## 2026-05-05 Code-Side PET TASKS Target Extraction

- [x] Extract quoted and bare Windows absolute paths from helper-pet commands.
- [x] Extract simple sprite row phrases such as `goose baby female blue`.
- [x] Store extracted targets in `TaskIntent.TargetPathsOrAssets`.
- [x] Resolve relative sprite row targets only under approved `spriteAudit` roots.
- [x] Block relative sprite targets that escape approved roots.
- [x] Add parser and sprite-audit adapter tests.
- Phase report: `C:\Users\fishe\.codex\worktrees\36d6\wevito\docs\CODE_SIDE_PHASE15_PET_TASK_TARGET_EXTRACTION_2026-05-05.md`.
- Validation result: `dotnet build vnext\Wevito.VNext.sln` passed with `0` warnings; full vNext tests passed `77 / 77`.
- Next safe step: add a non-mutating PET TASKS preview smoke/probe script for parser/dispatcher/adapter verification without Godot/package proofing.

## 2026-05-05 Code-Side PET TASKS Preview Probe

- [x] Add `PetTasksPreviewSmokeTests`.
- [x] Exercise parser -> dispatcher -> `spriteAudit` adapter end to end.
- [x] Prove `goose baby female blue` is extracted and routed into a focused sprite row report.
- [x] Prove the sampled PNG bytes are unchanged after preview.
- [x] Add `tools\probe-pet-tasks-preview.ps1`.
- [x] Keep the probe separate from Godot/package proofing and visual mutation workflows.
- Phase report: `C:\Users\fishe\.codex\worktrees\36d6\wevito\docs\CODE_SIDE_PHASE16_PET_TASKS_PREVIEW_PROBE_2026-05-05.md`.
- Validation result: PET TASKS preview probe passed `1 / 1`; `dotnet build vnext\Wevito.VNext.sln` passed with `0` warnings; full vNext tests passed `78 / 78`.
- Next safe step: optional manual app smoke of the `PET TASKS` `PREVIEW` button, or pause code-side PET TASKS expansion until visual-side cleanup needs the report surface.

## 2026-05-05 Code-Side PET TASKS Live UI Probe

- [x] Add `tools\probe-vnext-pet-tasks.ps1`.
- [x] Launch vNext Shell with isolated data and trace folders.
- [x] Open `TOOLS` -> `PET TASKS` through UIAutomation.
- [x] Submit `review goose baby female blue sprites`.
- [x] Click `PREVIEW` and wait for a `spriteAudit` `PreviewReady` trace.
- [x] Verify the PET TASKS audit report path exists.
- [x] Verify repo-root and active build-runtime target sprite rows are hash-stable.
- [x] Fix PET TASKS command text being erased by passive render sync before submit.
- Phase report: `C:\Users\fishe\.codex\worktrees\36d6\wevito\docs\CODE_SIDE_PHASE17_PET_TASKS_LIVE_UI_PROBE_2026-05-05.md`.
- Latest probe summary: `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\pet-task-probes\20260505-014813\summary.json`.
- Validation result: live PET TASKS UI probe passed; `dotnet build vnext\Wevito.VNext.sln` passed with `0` warnings; full vNext tests passed `78 / 78`.
- Next safe step: pause PET TASKS expansion unless visual-side cleanup needs more report detail; keep agent execution, Godot proofing, and visual mutation outside PET TASKS for now.

## 2026-05-05 Code-Side Game Interaction Readiness

- [x] Review `tools\build-vnext.ps1` and confirm `-SkipAssetPrep` is the safe code-side build path while visual cleanup is active.
- [x] Run `build-vnext.ps1 -Configuration Debug -SkipAssetPrep -StepTimeoutSeconds 120`.
- [x] Run the runtime action/tool probe.
- [x] Confirm doctor, medicine, bath, groom, feed, water, play, rest, and home action flows trace successfully.
- [x] Confirm settings/save and basket paste/open/delete flows trace successfully.
- [x] Fix `DescribeAging` shell text to use an ASCII separator instead of a middle dot.
- [x] Add test coverage for the shell-safe aging description.
- [x] Refresh the Debug published shell with `-SkipAssetPrep -SkipTests`.
- Phase report: `C:\Users\fishe\.codex\worktrees\36d6\wevito\docs\CODE_SIDE_PHASE18_GAME_INTERACTION_READINESS_2026-05-05.md`.
- Action probe summary: `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\action-probes\20260505-015533\summary.json`.
- Validation result: safe vNext publish passed; runtime action/tool probe passed; `dotnet build vnext\Wevito.VNext.sln` passed with `0` warnings; full vNext tests passed `79 / 79`.
- Next safe step: produce a compact code-side handoff for visual-side coordination or continue a deeper simulation design review.

## 2026-05-05 Code-Side To Visual Handoff

- [x] Summarize PET TASKS report-only infrastructure.
- [x] Summarize live PET TASKS UI probe status.
- [x] Summarize vNext action/tool runtime probe status.
- [x] Re-state `-SkipAssetPrep` as the safe build policy while visual cleanup is active.
- [x] Re-state that PET TASKS must not be used yet for mutation, import, generation, prop anchors, or Godot proofing.
- Handoff doc: `C:\Users\fishe\.codex\worktrees\36d6\wevito\docs\CODE_SIDE_TO_VISUAL_HANDOFF_2026-05-05.md`.

## 2026-05-05 Code-Side Pet Wellbeing Snapshots

- [x] Add derived `PetWellbeingSnapshot` contracts.
- [x] Add `PetWellbeingUrgency`, `PetDriveFamily`, and `PetEmotionChannel`.
- [x] Add `PetWellbeingInterpreter` in Core.
- [x] Convert vitals, habits, personality, statuses, and conditions into read-only debug/agent context.
- [x] Keep snapshots non-persistent and gameplay-neutral.
- [x] Add deterministic interpreter tests.
- Phase report: `C:\Users\fishe\.codex\worktrees\36d6\wevito\docs\CODE_SIDE_PHASE19_PET_WELLBEING_SNAPSHOTS_2026-05-05.md`.
- Validation result: `dotnet build vnext\Wevito.VNext.sln` passed with `0` warnings; full vNext tests passed `83 / 83`.
- Next safe step: expose the wellbeing snapshot in the Shell as read-only debug/helper context.

## 2026-05-05 Code-Side Wellbeing Shell Context

- [x] Add `PetCommandBarState.WellbeingSnapshots`.
- [x] Refresh wellbeing snapshots when rendering PET TASKS, submitting drafts, approving/cancelling cards, and running dry-run previews.
- [x] Add a compact PET TASKS popup wellbeing readout.
- [x] Extend the live PET TASKS probe to assert and record the wellbeing readout.
- [x] Keep the feature display-only and non-mutating.
- Phase report: `C:\Users\fishe\.codex\worktrees\36d6\wevito\docs\CODE_SIDE_PHASE20_WELLBEING_SHELL_CONTEXT_2026-05-05.md`.
- Latest probe summary: `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\pet-task-probes\20260505-021334\summary.json`.
- Validation result: `dotnet build vnext\Wevito.VNext.sln` passed with `0` warnings; full vNext tests passed `83 / 83`; safe vNext publish passed with `-SkipAssetPrep`; live PET TASKS UI probe passed.
- Next safe step: add a read-only debug-truth/pet-state report surface that checks UI/action readiness against derived wellbeing context.

## 2026-05-05 Code-Side Pet Debug Truth Report

- [x] Add `PetDebugTruthReport`, pet entries, and action entries.
- [x] Add `PetDebugTruthReportBuilder`.
- [x] Compare visible pet animation against expected debug-truth hints.
- [x] Explain primary action readiness without mutating gameplay.
- [x] Produce markdown output for future helper-agent reports.
- [x] Add deterministic report builder tests.
- Phase report: `C:\Users\fishe\.codex\worktrees\36d6\wevito\docs\CODE_SIDE_PHASE21_PET_DEBUG_TRUTH_REPORT_2026-05-05.md`.
- Validation result: `dotnet build vnext\Wevito.VNext.sln` passed with `0` warnings; full vNext tests passed `85 / 85`.
- Next safe step: expose this report through a no-mutation `petState` PET TASKS preview adapter that writes only timestamped markdown/JSON artifacts.

## 2026-05-05 Code-Side Pet State Preview Adapter

- [x] Add `TaskKind.ReviewPetState`.
- [x] Route pet-state, wellbeing, personality, pet-status, pet-needs, and debug-truth commands to `petState`.
- [x] Add preview-only `PetStatePreviewAdapter`.
- [x] Register `petState` in the PET TASKS dispatcher and Shell policies.
- [x] Keep `petState` read-only and artifact-only under `vnext\artifacts\pet-tasks`.
- [x] Extend the live PET TASKS probe so it can validate `petState` and still regression-check `spriteAudit`.
- Phase report: `C:\Users\fishe\.codex\worktrees\36d6\wevito\docs\CODE_SIDE_PHASE22_PET_STATE_PREVIEW_ADAPTER_2026-05-05.md`.
- Latest `petState` probe summary: `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\pet-task-probes\20260505-022254\summary.json`.
- Latest `spriteAudit` regression probe summary: `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\pet-task-probes\20260505-022329\summary.json`.
- Validation result: `dotnet build vnext\Wevito.VNext.sln` passed with `0` warnings; full vNext tests passed `89 / 89`; safe vNext publish passed with `-SkipAssetPrep`; live `petState` and `spriteAudit` UI probes passed.
- Next safe step: create a concise visual-side coordination update before any further PET TASKS capability expansion.

## 2026-05-05 Code-Side Cleanup And Runtime Validation

- [x] Ignore top-level `artifacts/` scratch/recovery output without deleting it.
- [x] Preserve visual-side `sprites_runtime` edits instead of reverting or normalizing them.
- [x] Re-run sprite contract validation: `30 / 30` source boards, `17 / 17` supporting inputs, `360 / 360` runtime variant dirs, `10800 / 10800` runtime frames, `0` errors.
- [x] Re-run runtime canvas mismatch reporter: `2880` sequences, `10800` frames, `0` mixed-canvas rows, `0` missing, `0` invalid.
- [x] Keep canonical-size mismatches as informational under the current natural-motion/no-boxing visual direction.
- [x] Re-run `dotnet build .\vnext\Wevito.VNext.sln`: passed with `0` warnings and `0` errors.
- [x] Re-run full vNext tests: passed `89 / 89`.
- [x] Re-run safe Debug publish with `-SkipAssetPrep -SkipTests`: passed.
- [x] Re-run action/tool probe: passed.
- [x] Fix the PET TASKS live probe harness to launch with an isolated pinned audit scenario so passive HUD collapse does not produce false missing-button failures.
- [x] Re-run live `spriteAudit` PET TASKS probe: passed with target sprite hashes unchanged.
- [x] Re-run live `petState` PET TASKS probe: passed.
- Cleanup report: `C:\Users\fishe\.codex\worktrees\36d6\wevito\docs\CODE_SIDE_CLEANUP_VALIDATION_2026-05-05.md`.
- Latest action probe summary: `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\action-probes\20260505-123214\summary.json`.
- Latest `spriteAudit` probe summary: `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\pet-task-probes\20260505-123138-223-450ea95a\summary.json`.
- Latest `petState` probe summary: `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\pet-task-probes\20260505-123138-240-0556575e\summary.json`.
- Next safe step: continue original code-side plan with non-mutating PET TASKS capability hardening and planning only; do not start mutation/import/generation.

## 2026-05-05 Code-Side Local Docs Report Artifacts

- [x] Add `LocalDocPreviewEntry` and `LocalDocsPreviewReport` contracts.
- [x] Make `localDocs` write `local-docs-preview-report.json`.
- [x] Make `localDocs` write `run-summary.md`.
- [x] Return `AuditLogPath` so the PET TASKS popup/report flow is consistent with `spriteAudit` and `petState`.
- [x] Block unsafe artifact roots outside a `pet-tasks` artifact folder.
- [x] Preserve read-only approved-root enforcement for target docs.
- [x] Preserve dry-run-only behavior.
- [x] Run focused adapter tests: passed `4 / 4`.
- [x] Run full vNext tests: passed `90 / 90`.
- [x] Run safe Debug publish with `-SkipAssetPrep -SkipTests`: passed.
- [x] Run live `localDocs` PET TASKS UI probe: passed.
- Phase report: `C:\Users\fishe\.codex\worktrees\36d6\wevito\docs\CODE_SIDE_PHASE23_LOCAL_DOCS_REPORT_ARTIFACTS_2026-05-05.md`.
- Latest `localDocs` probe summary: `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\pet-task-probes\20260505-123738-099-1e20daec\summary.json`.
- Latest `localDocs` report: `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\pet-tasks\20260505-163752-localdocs-summarizedocs\run-summary.md`.
- Next safe step: add read-only capability visibility/clarity in PET TASKS so users can understand what helper pets can preview without implying autonomous mutation.

## 2026-05-05 Code-Side PET TASKS Capability Clarity

- [x] Add a compact PET TASKS capability line naming preview-ready tools: `spriteAudit`, `petState`, `localDocs`.
- [x] Explicitly label locked actions: sprite import, generation, PNG mutation, prop anchors, build/proof execution, browser automation.
- [x] Extend the live PET TASKS probe to assert and record the capability text.
- [x] Keep the phase display-only and non-mutating.
- [x] Run full vNext tests: passed `90 / 90`.
- [x] Run safe Debug publish with `-SkipAssetPrep -SkipTests`: passed.
- [x] Run live `spriteAudit`, `localDocs`, and `petState` PET TASKS UI probes: all passed.
- Phase report: `C:\Users\fishe\.codex\worktrees\36d6\wevito\docs\CODE_SIDE_PHASE24_PET_TASKS_CAPABILITY_CLARITY_2026-05-05.md`.
- Latest `spriteAudit` probe summary: `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\pet-task-probes\20260505-124027-472-a16363a4\summary.json`.
- Latest `localDocs` probe summary: `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\pet-task-probes\20260505-124027-499-4042d40e\summary.json`.
- Latest `petState` probe summary: `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\pet-task-probes\20260505-124027-472-7d4a79dd\summary.json`.
- Next safe step: create a code-side visual handoff update before additional PET TASKS expansion.

## 2026-05-05 Code-Side To Visual Handoff Phase 24 Update

- [x] Summarize cleanup validation.
- [x] Summarize `localDocs` report artifact behavior.
- [x] Summarize PET TASKS capability clarity.
- [x] Re-state no-mutation/no-generation/no-import boundaries.
- [x] Re-state `-SkipAssetPrep` policy.
- Handoff doc: `C:\Users\fishe\.codex\worktrees\36d6\wevito\docs\CODE_SIDE_TO_VISUAL_HANDOFF_PHASE24_UPDATE_2026-05-05.md`.

## 2026-05-05 Tool-Agent UI Master Plan

- [x] Review The-Workinator app shape, concrete plan, audit/roadmap, capture flow, and coding workspace.
- [x] Convert Workinator lessons into a Wevito-specific one-entry-point helper-pet plan.
- [x] Add phase plan for Tool Hub, capture, asset inventory, code review, code patch planning, build/test proofing, Creative Learning Lab, sprite workflow, controlled mutation, short clips, and browser/research.
- [x] Keep the plan simplicity-first: one user request, one result card, advanced details hidden.
- Master plan doc: `C:\Users\fishe\.codex\worktrees\36d6\wevito\docs\WEVITO_TOOL_AGENT_UI_MASTER_PLAN_2026-05-05.md`.
- Recommended next step: Phase 25 Tool Hub information architecture, then Phase 26 Tool Hub shell refactor.

## 2026-05-05 Translation And Audio Tool Research

- [x] Research translation providers: DeepL, Google Cloud Translation, Azure AI Translator, LibreTranslate.
- [x] Research Windows audio implementation paths: Core Audio APIs, Audio Processing Objects, Equalizer APO, FxSound, EarTrumpet.
- [x] Add `translateText` tool-family phases to the master plan.
- [x] Add `audioAssist` tool-family phases to the master plan.
- [x] Keep translation network calls approval-gated.
- [x] Keep audio changes approval-gated.
- [x] Keep driver/APO install/config mutation blocked for now.
- Research plan doc: `C:\Users\fishe\.codex\worktrees\36d6\wevito\docs\TRANSLATION_AND_AUDIO_TOOL_RESEARCH_PLAN_2026-05-05.md`.

## 2026-05-05 Phase 25 Tool Hub Information Architecture

- [x] Define the compact PET TASKS information hierarchy: ask bar, safety/capability line, helper roles, current task, result/next action.
- [x] Keep all tool families behind one entry point instead of adding separate tool tabs.
- [x] Preserve the no-mutation/no-generation/no-import boundary for the first Tool Hub phases.
- [x] Define the Phase 26 UI refactor scope and validation plan.
- Phase 25 doc: `C:\Users\fishe\.codex\worktrees\36d6\wevito\docs\WEVITO_TOOL_HUB_INFORMATION_ARCHITECTURE_2026-05-05.md`.
- Next safe step: Phase 26 compact Tool Hub shell refactor, preserving PET TASKS automation IDs and no-mutation behavior.

## 2026-05-05 Phase 26 Tool Hub Shell Refactor

- [x] Rename the helper popup title to `Wevito PET TASKS` / `PET TASKS`.
- [x] Move the PET TASKS command textbox to the top of the helper surface.
- [x] Keep the safety/capability line visible and mark translation calls/audio changes as locked.
- [x] Add `PetTaskNextActionText` so the popup states the next safe user action.
- [x] Preserve existing PET TASKS automation IDs used by live probes.
- [x] Update the live PET TASKS probe for the renamed window and next-action text.
- [x] Run `dotnet build .\vnext\Wevito.VNext.sln`: passed with `0` warnings and `0` errors.
- [x] Run full vNext tests: passed `90 / 90`.
- [x] Run safe Debug publish with `-SkipAssetPrep -SkipTests`: passed.
- [x] Run live `spriteAudit`, `localDocs`, and `petState` PET TASKS UI probes: all passed.
- [x] Confirm PET TASKS probes left target sprite row hashes unchanged.
- Phase 26 report: `C:\Users\fishe\.codex\worktrees\36d6\wevito\docs\CODE_SIDE_PHASE26_TOOL_HUB_SHELL_REFACTOR_2026-05-05.md`.
- Latest `spriteAudit` probe summary: `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\pet-task-probes\20260505-130720-360-0e1c0d54\summary.json`.
- Latest `localDocs` probe summary: `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\pet-task-probes\20260505-130747-092-90efa10b\summary.json`.
- Latest `petState` probe summary: `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\pet-task-probes\20260505-130807-903-df134d0d\summary.json`.
- Next safe step: Phase 27 capture contracts and policy; do not implement screenshot or clip capture until capture scope, privacy, and artifact policy are explicit.

## 2026-05-05 Phase 27 Capture Contracts And Policy

- [x] Add capture request/result/manifest contracts.
- [x] Add capture preset, target, output, and privacy enums.
- [x] Add `CapturePolicyEvaluator`.
- [x] Allow Wevito-window/proof-surface still capture as low risk.
- [x] Require approval for selected region, last region, foreground window, full desktop, and recording outputs.
- [x] Block external upload/share.
- [x] Add contract round-trip tests for capture manifests/results.
- [x] Add capture policy tests for allowed, approval-required, high-risk, recording, and blocked-share paths.
- [x] Run focused capture/contract tests: passed `15 / 15`.
- [x] Run `dotnet build .\vnext\Wevito.VNext.sln`: passed with `0` warnings and `0` errors.
- [x] Run full vNext tests: passed `99 / 99`.
- [x] Run safe Debug publish with `-SkipAssetPrep -SkipTests`: passed.
- Phase 27 report: `C:\Users\fishe\.codex\worktrees\36d6\wevito\docs\CODE_SIDE_PHASE27_CAPTURE_CONTRACTS_POLICY_2026-05-05.md`.
- Next safe step: Phase 28 Wevito-window screenshot only; no full desktop capture, no recording, no external upload/share.

## 2026-05-05 Phase 28 Wevito Window Capture Proof

- [x] Add a narrow Wevito-window capture proof script.
- [x] Launch vNext shell with an isolated pinned scenario.
- [x] Support `-OpenPetTasks` to capture the PET TASKS proof surface.
- [x] Capture only a Wevito-owned window rectangle by process id/title.
- [x] Write `screenshot.png`, `manifest.json`, and `run-summary.md` under a timestamped `vnext\artifacts\pet-tasks` folder.
- [x] Record capture preset, target, privacy, output path, manifest path, region, and upload/share status.
- [x] Keep capture local-only, Wevito-only, non-recording, and non-uploading.
- [x] Fix capture timestamp to true UTC.
- [x] Harden launch state with a pinned capture scenario after an initial tab lookup flake.
- [x] Run PET TASKS capture proof: passed.
- Phase 28 report: `C:\Users\fishe\.codex\worktrees\36d6\wevito\docs\CODE_SIDE_PHASE28_WEVITO_WINDOW_CAPTURE_PROOF_2026-05-05.md`.
- Latest capture summary: `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\pet-tasks\20260505-131621-capture-wevito-window\run-summary.md`.
- Latest capture screenshot: `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\pet-tasks\20260505-131621-capture-wevito-window\screenshot.png`.
- Latest capture manifest: `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\pet-tasks\20260505-131621-capture-wevito-window\manifest.json`.
- Next safe step: Phase 29 asset inventory report tool, read-only only.

## 2026-05-05 Phase 29 Asset Inventory Report Tool

- [x] Add `TaskKind.InventoryAssets`.
- [x] Add asset inventory report contracts.
- [x] Add read-only `AssetInventoryPreviewAdapter`.
- [x] Route `assetInventory` through the PET TASKS dispatcher.
- [x] Parse user commands such as `inventory assets` and `count assets`.
- [x] Add shell policy and approved roots for `sprites_runtime` and `sprites_shared_runtime`.
- [x] Add `assetInventory` to the PET TASKS capability line and probe assertion.
- [x] Count runtime variant folders, runtime PNGs, shared PNGs, import sidecars, JSON files, and basic stale findings.
- [x] Keep output to markdown/JSON only; no contact sheets, no sprite mutation, no generation.
- [x] Run focused parser/dispatcher/adapter tests: passed `20 / 20` after rerunning alone because the first parallel attempt collided with the build writing the contracts DLL.
- [x] Run `dotnet build .\vnext\Wevito.VNext.sln`: passed with `0` warnings and `0` errors.
- [x] Run full vNext tests: passed `104 / 104`.
- [x] Run safe Debug publish with `-SkipAssetPrep -SkipTests`: passed.
- [x] Run live `assetInventory` PET TASKS UI probe: passed.
- Phase 29 report: `C:\Users\fishe\.codex\worktrees\36d6\wevito\docs\CODE_SIDE_PHASE29_ASSET_INVENTORY_REPORT_TOOL_2026-05-05.md`.
- Latest asset inventory probe summary: `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\pet-task-probes\20260505-132232-002-14ed7aee\summary.json`.
- Latest asset inventory report: `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\pet-tasks\20260505-172245-assetinventory-inventoryassets\run-summary.md`.
- Latest live asset counts: `360` runtime variant folders, `10800` runtime PNGs, `554` shared PNGs, `0` findings.
- Next safe step: Phase 30 code review preview tool, read-only only.

## 2026-05-05 Phase 30 Code Review Preview Tool

- [x] Add `TaskKind.ReviewCode`.
- [x] Add static code-review report contracts.
- [x] Add read-only `CodeReviewPreviewAdapter`.
- [x] Route `codeReview` through the PET TASKS dispatcher.
- [x] Parse commands such as `review code`, `code review`, and `inspect code`.
- [x] Add shell policy and approved roots for `vnext\src`, `vnext\tests`, `tools`, and `scripts`.
- [x] Add `codeReview` to the PET TASKS capability line and live probe assertion.
- [x] Detect merge conflict markers, TODO/FIXME markers, long lines, trailing whitespace, and recursive PowerShell delete warnings.
- [x] Keep output to markdown/JSON only; no code edits, no command execution, no asset mutation.
- [x] Correct early report noise by skipping `.codex-cache`, removing Markdown from code-review extensions, and narrowing shell roots away from full-repo scans.
- [x] Run focused parser/dispatcher/adapter tests: passed `23 / 23`.
- [x] Run `dotnet build .\vnext\Wevito.VNext.sln`: passed with `0` warnings and `0` errors.
- [x] Run full vNext tests: passed `110 / 110`.
- [x] Run safe Debug publish with `-SkipAssetPrep -SkipTests`: passed.
- [x] Run live `codeReview` PET TASKS UI probe: passed.
- [x] Confirm PET TASKS probe left target sprite row hashes unchanged.
- Phase 30 report: `C:\Users\fishe\.codex\worktrees\36d6\wevito\docs\CODE_SIDE_PHASE30_CODE_REVIEW_PREVIEW_TOOL_2026-05-05.md`.
- Latest code review probe summary: `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\pet-task-probes\20260505-133216-429-b5ab6972\summary.json`.
- Latest code review report: `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\pet-tasks\20260505-173230-codereview-reviewcode\run-summary.md`.
- Latest live code review summary: `80` files scanned, `200` findings, `didMutateCode=false`; main pattern is trailing whitespace in Godot scripts.
- Next safe step: Phase 31 code patch plan tool, read-only only.

## 2026-05-05 Phase 31 Code Patch Plan Tool

- [x] Add `TaskKind.PlanCodePatch`.
- [x] Add code patch plan report contracts.
- [x] Add read-only `CodePatchPlanPreviewAdapter`.
- [x] Route `codePatchPlan` through the PET TASKS dispatcher.
- [x] Parse commands such as `plan a code fix`, `code patch plan`, and `patch plan`.
- [x] Add shell policy and approved roots for `vnext\src`, `vnext\tests`, `tools`, and `scripts`.
- [x] Add `codePatchPlan` to the PET TASKS capability line and live probe assertion.
- [x] Generate candidate files, proposed scope, implementation steps, validation plan, rollback plan, and safety gates.
- [x] Keep output to markdown/JSON only; no code edits, no command execution, no asset mutation.
- [x] Block execute mode and targets outside approved roots.
- [x] Preserve visual-side safety gates: no runtime/source PNG mutation, no generation/import, no prop-anchor edits, and no asset prep.
- [x] Run focused contract/parser/dispatcher/adapter tests: passed `32 / 32`.
- [x] Run `dotnet build .\vnext\Wevito.VNext.sln`: passed with `0` warnings and `0` errors.
- [x] Run full vNext tests: passed `116 / 116`.
- [x] Run safe Debug publish with `-SkipAssetPrep -SkipTests`: passed.
- [x] Run live `codePatchPlan` PET TASKS UI probe: passed.
- [x] Confirm PET TASKS probe left target sprite row hashes unchanged.
- Phase 31 report: `C:\Users\fishe\.codex\worktrees\36d6\wevito\docs\CODE_SIDE_PHASE31_CODE_PATCH_PLAN_TOOL_2026-05-05.md`.
- Latest code patch plan probe summary: `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\pet-task-probes\20260505-134019-028-554bdc30\summary.json`.
- Latest code patch plan report: `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\pet-tasks\20260505-174032-codepatchplan-plancodepatch\run-summary.md`.
- Latest live code patch plan summary: `40` candidate files, `didMutate=false`, target sprite row hashes unchanged.
- Next safe step: Phase 32 build/test proof task planning, approval-gated and still using `-SkipAssetPrep`.

## 2026-05-05 Phase 32 Build Proof Plan Tool

- [x] Add build proof plan report contracts.
- [x] Add `BuildProofPreviewAdapter`.
- [x] Route `buildProof` through the PET TASKS dispatcher.
- [x] Preserve task-card approval requirement for `buildProof`.
- [x] Extend the live PET TASKS probe with `-ApproveBeforePreview`.
- [x] Add `buildProof plans` to the PET TASKS capability line while keeping build/proof execution locked.
- [x] Generate a command plan for solution build, vNext tests, safe Debug publish with `-SkipAssetPrep`, and optional live UI smoke proof.
- [x] Keep output to markdown/JSON only; no command execution, no code edits, no asset mutation.
- [x] Include stop conditions for missing proof inputs, any required asset prep, visual mutation needs, and conflicting dirty work.
- [x] Run focused contract/parser/dispatcher/adapter tests: passed `33 / 33`.
- [x] Run `dotnet build .\vnext\Wevito.VNext.sln`: passed with `0` warnings and `0` errors.
- [x] Run full vNext tests: passed `120 / 120`.
- [x] Run safe Debug publish with `-SkipAssetPrep -SkipTests`: passed.
- [x] Run live approval-aware `buildProof` PET TASKS UI probe: passed.
- [x] Confirm PET TASKS probe left target sprite row hashes unchanged.
- Phase 32 report: `C:\Users\fishe\.codex\worktrees\36d6\wevito\docs\CODE_SIDE_PHASE32_BUILD_PROOF_PLAN_TOOL_2026-05-05.md`.
- Latest build proof probe summary: `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\pet-task-probes\20260505-134547-188-21e32969\summary.json`.
- Latest build proof plan report: `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\pet-tasks\20260505-174601-buildproof-buildproof\run-summary.md`.
- Latest live build proof summary: `4` commands planned, `didRunCommands=false`, `didMutate=false`, target sprite row hashes unchanged.
- Next safe step: Phase 33 proof execution design only; do not unlock PET TASKS command execution without explicit approval.

## 2026-05-05 Phase 33 Proof Execution Design

- [x] Document future proof execution gate shape.
- [x] Define allowlist-only execution constraints.
- [x] Define proposed proof execution contracts.
- [x] Define simple PET TASKS UI flow for plan -> approve -> run one -> review.
- [x] Define required guardrails for protected sprites, asset prep, admin privileges, output roots, and unrelated dirty work.
- [x] Define future validation sequence using fake runner/stub execution before any real command execution.
- [x] Keep implementation held; no code execution adapter was added.
- Phase 33 design doc: `C:\Users\fishe\.codex\worktrees\36d6\wevito\docs\CODE_SIDE_PHASE33_PROOF_EXECUTION_DESIGN_2026-05-05.md`.
- Current decision: Phase 32 buildProof planning is enough for now while visual-side sprite cleanup is active.

## 2026-05-05 Phase 34 Translation Preview Tool

- [x] Add `TaskKind.TranslateText`.
- [x] Add translation provider status contracts.
- [x] Add `TranslationProviderRouter`.
- [x] Add read-only `TranslationPreviewAdapter`.
- [x] Route `translateText` through the PET TASKS dispatcher.
- [x] Parse commands such as `translate Hello goose to Spanish`.
- [x] Add shell policy for `translateText`.
- [x] Add `translateText preview` to the PET TASKS capability line while keeping translation provider calls locked.
- [x] Report provider readiness for DeepL, Google Cloud Translation, Azure AI Translator, and LibreTranslate.
- [x] Prefer DeepL when credentials are configured.
- [x] Keep output to markdown/JSON only; no provider call, no network transfer, no asset mutation.
- [x] Run focused contract/parser/dispatcher/adapter tests: passed `37 / 37`.
- [x] Run `dotnet build .\vnext\Wevito.VNext.sln`: passed with `0` warnings and `0` errors.
- [x] Run full vNext tests: passed `126 / 126`.
- [x] Run safe Debug publish with `-SkipAssetPrep -SkipTests`: passed.
- [x] Run live `translateText` PET TASKS UI probe: passed.
- [x] Confirm PET TASKS probe left target sprite row hashes unchanged.
- Phase 34 report: `C:\Users\fishe\.codex\worktrees\36d6\wevito\docs\CODE_SIDE_PHASE34_TRANSLATION_PREVIEW_TOOL_2026-05-05.md`.
- Latest translation probe summary: `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\pet-task-probes\20260505-140123-401-141e42b5\summary.json`.
- Latest translation preview report: `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\pet-tasks\20260505-180137-translatetext-translatetext\run-summary.md`.
- Latest live translation preview summary: text `Hello goose`, target `Spanish`, preferred provider `DeepL`, `didCallProvider=false`, `didMutate=false`, target sprite row hashes unchanged.
- Next safe step: DeepL execution adapter design/implementation, blocked unless credentials are present and explicit approval is granted.

## 2026-05-05 Phase 35 Translation DeepL Execution Adapter

- [x] Add `TranslationExecutionReport`.
- [x] Add `DeepLTranslationClient`.
- [x] Add `TranslationExecutionAdapter`.
- [x] Support DeepL auth through `DEEPL_API_KEY` or `DEEPL_AUTH_KEY`.
- [x] Select DeepL Free endpoint for keys ending in `:fx`, otherwise default to the Pro endpoint.
- [x] Build DeepL JSON requests with `text`, required `target_lang`, optional `source_lang`, and `show_billed_characters`.
- [x] Enforce explicit execute mode.
- [x] Enforce approval-gated network policy.
- [x] Block missing credentials.
- [x] Block missing/unknown target language.
- [x] Block text over DeepL's 128 KiB request limit.
- [x] Write `translated-text.txt`, `translation-execution-report.json`, and `run-summary.md` when execution succeeds.
- [x] Ensure API keys are not written to artifacts.
- [x] Keep PET TASKS UI provider calls locked; no user-facing live provider execution button was added.
- [x] Run focused fake-provider execution tests: passed `17 / 17`.
- [x] Run `dotnet build .\vnext\Wevito.VNext.sln`: passed with `0` warnings and `0` errors.
- [x] Run full vNext tests: passed `130 / 130`.
- [x] Run safe Debug publish with `-SkipAssetPrep -SkipTests`: passed.
- Phase 35 report: `C:\Users\fishe\.codex\worktrees\36d6\wevito\docs\CODE_SIDE_PHASE35_TRANSLATION_DEEPL_EXECUTION_ADAPTER_2026-05-05.md`.
- Current translation state: preview is live in PET TASKS; DeepL execution code is tested with fake HTTP; live provider execution still needs an explicit approval/execution UI gate plus credentials.
- Next safe step: audioAssist contracts and policy, or translation execution-gate UI if the user wants live translation before audio.

## 2026-05-05 Phase 36 Translation Execution Gate UI

- [x] Add PET TASKS `RUN` button with automation id `PetTaskExecuteButton`.
- [x] Add `PetTaskExecutionRequested` event.
- [x] Add shell execution handling for reviewed `translateText` task cards.
- [x] Keep translation execution policy network + medium risk + approval-gated.
- [x] Enable `RUN` only after a `translateText` card reaches `Reviewing`.
- [x] Update translation next-action text to tell the user to open the preview report before running.
- [x] Allow reviewed execution results to move to `Done`.
- [x] Extend live PET TASKS probe with `-ExpectExecuteEnabledAfterPreview`.
- [x] Keep provider calls explicit and credential-gated.
- [x] Keep non-translation execution rejected.
- [x] Run focused queue/translation tests: passed `14 / 14`.
- [x] Run `dotnet build .\vnext\Wevito.VNext.sln`: passed with `0` warnings and `0` errors.
- [x] Run full vNext tests: passed `131 / 131`.
- [x] Run safe Debug publish with `-SkipAssetPrep -SkipTests`: passed.
- [x] Run live `translateText` PET TASKS UI probe with execute-button assertion: passed.
- [x] Confirm PET TASKS probe left target sprite row hashes unchanged.
- Phase 36 report: `C:\Users\fishe\.codex\worktrees\36d6\wevito\docs\CODE_SIDE_PHASE36_TRANSLATION_EXECUTION_GATE_UI_2026-05-05.md`.
- Latest translation execution-gate probe summary: `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\pet-task-probes\20260505-142316-438-4c02437a\summary.json`.
- Latest translation preview report from gate proof: `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\pet-tasks\20260505-182329-translatetext-translatetext\run-summary.md`.
- Current translation state: preview works, `RUN` gate appears after preview, DeepL execution path exists, live provider execution requires a configured key.
- Next safe step: audioAssist contracts and policy.

## 2026-05-05 Phase 37 Audio Assist Status Preview

- [x] Add `TaskKind.AudioAssist`.
- [x] Add audio action vocabulary for inspect volume, set volume, mute, unmute, boost guidance, and external enhancer handoff.
- [x] Add read-only `AudioAssistPreviewAdapter`.
- [x] Route `audioAssist` through the PET TASKS dispatcher.
- [x] Parse commands such as `check audio volume`.
- [x] Add shell policy for `audioAssist`.
- [x] Add `audioAssist status` to the PET TASKS capability line while keeping audio changes locked.
- [x] Keep output to markdown/JSON only; no audio change, no driver/APO/equalizer config, no asset mutation.
- [x] Run focused audio/parser/dispatcher/contract tests: passed `40 / 40`.
- [x] Run `dotnet build .\vnext\Wevito.VNext.sln`: passed with `0` warnings and `0` errors.
- [x] Run full vNext tests: passed `136 / 136`.
- [x] Run safe Debug publish with `-SkipAssetPrep -SkipTests`: passed.
- [x] Run live `audioAssist` PET TASKS UI probe: passed.
- [x] Confirm PET TASKS probe left target sprite row hashes unchanged.
- Phase 37 report: `C:\Users\fishe\.codex\worktrees\36d6\wevito\docs\CODE_SIDE_PHASE37_AUDIO_ASSIST_STATUS_PREVIEW_2026-05-05.md`.
- Latest audioAssist probe summary: `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\pet-task-probes\20260505-142735-988-0763db87\summary.json`.
- Latest audioAssist report: `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\pet-tasks\20260505-182749-audioassist-audioassist\run-summary.md`.
- Current audio state: preview works and policy is in place; real Windows endpoint inspection is not wired yet; volume/mute changes remain locked.
- Next safe step: add fakeable read-only Windows endpoint inspection.

## 2026-05-05 Phase 38 Audio Assist Windows Status Inspection

- [x] Add `AudioEndpointStatus` to PET TASKS contracts.
- [x] Add fakeable `IAudioEndpointStatusReader`.
- [x] Add Windows Core Audio default render endpoint reader.
- [x] Report current default output volume percentage and mute state when available.
- [x] Keep audioAssist preview read-only.
- [x] Keep volume/mute changes locked.
- [x] Keep external volume boosters/equalizers as handoff-only.
- [x] Run focused audio/parser/dispatcher/contract tests: passed `40 / 40`.
- [x] Run `dotnet build .\vnext\Wevito.VNext.sln`: passed with `0` warnings and `0` errors.
- [x] Run full vNext tests: passed `136 / 136`.
- [x] Run safe Debug publish with `-SkipAssetPrep -SkipTests`: passed.
- [x] Run live `audioAssist` PET TASKS UI probe: passed.
- [x] Confirm PET TASKS probe left target sprite row hashes unchanged.
- Phase 38 report: `C:\Users\fishe\.codex\worktrees\36d6\wevito\docs\CODE_SIDE_PHASE38_AUDIO_ASSIST_WINDOWS_STATUS_2026-05-05.md`.
- Latest audioAssist Windows status probe summary: `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\pet-task-probes\20260505-143255-067-9a9f88c0\summary.json`.
- Latest audioAssist Windows status report: `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\pet-tasks\20260505-183308-audioassist-audioassist\run-summary.md`.
- Live result: default output volume `100%`, muted `False`, audio changed `False`, target sprite row hashes unchanged.
- Next safe step: add an explicitly approval-gated normal Windows volume/mute execution adapter.

## 2026-05-05 Phase 39 Audio Assist Approval-Gated Execution

- [x] Add `AudioAssistExecutionReport`.
- [x] Add fakeable `IAudioEndpointController`.
- [x] Extend Windows Core Audio support to set normal endpoint volume and mute state.
- [x] Add `AudioAssistExecutionAdapter`.
- [x] Require explicit execute mode.
- [x] Require approval-gated write policy.
- [x] Support `set volume` from `0%` through `100%`.
- [x] Support `mute` and `unmute`.
- [x] Block boost beyond normal Windows endpoint volume.
- [x] Keep drivers, APOs, equalizers, and external enhancer config as handoff-only.
- [x] Enable PET TASKS `RUN` only for reviewed audio action cards, not plain volume inspection.
- [x] Update capability text to separate approval-gated normal audio from locked boosters/drivers/APOs.
- [x] Run focused audio execution/preview/parser/dispatcher/contract tests: passed `44 / 44`.
- [x] Run `dotnet build .\vnext\Wevito.VNext.sln`: passed with `0` warnings and `0` errors.
- [x] Run full vNext tests: passed `140 / 140`.
- [x] Run safe Debug publish with `-SkipAssetPrep -SkipTests`: passed.
- [x] Run live `audioAssist` UI probe stopping at the approval gate: passed.
- [x] Confirm `RUN` is enabled after preview for `set volume to 100`.
- [x] Confirm PET TASKS probe left target sprite row hashes unchanged.
- Phase 39 report: `C:\Users\fishe\.codex\worktrees\36d6\wevito\docs\CODE_SIDE_PHASE39_AUDIO_ASSIST_APPROVAL_EXECUTION_2026-05-05.md`.
- Latest audioAssist execution-gate probe summary: `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\pet-task-probes\20260505-144012-626-f79fddea\summary.json`.
- Latest audioAssist execution-gate preview report: `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\pet-tasks\20260505-184026-audioassist-audioassist\run-summary.md`.
- Current audio state: preview reads live Windows volume/mute state; normal volume/mute execution exists behind reviewed `RUN`; live probe did not click `RUN` and did not change system volume.
- Next safe step: add screen/screenshot capture preview adapter.

## 2026-05-05 Phase 40 Screen Capture Preview

- [x] Add `TaskKind.ScreenCapture`.
- [x] Add screen capture action/capability contracts.
- [x] Add read-only `ScreenCapturePreviewAdapter`.
- [x] Route screenshot/screen-capture requests through PET TASKS.
- [x] Add shell policy for `screenCapture`.
- [x] Add `screenCapture preview` to the PET TASKS capability line.
- [x] Mark future window/region screenshots as approval-gated.
- [x] Mark full-desktop screenshots as high risk.
- [x] Keep screen recording/GIF capture blocked until a separate privacy/duration gate exists.
- [x] Keep output to markdown/JSON only; no screenshot, no recording, no asset mutation.
- [x] Run focused screen capture/parser/dispatcher/contract tests: passed `42 / 42`.
- [x] Run `dotnet build .\vnext\Wevito.VNext.sln`: passed with `0` warnings and `0` errors.
- [x] Run full vNext tests: passed `144 / 144`.
- [x] Run safe Debug publish with `-SkipAssetPrep -SkipTests`: passed.
- [x] Run live `screenCapture` PET TASKS UI probe: passed.
- [x] Confirm PET TASKS probe left target sprite row hashes unchanged.
- Phase 40 report: `C:\Users\fishe\.codex\worktrees\36d6\wevito\docs\CODE_SIDE_PHASE40_SCREEN_CAPTURE_PREVIEW_2026-05-05.md`.
- Latest screenCapture probe summary: `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\pet-task-probes\20260505-144510-522-75880ffe\summary.json`.
- Latest screenCapture preview report: `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\pet-tasks\20260505-184524-screencapture-screencapture\run-summary.md`.
- Current screen capture state: preview works and writes markdown/JSON only; screenshot execution is not implemented yet; recording remains blocked.
- Next safe step: add approval-gated Wevito-window screenshot execution only.

## 2026-05-05 Phase 41 Under-the-Hood PET TASKS Behavior

- [x] Record product decision: PET TASKS should not trigger special task completion/review/failure animations.
- [x] Remove visible pet animation pulses from PET TASKS command submission.
- [x] Remove visible pet animation pulses from PET TASKS approve/cancel transitions.
- [x] Remove visible pet animation pulses from preview and execution results.
- [x] Remove visible pet animation pulses from clipboard/link basket helper events.
- [x] Remove ambient `Waiting` animation override while tool windows are open.
- [x] Preserve normal pet-sim animation behavior for care/actions/dev scenarios.
- [x] Keep task status visible through PET TASKS cards, feedback text, reports, and trace logs.
- [x] Run `dotnet build .\vnext\Wevito.VNext.sln`: passed with `0` warnings and `0` errors.
- [x] Run full vNext tests: passed `144 / 144`.
- [x] Run safe Debug publish with `-SkipAssetPrep -SkipTests`: passed.
- [x] Run live `screenCapture` PET TASKS UI probe: passed.
- [x] Confirm PET TASKS probe left target sprite row hashes unchanged.
- Phase 41 report: `C:\Users\fishe\.codex\worktrees\36d6\wevito\docs\CODE_SIDE_PHASE41_UNDER_THE_HOOD_PET_TASKS_BEHAVIOR_2026-05-05.md`.
- Latest under-the-hood behavior probe summary: `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\pet-task-probes\20260505-185830-292-cd2e5f5d\summary.json`.
- Latest under-the-hood behavior preview report: `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\pet-tasks\20260505-225844-screencapture-screencapture\run-summary.md`.
- Next safe step: continue with approval-gated Wevito-window screenshot execution only.
