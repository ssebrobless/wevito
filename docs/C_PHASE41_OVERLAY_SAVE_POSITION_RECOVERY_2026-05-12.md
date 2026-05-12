# C-PHASE 41 Overlay Save Position Recovery

Date: 2026-05-12

Branch: `claude-implementation/c-phase-41-overlay-save-position-recovery`

## Goal

Prevent stale or off-stage saved pet positions from stranding pets outside the visible overlay/stage after launch or layout changes.

## Change Shape

```text
saved pet recovery
|
+-- before
|   +-- current position clamped when bounds applied
|   `-- stale target_position could remain off-stage
|
`-- after
    +-- current position clamped when bounds applied
    +-- target_position clamped to same bounds
    +-- internal movement target synchronized
    `-- focused automation scenario proves bad-save recovery
```

## Files Changed

- `scripts/pet.gd`
- `scripts/main_scene.gd`

## Implementation Notes

`Pet.set_wander_bounds()` now clamps both the pet's current position and saved target position to the active bounds. This matters because old saves can contain a valid visible position but a stale off-stage target, or vice versa. If the target is left stale, a restored wandering pet can continue trying to move toward coordinates that no longer match the overlay layout.

`scripts/main_scene.gd` now includes a focused automation scenario:

```text
WEVITO_AUTOMATION_SCENARIO=force_save_position_recovery
```

The scenario verifies loaded pets have both `position.x` and `target_position.x` inside the current layout bounds after startup/layout recovery.

## Validation

Build:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-release.ps1 -Version cphase41-positionrecovery1 -ExportTimeoutSeconds 300
```

Result: `PASS`

Focused bad-save recovery:

```text
seeded save:
  position=(99999, 99999)
  target_position=(-99999, -99999)
  desktop_companion_roam=false

automation:
  scenario=force_save_position_recovery
  exitCode=0
  passed=true
```

Report:

- `vnext/artifacts/c-phase-41-overlay-save-position-recovery/position-recovery-report.json`

Regression smoke:

| Run | Scenario | Exit code | Passed | Report |
| --- | --- | ---: | --- | --- |
| full | default full automation | 0 | true | `vnext/artifacts/c-phase-41-overlay-save-position-recovery/full-report.json` |
| drink | `force_low_hydration_drink` | 0 | true | `vnext/artifacts/c-phase-41-overlay-save-position-recovery/drink-report.json` |
| fetch | `force_fetch_sequence` | 0 | true | `vnext/artifacts/c-phase-41-overlay-save-position-recovery/fetch-report.json` |

## Release Impact

This is a low-risk player safety polish fix. It does not change sprites, runtime assets, prop anchors, model calls, PET TASKS execution, or release packaging policy.

## Remaining Manual QA

Human QA should still confirm the overlay feels recoverable in real multi-monitor use. This phase proves bad saved pet coordinates recover into stage bounds, but it does not replace user-visible launch/focus testing from `docs/C_PHASE40_MANUAL_RC3_PLAYER_QA_PACKET_2026-05-12.md`.

