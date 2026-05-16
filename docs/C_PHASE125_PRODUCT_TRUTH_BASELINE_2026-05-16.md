# C-PHASE 125 Product-Truth Baseline

Date: 2026-05-16
Branch: `claude-implementation/c-phase-125-product-truth-baseline`
Base commit: `c3572a176` (`C-PHASE 124: add pet behavior and AI awareness [I-Reframe]`)

## Evidence Shape

```text
fresh build
  |
  +-- validation green: build + 917 tests + SkipAssetPrep publish + diff check
  |
  +-- fresh-profile walkthrough
  |     +-- first-launch wizard observed
  |     +-- WPF egg prompt inspected by UIAutomation
  |     +-- DevControl spawn/action flow asserted
  |
  +-- product-truth diffs
        +-- BUG-001 stale reset path
        +-- BUG-002 missing onboarding task card
        +-- BUG-003 WPF/Godot egg mismatch
        `-- BUG-004 action animation feedback gap
```

## Commands And Validation

| Check | Result | Evidence |
|---|---:|---|
| `dotnet build .\vnext\Wevito.VNext.sln` | Pass | `vnext/artifacts/c-phase-125-truth/dotnet-build.log` |
| `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build` | Pass, 917/917 | `vnext/artifacts/c-phase-125-truth/dotnet-test-no-build.log` |
| `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests` | Pass | `vnext/artifacts/c-phase-125-truth/build-vnext-skip-asset-prep.log` |
| `git diff --check` | Pass | `vnext/artifacts/c-phase-125-truth/git-diff-check.log` |

The walkthrough used the freshly built shell at `vnext/src/Wevito.VNext.Shell/bin/Debug/net8.0-windows10.0.19041.0/Wevito.VNext.Shell.exe`.

## Verified True

- The synced repo is descended from `c3572a176`; `git log -5` is captured in `vnext/artifacts/c-phase-125-truth/git-log-5.txt`.
- The shell launches cleanly from the fresh build output.
- First-launch onboarding exists in the current WPF shell and writes append-only audit rows with `did_use_network=false`, `did_use_hosted_ai=false`, `did_use_local_model=false`, and `did_mutate=false`; see `vnext/artifacts/c-phase-125-truth/audit-tail-after-fresh-launch.jsonl`.
- The first-launch background option `Help with sprite cleanup` is recordable as `first_launch_background_choice=HelpWithSpriteCleanup`.
- WPF egg prompt shows seven ROYGBIV eggs, and the green egg is disabled; UIAutomation evidence is in `vnext/artifacts/c-phase-125-truth/uia-home-tree-egg-prompt.json`.
- DevControl `SpawnOrReplacePet` correctly mapped the tested red egg catalog species to `fox` for slot 0; see `vnext/artifacts/c-phase-125-truth/devcontrol-spawn-and-actions.json`.
- DevControl action calls for `feed`, `water`, `rest`, `play`, `groom`, `bath`, `medicine`, `doctor`, and `home` all returned `success=true`.
- Wevito-spawned processes were set to BelowNormal priority; evidence is in `vnext/artifacts/c-phase-125-truth/process-priorities.json` and `audit-tail-combined-after-actions.jsonl`.
- The product runtime did not call hosted AI during this walkthrough.

## Drifted From Plan

- The reset procedure in the C-PHASE 125 prompt targets `%LOCALAPPDATA%\Wevito\state.json`, but the current live save is `%LOCALAPPDATA%\WevitoVNext\wevito-vnext.db`. I backed up and deleted the SQLite DB to get a real fresh-profile walkthrough. This is tracked as BUG-001.
- First-launch `Help with sprite cleanup` records the choice, but no `spriteAudit` task card is drafted in persisted state. This is tracked as BUG-002.
- WPF and Godot egg-selection semantics are not aligned. WPF has seven eggs with green disabled and deterministic species mapping; Godot has six colors and uses random species selection in `add_pet_with_color`. This is tracked as BUG-003.
- Three successful care actions did not produce a distinct animation-state change in the sequential DevControl test: `water`, `groom`, and `doctor`. This is tracked as BUG-004.
- Ollama is not reachable at `http://127.0.0.1:11434/api/tags`; local model serving is not active in the current production path. Probe evidence is in `vnext/artifacts/c-phase-125-truth/ollama-probe.json`.
- `tools/capture-vnext-window.ps1` still references the older `net8.0-windows` shell path shape, while the current WPF shell build output is under `net8.0-windows10.0.19041.0`. Screenshots were captured with a direct current-window capture helper for this phase.

## Bug List

