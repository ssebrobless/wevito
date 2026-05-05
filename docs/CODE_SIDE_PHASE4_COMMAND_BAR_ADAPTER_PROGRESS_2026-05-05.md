# Code-Side Phase 4 Command Bar Adapter Progress

Date: 2026-05-05

Scope: draft-only command-bar state adapter for the future pet helper UI.

No Shell UI, WPF/XAML, runtime PNGs, source boards, Godot scripts, sprite generation, browser automation, or production apply paths were changed.

## Shape

```text
Helpers + policies + selected pet + input text
  │
  ▼
PetCommandBarService
  │
  ├── cap active helpers at 3
  ├── parse TaskIntent
  ├── create draft TaskCard
  ├── evaluate ToolPolicy
  └── return PetCommandBarState
        │
        ├── ActiveHelpers
        ├── InputText
        ├── LastIntent
        ├── LastTaskCard
        ├── LastPolicyDecision
        └── StatusMessage
```

The service gives Shell a clean binding target later without touching the already-dirty popup UI files in this phase.

## Files Added

- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Core\PetCommandBarService.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\tests\Wevito.VNext.Tests\PetCommandBarServiceTests.cs`

## Files Extended

- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Contracts\PetAgentContracts.cs`

Added:

- `PetCommandBarState`

## Behavior Added

`PetCommandBarService` now:

- normalizes the active helper roster to `3` helpers
- returns an initial command-bar state
- submits command text through `PetCommandParser`
- creates a draft task card
- evaluates the requested tool family through `ToolPolicyEvaluator`
- marks cards as `Draft`, `WaitingForApproval`, or `Blocked`
- adds a policy timeline entry
- returns a compact status message for UI display

## Validation

Focused tests:

```powershell
dotnet test vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "FullyQualifiedName~PetCommandBarServiceTests|FullyQualifiedName~PetCommandParserTests|FullyQualifiedName~ToolPolicyEvaluatorTests|FullyQualifiedName~PetAgentContractTests|FullyQualifiedName~SpriteWorkflowContractTests"
```

Result: passed `25 / 25`.

Build:

```powershell
dotnet build vnext\Wevito.VNext.sln
```

Result: passed with `0` warnings and `0` errors.

Full vNext tests:

```powershell
dotnet test vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
```

Result: passed `51 / 51`.

## Audit

```text
Phase audit
  ├── Shell UI untouched: yes
  ├── runtime assets untouched: yes
  ├── no tool execution: yes
  ├── helpers capped at 3: yes
  ├── policy decision visible in state: yes
  ├── blocked/approval/draft statuses represented: yes
  └── vNext build/tests green: yes
```

## Next Safe Phase

The next phase can begin UI integration, but it should be handled carefully because these files are already dirty:

- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Shell\ToolPopupWindow.xaml`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Shell\ToolPopupWindow.xaml.cs`

Recommended UI integration order:

1. Inspect existing dirty diffs in both files.
2. Add a compact read-only command-bar section behind a safe tool id or dev flag.
3. Bind to `PetCommandBarState`.
4. Keep submit behavior draft-only.
5. Add probe/test coverage before enabling any actual Tool Broker execution.

