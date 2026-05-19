# C-PHASE 153 KillSwitch Coverage Assertion

## Goal

Lock the v0 halt invariant for the supervised self-improvement namespace with a reflection test. This phase adds no runtime behavior.

## Scope

Added:

- `vnext/tests/Wevito.VNext.Tests/KillSwitchCoverageTests.cs`
- `vnext/tests/Wevito.VNext.Tests/Support/KillSwitchCoverageAllowList.cs`

Not touched:

- No production code.
- No content JSON.
- No sprite/runtime assets.
- No model adapters.
- No network, training, tool execution, or mutation surfaces.

## Implemented

- Added a reflection test that discovers every public type under namespaces starting with `Wevito.VNext.Core.SelfImprovement`.
- The test includes sub-namespaces:
  - `Wevito.VNext.Core.SelfImprovement`
  - `Wevito.VNext.Core.SelfImprovement.Eval`
  - `Wevito.VNext.Core.SelfImprovement.Experiments`
  - `Wevito.VNext.Core.SelfImprovement.Invariants`
  - `Wevito.VNext.Core.SelfImprovement.Maturity`
- Non-allow-listed runtime candidates must expose a public constructor parameter typed `KillSwitchService`.
- Added a test-only allow list for pure records, DTOs, enums, stateless rules, static descriptors, and legacy explicit-call surfaces.
- Added a guard that the allow list entries are discoverable and each entry has an inline justification comment.
- Added a guard that discovery and candidate checking cannot pass vacuously.

## Safety Boundaries

- This is test-only.
- No runtime service behavior changed.
- No packet producer was added.
- No capability flag was added or changed.
- No model call, hosted-AI call, network access, or mutation path was added.

Two preexisting explicit-call surfaces were allow-listed instead of changed because this phase forbids runtime edits:

- `ConstitutionalReviewedEmitter`: single explicit packet emitter; report notes direct KillSwitch injection should be considered before it grows into a background/loop surface.
- `SpriteRepairBatchProposalScope`: autonomous scope is gated by caller policy today; report notes direct KillSwitch injection should be considered in a future runtime-hardening phase.

## Validation

- Pre-flight `git status`: clean before implementation.
- Pre-flight `dotnet build .\vnext\Wevito.VNext.sln`: passed.
- Pre-flight `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build`: passed, 1144/1144.
- Pre-flight `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests`: passed.
- Pre-flight `git diff --check`: passed.
- Focused validation `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "KillSwitchCoverage|PlainLanguage|SelfImprovement"`: passed, 242/242.

## Stop-Gate Checklist

- [x] Allow-list entries have one-line justification comments.
- [x] Test has non-vacuous discovery and candidate-count assertions.
- [x] Discovery includes every self-improvement sub-namespace currently present.
- [x] No runtime behavior was changed.
- [x] Pre-flight commands passed.

## Next Phase

C-PHASE 154: Capability-flag audit panel. Auto-continue is No.
