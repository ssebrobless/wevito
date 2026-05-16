# Wevito Bug Board

Use this with `docs/RC_CHECKLIST_V1.md`.

## Summary

- Critical: 0
- Major: 4
- Minor: 0

## Open Bugs

| ID | Severity | RC Item | Pets | Mode | Title | Status |
|---|---|---|---|---|---|---|
| BUG-001 | Major | RC-29 | 0 | Focused | Fresh-profile reset target is stale; live save persists in WevitoVNext SQLite, not `%LOCALAPPDATA%\Wevito\state.json` | Open |
| BUG-002 | Major | RC-01 | 0 | Focused | First-launch `Help with sprite cleanup` choice records the setting but creates no `spriteAudit` draft task card | Open |
| BUG-003 | Major | RC-21 | 0 | Focused | WPF starter egg catalog and Godot egg picker disagree: WPF has seven ROYGBIV eggs with green disabled, Godot has six colors and random species mapping | Open |
| BUG-004 | Major | RC-25 | 1 | Focused | Successful DevControl care actions do not always produce distinct animation-state transitions (`water`, `groom`, `doctor`) | Open |

## In Progress

| ID | Severity | Owner | Notes |
|---|---|---|---|
|  |  |  |  |

## Ready For Retest

| ID | Linked RC Items | Fix Notes |
|---|---|---|
|  |  |  |

## Closed

| ID | Severity | RC Item | Resolution |
|---|---|---|---|
|  |  |  |  |

## Session Notes

- 2026-02-23: Phase 2 kicked off. Baseline startup/parse checks passed.
- 2026-02-23: Static behavior wiring verified in code; no runtime bug entries yet.
- 2026-05-16: C-PHASE 125 product-truth baseline appended BUG-001 through BUG-004 from live build evidence on commit `c3572a176`.

## BUG-001 Detail

- ID: BUG-001
- Severity: Major
- RC Item: RC-29
- Pet Count: 0
- Mode: Focused
- Steps:
  1. Follow C-PHASE 125 reset instruction to back up and delete `%LOCALAPPDATA%\Wevito\state.json`.
  2. Launch the current shell from `vnext\src\Wevito.VNext.Shell\bin\Debug\net8.0-windows10.0.19041.0`.
  3. Inspect persisted state.
- Expected: Fresh user profile appears after deleting the documented state file.
- Actual: No `%LOCALAPPDATA%\Wevito\state.json` existed; live companion state persisted in `%LOCALAPPDATA%\WevitoVNext\wevito-vnext.db`.
- Notes: Evidence in `vnext/artifacts/c-phase-125-truth/sqlite-state-after-actions.json`.

## BUG-002 Detail

- ID: BUG-002
- Severity: Major
- RC Item: RC-01
- Pet Count: 0
- Mode: Focused
- Steps:
  1. Start from a fresh `%LOCALAPPDATA%\WevitoVNext\wevito-vnext.db`.
  2. Complete first-launch wizard and choose `Help with sprite cleanup`.
  3. Read persisted companion state.
- Expected: A PET TASKS draft card with `Status=Draft` and `ToolFamily=spriteAudit`.
- Actual: `first_launch_background_choice=HelpWithSpriteCleanup` was recorded, but `taskCards=[]`.
- Notes: Evidence in `vnext/artifacts/c-phase-125-truth/sqlite-state-after-actions.json` and `audit-tail-after-fresh-launch.jsonl`.

## BUG-003 Detail

- ID: BUG-003
- Severity: Major
- RC Item: RC-21
- Pet Count: 0
- Mode: Focused
- Steps:
  1. Inspect WPF `StarterEggCatalog.Eggs`.
  2. Inspect Godot `scripts/game_manager.gd` and `scripts/main_scene.gd`.
  3. Compare color and species mapping behavior.
- Expected: WPF and Godot egg selection use the same ROYGBIV catalog semantics.
- Actual: WPF exposes red/orange/yellow/green/blue/indigo/violet with green disabled; Godot hardcodes six colors and random species selection.
- Notes: Evidence in `vnext/artifacts/c-phase-125-truth/egg-catalog-code-comparison.json`.

## BUG-004 Detail

- ID: BUG-004
- Severity: Major
- RC Item: RC-25
- Pet Count: 1
- Mode: Focused
- Steps:
  1. Use DevControl to spawn slot 0 as red baby female fox.
  2. Invoke actions `feed`, `water`, `rest`, `play`, `groom`, `bath`, `medicine`, `doctor`, `home`.
  3. Compare response success and before/after `AnimationState`.
- Expected: Each successful action gives a visible animation-state confirmation.
- Actual: All actions returned success, but `water`, `groom`, and `doctor` did not change `AnimationState` in the sequential check.
- Notes: Evidence in `vnext/artifacts/c-phase-125-truth/devcontrol-spawn-and-actions.json`.

## Bug Report Template

- ID: BUG-###
- Severity: Critical | Major | Minor
- RC Item: RC-##
- Pet Count: 1 | 2 | 3
- Mode: Focused | Unfocused
- Steps:
  1. 
  2. 
  3. 
- Expected:
- Actual:
- Notes:
