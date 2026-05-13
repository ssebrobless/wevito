# C-PHASE 85c: Promotion Snapshot And UI Entry

## Goal

Add the promotion criteria snapshot service, promotion eval runner, and a confirmation-gated Settings entry for the autonomous operations beta without enabling the beta by default.

## Scope

- Added `PromotionCriteriaSnapshot` and `PromotionDecisionLabel`.
- Added the `promotion-snapshot` soak-driver command and `tools/run-promotion-eval.ps1`.
- Added a Settings-panel promotion summary and visible `Try the autonomous beta` entry.
- Kept the entry disabled unless a latest promotion decision is `EnableAutonomousBeta`, beta is currently off, and KillSwitch is off.
- Replaced the old direct autonomous-beta checkbox enable path with read-only status plus consent-gated enable flow.

## Implemented

- Snapshot criteria cover the 11 planned safety and liveness gates:
  - window days
  - active uptime per active day
  - policy violations
  - mutation proof coverage
  - hosted AI calls in local-only mode
  - focus steal events
  - resource budget tolerance
  - citation coverage ratio
  - golden eval result
  - self-improvement reports per active day
  - kill switch active during the window
- Safety failures return `PauseForReliabilityWork`.
- Liveness failures return `KeepSupervisedPreview`.
- A complete synthetic 7-day fixture returns `EnableAutonomousBeta`.
- Active KillSwitch refuses computation with `KeepSupervisedPreview`.
- Snapshot and decision packet kinds are explained by `PlainLanguageExplainer` and listed in `tool_definitions.json`.
- Explicit user consent records `runtime_autonomous_beta_user_consent` before setting `runtime_autonomous_beta_enabled=true`.

## Safety Boundaries

- No hosted AI calls.
- No network access.
- No asset mutation.
- No default-off capability was turned on.
- C-PHASE 85c does not ship a default `EnableAutonomousBeta` decision packet.
- `tools/run-promotion-eval.ps1 -EmitDecisionOnly` emits a decision packet for review but exits successfully for validation even when the decision is `KeepSupervisedPreview`; non-dry-run mode preserves the promotion gate behavior.

## Validation

```powershell
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "PromotionCriteria|AutonomousBeta|ToolPopup"
dotnet build .\vnext\Wevito.VNext.sln
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\run-promotion-eval.ps1 -ArtifactRoot .\vnext\artifacts\promotion\ -EmitDecisionOnly
```

Results:

- Focused tests: passed, 27 / 27.
- Solution build: passed.
- Full tests: passed, 563 / 563.
- Safe vNext publish with `-SkipAssetPrep`: passed.
- Promotion eval dry-run: passed, emitted `KeepSupervisedPreview`.

## Next Phase

C-PHASE 85d must provide the real wall-clock evidence packet. The Settings entry should remain disabled until the latest reviewed `decision.json` returns `EnableAutonomousBeta`.
