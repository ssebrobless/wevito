# Code-Side Phase 3 Validation Sweep

Date: 2026-05-04

Worktree:

`C:\Users\fishe\.codex\worktrees\36d6\wevito`

Purpose:

Validate the current code-side runtime/canvas state after Phase 1 docs reconciliation and Phase 2 dirty-worktree merge-safety classification. No visual generation, sprite import, staging, or reverting was performed.

## Validation Shape

```text
Phase 3 validation
|
+-- Runtime canvas contract
|   +-- non-mutating reporter
|   +-- fail on sequence mismatch/missing/invalid
|
+-- vNext tests
|   +-- focused sprite/runtime + simulation slice
|   +-- full test project
|
+-- vNext publish path
|   +-- build-vnext with SkipAssetPrep
|   +-- tests + broker publish + shell publish
|
+-- UI/action probe
    +-- packaged shell automation
    +-- actions, settings, basket, save trace
```

## Commands Run

### Focused vNext Tests

Command:

```powershell
dotnet test .\vnext\Wevito.VNext.sln -c Debug --no-restore --filter "FullyQualifiedName~SpriteRuntimeCoverageTests|FullyQualifiedName~PetSimulationEngineTests"
```

Result:

- Passed.
- `19 / 19` tests passed.

### Runtime Canvas Contract Reporter

Command:

```powershell
python tools\report_runtime_canvas_mismatches.py --runtime-root sprites_runtime --output vnext\artifacts\runtime-canvas-contract-20260504-phase3-validation.json --markdown vnext\artifacts\runtime-canvas-contract-20260504-phase3-validation.md --fail-on-mismatch
```

Result:

- Passed.
- `2880` sequences checked.
- `10800` frames checked.
- `0` sequence canvas mismatches.
- `0` missing/count mismatches.
- `0` invalid PNGs.
- `3852` legacy fixed-canvas diagnostic mismatches.

Artifacts:

- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\runtime-canvas-contract-20260504-phase3-validation.json`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\runtime-canvas-contract-20260504-phase3-validation.md`

### Full vNext Tests

Command:

```powershell
dotnet test .\vnext\Wevito.VNext.sln -c Debug --no-restore
```

Result:

- Passed.
- `26 / 26` tests passed.

### Build/Publish Script

Command:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -StepTimeoutSeconds 180
```

Result:

- Passed.
- Sprite asset preparation was intentionally skipped.
- Full vNext tests passed inside the script: `26 / 26`.
- Broker publish passed.
- Shell publish passed.
- Broker output copied into shell output.

Published shell:

`C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\shell\Wevito.VNext.Shell.exe`

### Popup-Aware Action/Tool Probe

Command:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\probe-vnext-actions.ps1 -Configuration Debug -SkipBuild -StartupDelayMs 2500
```

Result:

- Passed.
- Summary artifact:

`C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\action-probes\20260504-205959\summary.json`

Actions validated:

- `doctor`: enabled, invoked, trace matched, shell alive.
- `medicine`: enabled, invoked, trace matched, shell alive.
- `bath`: enabled, invoked, trace matched, shell alive.
- `groom`: enabled, invoked, trace matched, shell alive.
- `feed`: enabled, invoked through action option, trace matched, shell alive.
- `water`: enabled, invoked, trace matched, shell alive.
- `play`: enabled, invoked through action option, trace matched, shell alive.
- `rest`: enabled, invoked, trace matched, shell alive.
- `home`: enabled, invoked/not-needed path accepted, trace matched, shell alive.

Tool/settings/basket validation:

- `compact_hud` trace present.
- `show_pet_names` trace present.
- `show_status_summary` trace present.
- Basket paste trace present.
- Basket open trace present.
- Basket delete trace present.
- Broker open-url trace present.
- Save trace present.
- Clipboard after copy: `https://example.com/actions`.
- Shell stayed alive.

## Phase 3 Audit Result

Pass.

The current vNext shell, test suite, runtime sprite canvas contract, and popup-aware action/tool automation path are all green using the current dirty worktree state.

## Remaining Cautions

- The worktree is still intentionally dirty and should not be staged with a broad `git add .`.
- The `1704` runtime PNG payload remains binary-heavy and should be isolated from code commits.
- The `3852` fixed-canvas diagnostic mismatches are expected under the current approved contract; they are not Phase 3 failures.
- Recovery backup PNGs under `artifacts\recovery` remain local-only by default.
- Godot script changes and Gemini/browser automation helpers remain separate-review items from Phase 2.

## Recommended Next Phase

Proceed to Phase 4: Runtime Contract Hardening.

Suggested scope:

1. Inspect the reporter/test/build contracts for duplicated assumptions.
2. Add or adjust small deterministic guardrails only if they reduce future regression risk.
3. Avoid visual generation, broad asset rewrites, or runtime PNG mutation.
4. Re-run the Phase 3 validation subset after any guardrail change.
