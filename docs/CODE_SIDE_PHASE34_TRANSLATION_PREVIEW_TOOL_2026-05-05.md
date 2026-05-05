# Code-Side Phase 34 Translation Preview Tool - 2026-05-05

## Goal

Add the first safe `translateText` capability to PET TASKS.

This phase makes translation requests understandable and reviewable without sending user text to any external provider.

```text
PET TASKS: "translate Hello goose to Spanish"
   |
   v
translateText preview adapter
   |
   v
translation-preview-report.json + run-summary.md
```

## Files Changed

- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Contracts\PetAgentContracts.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Core\TranslationProviderRouter.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Core\TranslationPreviewAdapter.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Core\PetCommandParser.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Core\PetTaskAdapterPreviewDispatcher.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Shell\ShellCoordinator.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Shell\ToolPopupWindow.xaml`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\tools\probe-vnext-pet-tasks.ps1`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\tests\Wevito.VNext.Tests\TranslationPreviewAdapterTests.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\tests\Wevito.VNext.Tests\PetAgentContractTests.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\tests\Wevito.VNext.Tests\PetCommandParserTests.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\tests\Wevito.VNext.Tests\PetTaskAdapterPreviewDispatcherTests.cs`

## What Changed

- Added `TaskKind.TranslateText`.
- Added translation provider status contracts:
  - `TranslationProviderKind`
  - `TranslationProviderAvailability`
  - `TranslationProviderStatus`
  - `TranslationPreviewReport`
- Added `TranslationProviderRouter`.
- Added `TranslationPreviewAdapter`.
- Added parser routing for translation commands.
- Added `translateText` routing to the PET TASKS dispatcher.
- Added shell policy for `translateText`.
- Added `translateText preview` to the PET TASKS capability line.

## Provider Router

Provider status is environment-based and does not call a network:

- DeepL: configured by `DEEPL_API_KEY` or `DEEPL_AUTH_KEY`
- Google Cloud Translation: configured by `GOOGLE_APPLICATION_CREDENTIALS` or `GOOGLE_CLOUD_TRANSLATION_API_KEY`
- Azure AI Translator: configured by `AZURE_TRANSLATOR_KEY`
- LibreTranslate: configured by `LIBRETRANSLATE_URL`

DeepL is preferred when configured. Otherwise the preview reports why DeepL is not ready.

## DeepL Research Basis

DeepL's text API uses `POST /v2/translate`, requires `target_lang`, can omit `source_lang` for auto-detection, and should read auth keys from environment/config rather than hard-coding them.

Source:

- `https://developers.deepl.com/api-reference/translate`

## Safety Behavior

- No provider is called.
- No text is sent over the network.
- No files are mutated except new markdown/JSON report artifacts.
- Translation provider calls remain locked in the UI.
- Future execution must require explicit approval before sending text to a provider.

## Validation

```text
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "FullyQualifiedName~TranslationPreviewAdapterTests|FullyQualifiedName~PetCommandParserTests|FullyQualifiedName~PetTaskAdapterPreviewDispatcherTests|FullyQualifiedName~PetAgentContractTests"
```

Result: passed `37 / 37`.

```text
dotnet build .\vnext\Wevito.VNext.sln
```

Result: passed, `0` warnings, `0` errors.

```text
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
```

Result: passed `126 / 126`.

```text
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
```

Result: passed.

Live PET TASKS probe:

```text
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\probe-vnext-pet-tasks.ps1 -TaskText "translate Hello goose to Spanish" -ExpectedToolFamily translateText -SkipBuild
```

Result: passed.

Latest probe summary:

```text
C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\pet-task-probes\20260505-140123-401-141e42b5\summary.json
```

Latest translation preview report:

```text
C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\pet-tasks\20260505-180137-translatetext-translatetext\run-summary.md
```

Latest live report summary:

- Requested text: `Hello goose`
- Target language: `Spanish`
- Preferred provider: `DeepL`
- Provider called: `false`
- Did mutate files: `false`
- Target sprite row hashes unchanged: `true`

## Audit

Pass.

- Correctness: PET TASKS can now parse and preview translation requests.
- Safety: no external provider call and no network transfer.
- UX: user can see provider readiness before approving any future translation call.
- Coordination: visual-side sprite cleanup remains protected.

## Next Phase Recommendation

Proceed to the first execution design/adapter for DeepL, but keep it approval-gated and blocked unless credentials are present.
