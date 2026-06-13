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
|  |  |  |

## Closed

| ID | Severity | RC Item | Resolution |
|---|---|---|---|
| BUG-001 | Major | RC-29 | Closed in C-PHASE 134. Live reset target is `%LOCALAPPDATA%\WevitoVNext\wevito-vnext.db`, and `tools/reset-wevito-vnext-profile.ps1` performed a backup-first reset. Evidence: `vnext/artifacts/c-phase-134-verification/reset-profile-dry-run.txt`, `vnext/artifacts/c-phase-134-verification/reset-profile-actual.txt`. |
| BUG-002 | Major | RC-01 | Closed in C-PHASE 134. Selecting `Help with sprite cleanup` drafted a persisted `TaskCard` with `Status=Draft` and `ToolFamily=spriteAudit`. Evidence: `vnext/artifacts/c-phase-134-verification/sqlite-state-after-help-choice.json`, `vnext/artifacts/c-phase-134-verification/devcontrol-snapshot-after-help-choice.json`. |
| BUG-003 | Major | RC-21 | Closed in C-PHASE 134. WPF rendered six v1 starter eggs only: red, orange, yellow, blue, indigo, violet; green was omitted, and Godot fallback matched the same six-color catalog. Evidence: `vnext/artifacts/c-phase-134-verification/egg-catalog-parity.json`. |
| BUG-004 | Major | RC-25 | Closed in C-PHASE 134. DevControl action retest returned success for all nine actions, and `water`, `groom`, and `doctor` now transitioned animation state. Evidence: `vnext/artifacts/c-phase-134-verification/devcontrol-red-fox-actions.json`. |
| BUG-006 | Critical | RC-01 | Closed same-day by `fix/bug-006-tool-popup-textbox-style`. The shell crashed at startup with `XamlParseException: Cannot find resource named 'PopupTextBoxStyle'` (`ToolPopupWindow.xaml` lines 1381 and 2054 referenced a style that was never defined; introduced by C-PHASE 150/156, so the shell has been unlaunchable since ~2026-05-18 — masked because the test suite never instantiates that XAML and no phase launched the app after C-134). Fix: defined `PopupTextBoxStyle` (TargetType TextBox) in `ToolPopupWindow.xaml` resources matching the popup theme. Verified: sandboxed shell launches to `startup-complete`, DevControl spawn + actions respond. Discovered during C-PHASE 198 in-app proof. |
| BUG-007 | Major | RC-26 | Closed in C-PHASE 203A + 203B Stage 2 (2026-06-12). Tint-conformance census + fix: goose blue ×6 and raccoon adult blue ×2 repaired in 203A; deer/fox/frog blue ×18 rows (natural-colored old families, same propagate-passthrough trap) repaired in 203B Stage 2. Final census 0/360 variants nonconforming; the authored apply lane now tints all six colors with no passthrough. Evidence: `vnext/artifacts/c-phase-203b-apply/tint-census-final.json`. |
| BUG-005 | Critical | RC-ApplyRunner | Closed in C-PHASE 196. The full BUG-005 cold-cache recipe (build-server shutdown, delete all 26 `bin`/`obj` under `vnext\`, cold build, filtered + full test) was re-run on merged main after PR #298 (C-PHASE 195 squash `d7a5e916a`). All five affected `ArtifactRenameApplyRunnerTests`/`ArtifactRenameRollbackRunnerTests` facts passed (filtered run 123/123; full suite 1715/1715). The empty-packet-list failure observed 2026-05-21 at `9b0dd40aa` is resolved by the C-PHASE 189–195 chain. Evidence: `vnext/artifacts/c-phase-196-main-truth/main_truth.json`. |

## Session Notes

- 2026-02-23: Phase 2 kicked off. Baseline startup/parse checks passed.
- 2026-02-23: Static behavior wiring verified in code; no runtime bug entries yet.
- 2026-05-16: C-PHASE 125 product-truth baseline appended BUG-001 through BUG-004 from live build evidence on commit `c3572a176`.
- 2026-05-16: C-PHASE 126a moved BUG-002 and BUG-003 to ready for retest; C-PHASE 126b moved BUG-001 and BUG-004 to ready for retest.
- 2026-05-18: C-PHASE 134 live retest closed BUG-001 through BUG-004 on commit `c6365a4fe`.
- 2026-05-21: C-PHASE 189 bridge preflight fuller-clean retry opened BUG-005 on origin/main commit `9b0dd40aa0ad6b2a678ef3617b5d294890a010e1`.
- 2026-06-12: C-PHASE 196 main-sync product truth closed BUG-005 on merged main (post-PR-#298, C-PHASE 195 squash `d7a5e916a`) via the exact cold-cache repro recipe; 123/123 filtered, 1715/1715 full.
- 2026-06-12: C-PHASE 198 in-app proof discovered BUG-006 (shell unlaunchable since ~2026-05-18, missing `PopupTextBoxStyle`); fixed same day on `fix/bug-006-tool-popup-textbox-style` and verified via sandboxed launch + DevControl drive.
- 2026-06-12: C-PHASE 198 contact-sheet review opened BUG-007 (color-variant tint inconsistency in required families for at least goose/blue and fox/blue). Feeds the C-PHASE 203 triage queue: the queue builder must add a color-conformance check (per-variant hue comparison of required families against the variant's own tinted optional families or sibling color variants).

## BUG-007 Detail

- ID: BUG-007
- Severity: Major
- RC Item: RC-26 (action animation/visual correctness)
- Pet Count: 1
- Mode: Focused
- Discovered: 2026-06-12, during C-PHASE 198 carry-family contact-sheet review
- Steps:
  1. Open `sprites_runtime/goose/baby/female/blue/idle_00.png` and `play_ball_00.png`.
  2. Compare dominant body hue.
  3. Repeat for `fox/baby/female/blue/idle_00.png` vs `rat/baby/female/blue/idle_00.png`.
- Expected: every frame in a `blue` variant directory is blue-tinted, consistently across required and optional families.
- Actual: goose/blue required families (idle/walk/eat/happy avg RGB ≈ 188,159,131 — cream) are untinted while the same variant's optional families are blue (avg ≈ 78,104,141). fox/blue is warm-brown (122,85,65). rat/blue is correctly blue (51,69,94). Full per-variant census not yet taken.
- Notes: Discovered visually; the C-128/130 contract + canvas audits do not check hue, and the C-197 quality audit compares shape vs source poses only. Disposition: C-PHASE 203 triage must include a deterministic color-conformance check and queue the failing rows for repair. Severity Major (visual correctness across potentially many variants), not Critical (game playable).
- 2026-06-12 C-PHASE 203B Stage 2 — CLOSED: the full-matrix census after the regen apply exposed the bug's true extent: deer/fox/frog blue variants (18 rows) carried natural-colored (brown/warm/green) frames in their non-regenerated families — same propagate-passthrough trap. All 360 old-family frames of those variants colorized with the pipeline's `colorize()` (no passthrough); final census: 0/360 variants nonconforming. The apply tool (`apply_authored_family_imports.py`) tints every authored frame into all six colors with NO passthrough color, so this class of bug cannot re-enter via the authored lane.
- 2026-06-12 C-PHASE 203A: deterministic conformance check landed in `tools/procedural_cleanup_sweep.py` (required-family circular-mean hue vs expected tint hue from `COLOR_VARIANTS`, ±40°, saturation ≥ 0.15). Full census over the 5 procedural-lane species found 8 nonconforming variants: goose blue all 6 rows (incl. the original baby/female case — caught only when measurement was restricted to required families; the variant's blue pilot optional frames had masked it in a whole-variant mean) + raccoon adult blue both genders (previously unknown instance). All 8 repaired with the pipeline's own `colorize()` and verified (post-apply census: 0 nonconforming). Remaining known instance: fox/blue (all rows suspected) — C-PHASE 203B regenerates fox art, conformance check re-runs there. Bug stays Open until the 203B lane closes fox.

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

## BUG-005 Detail

- ID: BUG-005
- Severity: Critical
- RC Item: RC-ApplyRunner
- Pet Count: N/A
- Mode: Test
- Discovered: 2026-05-21
- Discovered during: C-PHASE 189 bridge preflight (Stage 1 fuller-clean retry)
- Repro commit: `9b0dd40aa0ad6b2a678ef3617b5d294890a010e1` (`origin/main`, C-PHASE 188 merge)
- Affected tests:
  1. `Wevito.VNext.Tests.ArtifactRenameApplyRunnerTests.Apply_happy_path_writes_six_packets_in_order`
  2. `Wevito.VNext.Tests.ArtifactRenameApplyRunnerTests.Apply_backup_sha256_mismatch_rolls_back_emits_no_completed`
  3. `Wevito.VNext.Tests.ArtifactRenameApplyRunnerTests.Apply_post_proof_mismatch_rolls_back_destination_back_to_source`
  4. `Wevito.VNext.Tests.ArtifactRenameApplyRunnerTests.Apply_kill_switch_activated_between_backup_and_post_proof_rolls_back`
  5. `Wevito.VNext.Tests.ArtifactRenameRollbackRunnerTests.ExplicitRollback_happy_path_writes_started_and_completed_packets`
- Steps:
  1. Preserve the three untracked C-PHASE 189 planning docs.
  2. Delete every `bin` and `obj` directory under `vnext\`.
  3. Run `dotnet build .\vnext\Wevito.VNext.sln`.
  4. Run `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build --logger "console;verbosity=normal"`.
- Expected: All apply/rollback runner tests pass and observe the expected emitted audit-packet sequences.
- Actual: The five affected tests assert specific emitted audit-packet sequences, but each observed packet list is empty.
- Prior state: C-PHASE 188 merge report claims 1629/1629 passing at 2026-05-19. Regression appeared between that report and 2026-05-21.
- Remediation already attempted:
  1. `dotnet build-server shutdown`
  2. Stop `Wevito.VNext.UI` processes.
  3. Delete `vnext\src\Wevito.VNext.Core\bin` and `obj`.
  4. Delete every `bin\` and `obj\` directory under `vnext\`.
  5. Cold-cache `dotnet build .\vnext\Wevito.VNext.sln`, which passed.
- Notes: None of the remediation steps resolved the failures. Next step is a separate Claude-planned diagnostic phase to read `ArtifactRenameApplyRunner`, `ArtifactRenameRollbackRunner`, and the failing test fixtures side-by-side and identify why the audit ledger capture comes back empty. No bridge PR and no C-PHASE 189 implementation should proceed until this is resolved.

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
