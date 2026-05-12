# C-PHASE 57: Tool Hub Simplification

Date: 2026-05-12
Branch: `claude-implementation/c-phase-57-tool-hub-simplification`

## Scope

C-PHASE 57 simplified the PET TASKS surface into a clearer single command entry and task-card review flow while preserving the existing safe routing and execution gates.

No live model calls, new execution modes, sprite mutation, generation, import, special task-completion animations, or test-only live pet routing assumptions were introduced.

## Changes

- Renamed the PET TASKS panel copy to emphasize one command bar and one safe task-card flow.
- Replaced the long capability paragraph with the required concise categories:
  - Report-only tools: `localDocs`, `spriteAudit`, `assetInventory`, `petState`, `codeReview`, `codePatchPlan`
  - Approval-only tools: `buildProof`, `translateText`, `audioAssist`, `screenCapture`
  - Locked: model calls, sprite mutation, browser automation, recording, external audio boosters
- Updated task-card display text so the selected card surfaces:
  - helper
  - family
  - status
  - report path
  - next allowed action
- Updated queue labels to show `helper | family | status | report | next`.
- Added parser coverage for the exact Phase 57 no-prefix probe phrases so routing does not depend on test-only names like `Bean`.

## Files Changed

- `vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml`
- `vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml.cs`
- `vnext/tests/Wevito.VNext.Tests/PetCommandParserTests.cs`
- `docs/C_PHASE57_TOOL_HUB_SIMPLIFICATION_2026-05-12.md`

## Proof Probes

All six required no-execution probes passed with `-SkipBuild`:

| Family | Task text | Artifact |
| --- | --- | --- |
| `localDocs` | `summarize the local docs` | `vnext/artifacts/pet-task-probes/20260512-162048-433-dc90c95e/summary.json` |
| `spriteAudit` | `review goose baby female blue sprites` | `vnext/artifacts/pet-task-probes/20260512-162115-556-19e606e7/summary.json` |
| `assetInventory` | `inventory assets in sprites_runtime` | `vnext/artifacts/pet-task-probes/20260512-162139-746-33b52fea/summary.json` |
| `petState` | `review pet state` | `vnext/artifacts/pet-task-probes/20260512-162205-438-4efecb2d/summary.json` |
| `codeReview` | `review the code in Wevito.VNext.Core` | `vnext/artifacts/pet-task-probes/20260512-162230-105-1e109b76/summary.json` |
| `codePatchPlan` | `plan a code fix in vnext` | `vnext/artifacts/pet-task-probes/20260512-162250-764-b61776be/summary.json` |

## Validation

- `dotnet build .\vnext\Wevito.VNext.sln` passed.
- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build` passed: 286 / 286.

## Residuals

- This phase intentionally did not add any new tool capabilities.
- This phase did not make PET TASKS execute model calls or visual/sprite mutation.
- Task cards still use the existing WPF popup flow; broader visual redesign remains a future UI phase.
