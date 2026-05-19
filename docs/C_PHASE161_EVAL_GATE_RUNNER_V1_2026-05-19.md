# C-PHASE 161 - EvalGateRunner v1

## Goal

Promote `EvalGateRunner` from preview-only to a default-off v1 execution path for three cheap deterministic gates: Build, UnitTests, and ScopeHash.

## Scope

Files added:

- `vnext/src/Wevito.VNext.Core/SelfImprovement/Eval/EvalGateExecutionRequest.cs`
- `vnext/src/Wevito.VNext.Core/SelfImprovement/Eval/EvalGateExecutionResult.cs`
- `vnext/tests/Wevito.VNext.Tests/EvalGateRunnerV1Tests.cs`

Files extended:

- `vnext/src/Wevito.VNext.Core/SelfImprovement/Eval/EvalGateRunner.cs`
- `vnext/src/Wevito.VNext.Core/Audit/CapabilityFlagInventory.cs`
- `vnext/tests/Wevito.VNext.Tests/EvalGateManifestTests.cs`

Not touched:

- `EvalGateManifest.cs`
- held-out eval store implementation
- in-distribution eval store implementation
- model adapters
- autonomous scope producers
- audit packet taxonomy

## Implemented

- Added `eval_gate_runner_v1_enabled`, defaulted off through the capability flag inventory.
- Added `EvalGateExecutionRequest` and `EvalGateExecutionResult`.
- Added `EvalGateRunner.Execute(...)`.
- When the kill switch is active, every gate returns `NotApplicable("kill_switch=true")` and no command runner is invoked.
- When the flag is absent or false, every gate returns `NotApplicable("eval_gate_runner_v1_enabled=false")` and no command runner is invoked.
- When enabled, Build and UnitTests execute through `ICommandRunner`.
- Build runs `dotnet build .\vnext\Wevito.VNext.sln --nologo`.
- UnitTests runs `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build`.
- Build and UnitTests both run even if the first one fails, so both results are reported honestly.
- ScopeHash computes `ScopeHash.Compute(...)` and passes only when the result is 64 lowercase hex chars and `PacketKindsTouched` is non-empty.
- Every other manifest gate remains `NotApplicable("not_wired_in_v1")`.
- `Preview()` behavior remains unchanged: all gates remain not applicable with `no_eval_run_wired_v0`, or `kill_switch=true` if halted.
- Removed the previous unused held-out store dependency from `EvalGateRunner` so the runner has no held-out or in-distribution store constructor dependency.

## Safety Boundaries

- No production caller consumes `Execute(...)` in this phase.
- No held-out eval store is read.
- No in-distribution eval store is read.
- No direct `Process.Start` call was added; execution goes through `ICommandRunner`.
- No model call was added.
- No network surface was added.
- No new audit packet kind was added.
- No capability flag defaults to true.

## Validation

- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "EvalGate|EvalGateRunner|PlainLanguage|SelfImprovement"`: passed, 257/257.
- `dotnet build .\vnext\Wevito.VNext.sln`: passed.
- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build`: passed, 1211/1211.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests`: passed.
- `git diff --check`: passed.

## Stop-Gate Checklist

- [x] Runner does not add a network surface.
- [x] Runner does not read held-out eval store.
- [x] Runner does not read in-distribution eval store.
- [x] Runner does not call `Process.Start` directly.
- [x] `eval_gate_runner_v1_enabled` defaults to `false`.
- [x] `Preview()` behavior remains unchanged.

## Next Phase

C-PHASE 162 - second review-only scope - Auto-continue=No.
