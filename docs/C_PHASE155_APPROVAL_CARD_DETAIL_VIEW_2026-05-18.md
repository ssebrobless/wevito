# C-PHASE 155: Approval-Card Detail View

## Goal

Add a read-only Tool Popup detail panel for supervised self-improvement approval cards so the user can inspect the exact operation, scope hash, input file hashes, expected packet chain, safety copy, and artifact JSON path before any future apply runner exists.

## Scope

Files added:

- `vnext/src/Wevito.VNext.Core/SelfImprovement/ApprovalCardDetail.cs`
- `vnext/src/Wevito.VNext.Core/SelfImprovement/ApprovalCardDetailService.cs`
- `vnext/tests/Wevito.VNext.Tests/ApprovalCardDetailServiceTests.cs`
- `docs/C_PHASE155_APPROVAL_CARD_DETAIL_VIEW_2026-05-18.md`

Files extended:

- `vnext/src/Wevito.VNext.Shell/ShellCoordinator.cs`
- `vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml`
- `vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml.cs`

Not touched:

- No sprite assets.
- No content manifests.
- No model adapters.
- No network surfaces.
- No held-out eval stores or paths.
- No mutation runners.
- No approval execution behavior.

## Implemented

- Added `ApprovalCardDetailService`, a read-only SQLite-backed detail builder for the latest `self_improvement_apply_awaiting_approval` packet tied to an open task card.
- Added artifact-path guarding: the service only surfaces artifact JSON paths that canonicalize under `vnext/artifacts/`.
- Added SHA-256 input evidence rows for proposal, dry-run, and eval artifacts using the artifact JSON references.
- Added the canonical expected self-improvement packet chain for unblocked approval details.
- Added blocked detail states for KillSwitch, missing ledger rows, invalid artifact JSON, missing artifact JSON, and disallowed artifact paths.
- Wired `ShellCoordinator` to build approval-card details only for active supervised self-improvement awaiting-approval cards.
- Added a read-only `SupervisedApplyDetailExpander` in the Tool Popup with operation/scope metadata, input file hashes, expected packet chain, safety copy, and artifact path.
- Added tests proving happy-path rendering, KillSwitch blocking, outside-artifact-path blocking, missing-card blocking, read-only SQL behavior, no held-out eval reference, no audit write path, and no raw typed confirmation text exposure.

## Safety Boundaries

- The service does not write audit packets.
- The service has no `AuditLedgerService.Record` path.
- The service opens SQLite in read-only mode.
- The service does not reference `IHeldOutEvalStore` or held-out paths.
- The UI panel contains no apply, approve, mutate, or toggle button.
- Raw user typed confirmation text is not read or surfaced.
- Blocked details return empty evidence instead of partial approval data.

## Validation

- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "ApprovalCardDetail|PlainLanguage|SelfImprovement"`: passed, 247/247.
- `dotnet build .\vnext\Wevito.VNext.sln`: passed, 0 warnings, 0 errors.
- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build`: passed, 1161/1161.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests`: passed.
- `git diff --check`: passed with line-ending warnings only.

## Stop-Gate Checklist

- [x] Service writes no packet.
- [x] Service does not reference or read held-out eval paths.
- [x] Service refuses artifact paths outside `vnext/artifacts/`.
- [x] Service does not surface raw user typed confirmation text.
- [x] UI detail panel is read-only.
- [x] Pre-flight and final validation commands passed.

## Next Phase

C-PHASE 156: Operation timeline view. Auto-continue remains No.
