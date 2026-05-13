# Universal Home Habitat

Wevito now uses a single shared home-stage concept instead of ten species-specific habitat scenes.

## Runtime Contract

- `vnext/content/habitat_loadouts.json` remains the manifest source of truth.
- Every species keeps its own `speciesId` / `environmentId` identity for compatibility.
- Every species resolves to the same six slots:
  - `primary`: shared little home anchor, currently `log_shelter`
  - `bed-left`: first pet bed, currently `moss_bed`
  - `bed-center`: second pet bed, currently `moss_bed`
  - `bed-right`: third pet bed, currently `moss_bed`
  - `food`: shared food bowl, currently `snack_bowl`
  - `water`: shared water bowl, currently `water_bowl`
- The WPF home panel uses one neutral backdrop and always prefers manifest-driven stage props when available.
- Feed actions anchor to food props, water/bath actions anchor to container props, and rest/home actions prefer the three bed slots.

## Boundaries

- No sprite PNGs were mutated.
- No source boards were touched.
- No prop anchors were changed.
- Species-specific recommended care/food/action lists still exist; this only simplifies the visible habitat stage.

## Validation

- Focused `HabitatLoadout` tests pass.
- Full vNext tests pass.
- `tools/build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests` passes.
