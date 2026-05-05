# Wevito Crow Dark Background Contrast Review

Date: 2026-05-05

Purpose: verify the low-severity color warning from the crow palette review against darker backgrounds.

This is a non-mutating visual review. It does not generate, recolor, import, normalize, or edit sprite PNGs.

## Review Artifact

Generated packet:

```text
C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\visual-review\20260505-crow-dark-background-contrast-review\
  +-- manifest.json
  +-- run-summary.md
  +-- qa\
        +-- crow-adult-dark-background-contrast-sheet.png
```

The sheet composites `crow / adult / female|male / all six colors / walk_00` against:

- transparent checker
- dark overlay
- night blue
- warm brown

No sprite files were modified.

## Finding

The crow caution is real but not a recolor blocker.

```text
crow adult variants
  |
  +-- readable silhouette on all tested backgrounds
  +-- strongest readability on checker and night-blue
  +-- lower contrast on dark-overlay and warm-brown
  +-- red/orange/violet are the subtlest on dark warm surfaces
  |
  v
recommendation
  |
  +-- fix with staging/background contrast first
  +-- do not recolor crow variants yet
```

## Decision

```text
color repair queue
  |
  +-- still empty
```

## Recommended UI/Scene Guidance

For crow habitats and night scenes:

- avoid placing dark adult crow variants directly on dark-brown props
- use a small contact shadow that does not swallow feet
- keep a lighter perch/ground line under the pet
- prefer night-blue or cool dark backgrounds over warm dark-brown panels
- use habitat props to frame the crow rather than hiding its outline

## Future Trigger For Recolor

Only consider a targeted crow palette repair if a real runtime scene proof shows:

- the bird silhouette disappears at normal overlay scale
- beak/feet are unreadable during motion
- multiple user-facing backgrounds hide the same color variant
- the issue cannot be solved by staging, shadow, or backdrop adjustment

