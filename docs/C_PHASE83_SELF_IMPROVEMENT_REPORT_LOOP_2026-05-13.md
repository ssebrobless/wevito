# C-PHASE 83: Self-Improvement Report Loop

## Goal
Add a daily/weekly report loop that explains what Wevito did, what changed, and when a rollback should be reviewed after a learning regression.

## Scope
- Added `SelfImprovementReportService` to read the append-only audit ledger and write `report.md` plus `report.json` under `vnext/artifacts/pet-tasks/<ts>-self-improvement-report/`.
- Added `RollbackProposalService` to detect `tuning_apply` followed by `golden_eval` regression within 24 hours.
- Rollback proposals create `rollback_proposal` evidence packets and Draft `guardedMutation` task cards only.
- Added a manual runner at `tools/run-self-improvement-report.ps1`.
- Surfaced self-improvement status in the PET TASKS activity panel and a small home-panel hint when rollback proposals are waiting.
- Hardened the C-PHASE 82 golden dataset hash against Windows CRLF working-tree checkouts by normalizing dataset text before hashing in both C# and PowerShell.

## Safety Boundaries
- No hosted AI calls.
- No network calls.
- No model downloads.
- No asset/runtime PNG mutation.
- No automatic rollback execution.
- KillSwitch blocks both report generation and rollback proposal generation.
- Markdown reports contain plain English counts, flags, timestamps, and artifact links only; JSON detail stays in `report.json`.

## Validation
- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "SelfImprovement|RollbackProposal"` passed: 8/8.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\run-self-improvement-report.ps1 -Window day -ArtifactRoot .\vnext\artifacts\pet-tasks` passed and wrote a report packet.
- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "GoldenEval|EvalRegression"` passed: 7/7.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\run-golden-eval.ps1 -ArtifactRoot .\vnext\artifacts\eval-golden\` passed after CRLF-normalized hashing.
- `dotnet build .\vnext\Wevito.VNext.sln` passed.
- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build` passed: 501/501.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests` passed.

## Next Phase
C-PHASE 84 may begin after this PR is merged because this phase is auto-continue when validation is green.
