# Generic Sprite Kit Smoke Motion Authoring Roadmap

Updated: 2026-03-16

```text
╔════════════════════ Motion Authoring Flow ════════════════════╗
║ source boards                                                 ║
║   ▼                                                           ║
║ focused handoff packs                                         ║
║   ▼                                                           ║
║ Gemini direct full-size download                              ║
║   ▼                                                           ║
║ family import into sprites_authored_verified                          ║
║   ▼                                                           ║
║ local color/variant propagation                               ║
║   ▼                                                           ║
║ coverage + previews + runtime screenshots                     ║
╚════════════════════════════════════════════════════════════════╝
```

## Motion Families

Preset:

- `prop`

See `tools/motion_families.json` and customize it for your project.

## Why Split Packs

- keeps Gemini sharper
- reduces silhouette drift
- improves small-frame readability
- makes retries cheaper

## Recommended Order

1. hardest blur-prone or shape-sensitive props first
2. highest-value motion family before secondary families
3. one prop fully through base motion before moving to the next

## Gemini Rules

- reuse the already-open logged-in Gemini tab
- create a new chat from there
- upload a single prepared pack image
- use direct full-size download only
- import immediately after generation

## Domain Guidance

- Preserve construction details and hinge/segment logic.
- Mechanical motion should stay consistent across all frames.
- Prioritize readability of the state change over extra micro-motion.

## Seed Props

- `door`
- `console`
- `chest`
