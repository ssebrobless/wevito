# Wevito Execution Board

Scope lock: polish + bugfix + stability only. No major feature expansion.

## Status Snapshot

- Current phase: Phase 2 (Manual Bug Bash - In Progress)
- Ship blockers allowed: Yes (until Critical and Major are zero)
- Release gate: `docs/RC_CHECKLIST_V1.md`
- Bug tracking: `docs/BUG_BOARD.md`
- Phase 2 log: `docs/PHASE2_BUGBASH_LOG.md`
- Phase 2 runbook: `docs/PHASE2_RUNBOOK.md`

## Definition Of Done

- All RC checklist items pass.
- Critical bugs: 0.
- Major bugs: 0.
- Save/load/reset flow validated repeatedly.
- Focus/unfocus + roam/recall behavior stable for 1, 2, and 3 pets.

## Phase Plan

### Phase 1 - Release Gate Setup

- [x] RC checklist with 30 pass/fail items
- [x] Triage rubric
- [x] Bug report template

### Phase 2 - Manual Bug Bash

- [ ] Run checklist in focused mode (1/2/3 pets)
- [ ] Run checklist in unfocused mode (1/2/3 pets)
- [ ] Run overlay/modal stress checks
- [ ] Run resize/focus transition stress checks
- [x] Record baseline automated checks (parse/startup)
- [ ] Log all failures in bug board

### Phase 3 - Stability Fix Sprint

- [ ] Resolve all Critical bugs
- [ ] Resolve all Major bugs
- [ ] Re-test linked RC items

### Phase 4 - Persistence Hardening

- [ ] Save/load roundtrip validation
- [ ] Reset-save validation (settings preserved)
- [ ] Backward/missing-field tolerance checks

### Phase 5 - UX Consistency Pass

- [ ] Final copy consistency pass
- [ ] Final spacing/alignment pass
- [ ] Verify no overlap/clipping edge cases

### Phase 6 - Performance and Idle Sanity

- [ ] Unfocused 3-pet roam sanity run (10+ min)
- [ ] Focused action-tab spam sanity run

### Phase 7 - Ship Prep

- [ ] Final RC pass
- [ ] Update known limitations
- [ ] Prepare release notes
