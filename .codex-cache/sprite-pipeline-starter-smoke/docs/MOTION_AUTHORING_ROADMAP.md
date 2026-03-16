# Sprite Smoke Test Motion Authoring Roadmap

Updated: 2026-03-15

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
║ local color propagation                                       ║
║   ▼                                                           ║
║ coverage + previews + runtime screenshots                     ║
╚════════════════════════════════════════════════════════════════╝
```

## Motion Families

- `locomotion_idle`
- `locomotion_walk_a`
- `locomotion_walk_b`
- `care`
- `expression`

## Why Split Packs

- keeps Gemini sharper
- reduces silhouette drift
- improves small-animal readability
- makes retries cheaper

## Recommended Order

1. hardest blur-prone or shape-sensitive species first
2. locomotion before care/expression
3. one species fully through base motion before moving to the next

## Gemini Rules

- reuse the already-open logged-in Gemini tab
- create a new chat from there
- upload a single prepared pack image
- use direct full-size download only
- import immediately after generation

## Seed Species

- `fox`
- `crow`
- `snake`
