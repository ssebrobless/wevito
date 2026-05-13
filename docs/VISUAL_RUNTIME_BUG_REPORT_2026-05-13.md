# Visual Runtime Bug Report - 2026-05-13

## User-observed issues

- Fox visual breakage while roaming.
- Pets walk briefly, then continue walking in place after stopping.
- Pets can appear doubled while transitioning in/out of the home environment.
- One crow frame has a flattened/cropped top of head.

## Code-side triage

- The doubled sprite is a render ownership bug: passive mode can render the same roaming pet in both `HomePanelWindow` and `RoamBandWindow`.
- Walking in place is a presentation-state bug: passive pets keep `Walk` animation even when they have reached their roam target and are waiting for the next decision.

## Asset-side triage

- Fox visual breakage and the cropped crow-head frame are likely sprite PNG/frame quality defects. They should be routed to the visual cleanup queue rather than fixed with runtime code.

## Code-side fix plan

- Make the roam band the only pet renderer in passive mode.
- Make passive pets use `Walk` only while their current position is still moving toward the target.
- Add tests for both behaviors.

## Follow-up visual queue items

- Inspect current fox runtime frames for broken silhouettes/crops/noise.
- Inspect crow locomotion frames for top-edge crop, especially frames where the head reaches the canvas boundary.
