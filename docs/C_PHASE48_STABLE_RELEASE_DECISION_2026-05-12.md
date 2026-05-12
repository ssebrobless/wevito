# C-PHASE 48 Stable Release Decision

Date: 2026-05-12

Branch: `claude-implementation/c-phase-48-stable-release-decision`

## Decision

```text
stable release decision
|
+-- promote_current_rc
|   `-- rejected: RC3 is validated, but main has post-RC3 runtime/product changes
|
+-- publish_next_rc
|   `-- selected: package current main as RC4, then validate cleanly
|
`-- hold_release_for_fixes
    `-- not selected: no known release-blocking code/test issue is active
```

Decision label:

```text
publish_next_rc
```

RC3 should **not** be promoted directly to stable because current `origin/main` now contains player-facing and tool-facing improvements after the RC3 tag. The safest release path is to publish a new RC from current main, validate it from a clean GitHub download, then decide whether that newer RC can become stable.

## Current Public Release State

GitHub releases checked on 2026-05-12:

| Release | Type | Tag | Published |
| --- | --- | --- | --- |
| Wevito Desktop RC3 | Pre-release | `v0.1.0-desktop-rc3` | 2026-05-12T16:32:11Z |
| Wevito Desktop RC2 | Pre-release | `v0.1.0-desktop-rc2` | 2026-05-12T15:44:40Z |
| Wevito Desktop RC1 | Pre-release | `v0.1.0-desktop-rc1` | 2026-05-12T07:49:44Z |

Local tags checked:

```text
v0.1.0-desktop-rc3
v0.1.0-desktop-rc2
v0.1.0-desktop-rc1
v0.1.0-vnext-rc1
```

## Trusted RC3 Evidence

RC3 remains the latest validated public package.

```text
RC3 validation
|
+-- clean download: PASS
+-- zip hash match: PASS
+-- bundle structure: PASS
+-- full automation: PASS
+-- drink forced scenario: PASS
+-- fetch forced scenario: PASS
`-- app-rendered goose viewport proof: PASS
```

Primary docs:

- `docs/C_PHASE38_RC3_CLEAN_VALIDATION_2026-05-12.md`
- `docs/C_PHASE40_MANUAL_RC3_PLAYER_QA_PACKET_2026-05-12.md`

RC3 package:

```text
release: v0.1.0-desktop-rc3
zip: WevitoDesktopPet-vcphase37-fetchfix3-win64.zip
sha256: ce33cb333c6f751c0661520c571ce72f5a925805528e3865ecee23056bb5a19e
```

## Why RC3 Should Not Be Promoted Directly

Current `origin/main` includes these post-RC3 changes:

```text
v0.1.0-desktop-rc3..origin/main
|
+-- C-PHASE 39: release QA backlog
+-- C-PHASE 40: RC3 player QA packet
+-- C-PHASE 41: overlay stale-position recovery
+-- C-PHASE 42: PET TASKS UX clarity
+-- C-PHASE 43: care/item/habitat mapping polish
+-- C-PHASE 44: screenshot/capture UX proof wording
+-- C-PHASE 45: translation/audio provider wording
+-- C-PHASE 46: AI helper gate review
`-- C-PHASE 47: visual-side integration sync
```

The decisive item is C-PHASE 41:

- It changed `scripts/pet.gd` and `scripts/main_scene.gd`.
- It fixed stale saved `target_position` recovery after launch/layout changes.
- It added a focused automation scenario for bad saved positions.
- It is low-risk, but it is still player-facing runtime behavior.

Because RC3 does not contain that recovery fix, promoting RC3 would knowingly ship an older package than the current release-prep baseline.

## Current Gates That Stay Closed

