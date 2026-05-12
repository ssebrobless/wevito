# C-PHASE 59 Model Capability Flag And Consent UI

Date: 2026-05-12
Branch: `claude-implementation/c-phase-59-model-consent-flag`

## Outcome

C-PHASE 59 adds disabled-by-default PET TASKS model capability settings and a first-call consent surface without wiring any live provider path into Shell.

## What Changed

- Added default settings in `ShellCoordinator.ApplyDefaultSettings`:
  - `pet_model_adapter_enabled=false`
  - `pet_model_first_call_approved=false`
- Added Settings UI copy for AI helper summaries:
  - Provider: Anthropic
  - Credential target: `Wevito/anthropic/api-key`
  - Data sent summary
  - Local `model-call.json` audit expectation
  - Helper/tool allowlist summary
  - Explicit statement that enabling summaries does not execute tools, mutate sprites, run commands, or bypass approval gates
- Added an inert Settings consent button that only persists `pet_model_first_call_approved=true`.
- Kept Shell provider-free:
  - no `AnthropicModelAdapter` instantiation
  - no Windows Credential Manager reads
  - no Anthropic/OpenAI/provider call path
- Tightened `PetModelSummaryService` so even an injected test adapter is not called unless `approvedForModelCall=true`.

## Tests Added Or Updated

- `ModelConsentNoticeBuilderTests` now verifies provider, credential target, data sent, local audit, allowlist, and no-tool-execution copy.
- `ModelCapabilitySettingsTests` verifies default-disabled settings, explicit setting preservation, disabled model-call behavior, and fake-adapter-only append behavior under approval.
- `PetTaskAdapterPreviewDispatcherTests` verifies dry-run previews do not append model summaries by default.

## Validation

- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "Model|PetTask|Consent|Capability"` passed: 38 / 38.
- `dotnet build .\vnext\Wevito.VNext.sln` passed: 0 warnings, 0 errors.
- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build` passed: 291 / 291.
- Shell grep for `AnthropicModelAdapter`, `CredentialManager`, and `Wevito/anthropic/api-key` returned no live Shell hits.

## Remaining Gate

First live model calls remain blocked. A later explicit user approval phase must decide provider activation, Credential Manager read behavior, first-call execution proof, and live audit artifact expectations.
