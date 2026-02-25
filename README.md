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

## Features Implemented

### Pet Behavior
- Wanders within confined area when bored
- Different movement speeds per animal type
- Gender-based movement modifiers (males faster)
- Interaction stops wandering

### Stats System
- 8 stats: hunger, happiness, energy, health, cleanliness, affection, grooming, fitness
- Automatic stat decay over time
- Critical thresholds trigger health damage
- Condition system (obesity, depression, anxiety, etc.)

### Window Features
- Always-on-top overlay window
- Transparent background
- Borderless window
- Focus detection: UI collapses when window loses focus
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
