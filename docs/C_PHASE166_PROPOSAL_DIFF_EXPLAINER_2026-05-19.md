# C-PHASE 166: Proposal Diff Explainer Panel

## Goal

Add a read-only Tool Popup panel that summarizes the structured parts of an awaiting supervised self-improvement apply proposal: source paths, proposed tools, dry-run mutation count, eval gate statuses, scope hash, and experiment manifest hash.

Base commit verified before implementation:

- `4b0450090ea02d4c62a925e96f1e79954cadd39a`

## Scope

Files added:

- `vnext/src/Wevito.VNext.Core/SelfImprovement/ProposalDiffExplanation.cs`
- `vnext/src/Wevito.VNext.Core/SelfImprovement/ProposalDiffExplainerService.cs`
- `vnext/tests/Wevito.VNext.Tests/ProposalDiffExplainerServiceTests.cs`

Files extended:

- `vnext/src/Wevito.VNext.Core/ShellCompositionRoot.cs`
- `vnext/src/Wevito.VNext.Shell/ShellCoordinator.cs`
- `vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml`
- `vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml.cs`

Files intentionally not touched:

- Audit packet taxonomy
- Capability flags
- Model adapters
- Held-out or in-distribution eval stores
- Apply/approve/mutation runners
- Sprite/runtime assets

## Implemented

- Added `ProposalDiffExplanation` as the UI-facing explanation model.
- Added `ProposalDiffExplainerService` with read-only SQLite access.
- The service finds the latest `self_improvement_apply_awaiting_approval` row for an operation id.
- The service refuses artifact paths outside `vnext/artifacts/`.
- The service parses structured artifact JSON only:
  - `proposalPath` / `proposal_path`
  - `dryRunPath` / `dry_run_path`
  - `evalPath` / `eval_path`
  - `scopeHash` / `scope_hash`
  - `experimentManifestHash` / `experiment_manifest_hash`
  - proposal `sourcePaths`
  - proposal `tools` / `recommendedTools`
  - dry-run `mutations`
  - eval `results[*].status`
- Added Shell composition and coordinator wiring.
- Added a read-only `Proposal diff (read-only)` expander under the existing supervised apply detail expander.
- Added tests for happy path, kill-switch blocking before SQL, missing input, artifact root refusal, missing awaiting row, no write path, no held-out/in-distribution store dependency, and no network surface.

## Safety Boundaries

- No hosted AI calls.
- No model calls.
- No network calls.
- No audit writes.
- No new packet kind.
- No new capability flag.
- No apply, approve, mutate, or toggle button in the new panel.
- Raw audit ledger `summary` and `error` strings are not displayed.
- Artifact reads are limited to structured JSON under `vnext/artifacts/`.

## Validation

- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "ProposalDiffExplainer|ApprovalCardDetail|PlainLanguage|SelfImprovement"`: passed, `260/260`.
- `dotnet build .\vnext\Wevito.VNext.sln`: passed.
- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build`: passed, `1248/1248`.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests`: passed.
- `git diff --check`: passed.

## Stop-Gate Checklist

- [x] Service writes no audit packet.
- [x] Service reaches no network surface.
- [x] UI panel has no apply, approve, mutate, or capability-toggle button.
- [x] Raw summary/error text is not surfaced.
- [x] Pre-flight and final validation commands pass.

## Next Phase

This completes the C-PHASE 159-166 batch. Request the next plan before drafting any further phases.
