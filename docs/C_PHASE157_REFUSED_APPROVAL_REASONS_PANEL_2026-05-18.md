# C-PHASE 157: Refused-Approval Reasons Panel

## Goal

Add a read-only Evidence-tab aggregate for `self_improvement_apply_refused` reasons and improve the user-facing explanation for the v0 apply-runner refusal.

## Scope

Files added:

- `vnext/src/Wevito.VNext.Core/SelfImprovement/RefusedApprovalAggregate.cs`
- `vnext/src/Wevito.VNext.Core/SelfImprovement/RefusedApprovalAggregateService.cs`
- `vnext/src/Wevito.VNext.Core/SelfImprovement/RefusedApprovalReasonCatalog.cs`
- `vnext/tests/Wevito.VNext.Tests/RefusedApprovalAggregateServiceTests.cs`
- `docs/C_PHASE157_REFUSED_APPROVAL_REASONS_PANEL_2026-05-18.md`

Files extended:

- `vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml`
- `vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml.cs`

Not touched:

- No sprite assets.
- No content manifests.
- No model adapters.
- No network surfaces.
- No packet producers.
- No apply runner or mutation pathway.
- No held-out eval stores or paths.

## Implemented

- Added `RefusedApprovalReasonCatalog` with the known production-emitted refusal reasons.
- Added clearer v0 copy for `apply_runner_not_implemented_in_v0`: "Apply runner is intentionally not implemented in v0. The supervised loop refused this apply as designed."
- Added `RefusedApprovalAggregateService`, a read-only SQLite aggregate over `self_improvement_apply_refused` rows.
- Counted known reasons by reason id.
- Bucketed unknown reasons as `other:<sha256-prefix>` so raw user-entered or unexpected refusal text is not displayed.
- Added KillSwitch behavior: active KillSwitch returns a blocked aggregate with no reason rows.
- Added an Evidence-tab `Refused approvals` panel with a read-only grid of reason, meaning, and count.
- Added tests proving known reasons are counted, unknown reasons are hashed, raw user text is hidden, KillSwitch blocks, SQL is read-only, no packet writes exist, and the catalog covers production-emitted reasons.

## Safety Boundaries

- The service does not write audit packets.
- The service has no `AuditLedgerService.Record` path.
- The service opens SQLite in read-only mode.
- The panel has no apply, approve, mutate, or capability-toggle button.
- Unknown refusal text is never displayed verbatim.
- No hosted AI, local model, web, training, file mutation, or sprite mutation path was touched.

## Validation

- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "RefusedApprovalAggregate|RefusedApprovalReason|PlainLanguage|SelfImprovement"`: passed, 249/249.
- `dotnet build .\vnext\Wevito.VNext.sln`: passed, 0 warnings, 0 errors.
- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build`: passed, 1173/1173.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests`: passed.
- `git diff --check`: passed with line-ending warnings only.

## Stop-Gate Checklist

- [x] Service writes no packet.
- [x] Panel displays no raw user-typed text.
- [x] Catalog includes production-emitted refusal reasons.
- [x] Unknown reasons are represented only by hash prefix.
- [x] No model call, network call, held-out eval read, or mutation path was added.
- [x] Pre-flight and final validation commands passed.

## Next Phase

C-PHASE 158: Deterministic-replay verification. Auto-continue remains No.
