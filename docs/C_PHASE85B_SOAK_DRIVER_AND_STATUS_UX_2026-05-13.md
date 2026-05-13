# C-PHASE 85b: Soak Driver And Evidence-Collection Status UX

## Goal
Add the local soak-driver tooling and read-only status surfaces needed to collect a real evidence window before any autonomous-operations promotion.

## Scope
- Added `EvidenceCollectionStatusService` to read the audit ledger and latest soak manifest without writing rows.
- Added `SoakDriverCommandService` plus `tools/soak-driver-cli/` for heartbeat, day-end, window-end, and status commands.
- Added `tools/run-soak-driver.ps1` for the user-run soak loop with process and default-off capability preflights.
- Replaced `tools/run-soak-validation.ps1` with a compatibility wrapper that delegates to the new soak driver.
- Added `tools/check-evidence-readiness.ps1` for a read-only one-screen readiness table.
- Updated `tools/build-vnext.ps1` to publish the soak driver CLI into `vnext/artifacts/soak-driver/`.
- Added evidence collection status to the home badge, roam-band suffix, and tool popup activity panel.
- Added tests for status calculation, kill-switch handling, soak command behavior, window-end packets, and private-text-safe UI formatting.

## Safety Boundaries
- No settings are flipped by the soak driver, CLI, or status UI.
- No hosted AI, model call, web fetch, training, sprite mutation, or asset mutation is introduced.
- The driver refuses to start if Wevito is not running.
- The driver refuses to start if any guarded default-off capability is enabled.
- The status service is read-only and never writes ledger rows.
- The activity-panel status uses counts, packet kinds, and safe timestamps only; it does not display raw private ledger summaries.
- `soak_window_end` is covered by `PlainLanguageExplainer`.

## Validation
- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "EvidenceCollectionStatus|SoakDriver"` passed: 10/10.
- `dotnet build .\vnext\Wevito.VNext.sln` passed.
- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build` passed: 541/541.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests` passed and published `vnext\artifacts\soak-driver\Wevito.VNext.SoakDriver.exe`.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\check-evidence-readiness.ps1` passed and reported `not_started`.

## Next Phase
C-PHASE 85c is next, but auto-continue is **NO**. This is the first end-to-end soak surface and should stop for review before promotion snapshot and confirmation-gated UI work.
