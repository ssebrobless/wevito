# C-PHASE 168 — Replay Runner CLI + Read-Only Replay Log Viewer

## Goal

Add a deterministic replay verification surface for captured replay packets without enabling apply behavior, model calls, production audit writes, network access, or hidden mutation.

Base commit recorded at phase start:

`7f8015819959e29b729432479da67a3c514e9a75`

## Scope

Files added:

- `vnext/tools/Wevito.Tools.ReplayRunner/Wevito.Tools.ReplayRunner.csproj`
- `vnext/tools/Wevito.Tools.ReplayRunner/Program.cs`
- `vnext/src/Wevito.VNext.Core/SelfImprovement/Replay/ReplayResultStore.cs`
- `vnext/src/Wevito.VNext.Core/SelfImprovement/Replay/ReplayResultSummary.cs`
- `vnext/tests/Wevito.VNext.Tests/ReplayRunnerCliTests.cs`
- `vnext/tests/Wevito.VNext.Tests/ReplayResultStoreTests.cs`

Files extended:

- `vnext/src/Wevito.VNext.Core/SelfImprovement/Replay/ReplayHarness.cs`
- `vnext/src/Wevito.VNext.Core/ShellCompositionRoot.cs`
- `vnext/src/Wevito.VNext.Shell/ShellCoordinator.cs`
- `vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml`
- `vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml.cs`
- `vnext/Wevito.VNext.sln`

Files intentionally not touched:

- `vnext/tools/Wevito.Tools.SnapshotExport/*`
- `vnext/tools/Wevito.Tools.SnapshotVerify/*`
- Production audit packet kind registries
- Capability flag defaults
- Held-out and in-distribution eval stores
- Sprite assets, content JSON, and apply/mutation services

## Implemented

- Added `Wevito.Tools.ReplayRunner`, a standalone CLI with `run --captured <path> [--result <path>]`.
- Exit codes:
  - `0` for `Identical`
  - `2` for `NotApplicable`
  - `3` for `Diverged`
  - `4` for missing capture file
- Added replay result writing through `ReplayHarness.WriteResult`.
- Added `ReplayResultSummary` with operation id, result kind, diff count, replay time, and first ten diff lines.
- Added `ReplayResultStore.GetLatest(operationId)` for read-only lookup of the newest `replay-result.json`.
- Wired `ReplayResultStore` through `ShellCompositionRoot` and `ShellCoordinator`.
- Added a `Replay log (read-only)` Tool Popup expander below `Proposal diff (read-only)`.
- The replay expander shows result kind, diff count, replay timestamp, and first ten diff lines.
- The replay expander contains no button, toggle, checkbox, apply control, approval control, or capability control.
- Added tests for CLI exit behavior, artifact confinement, latest-result selection, KillSwitch behavior, missing roots, UI read-only shape, and replay-side visibility boundaries.

## Safety Boundaries

- No audit ledger writes were added to replay runner, replay result store, or the Tool Popup replay surface.
- No network or socket code was added.
- No hosted AI, local model, or training path was added.
- No held-out or in-distribution eval store reference was added.
- No new audit packet kind was added.
- No capability flag was added.
- `ReplayHarness.WriteResult` refuses paths outside `vnext/artifacts/`.
- `ReplayResultStore.GetLatest` returns `null` when `KillSwitchService.IsActive()` is true.
- The replay runner project references `Wevito.VNext.Core` only and has no ProjectReference from Shell or Tests.
- The Tool Popup replay surface is read-only and cannot rerun, approve, apply, mutate, or flip flags.

## Validation

Focused validation:

`dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "ReplayRunnerCli|ReplayResultStore|ReplayHarness|PlainLanguage|SelfImprovement" --no-restore`

Result: passed, `262/262`.

Build:

`dotnet build .\vnext\Wevito.VNext.sln`

Result: passed, `0` warnings, `0` errors.

Full tests:

`dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build`

Result: passed, `1266/1266`.

Publish:

`powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests`

Result: passed.

Whitespace:

`git diff --check`

Result: passed.

## Stop-Gate Checklist

- [x] Replay path does not write to production audit ledger.
- [x] Replay expander has no Button, ToggleSwitch, or CheckBox.
- [x] CLI has no ProjectReference from `Wevito.VNext.Shell`.
- [x] CLI has no ProjectReference from `Wevito.VNext.Tests`.
- [x] Replay result writing is confined to `vnext/artifacts/`.
- [x] Replay-side files do not import held-out or in-distribution eval stores.
- [x] Pre-flight and final validation commands pass.

All stop gates are false.

## Next Phase

C-PHASE 169 — In-distribution eval seed CLI + extended visibility lockdown.

Auto-continue: No.
