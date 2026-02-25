# Phase 2 Manual Runbook

Use this runbook together with:

- `docs/RC_CHECKLIST_V1.md`
- `docs/PHASE2_BUGBASH_LOG.md`
- `docs/BUG_BOARD.md`

## Environment Setup

1. Launch game in editor or project runner.
2. Ensure a clean profile if possible (or run reset-save before first pass).
3. Keep `PHASE2_BUGBASH_LOG.md` open for live note-taking.
4. Log every failure immediately in `BUG_BOARD.md`.

## Pass 1 - Focused, 1 Pet

1. Start with exactly 1 active pet.
2. Verify focused home window appears with full HUD.
3. Open and close each action tab once (`Feed`, `Pet`, `Rest`, `Groom`, `Exercise`, `Medicine`).
4. Confirm pet recalls on tab open and gets 2s hold only after tab close.
5. Trigger settings modal and close it.
6. Verify no overlap/clipping in top strip, identity row, stats, actions.
7. Record RC ranges and PASS/FAIL in `PHASE2_BUGBASH_LOG.md`.

## Pass 2 - Focused, 2 Pets

1. Add second pet.
2. Verify two side-by-side environments with no overlap.
3. Cycle active pet arrows repeatedly.
4. Open/close one action tab and confirm both pets recall home.
5. Confirm 2s hold starts after tab close.
6. Record PASS/FAIL by RC range.

## Pass 3 - Focused, 3 Pets

1. Add third pet.
2. Verify three side-by-side environments and no overlap.
3. Repeat tab open/close while switching active pet.
4. Verify action feedback toasts remain deduped/single.
5. Confirm no UI clipping under higher density.
6. Record PASS/FAIL by RC range.

## Pass 4 - Unfocused, 1 Pet

1. Reduce to 1 active pet scenario (or isolate behavior on selected pet).
2. Defocus window.
3. Verify HUD hidden, pet roams monitor bounds, and desktop remains usable outside pet bounds.
4. Refocus and confirm return to compact focused home window.
5. Record PASS/FAIL by RC range.

## Pass 5 - Unfocused, 2 Pets

1. Defocus with 2 pets active.
2. Verify both pets roam without overlap bugs or freezing.
3. Refocus and verify both return home cleanly.
4. Record PASS/FAIL by RC range.

## Pass 6 - Unfocused, 3 Pets

1. Defocus with 3 pets active.
2. Verify roaming remains smooth and desktop click-through still usable.
3. Refocus and verify stable return-home behavior.
4. Record PASS/FAIL by RC range.

## Pass 7 - Overlay/Modal Stress

1. Repeat open/close cycles (10x each): settings, action tabs, egg selection (when available).
2. Trigger naming/death modal paths where possible.
3. Verify modals remain centered, opaque, and input-blocking.
4. Verify closing leaves no invisible blocker.
5. Record targeted RC items (`RC-21..RC-24`, `RC-15`).

## Pass 8 - Resize/Focus Stress

1. While focused, resize/move through available states quickly.
2. Rapidly switch focus in/out with tabs/modals open.
3. Verify layout reflow stays clean and no duplicate overlays/tabs appear.
4. Record targeted RC items (`RC-02`, `RC-03`, `RC-05`, `RC-16`, `RC-19`, `RC-23`).

## Bug Logging Rule

For every failure:

1. Add bug row in `docs/BUG_BOARD.md` with new BUG ID.
2. Include RC item and exact repro steps.
3. Set severity using rubric in `RC_CHECKLIST_V1.md`.
4. Cross-link BUG IDs in the pass block notes.
