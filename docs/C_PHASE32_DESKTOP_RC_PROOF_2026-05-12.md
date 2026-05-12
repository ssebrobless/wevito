# C-PHASE 32 Desktop RC Proof

Date: 2026-05-12

Branch: `claude-implementation/c-phase-32-desktop-rc-proof`

Base: `origin/main` after C-PHASE 31 merge (`1be84708c`)

## Goal

Produce and verify a desktop release-candidate package from the merged packaging-hardening code without creating a public release tag yet.

## Release Shape

```text
builds/release/
├── WevitoDesktopPet-vcphase32-desktop-rc-proof-win64.exe
├── WevitoDesktopPet-vcphase32-desktop-rc-proof-win64.zip
├── WevitoDesktopPet-latest-win64.exe
├── WevitoDesktopPet-latest-win64.zip
└── WevitoDesktopPet-vcphase32-desktop-rc-proof-win64/
    ├── WevitoDesktopPet.exe
    ├── WevitoDesktopBridge.exe
    └── assets/
        ├── sprites_runtime/
        ├── sprites_shared_runtime/
        ├── sprites/egg/
        └── vnext/content/
```

The zip/folder bundle is the correct desktop artifact. The lone exe is not sufficient by itself because heavy runtime art is intentionally external to avoid Godot import/export stalls.

## Commands Run

| Command | Result |
| --- | --- |
| `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-release.ps1 -Version cphase32-desktop-rc-proof -ExportTimeoutSeconds 300` | PASS |
| Zip integrity check | PASS |
| Bundle residue check | PASS |
| Launch smoke from bundled folder for 8 seconds | PASS |

## Proof Artifacts

- `builds/release/WevitoDesktopPet-vcphase32-desktop-rc-proof-win64.exe`
- `builds/release/WevitoDesktopPet-vcphase32-desktop-rc-proof-win64.zip`
- `builds/release/WevitoDesktopPet-latest-win64.exe`
- `builds/release/WevitoDesktopPet-latest-win64.zip`
- `builds/release/WevitoDesktopPet-vcphase32-desktop-rc-proof-win64/`

## Bundle Audit

| Check | Result |
| --- | --- |
| Zip entry count | 11499 |
| `WevitoDesktopPet.exe` present | PASS |
| `WevitoDesktopBridge.exe` present | PASS |
| `assets/sprites_runtime/*` present | PASS |
| `assets/sprites_shared_runtime/*` present | PASS |
| `assets/vnext/content/*` present | PASS |
| `.import` sidecars in zip | 0 |
| `.staging` entries in zip | 0 |

External package asset counts:

| Package path | File count |
| --- | ---: |
| `builds/release/assets/sprites_runtime` | 10818 |
| `builds/release/assets/sprites_shared_runtime` | 557 |
| `builds/release/assets/sprites/egg` | 5 |
| `builds/release/assets/vnext/content` | 12 |

## Tag Decision Needed

Recommended tag after review:

- `v0.1.0-desktop-rc1`

Reason:

- `v0.1.0-vnext-rc1` already exists for the vNext-only safe release candidate.
- This proof is the first desktop/Godot bundle candidate after packaging hardening.
- A distinct tag avoids confusing the earlier vNext-only RC with the desktop-bundle RC.

Do not retag or move `v0.1.0-vnext-rc1`.
