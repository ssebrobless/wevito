# C-PHASE 198: Ball-Family Pilot Completion (goose/baby/female/blue)

Date: 2026-06-12
Branch: `claude-implementation/c-phase-198-ball-pilot-completion`
Plan: `docs/CLAUDE_C_PHASE196_PLUS_PET_SIM_COMPLETION_PLAN_2026-06-12.md` §6.3
Prompt: `docs/CLAUDE_C_PHASE196_PLUS_CODEX_MEDIUM_PROMPTS_2026-06-12.md` §3

## Goal

Make exactly one variant fetch-complete (all six ball families) and prove the new
render path in the running app, de-risking the C-199/200 matrix work.

## Declared Mutation Scope — honored

`sprites_runtime/goose/baby/female/blue/carry_ball_walk_00..05.png` +
`carry_ball_run_00..05.png`: 12 ADDITIVE files. Directory file count 60 → 72; all 60
pre-existing files hash-verified untouched (backup manifest). No other sprite, content,
or source file was mutated in this phase's commit scope.

## What Was Done

1. **D3 recorded.** `accept_candidate_for_apply_plan` appended to the C-56 packet's
   `decision-needed.md` (tracked file), dated, with evidence pointers.
2. **Candidate verification.** All six C-56 `carry_ball_walk` candidates re-hashed
   against `target-manifest.json`: 6/6 OK.
3. **Normalization.** Candidates (80×72, one 80×73) normalized to a uniform 80×73
   bottom-centered row canvas — same convention as
   `tools/normalize_runtime_row_canvas.py` (no repaint, no scale, no mirror).
4. **carry_ball_run synthesis (deterministic).** The six normalized carry poses with a
   run-bounce vertical lift of (0,1,2,1,0,2) px per frame index; ground-contact frames
   0 and 4 stay planted. No ball pixels (overlay-only contract). Script preserved at
   the artifact root.
5. **Guarded apply.** Backup sha256 manifest of all 60 existing files → zero-collision
   check → 12-file copy → post-proof → `rollback.ps1` (deletes the 12, re-verifies the
   60 hashes).
6. **Post-proof.** Contract audit errors: 0. Canvas report: mismatch/missing/invalid
   all 0. Optional readiness: pilot variant now 7/7 families
   `runtime_only_complete`; matrix fallback-only count 2,156 → 2,154.
7. **In-app proof (sandboxed).** Shell launched with `WEVITO_VNEXT_DATA_ROOT`
   sandbox profile + dedicated DevControl pipe; spawned goose/baby/female/blue;
   `ApplyAction play` → snapshot shows `ActionVisualFamily=PlayBall`,
   `LastActionId=play`; window captures archived pre/during. This proves the
   optional-family render path (`SpriteAssetService.GetAnimationId` honors
   `CurrentActionVisualIntent` and maps all six ball families).

## Findings (three, all load-bearing for later phases)

1. **BUG-006 (Critical, fixed same day, PR #302).** The shell was UNLAUNCHABLE on
   main since ~2026-05-18: `ToolPopupWindow.xaml` referenced an undefined
   `PopupTextBoxStyle` (introduced by C-PHASE 150/156). Tests never caught it because
   nothing instantiates that XAML; no phase launched the app after C-134. Fixed on
   `fix/bug-006-tool-popup-textbox-style` (merged before this PR); this phase's
   in-app proof ran on the fixed build.
2. **Fetch sequence is unwired (expected; C-204 scope, now confirmed).**
   `StartFetchSequence`/`AdvanceFetchSequence`/`ResolveFetchStageIntent` have NO
   production callers — the multi-stage fetch (pickup → hold → carry-walk →
   carry-run → drop) cannot be triggered in-app yet. Consequently this phase's
   in-app proof covers the PlayBall single-family path; the per-stage fetch capture
   moves to C-PHASE 204, which already owns the wiring work. The carry families are
   proven at asset level (contact sheet + audits). A possible related quirk: the
   `CurrentActionVisualIntent` appeared to persist past its 2.4 s override window in
   the sandbox session — C-204 should verify intent expiry during its wiring pass.
3. **BUG-007 (Major, open).** Color-variant tint inconsistency: goose/blue's required
   families are cream/untinted while its own optional families are blue-tinted;
   fox/blue is warm-toned; rat/blue is correct. Filed on the bug board with a
   disposition: the C-PHASE 203 triage queue builder must add a deterministic
   color-conformance check. (This phase's new frames are blue — consistent with the
   variant's optional families and the in-app blue rendering.)

## Artifacts (local root `vnext/artifacts/c-phase-198-ball-pilot/`)

- `staged-sha256.json`, `backup-manifest.json`, `rollback.ps1`
- `sprite_contract.json`, `runtime_canvas.json/.md`, `optional_readiness.json/.md`
- `carry-families-contact-sheet.png` (walk reference vs the two new families)
- `in-app-proof/` (spawn/play DevControl responses + snapshots + window captures)
- `trace/` (shell trace incl. `dev-control server-started`, `startup-complete`)
- `sandbox-profile/` (isolated DB; the live user profile was never touched)

## Stop-Gate Checklist

- [x] All six C-56 candidate sha256 values match the manifest.
- [x] No write outside the declared 12-file scope + artifact roots.
- [x] Post-proof green (contract 0 errors; canvas clean; readiness 7/7 for pilot).
- [x] No ball pixels baked into any new frame.
- [~] Fetch-stage in-app advance: NOT POSSIBLE on current main (no production
      caller — pre-existing gap, deferred to C-204 per plan; PlayBall single-family
      render path proven in-app instead). Recorded as Finding 2 rather than a silent
      pass.

## Validation

- `dotnet build .\vnext\Wevito.VNext.sln` — PASS (0/0)
- Focused `--filter "FullyQualifiedName~PetSimulation|FullyQualifiedName~Sprite"` — see PR body
- Full suite `--no-build` — 1715/1715
- `build-vnext.ps1 -SkipAssetPrep -SkipTests` — PASS
- `git diff --check` — PASS
- Contract + canvas + readiness audits — green (see Artifacts)
