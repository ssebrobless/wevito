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
- Screenshot harness summary: `C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\screenshots\20260311-194012\summary.json`
- Flicker harness summary: `C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\flicker\20260311-194054\summary.json`
- Pinned interaction probe summary: `C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\probes\20260311-194034\summary.json`
- Action and tool probe summary: `C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\action-probes\20260311-193816\summary.json`

## Notes

- Focus-sensitive harnesses must be run sequentially. Parallel runs produce false failures because they compete for foreground state and publish output.
- Basket copy/open/delete is validated in the action probe. Clipboard writes were hardened in the shell to avoid transient `OpenClipboard` crashes.
