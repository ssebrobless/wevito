# Code-Side Phase 36 Translation Execution Gate UI - 2026-05-05

## Goal

Expose a controlled `RUN` gate for translation after the user previews the translation packet.

This phase does not auto-call DeepL. It makes the user approval step visible and testable.

```text
translateText task
   |
   v
PREVIEW
   |
   v
review translation-preview-report
   |
   v
RUN button enabled only for translateText review cards
   |
   v
TranslationExecutionAdapter
```

## Files Changed

- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Core\PetTaskCardQueueService.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Shell\ShellCoordinator.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Shell\ToolPopupWindow.xaml`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Shell\ToolPopupWindow.xaml.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\tools\probe-vnext-pet-tasks.ps1`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\tests\Wevito.VNext.Tests\PetTaskCardQueueServiceTests.cs`

## What Changed

- Added `PetTaskExecuteButton` with automation id `PetTaskExecuteButton`.
- Added `PetTaskExecutionRequested` event.
- Added shell handling for executing reviewed `translateText` cards.
- Added translation execution policy snapshot:
  - tool family: `translateText`
  - access mode: `Network`
  - risk: `Medium`
  - approval requirement: `BeforeExecution`
- Enabled `RUN` only when:
  - selected task card status is `Reviewing`
  - selected task card tool family is `translateText`
- Updated next-action text after translation preview:
  - `Next: open the preview report, then RUN only if you approve sending this text to the configured provider.`
- Allowed adapter results to move a reviewed execution result to `Done`.
- Extended live PET TASKS probe with `-ExpectExecuteEnabledAfterPreview`.

## Safety Behavior

- `RUN` does not appear as enabled before preview.
- `RUN` is only enabled for reviewed `translateText` tasks.
- Non-translation task execution is rejected.
- Translation execution still blocks without DeepL credentials.
- Provider calls remain explicit user action, not automatic preview behavior.
- No sprite/runtime/source PNG mutation.

## Validation

Focused tests:

```text
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "FullyQualifiedName~PetTaskCardQueueServiceTests|FullyQualifiedName~TranslationExecutionAdapterTests|FullyQualifiedName~TranslationPreviewAdapterTests"
```

Result: passed `14 / 14`.

Full validation:

```text
dotnet build .\vnext\Wevito.VNext.sln
```

Result: passed, `0` warnings, `0` errors.

```text
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
```

Result: passed `131 / 131`.

```text
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
```

Result: passed.

Live PET TASKS probe:

```text
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\probe-vnext-pet-tasks.ps1 -TaskText "translate Hello goose to Spanish" -ExpectedToolFamily translateText -ExpectExecuteEnabledAfterPreview -SkipBuild
```

Result: passed.

Latest probe summary:

```text
C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\pet-task-probes\20260505-142316-438-4c02437a\summary.json
```

Latest translation preview report:

```text
C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\pet-tasks\20260505-182329-translatetext-translatetext\run-summary.md
```

Latest live proof summary:

- Execute button enabled after preview: `true`
- Target sprite row hashes unchanged: `true`
- Provider called during preview: `false`

## Audit

Pass.

- Correctness: translation now has a visible post-preview execution gate.
- Safety: provider calls remain explicit and credential-gated.
- UX: the next-action text explains exactly when `RUN` should be used.
- Coordination: visual-side asset cleanup remains untouched.

## Current Translation Status

Translation is now user-flow ready up to the credential boundary:

- preview works,
- provider readiness works,
- `RUN` gate appears after preview,
- DeepL execution path exists,
- live provider calls require a configured key.
