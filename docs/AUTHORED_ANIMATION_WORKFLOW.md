# Authored Animation Workflow

Updated: 2026-03-11

```text
╔══════════════════════ Authored Animation Lane ══════════════════════╗
║ canonical source sheets                                            ║
║   incoming_sprites/<species-age>.png                               ║
║   male-left / female-right                                         ║
║                                                                     ║
║ export authoring pack                                               ║
║   tools/export_species_authoring_pack.py                            ║
║   ├─ base-pose.png                                                  ║
║   ├─ editable-board.png                                             ║
║   ├─ runtime-reference-blue.png                                     ║
║   ├─ color-strip.png                                                ║
║   └─ authoring-brief.md                                             ║
║                                                                     ║
║ image-assisted frame authoring                                      ║
║   preserve canonical look                                           ║
║   improve motion frame-by-frame                                     ║
║                                                                     ║
║ authored runtime overrides                                          ║
║   sprites_authored/<species>/<age>/<gender>/<color>/<anim>_<nn>.png ║
║                                                                     ║
║ shell + preview renderer                                            ║
║   authored overrides preferred over generated runtime               ║
╚══════════════════════════════════════════════════════════════════════╝
```

## Purpose

This lane is for replacing synthesized motion with better authored animation while preserving the uploaded pet appearance from `incoming_sprites`.

The model to follow is:
- lock the canonical source pose
- edit precisely
- validate visually in the app

## Focused Motion Families

The preferred workflow is now focused family packs instead of only the old full-board pass:

- `locomotion`
  - `idle_*`
  - `walk_*`
- `care`
  - `eat_*`
  - `sleep_*`
- `expression`
  - `happy_*`
  - `sad_*`
  - `sick_*`
  - `bathe_*`

Prepare them with:

```powershell
python .\tools\prepare_motion_gemini_handoff.py --family locomotion --species snake crow fox frog pigeon raccoon squirrel rat deer goose
python .\tools\prepare_motion_gemini_handoff.py --family care --species snake crow fox frog pigeon raccoon squirrel rat deer goose
```

This is better for animation quality because it lets Gemini focus on one motion problem at a time instead of one giant mixed board.

## Rules

- Do not redesign the pet.
- Preserve the exact character identity from the canonical source sheet.
- Keep transparent backgrounds.
- Keep the runtime contract:
  - `28x24`
  - 8 animations
  - expected frame counts unchanged
- Author frames into `sprites_authored`, not by editing `incoming_sprites`.
- Let local tooling continue to handle color expansion when possible, but authored per-color overrides are allowed when needed.

## Pilot Command

Export a pilot authoring pack for `rat`:

```powershell
python .\tools\export_species_authoring_pack.py --species rat
```

Output:
- `C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\authoring-packs\rat`

## Easier Gemini Handoff

If you want a simpler upload flow, prepare a handoff folder with numbered files and a copy-paste prompt:

```powershell
python .\tools\prepare_gemini_handoff.py --species rat
```

That creates:
- `C:\Users\fishe\Documents\projects\wevito\incoming_sprites\gemini_handoff\rat`

For the fastest flow, use:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\open-gemini-handoff.ps1 -Species rat -Age adult -Gender male -OpenGemini
```

That will:
- create the handoff folder if needed
- open the exact variant folder in Explorer
- copy the matching prompt to the clipboard
- optionally open Gemini

## Browser Automation

If you want to automate the Gemini upload/prompt/send flow with a dedicated persistent browser profile:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\setup-gemini-automation.ps1
powershell -ExecutionPolicy Bypass -File .\tools\run-gemini-automation.ps1 -Species rat -Age adult -Gender male -SetupOnly -KeepOpen
```

Then open the target Gemini chat in that persistent browser once, and rerun:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\run-gemini-automation.ps1 -Species rat -Age adult -Gender male
```

## Runtime Override Contract

If an authored animation exists at:

`sprites_authored/<species>/<age>/<gender>/<color>/<animation>_<nn>.png`

the shell and sprite preview tools should prefer that authored set over the generated runtime frames for that animation.

This lets us replace one action at a time:
- `idle` first
- then `walk`
- then the remaining actions
