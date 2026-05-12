# C-PHASE 49 RC4 Package And Clean Validation

Date: 2026-05-12

Branch: `claude-implementation/c-phase-49-rc4-package-validation`

## Decision

```text
RC4 package decision
|
+-- package current main: PASS
+-- publish GitHub prerelease: PASS
+-- clean-download public asset: PASS
+-- hash public asset: PASS
+-- packaged automation: PASS
+-- position recovery scenario: PASS
`-- stable promotion: HOLD
```

RC4 is now the latest validated prerelease candidate. Do not promote to stable automatically from this phase; ask the user after any desired manual player QA.

Release:

```text
tag: v0.1.0-desktop-rc4
name: Wevito Desktop RC4
url: https://github.com/ssebrobless/wevito/releases/tag/v0.1.0-desktop-rc4
target commit: 97285ad9887423fc42cc3b562087180ff0d8f90e
prerelease: true
```

Asset:

```text
name: WevitoDesktopPet-v0.1.0-desktop-rc4-win64.zip
size: 141677058 bytes
sha256: c12a61d0ddd55bfa611bc14480dd298584e2e9d3849ea8e01064beb71c4efacc
download: https://github.com/ssebrobless/wevito/releases/download/v0.1.0-desktop-rc4/WevitoDesktopPet-v0.1.0-desktop-rc4-win64.zip
```

## Build Command

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-release.ps1 -Version 0.1.0-desktop-rc4 -ExportTimeoutSeconds 300
```

Result:

```text
build: PASS
zip: builds/release/WevitoDesktopPet-v0.1.0-desktop-rc4-win64.zip
```

The release script copied existing runtime/shared assets into the package. No broad sprite generation/import was started and no separate asset-prep regeneration command was run.

## Clean Download And Bundle Check

Clean validation root:

```text
vnext/artifacts/c-phase-49-rc4-package-validation/
```

Downloaded public asset:

```text
vnext/artifacts/c-phase-49-rc4-package-validation/download/WevitoDesktopPet-v0.1.0-desktop-rc4-win64.zip
```

Extracted executable:

```text
vnext/artifacts/c-phase-49-rc4-package-validation/extract/WevitoDesktopPet.exe
```

Bundle contents:

| Check | Result |
| --- | --- |
| Zip SHA256 matches release notes | PASS |
| `WevitoDesktopPet.exe` present | PASS |
| `WevitoDesktopBridge.exe` present | PASS |
| Runtime sprite files | 10818 |
| Shared runtime files | 557 |
| Content files | 12 |

## Packaged Automation Results

All runs used isolated temp `APPDATA` roots so normal local save data was not touched.

| Run | Scenario | Exit code | Report passed | Result | Report |
| --- | --- | ---: | --- | --- | --- |
| full | `fresh` | 0 | true | PASS | `vnext/artifacts/c-phase-49-rc4-package-validation/rc4-full-report.json` |
| drink | `force_low_hydration_drink` | 0 | true | PASS | `vnext/artifacts/c-phase-49-rc4-package-validation/rc4-drink-report.json` |
| fetch | `force_fetch_sequence` | 0 | true | PASS | `vnext/artifacts/c-phase-49-rc4-package-validation/rc4-fetch-report.json` |
| position recovery | `force_save_position_recovery` | 0 | true | PASS | `vnext/artifacts/c-phase-49-rc4-package-validation/rc4-position-recovery-report.json` |
| goose viewport | `c_phase_6_5_habitat_mirror_goose` | 0 | true | PASS | `vnext/artifacts/c-phase-49-rc4-package-validation/rc4-goose-viewport-report.json` |

Summary:

```text
vnext/artifacts/c-phase-49-rc4-package-validation/rc4-validation-summary.json
```

Screenshot proof:

```text
vnext/artifacts/c-phase-49-rc4-package-validation/rc4-goose-viewport.png
```

Screenshot size:

```text
13098 bytes
```

Godot emitted the known `ObjectDB instances leaked at exit` warning after some runs. The validation script ignores that specific warning, matching the existing automation convention, and found zero `SCRIPT ERROR` / non-ObjectDB `ERROR:` lines in copied logs.

## Gates Preserved

```text
still closed
|
+-- stable release promotion
+-- live model calls
+-- sprite generation/import
+-- broad runtime/source PNG mutation
+-- prop-anchor edits
+-- all-color propagation
+-- screen recording
`-- external audio booster control
```

## Release Notes Used

```text
Wevito Desktop RC4 focuses on release-readiness polish after RC3.

Changes since RC3:
- Recovers stale saved pet target positions so pets do not keep moving toward off-stage coordinates after launch/layout changes.
- Clarifies PET TASKS as a safe report-first flow with PREPARE, PREVIEW, and RUN APPROVED language.
- Improves care/item/habitat recommendation previews using existing approved shared item art where it is small-icon-safe.
- Clarifies screenshot/capture preview vs live capture boundaries.
- Clarifies translation provider behavior and audio-assist safety limits.
- Keeps live model calls disabled pending a future capability flag and first-call consent UI.

Still gated:
- broad sprite mutation or generation/import,
- asset-prep regeneration,
- live model calls,
- screen recording,
- external audio booster control.
```

## Recommended Next User Decision

Ask the user for one label:

```text
promote_rc4_to_stable
publish_rc5_for_fixes
hold_release_for_manual_qa
```

Recommended default:

```text
hold_release_for_manual_qa
```

Reason: RC4 is machine-validated, but Wevito is a visual desktop pet overlay. Before stable, a human should still sanity-check launch feel, overlay placement, control discoverability, visual motion, PET TASKS clarity, and whether the pet remains pleasant while the user does other work.

## Validation For This Phase

Fresh local validation after publishing:

```powershell
dotnet build .\vnext\Wevito.VNext.sln
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
```
