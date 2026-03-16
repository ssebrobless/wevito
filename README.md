# Wevito - Godot Rebuild

A virtual pet simulation game built with Godot 4.

## Project Status

### Completed
- [x] Sprite generator - 3,360 sprites generated
- [x] 10 animals with proper pixel art
- [x] Male/female size variants  
- [x] 6 color palette options per animal
- [x] 8 animations per variant (idle, walk, eat, happy, sad, sleep, sick, bathe)
- [x] Godot 4 project setup with window config
- [x] Pet data class with all stats
- [x] Wandering behavior system
- [x] UI with stats bars and action buttons
- [x] Game loop with stat decay
- [x] Condition system
- [x] Focus-based UI collapse

### How to Run

1. **Install Godot 4.x** (from godotengine.org)
2. **Import the project:**
   - Open Godot
   - Click "Import"
   - Navigate to `wevito-godot/`
   - Click "Import & Edit"
3. **Run the game:** Press F5 or click the Play button

## Build Release (Windows)

Use the one-click script to export a player-ready Windows build from the current project state:

```bat
build-release.bat 0.1.0
```

Output files:

- `builds/release/WevitoDesktopPet-v0.1.0-win64.exe`
- `builds/release/WevitoDesktopPet-latest-win64.exe`

PowerShell direct usage:

```powershell
./tools/build-release.ps1 -Version 0.1.0
```

If Godot is not auto-detected, pass `-GodotPath "C:\path\to\Godot.exe"` or set `GODOT_PATH`.

### Fast Local Testing Loop

- Build and launch latest in one step (auto-rebuilds if project changed):

```bat
run-latest.bat
```

- Build and launch latest with an explicit version tag:

```bat
run-latest.bat 0.1.0
```

This keeps your testing executable (`builds/release/WevitoDesktopPet-latest-win64.exe`) aligned with current project changes whenever you run `run-latest.bat`.

If you want an explicit build step before launch, `test-latest.bat` is still available.

Note: Export templates are required once per machine.
In Godot editor, use `Editor -> Manage Export Templates -> Download and Install`.

## Sprite Summary

**Location:** `wevito-godot/sprites/`

| Animals | 10 | rat, crow, fox, snake, deer, frog, pigeon, raccoon, squirrel, goose |
|---------|-----|---------------------------------------------------------------------|
| Genders | 2 | male (larger, bolder), female (smaller, calmer) |
| Colors | 6 | red, orange, yellow, blue, indigo, violet |
| Animations | 8 | idle, walk, eat, happy, sad, sleep, sick, bathe |
| **Total** | **3,360** | Sprite frames |

## Controls

| Key | Action |
|-----|--------|
| F | Feed |
| P | Pet |
| R | Rest (toggle sleep) |
| G | Groom |
| B | Bathe |
| X | Exercise |
| Ctrl+Shift+P | Pin/release the HUD over other apps |
| Ctrl+Shift+B | Capture the current clipboard link into the basket |

Overlay command:
- `powershell -ExecutionPolicy Bypass -File .\tools\toggle-overlay-ui.ps1 -Command pin`
- `powershell -ExecutionPolicy Bypass -File .\tools\toggle-overlay-ui.ps1 -Command release`
- `powershell -ExecutionPolicy Bypass -File .\tools\capture-basket-link.ps1`

## vNext Visual Loop

Use the Windows-native vNext build and capture a visual proof set for focused, passive, pinned, and basket states:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\capture-vnext-visuals.ps1 -Configuration Debug
```

Artifacts are written under `vnext\artifacts\screenshots\<timestamp>\` with:
- desktop screenshots for each state
- cropped home/roam/basket window captures
- `summary.json` containing window bounds and paths

For flicker and transition debugging, record a short desktop clip with app/broker traces and frame-diff analysis:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\record-vnext-flicker.ps1 -Configuration Debug
```

