# Wevito

Wevito is a local AI assistant. The desktop pet simulator is its visual surface.

The goal is not "a pet game with tools bolted on." The goal is a Windows desktop AI that can chat, use approved local tools, remember reviewed learning, and eventually improve itself inside strict local-first safety gates while the pets stay as calm, ordinary pet-sim companions.

```text
User
  |
  +-- chat, tools, settings
  |       |
  |       v
  |   Wevito vNext Shell (WPF)
  |       |
  |       v
  |   Wevito.VNext.Core
  |       |
  |       +-- local reasoning model routing
  |       +-- audit ledger and policy gates
  |       +-- task cards and agent slots
  |       +-- sprite/workflow/dev proof tools
  |
  +-- pet clicks and visual feedback
          |
          v
      desktop pet surface
      pets stay visually pet-like
```

## Current Product Shape

- Local AI identity: default name is `Wevito`, configurable in the first-launch wizard.
- Chat-first UI: the tool popup is the main assistant surface.
- Three agent slots: behind-the-scenes helper slots can be named independently from the pet visuals.
- Pet visuals: pets remain cosmetic/gameplay companions, not task-completion animations.
- Safety posture: local-only model routing, audit ledger, KillSwitch, dry-run/proof/rollback patterns for mutations.
- Sprite pipeline: runtime sprites are guarded by source-of-truth reports and visual QA tools.

## Run The vNext App

Use the Windows-native vNext build for the current assistant/pet overlay:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
```

The stable release lock protects runtime sprites. Do not run asset prep unless the current phase explicitly allows it.

## Validate The Repo

```powershell
dotnet build .\vnext\Wevito.VNext.sln
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
```

## Important Docs

- `WHAT_IS_WEVITO.md` - plain-language product and architecture explanation.
- `docs/INDEX.md` - map of the docs folder by era and topic.
- `docs/DECISION_LEDGER_2026-05-15.md` - authoritative decision record.
- `docs/CLAUDE_MASTER_PLAN_2026-05-15.md` - current high-level implementation plan.
- `docs/CLAUDE_REFRAME_PHASES_106_THROUGH_116_PLAN_2026-05-15.md` - current reframe phase specs.
- `SPRITE_PIPELINE_KIT/README.md` - reusable sprite workflow kit.

## Legacy Godot Release Scripts

The older Godot release helpers still exist for packaged pet-surface work:

```bat
build-release.bat 0.1.0
run-latest.bat
```

For current code-side work, prefer the vNext build path above unless a phase explicitly asks for Godot packaging.

## Sprite Summary

Runtime sprites live under `sprites_runtime/`. The intended pet matrix is:

| Axis | Values |
| --- | --- |
| Species | rat, crow, fox, snake, deer, frog, pigeon, raccoon, squirrel, goose |
| Ages | baby, teen, adult |
| Genders | male, female |
| Colors | red, orange, yellow, blue, indigo, violet |
| Families | locomotion, care, expression, optional interaction families |

Sprite changes must use guarded workflows: report first, candidate packet, dry-run apply, backup hashes, proof, rollback path.
