# Code-Side Phase 13: PET TASKS Adapter Preview Dispatcher

Date: 2026-05-05

## Scope

Add a Core-only dispatcher that routes approved PET TASKS dry-run preview requests to the first safe adapters:

```text
PET TASKS TaskAdapterRequest
        |
        v
PetTaskAdapterPreviewDispatcher
        |
        +-- localDocs    -> LocalDocsPreviewAdapter
        |
        +-- spriteAudit  -> SpriteAuditPreviewAdapter
        |
        +-- other        -> Blocked result
```

This phase intentionally does not wire adapter execution into the Shell, task queue, Godot proofing, or any visual mutation workflow.

## Files Added

- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Core\PetTaskAdapterPreviewDispatcher.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\tests\Wevito.VNext.Tests\PetTaskAdapterPreviewDispatcherTests.cs`

## Behavior

- Routes `localDocs` requests to `LocalDocsPreviewAdapter`.
- Routes `spriteAudit` requests to `SpriteAuditPreviewAdapter`.
- Blocks unknown tool families.
- Blocks mismatched intent/policy tool families before any adapter can run.
- Preserves the existing adapter constraints:
  - dry-run preview/report only
  - read-only policy
  - approved-root enforcement
  - no runtime/source PNG mutation
  - no sprite import
  - no visual generation
  - no prop-anchor edits
  - no candidate apply/proof automation

## Tests Added

- `BuildPreview_RoutesLocalDocsRequests`
- `BuildPreview_RoutesSpriteAuditRequests`
- `BuildPreview_BlocksUnknownToolFamily`
- `BuildPreview_BlocksMismatchedIntentAndPolicyFamiliesBeforeRouting`

## Validation

- `dotnet build vnext\Wevito.VNext.sln` passed.
- `dotnet test vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build` passed `71 / 71`.

## Audit Notes

- No Shell UI execution control was added.
- No task-card status mutation is performed by the dispatcher.
- The dispatcher does not write artifacts directly; artifact behavior remains owned by each adapter.
- The goose `drop_ball` one-row apply/proof remains outside PET TASKS.

## Next Safe Step

Add a non-executing Shell preview bridge only after deciding the exact UI gate:

- likely selected approved card
- explicit preview button
- read-only adapter only
- result summary/audit path written back to the task card
- no `Execute` run mode yet
