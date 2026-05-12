# C-PHASE 34 Clean Release Smoke

Date: 2026-05-12

Branch: `claude-implementation/c-phase-34-clean-release-smoke`

## Goal

Verify the uploaded GitHub draft release asset for `v0.1.0-desktop-rc1` works when downloaded and launched from a clean folder outside the repository.

## Proof Flow

```text
GitHub draft release
└── v0.1.0-desktop-rc1
    └── download zip to temp folder
        └── extract outside repo
            ├── verify exe + bridge
            ├── verify external assets
            └── launch WevitoDesktopPet.exe for smoke test
```

## Command Summary

- Downloaded the draft release asset with `gh release download v0.1.0-desktop-rc1`.
- Extracted to a clean temp folder outside the repo.
- Launched `WevitoDesktopPet.exe` from the extracted folder.
- Stopped the process after an 8-second smoke window.

## Results

| Check | Result |
| --- | --- |
| Downloaded zip exists | PASS |
| `WevitoDesktopPet.exe` exists | PASS |
| `WevitoDesktopBridge.exe` exists | PASS |
| `assets/sprites_runtime` exists | PASS |
| `assets/sprites_shared_runtime` exists | PASS |
| `assets/vnext/content` exists | PASS |
| Runtime sprite files | 10818 |
| Shared runtime files | 557 |
| Content files | 12 |
| Launch smoke | PASS, process stayed alive for 8 seconds |

Clean smoke root:

`C:\Users\fishe\AppData\Local\Temp\wevito-desktop-rc1-clean-smoke-20260512-034530`

Downloaded zip:

`C:\Users\fishe\AppData\Local\Temp\wevito-desktop-rc1-clean-smoke-20260512-034530\download\WevitoDesktopPet-vcphase32-desktop-rc-proof-win64.zip`

## Decision

The draft release is ready for manual visual/user review. If that review is acceptable, the next safe step is publishing the existing draft prerelease. No retagging is required.
