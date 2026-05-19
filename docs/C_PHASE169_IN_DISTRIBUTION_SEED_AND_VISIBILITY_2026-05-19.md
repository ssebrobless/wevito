# C-PHASE 169 — In-Distribution Eval Seed CLI + Visibility Lockdown

## Goal

Add a test-only in-distribution eval seed CLI and extend visibility lockdown tests so held-out and in-distribution eval stores stay out of replay, snapshot, dashboard, judge, and proposal surfaces.

Base commit recorded at phase start:

`ab62bc54a58e74426b35a2bb914747eefd5866f2`

## Scope

Files added:

- `vnext/tools/Wevito.Tools.InDistributionSeed/Wevito.Tools.InDistributionSeed.csproj`
- `vnext/tools/Wevito.Tools.InDistributionSeed/Program.cs`
- `vnext/tests/Wevito.VNext.Tests/InDistributionEvalCaseSeedCliTests.cs`
- `vnext/tests/Wevito.VNext.Tests/InDistributionEvalStoreVisibilityTests.cs`
- `vnext/tests/Wevito.VNext.Tests/SnapshotToolsHeldOutSeparationTests.cs`

Files extended:

- `vnext/tests/Wevito.VNext.Tests/HeldOutEvalStoreVisibilityTests.cs`
- `vnext/Wevito.VNext.sln`

Files intentionally not touched:

- `vnext/src/Wevito.VNext.Core/SelfImprovement/Eval/InDistributionEvalStore.cs`
- `vnext/src/Wevito.VNext.Core/SelfImprovement/Eval/IInDistributionEvalStore.cs`
- `vnext/src/Wevito.VNext.Core/SelfImprovement/Eval/HeldOutEvalStore.cs`
- `vnext/src/Wevito.VNext.Core/SelfImprovement/Eval/IHeldOutEvalStore.cs`
- `vnext/eval/in-distribution/*`
- Any product runtime producer, model adapter, network surface, or mutation surface

## Implemented

- Added `Wevito.Tools.InDistributionSeed`, a standalone CLI with:
  - `add --domain <X> --prompt-file <P> --expected-file <E> --root <R> [--id <ID>] [--notes <N>]`
  - SHA-256 hashing for prompt and expected files.
  - `InDistributionEvalCase` output with `case_kind = "in_distribution"`.
  - Validation equivalent to the held-out case rules for id, domain, hashes, distinct prompt/expected hashes, authored timestamp, and notes length.
  - Duplicate refusal.
  - Canonical output confinement under `--root`.
- Added tests for valid add, duplicate refusal, output escape refusal, invalid id/domain refusal, store-type separation, and no network/audit/SQLite imports in the seed CLI.
- Extended held-out store visibility tests with C-PHASE 168 replay result types and proposal diff types.
- Added in-distribution visibility tests forbidding the same surfaces from depending on `IInDistributionEvalStore` or `InDistributionEvalStore`.
- Added snapshot/replay/seed tool text-inspection tests so snapshot and replay tools remain separated from eval store surfaces.

## Safety Boundaries

- The seed CLI is test/tooling-only and is not referenced by Shell.
- The seed CLI is not referenced by Tests through a ProjectReference; tests load it by building the standalone project.
- No in-distribution case JSON was committed under `vnext/eval/in-distribution/`.
- No held-out or in-distribution store API was modified.
- No network, HTTP, SQLite, audit-ledger, model, training, or mutation path was added.
- No capability flag was added or changed.
- No audit packet kind was added.
- The new visibility tests use runtime type inspection or file-text inspection; they do not import `Wevito.Tools.*` types via `typeof(...)`.

## Validation

Focused validation:

`dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "InDistribution|HeldOut|Snapshot|PlainLanguage|SelfImprovement" --no-restore`

Result: passed, `334/334`.

Build:

`dotnet build .\vnext\Wevito.VNext.sln`

Result: passed, `0` warnings, `0` errors.

Full tests:

`dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build`

Result: passed, `1276/1276`.

Publish:

`powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests`

Result: passed.

Whitespace:

`git diff --check`

Result: passed.

## Stop-Gate Checklist

- [x] No real in-distribution case JSON committed under `vnext/eval/in-distribution/`.
- [x] `InDistributionEvalStore.cs` was not modified.
- [x] `IInDistributionEvalStore.cs` was not modified.
- [x] `HeldOutEvalStore.cs` was not modified.
- [x] `IHeldOutEvalStore.cs` was not modified.
- [x] No held-out store API surface widened.
- [x] Forbidden tests do not import `Wevito.Tools.*` types via `typeof(...)`.
- [x] Validation commands pass.

All stop gates are false.

## Next Phase

C-PHASE 170 — Local scoring provider contract.

Auto-continue: No.