| Bug | Severity | Summary | Evidence |
|---|---|---|---|
| BUG-001 | Major | Fresh-profile reset target is stale; live save persists in SQLite | `vnext/artifacts/c-phase-125-truth/sqlite-state-after-actions.json` |
| BUG-002 | Major | First-launch sprite cleanup choice does not draft a `spriteAudit` task card | `vnext/artifacts/c-phase-125-truth/sqlite-state-after-actions.json` |
| BUG-003 | Major | WPF and Godot egg catalogs disagree | `vnext/artifacts/c-phase-125-truth/egg-catalog-code-comparison.json` |
| BUG-004 | Major | Some successful care actions do not visibly transition animation state | `vnext/artifacts/c-phase-125-truth/devcontrol-spawn-and-actions.json` |

## Sprite Quality Snapshot

This is an objective runtime PNG snapshot, not a subjective art score. Contact sheets are receipts for later human review.

| Species | Objective Notes | Receipt |
|---|---|---|
| crow | 1080 PNGs; 0 edge-touch frames; multiple intentional canvas sizes across stages | `vnext/artifacts/c-phase-125-truth/sprite-quality/crow.png` |
| deer | 1080 PNGs; 24 frames touch canvas edge | `vnext/artifacts/c-phase-125-truth/sprite-quality/deer.png` |
| fox | 1080 PNGs; 0 edge-touch frames; uniform 72x64 canvas | `vnext/artifacts/c-phase-125-truth/sprite-quality/fox.png` |
| frog | 1080 PNGs; 576 frames touch canvas edge; multiple dimensions | `vnext/artifacts/c-phase-125-truth/sprite-quality/frog.png` |
| goose | 1098 PNGs; 756 frames touch canvas edge; includes the prior 60x59 optional pilot frames | `vnext/artifacts/c-phase-125-truth/sprite-quality/goose.png` |
| pigeon | 1080 PNGs; 30 frames touch canvas edge; multiple large canvases | `vnext/artifacts/c-phase-125-truth/sprite-quality/pigeon.png` |
| raccoon | 1080 PNGs; 12 frames touch canvas edge; uniform 72x64 canvas | `vnext/artifacts/c-phase-125-truth/sprite-quality/raccoon.png` |
| rat | 1080 PNGs; 0 edge-touch frames; uniform 72x64 canvas | `vnext/artifacts/c-phase-125-truth/sprite-quality/rat.png` |
| snake | 1080 PNGs; 1056 frames touch canvas edge; 24 frames have opaque corner pixels | `vnext/artifacts/c-phase-125-truth/sprite-quality/snake.png` |
| squirrel | 1080 PNGs; 0 edge-touch frames; uniform 72x64 canvas | `vnext/artifacts/c-phase-125-truth/sprite-quality/squirrel.png` |

Full objective data: `vnext/artifacts/c-phase-125-truth/sprite-quality-snapshot.json`.

## Local-AI Status

- Ollama probe result: not reachable, timeout at `http://127.0.0.1:11434/api/tags`.
- Audit packet `model_bootstrap_runtime_absent` says deterministic local fallback remains active.
- No hosted AI call was observed from product runtime.
- No local model serving path is active in production behavior today.
- Current "AI" behavior in the tested shell is local scaffolding plus deterministic fallback, not an independently serving reasoning model.

## Stop-Gate Checklist

- [x] No hosted AI call observed from product runtime.
- [x] No silent network access observed.
- [x] No silent out-of-scope file mutation observed. Expected local save/audit writes occurred during launch and walkthrough.
- [x] No new capability flags flipped on.
- [x] AuditLedger remains append-only.
- [x] All five pre-flight/final validation commands still pass.
- [x] Shell launched cleanly.

## Evidence Artifacts

- `vnext/artifacts/c-phase-125-truth/01-first-launch-overlay.png`
- `vnext/artifacts/c-phase-125-truth/03-after-spawn-and-actions.png`
- `vnext/artifacts/c-phase-125-truth/audit-tail-after-fresh-launch.jsonl`
- `vnext/artifacts/c-phase-125-truth/audit-tail-combined-after-actions.jsonl`
- `vnext/artifacts/c-phase-125-truth/devcontrol-snapshot-initial.json`
- `vnext/artifacts/c-phase-125-truth/devcontrol-spawn-and-actions.json`
- `vnext/artifacts/c-phase-125-truth/egg-catalog-code-comparison.json`
- `vnext/artifacts/c-phase-125-truth/final-validation-summary.json`
- `vnext/artifacts/c-phase-125-truth/ollama-probe.json`
- `vnext/artifacts/c-phase-125-truth/process-priorities.json`
- `vnext/artifacts/c-phase-125-truth/sprite-quality-snapshot.json`
