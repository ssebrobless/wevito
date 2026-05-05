# Code-Side Phase 10 Cross-Thread Adapter Coordination - 2026-05-05

## Purpose

Create the coordination checkpoint required before implementing any real `PET TASKS` execution adapters.

Code-side now has enough UI/state infrastructure for helper-pet task cards, but execution must not begin until visual-side confirms the proof/sprite-audit boundaries and the user explicitly approves the first adapter phase.

## Current Shape

```text
Implemented now
  |
  +-- PET TASKS text input
  +-- deterministic command parser
  +-- dry-run policy evaluator
  +-- durable TaskCard queue
  +-- queue summary in popup
  +-- local Approve / Cancel status edits
  |
  +-- NOT implemented
        |
        +-- no adapter dispatcher
        +-- no tool execution
        +-- no browser automation
        +-- no build/probe runner from PET TASKS
        +-- no sprite audit runner from PET TASKS
        +-- no proof/contact-sheet runner from PET TASKS
        +-- no visual generation/import/apply path
```

## Code-Side Green Lane

These are safe for code-side to design or implement next after coordination:

- `localDocs` read-only adapter planning.
- `spriteAudit` non-mutating report adapter planning.
- `proofCapture` proof/contact-sheet adapter planning.
- `checklist` docs-only draft adapter planning.
- adapter audit-log contracts.
- dry-run preview contracts.
- root-path allowlist enforcement.
- cancellation/status transition plumbing.

## Code-Side Yellow Lane

These need explicit sequencing and proof expectations before implementation:

- running Godot proof/contact-sheet scripts from `PET TASKS`.
- running vNext/Godot build probes from `PET TASKS`.
- writing reports under `vnext/artifacts`.
- opening proof artifacts from the pet UI.
- normalizing any generated proof manifest fields.

## Red Lane / Still Paused

These remain blocked unless separately approved:

- visual generation.
- sprite import.
- runtime PNG mutation.
- source-board mutation.
- broad optional-animation expansion.
- all-color propagation.
- changing `sprites_runtime\_metadata\prop_anchors.json`.
- changing `sprites_shared_runtime\items\toys_a\ball.png`.
- any adapter that can silently modify project files without an explicit approved task card.

## Visual-Side Coordination Questions

Before code-side implements the first proof/sprite adapter, visual-side should confirm:

1. Which proof surface should be first for `PET TASKS`: Godot packaged proof, vNext proof, or report-only proof?
2. Which current visual scripts/artifact conventions should code-side call instead of inventing new ones?
3. For `spriteAudit`, what counts as read-only output: markdown report only, JSON report, contact sheet, or all three?
4. For `proofCapture`, should contact sheets include runtime overlay metadata by default?
5. Are there any current visual-side artifact folders that code-side must not write into?
6. Should the goose `drop_ball` one-row apply/proof remain a manual code-side operation outside `PET TASKS` for now?

## Recommended Joint Decision

Keep the goose `drop_ball` one-row apply/proof outside the pet-agent adapter system for now.

Reason:

- It mutates runtime PNGs.
- It has strict rollback/hash protection requirements.
- It is a production apply/proof pilot, not a generic pet-helper task.
- It should remain hand-controlled until the manifest/provenance/apply workflow is mature.

Use `PET TASKS` first for non-mutating proof/report adapters only.

## Recommended Next Code-Side Adapter Phase

Implement a no-mutation `localDocs` or `spriteAudit` dry-run adapter first.

Suggested first adapter:

```text
Tool family: spriteAudit
Allowed side effects: write markdown/json report under vnext/artifacts only
Forbidden side effects: no PNG writes, no source-board writes, no manifest apply, no runtime mutation
Task statuses:
  Approved -> Running -> Reviewing -> Done
  Approved -> Blocked if policy/root/artifact path fails
```

## Latest Code-Side Validation

- `dotnet build vnext\Wevito.VNext.sln` passed.
- `dotnet test vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build` passed `58 / 58`.

## Coordination Result

Code-side is ready to continue with adapter contracts/planning after visual-side acknowledges the above boundaries.

Do not implement a real execution adapter until the user provides the go-ahead after this cross-thread checkpoint.
