# Code-Side Phase 35 Translation DeepL Execution Adapter - 2026-05-05

## Goal

Add a real-provider-capable DeepL translation execution layer while keeping PET TASKS provider calls locked until an explicit execution gate is approved.

```text
translateText preview
   |
   v
future explicit approval
   |
   v
TranslationExecutionAdapter
   |
   v
DeepL POST /v2/translate
   |
   v
translated-text.txt + translation-execution-report.json + run-summary.md
```

## Files Changed

- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Contracts\PetAgentContracts.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Core\DeepLTranslationClient.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Core\TranslationExecutionAdapter.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\tests\Wevito.VNext.Tests\TranslationExecutionAdapterTests.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\tests\Wevito.VNext.Tests\PetAgentContractTests.cs`

## What Changed

- Added `TranslationExecutionReport`.
- Added `DeepLTranslationClient`.
- Added `TranslationExecutionAdapter`.
- Added DeepL auth-key environment support:
  - `DEEPL_API_KEY`
  - `DEEPL_AUTH_KEY`
- Added DeepL endpoint selection:
  - default Pro endpoint: `https://api.deepl.com/v2/translate`
  - Free endpoint when the key ends in `:fx`: `https://api-free.deepl.com/v2/translate`
- Added basic language-name mapping for common targets such as Spanish, French, German, Japanese, English, Italian, Portuguese, Polish, Dutch, and Chinese.

## DeepL Research Basis

DeepL's current text translation API uses:

- `POST /v2/translate`
- `Authorization: DeepL-Auth-Key <key>`
- JSON body with `text` array and required `target_lang`
- optional `source_lang`
- 128 KiB request body limit for text translation
- Free API users should use `https://api-free.deepl.com`

Source:

- `https://developers.deepl.com/api-reference/translate`

## Safety Behavior

- Execution requires `TaskAdapterRunMode.Execute`.
- Execution requires a `ToolAccessMode.Network` policy.
- Execution requires an approval-gated policy.
- Execution blocks when DeepL credentials are missing.
- Execution blocks when target language is missing/unknown.
- Execution blocks when text exceeds DeepL's 128 KiB limit.
- API keys are read from environment and are not written into artifacts.
- Tests use fake HTTP only; no real DeepL call was made during validation.
- PET TASKS UI still says translation provider calls are locked.

## Validation

Focused fake-provider tests:

```text
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "FullyQualifiedName~TranslationExecutionAdapterTests|FullyQualifiedName~TranslationPreviewAdapterTests|FullyQualifiedName~PetAgentContractTests"
```

Result: passed `17 / 17`.

Full validation:

```text
dotnet build .\vnext\Wevito.VNext.sln
```

Result: passed, `0` warnings, `0` errors.

```text
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
```

Result: passed `130 / 130`.

```text
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
```

Result: passed.

## Audit

Pass.

- Correctness: the fake DeepL response path writes translated text and execution metadata.
- Safety: missing credentials block cleanly; no API key is written to reports.
- Network discipline: tests do not call a live provider.
- UI boundary: provider calls remain locked until a separate execution gate is approved.

## Current Translation Status

Translation is now partially functional:

- PET TASKS preview works live.
- Provider readiness reporting works.
- DeepL execution code exists and is tested with fake HTTP.
- Live provider execution is not yet exposed through PET TASKS.

To make end-user translation fully live, the next phase must add an explicit approval/execution gate and a configured `DEEPL_API_KEY` or `DEEPL_AUTH_KEY`.
