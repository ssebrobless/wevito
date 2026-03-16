# Sprite Pipeline Kit

Updated: 2026-03-16

```text
╔════════════════════ Sprite Pipeline Kit ════════════════════╗
║ bootstrap wrapper  │ scaffold a new sprite project         ║
║ templates          │ reusable generic docs + manifests     ║
║ workflow playbook  │ repeat the process without re-explaining║
║ preset guide       │ pick a starting shape for the project ║
╚═════════════════════════════════════════════════════════════╝
```

This folder is the easiest place to find the reusable sprite and animation workflow we refined here.

## Open These First

- [PROCESS_PLAYBOOK.md](C:/Users/fishe/Documents/projects/wevito/SPRITE_PIPELINE_KIT/PROCESS_PLAYBOOK.md)
- [PRESET_GUIDE.md](C:/Users/fishe/Documents/projects/wevito/SPRITE_PIPELINE_KIT/PRESET_GUIDE.md)
- [bootstrap_sprite_pipeline.ps1](C:/Users/fishe/Documents/projects/wevito/SPRITE_PIPELINE_KIT/bootstrap_sprite_pipeline.ps1)
- [templates](C:/Users/fishe/Documents/projects/wevito/SPRITE_PIPELINE_KIT/templates)

## Quick Start

From the Wevito repo root:

```powershell
powershell -ExecutionPolicy Bypass -File .\SPRITE_PIPELINE_KIT\bootstrap_sprite_pipeline.ps1 -TargetRoot "C:\path\to\new-project" -ProjectName "New Project" -Preset character
```

For a fully custom scaffold:

```powershell
powershell -ExecutionPolicy Bypass -File .\SPRITE_PIPELINE_KIT\bootstrap_sprite_pipeline.ps1 -TargetRoot "C:\path\to\new-project" -ProjectName "New Project" -Entities hero turret explosion -EntityLabelSingular asset -EntityLabelPlural assets -VariantAxes state faction palette
```

## Presets

- `generic`
- `character`
- `creature`
- `prop`
- `vfx`
- `ui_mascot`

## What The Bootstrap Creates

- `docs/SPRITE_SOURCE_OF_TRUTH.md`
- `docs/MOTION_AUTHORING_ROADMAP.md`
- `docs/SPRITE_PIPELINE_CHECKLIST.md`
- `docs/GEMINI_PROMPT_RULES.md`
- `docs/SPRITE_PIPELINE_START_HERE.md`
- `tools/incoming_sprite_manifest.json`
- `tools/motion_families.json`

## Folder Shape

```text
SPRITE_PIPELINE_KIT/
├─ README.md
├─ PROCESS_PLAYBOOK.md
├─ PRESET_GUIDE.md
├─ bootstrap_sprite_pipeline.ps1
└─ templates/
   ├─ README.template.md
   ├─ SPRITE_SOURCE_OF_TRUTH.template.md
   ├─ MOTION_AUTHORING_ROADMAP.template.md
   ├─ SPRITE_PIPELINE_CHECKLIST.template.md
   ├─ GEMINI_PROMPT_RULES.template.md
   ├─ incoming_sprite_manifest.template.json
   └─ motion_families.template.json
```

## Notes

- The PowerShell wrapper calls the real bootstrap logic in [bootstrap_sprite_pipeline.py](C:/Users/fishe/Documents/projects/wevito/tools/bootstrap_sprite_pipeline.py).
- The canonical long-form version of the method still lives at [REUSABLE_SPRITE_PIPELINE_PLAYBOOK.md](C:/Users/fishe/Documents/projects/wevito/docs/REUSABLE_SPRITE_PIPELINE_PLAYBOOK.md).
- This folder exists so you do not have to remember which docs, tools, and templates were involved.
