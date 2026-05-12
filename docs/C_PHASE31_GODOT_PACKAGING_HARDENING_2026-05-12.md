# C-PHASE 31 Godot Packaging Hardening

Date: 2026-05-12

Branch: `claude-implementation/c-phase-31-godot-packaging-hardening`

## Goal

Resolve the C-PHASE 30 Godot packaged-export blocker without regenerating sprite runtime art or changing visual content.

## Problem Shape

```text
Before
╔════════════════════════════════════════════════════════════╗
║ Godot export from full repo                               ║
╠════════════════════════════════════════════════════════════╣
║ scripts + scenes + docs + tools + vnext + sprite forests  ║
║                         │                                  ║
║                         ▼                                  ║
║ Godot scans/imports thousands of loose PNGs                ║
║                         │                                  ║
║                         ▼                                  ║
║ export times out before packaged build is usable           ║
╚════════════════════════════════════════════════════════════╝

After
╔══════════════════════════════╗      ╔══════════════════════╗
║ Lean staging Godot project   ║      ║ External asset pack  ║
╠══════════════════════════════╣      ╠══════════════════════╣
║ project.godot                ║      ║ sprites_runtime      ║
║ export_presets.cfg           ║      ║ sprites_shared_runtime║
║ scenes/                      ║      ║ sprites/egg          ║
║ scripts/                     ║      ║ vnext/content        ║
║ vnext/content                ║      ╚══════════════════════╝
║          │                                      ▲
║          ▼                                      │
║ Fast Godot exe export ───── runtime loader ─────┘
╚══════════════════════════════╝
```

## Changes

- `tools/build-release.ps1` now exports from a temporary lean staging project by default.
- The staging project includes only `project.godot`, `export_presets.cfg`, `icon.svg`, `scenes`, `scripts`, and `vnext/content`.
- Release packaging now copies runtime art and content to `builds/release/assets/`.
- `scripts/pet.gd` can load exported pet frames from external package assets before falling back to packed/editor resources.
- `scripts/main_scene.gd` can load exported UI/shared art from external package assets before falling back to packed/editor resources.
- `tools/build-release.ps1 -NoStagingProject` remains available as an escape hatch for the old direct export path.

## Validation

| Check | Result |
| --- | --- |
| PowerShell parse for `tools/build-release.ps1` | PASS |
| Godot check-only `res://scripts/main_scene.gd` | PASS |
| Godot check-only `res://scripts/pet.gd` | PASS |
| `tools/build-release.ps1 -Version cphase31-staging1 -ExportTimeoutSeconds 300` | PASS, 23.4 seconds |
| `tools/build-release.ps1 -Version cphase31-staging2 -ExportTimeoutSeconds 300` | PASS, 18.6 seconds |
| `tools/build-release.ps1 -Version cphase31-bundle3 -ExportTimeoutSeconds 300` | PASS, produced self-contained zip |
| Packaged exe launch smoke from bundle folder, 8 seconds | PASS, process stayed alive |
| Bundle audit | PASS, required exe/bridge/assets present |
| Bundle residue audit | PASS, `0` `.import` files and `0` `.staging` entries |

Latest proof output:

- `builds/release/WevitoDesktopPet-vcphase31-bundle3-win64.exe`
- `builds/release/WevitoDesktopPet-latest-win64.exe`
- `builds/release/WevitoDesktopPet-vcphase31-bundle3-win64.zip`
- `builds/release/WevitoDesktopPet-latest-win64.zip`
- `builds/release/assets/`

External package asset counts from the successful build:

| Package path | File count |
| --- | ---: |
| `builds/release/assets/sprites_runtime` | 10818 |
| `builds/release/assets/sprites_shared_runtime` | 557 |
| `builds/release/assets/sprites/egg` | 5 |
| `builds/release/assets/vnext/content` | 12 |

## Risk Notes

- This phase intentionally does not mutate `sprites_runtime`, `sprites_shared_runtime`, or authored/source art.
- The external asset loader is active only outside the editor and still preserves `res://` fallback behavior.
- The release folder now needs the `assets/` directory to travel with the exe.
- The build script now emits a zip/folder bundle so the exe, helper, and external assets travel together.
- The old direct export path is retained behind `-NoStagingProject` for diagnostics.

## Next Checks

- Run a longer player-facing packaged proof if we want visual confirmation of several pet families in the exported app.
- Consider adding a build smoke that fails when any required external package folder is missing.
- Consider documenting release distribution as a folder/zip instead of a single exe, because runtime art is now deliberately external.
