# C-PHASE 43 - Care, Item, And Habitat Mapping Polish

Date: 2026-05-12
Branch: `claude-implementation/c-phase-43-care-item-habitat-mapping`

## Summary

C-PHASE 43 connects the existing shared item visual manifest to the runtime habitat recommendation surfaces without mutating any art.

```
item_visual_mapping.json
        │
        ▼
HabitatLoadoutResolver
        │  attaches VisualMappingId + SmallIconSafe
        ▼
HabitatDisplayItem
        │
        ├── Home recommended item strip
        └── Tool popup action-option previews
```

## Changes

- `HabitatDisplayItem` now carries `IsSmallIconSafe` and `VisualMappingId`.
- `HabitatLoadoutResolver` enriches all recommended/action items from `vnext/content/item_visual_mapping.json`.
- Recommended item previews now prefer real shared item art when the mapped asset is small-icon-safe.
- Narrow care assets such as `medicine_dropper` and `thermometer` continue to fall back to generic care/action icons in compact UI.
- Added tests that assert runtime recommendations receive visual mapping metadata and preserve narrow-care small-icon safety.

## Boundaries

- No sprite PNGs changed.
- No shared item PNGs changed.
- No source boards changed.
- No habitat loadout manifest changes.
- No asset-prep command was run.

## Validation

- `dotnet build .\vnext\Wevito.VNext.sln` passed.
- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build` passed: `280 / 280`.

## Follow-Up

- A later UI pass can make compact action buttons themselves use mapped item art per selected action option. This phase intentionally kept button icons stable and only improved recommendation/option previews.