```text
closed for stable-release decision
|
+-- live model calls
+-- broad sprite mutation
+-- visual generation/import
+-- prop-anchor edits
+-- all-color propagation
+-- asset prep without explicit approval
+-- screen recording
`-- external audio booster control
```

The release decision does not open any of these gates.

## Recommended RC4 Scope

Package current `origin/main` as the next prerelease candidate.

Suggested tag:

```text
v0.1.0-desktop-rc4
```

Suggested asset name:

```text
WevitoDesktopPet-v0.1.0-desktop-rc4-win64.zip
```

RC4 should include:

- all RC3 fixes and validated runtime assets,
- C-PHASE 41 stale-position recovery,
- C-PHASE 42 PET TASKS clarity,
- C-PHASE 43 care/item/habitat mapping polish,
- C-PHASE 44-45 provider/capture wording,
- C-PHASE 46-47 documentation/gate alignment.

RC4 should not include:

- live model calls,
- new sprite generation/import,
- broad visual cleanup,
- asset-prep regeneration unless explicitly approved as part of packaging,
- optional-animation expansion beyond what is already in main,
- external audio booster control.

## RC4 Validation Plan

```text
RC4 validation
|
+-- build package from current main
+-- publish as prerelease
+-- clean download from GitHub
+-- verify zip SHA256
+-- verify exe + bridge + runtime/shared assets
+-- run full automation
+-- run force_low_hydration_drink
+-- run force_fetch_sequence
+-- run force_save_position_recovery
+-- capture goose habitat viewport proof
`-- perform short manual player QA
```

Minimum commands for the next packaging phase should be based on the existing release tooling:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-release.ps1 -Version v0.1.0-desktop-rc4 -ExportTimeoutSeconds 300
```

Then validate the public GitHub asset the same way C-PHASE 38 and C-PHASE 40 validated RC3.

## Release Notes Draft For RC4

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

## Stop / Ask Before Stable

After RC4 is published and cleanly validated, ask the user for one decision:

```text
promote_rc4_to_stable
publish_rc5_for_fixes
hold_release_for_manual_qa
```

Do not tag a stable release automatically from this phase.

## Validation For This Phase

This phase is documentation/decision only. It should not build, tag, publish, or mutate release assets.

Required local validation:

```powershell
dotnet build .\vnext\Wevito.VNext.sln
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
```

## Copy-Paste Prompt For Next Code Phase

```text
You are working in the Wevito repo:
C:\Users\fishe\Documents\projects\wevito

Read:
C:\Users\fishe\Documents\projects\wevito\docs\C_PHASE48_STABLE_RELEASE_DECISION_2026-05-12.md
C:\Users\fishe\Documents\projects\wevito\docs\C_PHASE38_RC3_CLEAN_VALIDATION_2026-05-12.md
C:\Users\fishe\Documents\projects\wevito\docs\C_PHASE40_MANUAL_RC3_PLAYER_QA_PACKET_2026-05-12.md
C:\Users\fishe\Documents\projects\wevito\docs\C_PHASE41_OVERLAY_SAVE_POSITION_RECOVERY_2026-05-12.md

Decision from C-PHASE 48:
publish_next_rc

Start a new phase to package current origin/main as v0.1.0-desktop-rc4.

Hard boundaries:
- Do not enable live model calls.
- Do not start sprite generation/import.
- Do not broadly mutate runtime/source PNGs.
- Do not edit prop anchors.
- Do not add all-color propagation.
- Do not run asset prep unless the existing release packaging command explicitly requires it and you document the resulting runtime asset counts/hashes.

Required output:
1. Build the RC4 zip from current main using the existing release tooling.
2. Publish it as a GitHub prerelease named Wevito Desktop RC4.
3. Clean-download the GitHub asset and verify SHA256.
4. Run full automation, force_low_hydration_drink, force_fetch_sequence, force_save_position_recovery, and goose habitat viewport proof against the clean downloaded package.
5. Save a report under docs/C_PHASE49_RC4_PACKAGE_AND_CLEAN_VALIDATION_2026-05-12.md or the current date if different.
6. Stop before stable promotion and ask for:
   promote_rc4_to_stable
   publish_rc5_for_fixes
   hold_release_for_manual_qa
```
