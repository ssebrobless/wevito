# Code-Side Phase 11 Local Docs Preview Adapter - 2026-05-05

## Scope

Added the first no-mutation pet-agent adapter seam in Core: a `localDocs` dry-run preview adapter. It is not wired to a Shell run button and cannot execute from `PET TASKS` yet.

## Shape

```text
TaskCard / TaskIntent
      |
      v
TaskAdapterRequest
      |
      v
LocalDocsPreviewAdapter
      |
      +-- validates tool family = localDocs
      +-- validates read-only policy
      +-- validates approved roots
      +-- blocks outside-root targets
      +-- enumerates supported docs only
      |
      v
TaskAdapterResult
      |
      +-- status = PreviewReady or Blocked
      +-- didMutate = false
      +-- writtenPaths = []
```

## Files Changed

- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Contracts\PetAgentContracts.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Core\LocalDocsPreviewAdapter.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\tests\Wevito.VNext.Tests\LocalDocsPreviewAdapterTests.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\tests\Wevito.VNext.Tests\PetAgentContractTests.cs`

## Implemented

- Added `TaskAdapterRunMode`.
- Added `TaskAdapterResultStatus`.
- Added `TaskAdapterRequest`.
- Added `TaskAdapterResult`.
- Added `LocalDocsPreviewAdapter`.
- Enforced:
  - dry-run preview mode only,
  - `localDocs` task/policy family match,
  - read-only policy,
  - at least one existing approved root path,
  - requested targets must stay inside approved roots,
  - no writes.

## Validation

- `dotnet build vnext\Wevito.VNext.sln` passed.
- `dotnet test vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build` passed `63 / 63`.

## Boundaries

- No Shell run button was added.
- No task-card status auto-advancement was added.
- No Tool Broker command was added.
- No browser/build/sprite/proof automation was added.
- No reports or files are written by this adapter.

## Audit Result

Pass. This creates the first adapter contract and a read-only preview implementation while preserving the current non-execution UI boundary.

## Next Safe Step

Add a dispatcher dry-run preview service that can select preview adapters by tool family, still without wiring execution to the Shell UI.
