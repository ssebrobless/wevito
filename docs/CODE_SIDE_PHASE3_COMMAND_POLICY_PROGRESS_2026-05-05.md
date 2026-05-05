# Code-Side Phase 3 Command Parser And Policy Progress

Date: 2026-05-05

Scope: deterministic command parsing, draft task-card creation, and dry-run Tool Broker policy evaluation.

No tools execute in this phase. No runtime PNGs, source boards, Godot scripts, sprite generation, browser automation, or production apply paths were changed.

## Shape

```text
Pet command bar text
  │
  ▼
PetCommandParser
  │
  ├── @PetName addressing
  ├── "PetName, ..." addressing
  ├── selected-pet fallback
  ├── best-helper routing by role
  └── risky-command blocking
  │
  ▼
TaskIntent
  │
  ▼
Draft TaskCard
  │
  ├── draft
  ├── waiting_for_approval
  └── blocked
```

```text
TaskIntent
  │
  ▼
ToolPolicyEvaluator
  │
  ├── missing policy      ──▶ blocked
  ├── disabled policy     ──▶ blocked
  ├── intent refusal      ──▶ blocked
  ├── approval required   ──▶ approval_required
  └── safe read-only      ──▶ allowed
```

## Files Added

- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Core\PetCommandParser.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Core\ToolPolicyEvaluator.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\tests\Wevito.VNext.Tests\PetCommandParserTests.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\tests\Wevito.VNext.Tests\ToolPolicyEvaluatorTests.cs`

## Files Extended

- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Contracts\PetAgentContracts.cs`

Added:

- `ToolPolicyDecisionStatus`
- `ToolPolicyDecision`

## Behavior Added

`PetCommandParser` now supports:

- `@Bean check the sprite audit`
- `Pip, summarize the latest docs`
- selected-pet fallback
- route-to-best-helper fallback
- blocked draft cards for unknown helper names
- blocked draft cards for high-risk terms like upload, delete, password, purchase, login, install, and external sending
- approval-required cards for medium-risk build/proof tasks

`ToolPolicyEvaluator` now supports:

- allowed read-only policy decisions
- approval-required policy decisions
- blocked missing-policy decisions
- blocked disabled-policy decisions
- blocked intent-refusal decisions

## Validation

Focused parser/contract/policy tests:

```powershell
dotnet test vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "FullyQualifiedName~PetAgentContractTests|FullyQualifiedName~SpriteWorkflowContractTests|FullyQualifiedName~PetCommandParserTests|FullyQualifiedName~ToolPolicyEvaluatorTests"
```

Result: passed `20 / 20`.

Build:

```powershell
dotnet build vnext\Wevito.VNext.sln
```

Result: passed with `0` warnings and `0` errors.

Full vNext tests:

```powershell
dotnet test vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
```

Result: passed `46 / 46`.

## Audit

```text
Phase audit
  ├── deterministic parser only: yes
  ├── no model dependency: yes
  ├── no tool execution: yes
  ├── pet names route but do not authorize: yes
  ├── risky commands blocked before policy: yes
  ├── policy evaluator is dry-run only: yes
  ├── tests cover safe/risky paths: yes
  └── vNext build/tests green: yes
```

## Next Safe Phase

The next phase should connect these services to a UI surface carefully:

- Add a command-bar view model or shell-facing adapter.
- Keep generated task cards draft-only.
- Display policy decisions visibly.
- Do not execute tools.
- Avoid touching the already-dirty `ToolPopupWindow` surface until a focused UI-integration pass is approved.

Potential first code-side target:

```text
PetCommandBarViewModel
  ├── helpers
  ├── input text
  ├── parsed TaskIntent
  ├── draft TaskCard
  └── ToolPolicyDecision
```

