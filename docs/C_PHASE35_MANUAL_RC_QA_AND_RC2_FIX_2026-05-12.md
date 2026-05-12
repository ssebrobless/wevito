# C-PHASE 35 Manual RC QA and RC2 Fix

Date: 2026-05-12

Branch: `claude-implementation/c-phase-35-manual-rc-qa`

## Goal

Run user-style QA on the published `v0.1.0-desktop-rc1` prerelease and repair any release-blocking issue before continuing.

## QA Result Shape

```text
Published RC1
└── Download zip from GitHub
    └── Extract outside repo
        ├── exe/bridge/assets present: PASS
        ├── process launches/stays alive: PASS
        └── visible app is black/empty: FAIL
            └── log root cause: missing preloaded GDScript dependencies

Fix
└── export lean staging project as all_resources
    ├── scripts and scene dependencies included
    ├── external assets still bundled beside exe
    └── Godot viewport proof renders goose habitat: PASS
```

## Published RC1 Finding

The published `v0.1.0-desktop-rc1` zip downloaded and extracted correctly, but launch QA showed a black/empty overlay window.

Runtime log evidence:

```text
SCRIPT ERROR: Parse Error: Preload file "res://scripts/game_manager.gd" does not exist.
SCRIPT ERROR: Parse Error: Preload file "res://scripts/pet.gd" does not exist.
SCRIPT ERROR: Parse Error: Preload file "res://scripts/pet_data.gd" does not exist.
SCRIPT ERROR: Parse Error: Preload file "res://scripts/sound_manager.gd" does not exist.
ERROR: Failed to load script "res://scripts/main_scene.gd" with error "Parse error".
```

Root cause:

- C-PHASE 31 changed the package to export from a lean staging project.
- The preset still used `export_filter="scenes"`, which embedded `main_scene.gd` but did not include every script that `main_scene.gd` preloads.
- The exe could start, but the main scene could not parse fully, so the overlay appeared empty.

## Fix

Changed `export_presets.cfg`:

- From `export_filter="scenes"`
- To `export_filter="all_resources"`

This is safe because release export now runs from a lean staging project that contains only:

- `project.godot`
- `export_presets.cfg`
- `icon.svg`
- `scenes/`
- `scripts/`
- `vnext/content/`

It does not reintroduce the original sprite-forest import/export bottleneck because runtime art is still packaged externally under `assets/`.

## Fixed Package Validation

Command:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-release.ps1 -Version cphase35-allresources1 -ExportTimeoutSeconds 300
```

Result:

- Build completed.
- Zip produced: `builds/release/WevitoDesktopPet-vcphase35-allresources1-win64.zip`
- External assets still packaged beside the exe.

Internal Godot viewport proof:

- Scenario: `c_phase_6_5_habitat_mirror_goose`
- Screenshot: `vnext/artifacts/c-phase-35-manual-rc-qa/desktop-allresources-godot-viewport-goose.png`
- Automation report: PASS

Proof details:

```text
boot_completed: PASS
startup_overlay_or_restore: PASS
three_pets_active: PASS
core_ui_buttons_hit_testable: PASS
focused_overlay_input_region_enabled: PASS
all_action_tabs_open: PASS
c_phase_6_5_habitat_mirror_screenshot: PASS
```

## Desktop Screenshot Caveat

Desktop `CopyFromScreen` captures the transparent/layered overlay window as a black rectangle even after the internal viewport proof renders correctly. For release QA, app-rendered Godot screenshots are more trustworthy than desktop screenshots for this transparent overlay surface.

## Required Follow-Up

`v0.1.0-desktop-rc1` should be treated as superseded by this packaging fix.

Recommended next release step:

1. Merge this branch.
2. Tag merged `origin/main` as `v0.1.0-desktop-rc2`.
3. Upload `builds/release/WevitoDesktopPet-vcphase35-allresources1-win64.zip`.
4. Mark RC2 as prerelease.
5. Edit RC1 release notes to say it is superseded by RC2 due the missing-script packaging issue.
