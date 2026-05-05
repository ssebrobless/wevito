# Code-Side Phase 30 Code Review Preview Tool - 2026-05-05

## Goal

Add the first read-only coding capability to PET TASKS.

The `codeReview` tool creates a static review report without editing files or running commands.

```text
PET TASKS: "review code"
   |
   v
codeReview preview adapter
   |
   v
code-review-report.json + run-summary.md
```

## Files Changed

- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Contracts\PetAgentContracts.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Core\PetCommandParser.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Core\CodeReviewPreviewAdapter.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Core\PetTaskAdapterPreviewDispatcher.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Shell\ShellCoordinator.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Shell\ToolPopupWindow.xaml`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\tools\probe-vnext-pet-tasks.ps1`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\tests\Wevito.VNext.Tests\CodeReviewPreviewAdapterTests.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\tests\Wevito.VNext.Tests\PetCommandParserTests.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\tests\Wevito.VNext.Tests\PetTaskAdapterPreviewDispatcherTests.cs`

## What Changed

- Added `TaskKind.ReviewCode`.
- Added code review report contracts:
  - `CodeReviewFileSummary`
  - `CodeReviewFinding`
  - `CodeReviewReport`
- Added `CodeReviewPreviewAdapter`.
- Added `codeReview` routing to the PET TASKS dispatcher.
- Added parser routing for commands such as `review code`, `code review`, and `inspect code`.
- Added shell policy and default code-review roots:
  - `vnext\src`
  - `vnext\tests`
  - `tools`
  - `scripts`
- Added `codeReview` to the PET TASKS capability line and probe assertion.

## Current Review Checks

- Unresolved merge-conflict markers.
- TODO/FIXME markers.
- Long lines over 180 characters.
- Trailing whitespace.
- Recursive PowerShell `Remove-Item` warning when `-Recurse` appears.

## Audit Corrections During Phase

Initial live report was mechanically correct but too noisy:

- First version scanned `.codex-cache` Chrome profile files.
- Fix: skip local cache/build/artifact/asset directories.
- Second version scanned docs first because Markdown was treated as code-review input.
- Fix: remove `.md` from code-review extensions; `localDocs` owns docs.
- Third version scanned incoming sprite metadata first because default root was the full repo.
- Fix: default shell approved roots now target code/tool/script directories.

## Validation

```text
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build --filter "FullyQualifiedName~CodeReviewPreviewAdapterTests|FullyQualifiedName~PetCommandParserTests|FullyQualifiedName~PetTaskAdapterPreviewDispatcherTests"
```

Result: passed `23 / 23`.

```text
dotnet build .\vnext\Wevito.VNext.sln
```

Result: passed, `0` warnings, `0` errors.

```text
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
```

Result: passed `110 / 110`.

```text
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
```

Result: passed.

Live PET TASKS probe:

```text
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\probe-vnext-pet-tasks.ps1 -TaskText "review code" -ExpectedToolFamily codeReview -SkipBuild
```

Result: passed.

Latest probe summary:

```text
C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\pet-task-probes\20260505-133216-429-b5ab6972\summary.json
```

Latest code review report:

```text
C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\pet-tasks\20260505-173230-codereview-reviewcode\run-summary.md
```

Latest live report summary:

- Files scanned: `80`
- Findings: `200`
- Did mutate code: `false`
- Main finding pattern: trailing whitespace in Godot scripts.

## Audit

Pass.

- Correctness: adapter writes useful static review reports.
- Safety: no edits, no command execution, no asset mutation.
- Maintainability: follows PET TASKS adapter/report pattern.
- UX: user can ask `review code` from the same PET TASKS bar.
- Scope: default shell roots now focus on project code/tool/script paths.

## Next Phase Recommendation

Proceed to Phase 31: code patch plan tool.

Keep Phase 31 read-only:

- produce proposed scope,
- affected files,
- change steps,
- validation plan,
- rollback plan,
- no edits.
