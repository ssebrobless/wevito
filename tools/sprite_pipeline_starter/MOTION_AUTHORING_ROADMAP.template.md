# {{PROJECT_NAME}} Motion Authoring Roadmap

Updated: 2026-03-15

```text
╔════════════════════ Motion Authoring Flow ════════════════════╗
║ source boards                                                 ║
║   ▼                                                           ║
║ focused handoff packs                                         ║
║   ▼                                                           ║
║ Gemini direct full-size download                              ║
║   ▼                                                           ║
║ family import into {{AUTHORED_ROOT}}                          ║
║   ▼                                                           ║
║ local color propagation                                       ║
║   ▼                                                           ║
║ coverage + previews + runtime screenshots                     ║
╚════════════════════════════════════════════════════════════════╝
```

## Motion Families

Preset:

- `{{PRESET_NAME}}`

See `tools/motion_families.json` and customize it for your project.

Common examples:

- character motion
  - `locomotion_idle`
  - `locomotion_walk_a`
  - `locomotion_walk_b`
  - `attack`
  - `hurt`
- prop motion
  - `open_close`
  - `active_loop`
  - `break`
- effects motion
  - `spawn`
  - `loop`
  - `dissipate`

## Why Split Packs

- keeps Gemini sharper
- reduces silhouette drift
- improves small-frame readability
- makes retries cheaper

## Recommended Order

1. hardest blur-prone or shape-sensitive {{ENTITY_LABEL_PLURAL}} first
2. locomotion before care/expression
3. one {{ENTITY_LABEL_SINGULAR}} fully through base motion before moving to the next

## Gemini Rules

- reuse the already-open logged-in Gemini tab
- create a new chat from there
- upload a single prepared pack image
- use direct full-size download only
- import immediately after generation

## Domain Guidance

{{DOMAIN_GUIDANCE_BULLETS}}

## Seed {{ENTITY_LABEL_PLURAL_TITLE}}

{{ENTITY_BULLETS}}
