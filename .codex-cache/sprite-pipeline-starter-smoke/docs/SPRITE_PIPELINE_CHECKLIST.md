# Sprite Smoke Test Sprite Pipeline Checklist

Updated: 2026-03-15

## Setup

- [ ] Fill out `docs/SPRITE_SOURCE_OF_TRUTH.md`
- [ ] Replace placeholder entries in `tools/incoming_sprite_manifest.json`
- [ ] Lock runtime file naming and frame-family contract
- [ ] Decide which families require split Gemini packs

## Export And Generation

- [ ] Build focused authoring/handoff pack exporter
- [ ] Build family-aware import path
- [ ] Build color propagation path
- [ ] Reuse the already-open Gemini tab by default
- [ ] Enforce direct full-size Gemini downloads only

## Validation

- [ ] Add family-aware coverage reporting
- [ ] Add extracted-board visual inspection
- [ ] Add preview/contact-sheet generation
- [ ] Add runtime screenshot validation
- [ ] Add an explicit resume queue for interrupted species

## Production Readiness

- [ ] Complete locomotion across all species/ages/genders
- [ ] Complete care family across all species/ages/genders
- [ ] Complete expression family across all species/ages/genders
- [ ] Approve color propagation across all variants
- [ ] Archive evidence paths for the final verified set
