# Phase 2 Bug Bash Log

Date started: 2026-02-23
Owner: OpenCode
Runbook: `docs/PHASE2_RUNBOOK.md`

## Progress

- Baseline automated checks: PASS
- Manual pass 1 (Focused, 1 pet): Pending
- Manual pass 2 (Focused, 2 pets): Pending
- Manual pass 3 (Focused, 3 pets): Pending
- Manual pass 4 (Unfocused, 1 pet): Pending
- Manual pass 5 (Unfocused, 2 pets): Pending
- Manual pass 6 (Unfocused, 3 pets): Pending
- Overlay/modal stress: Pending
- Resize/focus stress: Pending

## Session 1 - Kickoff Baseline

- Scope: startup/parsing baseline before manual checklist sweep.
- Command: `Godot_v4.6.1-stable_win64.exe --headless --path "C:\Users\fishe\Documents\projectdirectory\games\wevito-godot" --check-only --quit`
- Result: PASS
- Command: `Godot_v4.6.1-stable_win64.exe --headless --path "C:\Users\fishe\Documents\projectdirectory\games\wevito-godot" --quit`
- Result: PASS

## Session 2 - Static Behavior Verification (Code-Level)

- Scope: verify that key design-lock behaviors are wired before manual runs.
- Result: PASS (code-path presence verified; runtime confirmation still required in manual passes)

### Verified Wiring

- Action tab recalls all pets and defers hold until tab close:
  - `scripts/main_scene.gd` uses `action_tab_recall_pending_hold` + `_recall_all_pets_home(0.0)` on tab open.
  - `scripts/main_scene.gd` applies `_recall_all_pets_home(2.0)` on tab close.
- Pet home lock timer support present:
  - `scripts/pet.gd` defines `_home_lock_timer` and decrements it in `update_wandering()`.
- Unfocused roam mode wiring present:
  - `scripts/main_scene.gd` full monitor mode in `_apply_window_mode_layout(false)`.
  - `scripts/main_scene.gd` calls `_start_monitor_roam_all_pets()` on focus loss.
  - `scripts/main_scene.gd` keeps focused-only environment stage visibility via `_set_environment_stages_visible(focused)`.

## Manual Pass Template

- Use this for each pass and append a new block.

### Pass X - <Mode>, <Pet Count>

- Time:
- Tester:
- Scope:
- Result: PASS | FAIL

| RC Range | Status | Notes | Bug IDs |
|---|---|---|---|
| RC-01 to RC-05 |  |  |  |
| RC-06 to RC-10 |  |  |  |
| RC-11 to RC-15 |  |  |  |
| RC-16 to RC-20 |  |  |  |
| RC-21 to RC-24 |  |  |  |
| RC-25 to RC-27 |  |  |  |
| RC-28 to RC-30 |  |  |  |

## Manual Runs Queue

- Focused mode RC sweep (1, 2, 3 pets)
- Unfocused mode RC sweep (1, 2, 3 pets)
- Overlay/modal stress cycle
- Resize/focus transition stress cycle

## Manual Run Blocks

### Pass 1 - Focused, 1 Pet

- Time:
- Tester:
- Scope:
- Result:

| RC Range | Status | Notes | Bug IDs |
|---|---|---|---|
| RC-01 to RC-05 | Pending |  |  |
| RC-06 to RC-10 | Pending |  |  |
| RC-11 to RC-15 | Pending |  |  |
| RC-16 to RC-20 | Pending |  |  |
| RC-21 to RC-24 | Pending |  |  |
| RC-25 to RC-27 | Pending |  |  |
| RC-28 to RC-30 | Pending |  |  |

### Pass 2 - Focused, 2 Pets

- Time:
- Tester:
- Scope:
- Result:

| RC Range | Status | Notes | Bug IDs |
|---|---|---|---|
| RC-01 to RC-05 | Pending |  |  |
| RC-06 to RC-10 | Pending |  |  |
| RC-11 to RC-15 | Pending |  |  |
| RC-16 to RC-20 | Pending |  |  |
| RC-21 to RC-24 | Pending |  |  |
| RC-25 to RC-27 | Pending |  |  |
| RC-28 to RC-30 | Pending |  |  |

### Pass 3 - Focused, 3 Pets

- Time:
- Tester:
- Scope:
- Result:

| RC Range | Status | Notes | Bug IDs |
|---|---|---|---|
| RC-01 to RC-05 | Pending |  |  |
| RC-06 to RC-10 | Pending |  |  |
| RC-11 to RC-15 | Pending |  |  |
| RC-16 to RC-20 | Pending |  |  |
| RC-21 to RC-24 | Pending |  |  |
| RC-25 to RC-27 | Pending |  |  |
| RC-28 to RC-30 | Pending |  |  |

### Pass 4 - Unfocused, 1 Pet

- Time:
- Tester:
- Scope:
- Result:

| RC Range | Status | Notes | Bug IDs |
|---|---|---|---|
| RC-01 to RC-05 | Pending |  |  |
| RC-06 to RC-10 | Pending |  |  |
| RC-11 to RC-15 | Pending |  |  |
| RC-16 to RC-20 | Pending |  |  |
| RC-21 to RC-24 | Pending |  |  |
| RC-25 to RC-27 | Pending |  |  |
| RC-28 to RC-30 | Pending |  |  |

### Pass 5 - Unfocused, 2 Pets

- Time:
- Tester:
- Scope:
- Result:

| RC Range | Status | Notes | Bug IDs |
|---|---|---|---|
| RC-01 to RC-05 | Pending |  |  |
| RC-06 to RC-10 | Pending |  |  |
| RC-11 to RC-15 | Pending |  |  |
| RC-16 to RC-20 | Pending |  |  |
| RC-21 to RC-24 | Pending |  |  |
| RC-25 to RC-27 | Pending |  |  |
| RC-28 to RC-30 | Pending |  |  |

### Pass 6 - Unfocused, 3 Pets

- Time:
- Tester:
- Scope:
- Result:

| RC Range | Status | Notes | Bug IDs |
|---|---|---|---|
| RC-01 to RC-05 | Pending |  |  |
| RC-06 to RC-10 | Pending |  |  |
| RC-11 to RC-15 | Pending |  |  |
| RC-16 to RC-20 | Pending |  |  |
| RC-21 to RC-24 | Pending |  |  |
| RC-25 to RC-27 | Pending |  |  |
| RC-28 to RC-30 | Pending |  |  |

### Pass 7 - Overlay/Modal Stress

- Time:
- Tester:
- Scope: repeated open/close loops for egg, naming, death, settings, action tabs.
- Result:

| RC Item | Status | Notes | Bug IDs |
|---|---|---|---|
| RC-21 | Pending |  |  |
| RC-22 | Pending |  |  |
| RC-23 | Pending |  |  |
| RC-24 | Pending |  |  |
| RC-15 | Pending |  |  |

### Pass 8 - Resize/Focus Stress

- Time:
- Tester:
- Scope: resize while focused/unfocused, rapid focus flips, tab/modal open during transitions.
- Result:

| RC Item | Status | Notes | Bug IDs |
|---|---|---|---|
| RC-02 | Pending |  |  |
| RC-03 | Pending |  |  |
| RC-05 | Pending |  |  |
| RC-16 | Pending |  |  |
| RC-19 | Pending |  |  |
| RC-23 | Pending |  |  |

## Notes

- Manual checks should be recorded against RC IDs in `docs/RC_CHECKLIST_V1.md`.
- Any failures should be logged in `docs/BUG_BOARD.md` with BUG-### IDs.
