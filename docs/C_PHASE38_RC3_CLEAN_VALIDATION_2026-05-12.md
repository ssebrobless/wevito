# C-PHASE 38 RC3 Clean Validation

Date: 2026-05-12

Branch: `claude-implementation/c-phase-38-rc3-clean-validation`

## Goal

Validate the public `v0.1.0-desktop-rc3` prerelease zip from a clean GitHub download before resuming broader product work.

## Validation Shape

```text
GitHub release: v0.1.0-desktop-rc3
└── clean download + extract
    ├── bundle structure: PASS
    ├── full automation: PASS
    ├── drink scenario: PASS
    ├── fetch scenario: PASS
    └── app-rendered goose habitat proof: PASS
```

## Artifact Under Test

- Release: `v0.1.0-desktop-rc3`
- Zip: `WevitoDesktopPet-vcphase37-fetchfix3-win64.zip`
- Zip size: `141675177` bytes
- Clean root: `C:\Users\fishe\AppData\Local\Temp\wevito-desktop-rc3-clean-validation-20260512-123538`

## Bundle Checks

| Check | Result |
| --- | --- |
| `WevitoDesktopPet.exe` present | PASS |
| `WevitoDesktopBridge.exe` present | PASS |
| Runtime sprite files | 10818 |
| Shared runtime files | 557 |

## Automation Checks

| Run | Scenario | Exit code | Passed | Report |
| --- | --- | ---: | --- | --- |
| full | default full automation | 0 | true | `vnext/artifacts/c-phase-38-rc3-clean-validation/rc3-full-report.json` |
| drink | `force_low_hydration_drink` | 0 | true | `vnext/artifacts/c-phase-38-rc3-clean-validation/rc3-drink-report.json` |
| fetch | `force_fetch_sequence` | 0 | true | `vnext/artifacts/c-phase-38-rc3-clean-validation/rc3-fetch-report.json` |
| goose viewport | `c_phase_6_5_habitat_mirror_goose` | 0 | true | `vnext/artifacts/c-phase-38-rc3-clean-validation/rc3-goose-viewport-report.json` |

Screenshot proof:

`vnext/artifacts/c-phase-38-rc3-clean-validation/rc3-godot-viewport-goose.png`

## Decision

`v0.1.0-desktop-rc3` is the current validated packaged baseline.

Safe next phase:

- Build the release-facing manual QA checklist and product-polish backlog from RC3.
- Keep broad new visual/tool work behind this validated baseline.
- Do not enable live model calls as part of release QA.
