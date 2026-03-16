# Sprite Smoke Test Sprite Pipeline Starter

Updated: 2026-03-15

```text
╔════════════════════ Start Here ════════════════════╗
║ 1. fill out SPRITE_SOURCE_OF_TRUTH.md             ║
║ 2. replace the manifest placeholder entries       ║
║ 3. customize MOTION_AUTHORING_ROADMAP.md          ║
║ 4. lock Gemini prompt rules                       ║
║ 5. build handoff/export/import scripts            ║
║ 6. start with locomotion_idle / walk_a / walk_b   ║
╚════════════════════════════════════════════════════╝
```

Starter files:

- `docs/SPRITE_SOURCE_OF_TRUTH.md`
- `docs/MOTION_AUTHORING_ROADMAP.md`
- `docs/SPRITE_PIPELINE_CHECKLIST.md`
- `docs/GEMINI_PROMPT_RULES.md`
- `tools/incoming_sprite_manifest.json`

Suggested first customization:

1. replace placeholder species with the real roster
2. define your runtime frame contract
3. decide which families should be split for quality
4. keep direct-download-only as a hard rule if you use Gemini

Project roots seeded by the bootstrap:

- source art: `incoming_sprites`
- runtime sprites: `sprites_runtime`
- verified authored sprites: `sprites_authored_verified`

Seed species:

- `fox`
- `crow`
- `snake`
