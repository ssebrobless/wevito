# Wevito Bug Board

Use this with `docs/RC_CHECKLIST_V1.md`.

## Summary

- Critical: 0
- Major: 0
- Minor: 0

## Open Bugs

| ID | Severity | RC Item | Pets | Mode | Title | Status |
|---|---|---|---|---|---|---|
|  |  |  |  |  |  |  |

## In Progress

| ID | Severity | Owner | Notes |
|---|---|---|---|
|  |  |  |  |

## Ready For Retest

| ID | Linked RC Items | Fix Notes |
|---|---|---|
| BUG-001 | RC-29 | C-PHASE 126b adds the live SQLite profile path contract plus a backup-first reset script at `tools/reset-wevito-vnext-profile.ps1`. |
| BUG-002 | RC-01 | C-PHASE 126a drafts a `spriteAudit` TaskCard from `Help with sprite cleanup`; ready for live retest. |
| BUG-003 | RC-21 | C-PHASE 126a moved WPF/Godot to the shared ROYGBIV starter egg catalog; ready for live retest. |
| BUG-004 | RC-25 | C-PHASE 126b gives `water`, `groom`, and `doctor` first-class confirmation states while keeping sprite fallback safe. |

## Closed

| ID | Severity | RC Item | Resolution |
|---|---|---|---|
|  |  |  |  |

## Session Notes

- 2026-02-23: Phase 2 kicked off. Baseline startup/parse checks passed.
- 2026-02-23: Static behavior wiring verified in code; no runtime bug entries yet.
- 2026-05-16: C-PHASE 125 product-truth baseline appended BUG-001 through BUG-004 from live build evidence on commit `c3572a176`.
- 2026-05-16: C-PHASE 126a moved BUG-002 and BUG-003 to ready for retest; C-PHASE 126b moved BUG-001 and BUG-004 to ready for retest.

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
