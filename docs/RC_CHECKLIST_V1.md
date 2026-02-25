# Wevito Release Candidate Checklist v1

This checklist is the release gate for the current scope lock (polish + bugfix + stability only).

## How To Use

- Run each item against 1, 2, and 3 active pets unless stated otherwise.
- Mark each item `PASS` or `FAIL` and attach a bug ID for failures.
- Severity rules are defined in the triage rubric below.

## Test Matrix

- Modes: focused window, unfocused desktop roam.
- Pet counts: 1 pet, 2 pets, 3 pets.
- Contexts: normal play, action tab open, priority modal open, save/load cycle.

---

## RC Checklist (30 Items)

### A) Boot, Window, Focus, and Input

- [ ] `RC-01` App launches to focused home window with HUD visible and no parse/runtime errors.
- [ ] `RC-02` Focus loss hides HUD and leaves pets roaming monitor area without frozen state.
- [ ] `RC-03` Focus return restores focused home window and re-enables full HUD interaction.
- [ ] `RC-04` Unfocused click-through only captures around pet area; desktop remains usable elsewhere.
- [ ] `RC-05` Focus transitions do not duplicate tabs/modals or leave invisible clickable UI.

### B) Multi-Pet Environment and Movement

- [ ] `RC-06` Focused mode shows side-by-side environment slots for all active pets with no overlap.
- [ ] `RC-07` Adding pets reflows slots cleanly (1->2->3) and preserves readability.
- [ ] `RC-08` Pet death removes slot cleanly and reflows remaining slots with no gaps/overlap.
- [ ] `RC-09` Unfocused mode hides environment slots while pets continue roaming monitor bounds.
- [ ] `RC-10` Pets never clip outside assigned roam bounds in focused or unfocused mode.

### C) Action Tabs, Recall, and Home Hold

- [ ] `RC-11` Opening any action tab recalls all pets home (not only selected pet).
- [ ] `RC-12` While tab is open, pets stay home without immediate post-recall drift.
- [ ] `RC-13` Closing action tab starts 2s home hold timer for all recalled pets.
- [ ] `RC-14` After hold expires, pets resume normal wandering behavior.
- [ ] `RC-15` Repeated open/close tab cycles do not stack broken states or movement glitches.

### D) UI Structure and Readability

- [ ] `RC-16` No overlap between top controls, identity row, stats panel, and action row at supported sizes.
- [ ] `RC-17` Action buttons auto-fit container and remain readable (no clipping/truncation artifacts).
- [ ] `RC-18` Name/gender/age row remains readable with long pet names (truncation + tooltip works).
- [ ] `RC-19` Action tab content remains clipped to panel bounds and does not spill.
- [ ] `RC-20` Toast feedback appears as single deduped message (no stacking/flicker).

### E) Priority Modals and Overlay Behavior

- [ ] `RC-21` Egg select, naming, death, settings all use centered opaque priority cards.
- [ ] `RC-22` Priority modals block background input correctly while open.
- [ ] `RC-23` Modal cards recenter correctly on resize/focus/window mode changes.
- [ ] `RC-24` Closing modals does not leave stale blockers or invisible overlays.

### F) Actions, Effects, and Feedback

- [ ] `RC-25` Action outcomes update the intended stats and feedback copy matches effect intent.
- [ ] `RC-26` Forage, workout, and sleep behaviors follow current tuned risk/recovery profile.
- [ ] `RC-27` Medicine flow behaves correctly (right medicine helps, wrong medicine penalizes).

### G) Save, Load, Reset, and Recovery

- [ ] `RC-28` Save/load round-trip preserves pets, settings, and core runtime-compatible state.
- [ ] `RC-29` Reset save returns fresh progress while preserving settings.
- [ ] `RC-30` App recovers safely from missing/older save fields without blocking startup.

---

## Bug Triage Rubric

- `Critical`
  - Crash, startup failure, data loss/corruption, unrecoverable input lock, or release-gate feature unusable.
  - Ship blocker; must be fixed before release.

- `Major`
  - Core behavior incorrect/inconsistent (focus/roam/recall/save/action outcomes), serious readability/interaction issue.
  - Must be fixed before release.

- `Minor`
  - Cosmetic/polish issue with workaround; does not break core loop.
  - Can be deferred only if documented in known limitations.

## Bug Report Template

- `ID`: BUG-###
- `Severity`: Critical | Major | Minor
- `RC Item`: RC-##
- `Pet Count`: 1 | 2 | 3
- `Mode`: Focused | Unfocused
- `Steps`: numbered reproducible steps
- `Expected`: what should happen
- `Actual`: what happened
- `Notes`: logs/screenshots if available
