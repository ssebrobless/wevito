# C-PHASE 36 RC2 Clean Smoke

Date: 2026-05-12

Branch: `claude-implementation/c-phase-36-rc2-clean-smoke`

## Goal

Verify the public `v0.1.0-desktop-rc2` prerelease asset after downloading it from GitHub and extracting it outside the repository.

## Result

```text
v0.1.0-desktop-rc2
└── GitHub release zip
    └── clean temp extraction
        ├── exe + bridge present: PASS
        ├── external assets present: PASS
        ├── automation exits 0: PASS
        └── app-rendered goose habitat screenshot: PASS
```

## Command Flow

1. Downloaded `WevitoDesktopPet-vcphase35-allresources1-win64.zip` from `v0.1.0-desktop-rc2`.
2. Extracted it to a clean temp folder outside the repo.
3. Ran `WevitoDesktopPet.exe` with:
   - `WEVITO_AUTOMATION=1`
   - `WEVITO_AUTOMATION_SCENARIO=c_phase_6_5_habitat_mirror_goose`
   - `WEVITO_AUTOMATION_SCREENSHOT_PATH=<artifact path>`
4. Verified automation exit code and app-rendered screenshot.

## Evidence

Clean smoke root:

`C:\Users\fishe\AppData\Local\Temp\wevito-desktop-rc2-clean-smoke-20260512-114603`

Downloaded zip:

`C:\Users\fishe\AppData\Local\Temp\wevito-desktop-rc2-clean-smoke-20260512-114603\download\WevitoDesktopPet-vcphase35-allresources1-win64.zip`

App-rendered screenshot:

`vnext/artifacts/c-phase-36-rc2-clean-smoke/rc2-godot-viewport-goose.png`

## Checks

| Check | Result |
| --- | --- |
| Zip length | 141674763 bytes |
| `WevitoDesktopPet.exe` present | PASS |
| `WevitoDesktopBridge.exe` present | PASS |
| Runtime sprite files | 10818 |
| Shared runtime files | 557 |
| Automation timed out | false |
| Automation exit code | 0 |
| Automation report `passed` | true |
| Screenshot exists | PASS |

Automation report checks:

```text
boot_completed: PASS
startup_overlay_or_restore: PASS
three_pets_active: PASS
core_ui_buttons_hit_testable: PASS
focused_overlay_input_region_enabled: PASS
all_action_tabs_open: PASS
c_phase_6_5_habitat_mirror_screenshot: PASS
```

## Decision

`v0.1.0-desktop-rc2` is the current valid desktop prerelease. `v0.1.0-desktop-rc1` remains superseded.

Recommended next phase:

- Resume product work from the published RC2 baseline.
- Prioritize release-facing UX/manual QA findings before starting broad new AI/helper/tool features.
