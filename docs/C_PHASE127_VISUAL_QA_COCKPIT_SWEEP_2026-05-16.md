# C-PHASE 127: Visual QA Cockpit Sweep

## Goal

Build a non-mutating Visual QA cockpit sweep that drives the live vNext shell through DevControl, verifies every active pet matrix cell can spawn, and records frame-level visual evidence for the next sprite repair phase.

## Scope

Implemented:

- Added a `Wevito.VNext.AutomationRunner` console project for matrix sweeps.
- Added `tools/run-matrix-sweep.ps1` to launch an isolated sandbox shell, run the sweep, and write evidence.
- Added visual QA matrix contracts for manifest rows and animation observations.
- Added isolated DevControl pipe support via `WEVITO_DEV_CONTROL_PIPE`.
- Added automation-only first-launch skip and fast-mode gates for the shell.
- Added a Visual QA audit sink at `%LOCALAPPDATA%\Wevito\audit\visual-qa-issues.jsonl`.
- Added tests for DevControl catalog coverage, Visual QA audit writing, and manifest parity.

Not changed:

- No runtime PNGs were mutated.
- No source boards were mutated.
- No content manifests were changed.
- No sprite import, generation, model call, training, hosted AI, or network behavior was added.

## Evidence Shape

The sweep covers this active matrix:

```text
10 species x 3 life stages x 2 genders x 6 colors = 360 rows
```

Life stages exclude `Senior`, per the locked C-PHASE 127 contract.

Animation families inspected:

```text
idle, walk, eat, drink, sleep, groom, bathe, play,
sad, happy, play_ball, hold_ball, carry_ball_walk,
carry_ball_run, pickup_ball, drop_ball
```

Evidence artifacts:

- `vnext/artifacts/c-phase-127-matrix-sweep/visual_qa_manifest.json`
- `vnext/artifacts/c-phase-127-matrix-sweep/visual_qa_manifest.md`
- `vnext/artifacts/c-phase-127-matrix-sweep/cells/`
- `vnext/artifacts/c-phase-127-matrix-sweep/trace/`
- `%LOCALAPPDATA%\Wevito\audit\visual-qa-issues.jsonl`

## Results

- Expected rows: `360`
- Actual rows: `360`
- Tagged rows: `360`
- Animation families per row: `16`

Tag counts:

- `missing_frames`: `360`
- `crop_detected`: `204`
- `box_background`: `6`

The `missing_frames` tag is currently dominated by missing direct `drink` and `groom` runtime rows. The runtime can still fall back visually for those actions, so this is not a crash or spawn failure. It is recorded as product-truth evidence for C-PHASE 128 because the project goal is visual completeness, not merely fallback-safe behavior.

## Safety Boundaries

- The sweep uses a sandbox profile under the artifact folder.
- The shell is launched on an isolated DevControl named pipe.
- `WEVITO_SKIP_FIRST_LAUNCH_WIZARD=1` is only used by the automation run.
- `WEVITO_VISUAL_QA_FAST_MODE=1` skips expensive persistence/render churn only during the automation run.
- The sweep never writes runtime sprite assets.
- The sweep never writes source sprite boards.
- The sweep never touches `prop_anchors.json`.
- The sweep does not run asset prep.
- No hosted AI calls, local model calls, training, or network calls were introduced.

## Validation

- `dotnet build .\vnext\src\Wevito.VNext.DevController\Wevito.VNext.DevController.csproj`: passed.
- `dotnet build .\vnext\Wevito.VNext.sln`: passed.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\run-matrix-sweep.ps1`: passed, wrote `360` rows.
- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "DevControl|VisualQa"`: passed, `12/12`.
- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build`: passed, `943/943`.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests`: passed.
- `git diff --check`: passed with existing line-ending normalization warnings only.

## Stop-Gate Checklist

- [x] Cockpit/pipe connected successfully.
- [x] Every matrix cell spawned successfully.
- [x] Manifest row count is exactly `360`.
- [x] At least one cell was tagged with non-empty tags.
- [x] Visual QA audit evidence landed in `%LOCALAPPDATA%\Wevito\audit\visual-qa-issues.jsonl`.
- [x] No runtime/source sprite PNG mutation occurred.
- [x] No content manifest mutation occurred.
- [x] No hosted AI, local model, training, or network behavior was added.

## Next Phase

C-PHASE 128 should consume this manifest as the source-of-truth repair queue. The first practical targets are the direct missing `drink` / `groom` rows and the rows tagged with `crop_detected` or `box_background`.