Artifacts are written under `vnext\artifacts\flicker\<timestamp>\` with:
- `transition-capture.mp4`
- `trace\*.trace.log`
- `automation.log`
- `flicker-summary.json` with nearest automation marker annotations for each spike
- extracted spike frames under `spikes\`

To verify the canonical sprite source contract against both `incoming_sprites` and `sprites_runtime`, run:

```powershell
python .\tools\audit_sprite_contract.py
```

Use `--output <path>` to save a full JSON audit report.

To export an authored-animation pack for a pilot species while preserving the canonical uploaded sprite look:

```powershell
python .\tools\export_species_authoring_pack.py --species rat
```

Author improved frames into:

```text
sprites_authored/<species>/<age>/<gender>/<color>/<animation>_<nn>.png
```

The shell and sprite preview tools will prefer authored frames when they exist.

For the reusable cross-project version of the workflow that got this pipeline working, start here:

- `C:\Users\fishe\Documents\projects\wevito\SPRITE_PIPELINE_KIT`

Most useful files in that folder:

- `C:\Users\fishe\Documents\projects\wevito\SPRITE_PIPELINE_KIT\README.md`
- `C:\Users\fishe\Documents\projects\wevito\SPRITE_PIPELINE_KIT\PROCESS_PLAYBOOK.md`
- `C:\Users\fishe\Documents\projects\wevito\SPRITE_PIPELINE_KIT\PRESET_GUIDE.md`
- `C:\Users\fishe\Documents\projects\wevito\SPRITE_PIPELINE_KIT\bootstrap_sprite_pipeline.ps1`

To scaffold the same process into a different project root, use:

```powershell
powershell -ExecutionPolicy Bypass -File .\SPRITE_PIPELINE_KIT\bootstrap_sprite_pipeline.ps1 -TargetRoot "C:\path\to\new-project" -ProjectName "New Project" -Preset generic -Entities hero enemy turret -EntityLabelSingular entity -EntityLabelPlural entities -VariantAxes state faction palette
```

That will create a starter kit with:

- source-of-truth doc
- motion roadmap
- pipeline checklist
- Gemini prompt rules
- starter manifest
- motion families template

Canonical templates now live under:

- `C:\Users\fishe\Documents\projects\wevito\SPRITE_PIPELINE_KIT\templates`

To clean baked Gemini checkerboard/background remnants from shared non-pet assets into a runtime-ready tree, run:

```powershell
python .\tools\clean_shared_sprite_assets.py --source .\sprites --output .\sprites_shared_runtime --clean-folders environment items status --copy-folders icons celestial portraits
```

The vNext shell will prefer `sprites_shared_runtime` for shared assets when present.

## vNext Dev Tools

The debug/testing build now includes development-only pet scenario tools. These are meant for QA and balancing, not the final ship surface.

Use:

```text
Ctrl+Shift+D
```

You can:
- add, remove, or clear pets entirely
- switch species / age / gender / color on a selected pet
- spawn a full color set for the chosen species / age / gender combination
- force any animation state for a chosen duration, then clear it
- directly add or clear named medical conditions with severity
- force hunger, thirst, fatigue, dirtiness, loneliness, sickness, healthy recovery, comfort, and recall states
- force obesity, malnutrition, anxiety, depression, injury, elder-age, and personality archetype states
- directly adjust vitals, fitness, and biological age for rapid scenario testing
- inspect live personality, habit, aging, and condition summaries without leaving the running app

The visual harness now captures the dev tools popup as part of the screenshot proof set.

## vNext Webtools

The focused HUD now exposes a single webtools toggle on the main panel. When opened, it reveals a five-slot webtools hotbar:

- `LINK BIN` is live now
- 4 additional empty slots are reserved for future PC tools

The link bin itself now keeps its actions inside the popup:

- `PASTE` adds the current clipboard link if it is valid
- `DELETE` removes checked rows
- clicking a link row opens it directly

## Features Implemented

### Pet Behavior
- Wanders within confined area when bored
- Different movement speeds per animal type
- Gender-based movement modifiers (males faster)
- Interaction stops wandering
- Long-term personality building from care habits
- Biological aging rate changes with habits, stress, and medical conditions
- Species-seeded innate conditions plus acquired medical issues

### Stats System
- 8 stats: hunger, happiness, energy, health, cleanliness, affection, grooming, fitness
- Automatic stat decay over time
- Critical thresholds trigger health damage
- Condition system (obesity, depression, anxiety, etc.)

### Window Features
- Always-on-top overlay window
- Transparent background
- Borderless window
- Focus detection: HUD collapses when another window takes focus
- Default unfocused mode keeps the pet world visible while mouse input passes through to other apps
- Pinned HUD mode keeps the UI visible and clickable over other apps
- Link basket stores up to 5 URLs, supports clipboard capture, and can ingest dropped URL/text shortcut files
- Positioned in bottom-right corner

## Project Structure

```
wevito-godot/
├── project.godot       # Godot project config
├── icon.svg           # Project icon
├── .godotignore      # Ignore patterns
├── scenes/
│   └── main.tscn     # Main scene
├── scripts/
│   ├── main_scene.gd   # Main game logic & UI
│   ├── pet.gd          # Pet behavior & animations
│   ├── pet_data.gd    # Pet data structure
│   ├── game_manager.gd # Game loop & stat system
│   ├── signal_bus.gd  # Global signals
│   └── main.gd        # Window management
└── sprites/           # 3,360 sprite frames
    ├── rat/
    │   ├── male/
    │   │   ├── red/
    │   │   │   ├── idle_00.png
    │   │   │   └── ...
```

## Development Notes

- Uses Godot 4.2+ with GL Compatibility renderer
- Window: 320x420 pixels
- Pixel-perfect rendering enabled
- Stat decay: ~1 tick per second (configurable)
- Release gate checklist: `docs/RC_CHECKLIST_V1.md`
- Execution board: `docs/EXECUTION_BOARD.md`
- Bug tracker board: `docs/BUG_BOARD.md`
- Phase 2 run log: `docs/PHASE2_BUGBASH_LOG.md`
- Phase 2 runbook: `docs/PHASE2_RUNBOOK.md`
- Monitor roam diagnosis: `docs/MONITOR_ROAM_DIAGNOSIS.md`
- Gemini handoff helper:
  - `python .\tools\prepare_gemini_handoff.py --species rat`
  - `powershell -ExecutionPolicy Bypass -File .\tools\open-gemini-handoff.ps1 -Species rat -Age adult -Gender male -OpenGemini`
