# C-PHASE 46 AI Helper Gate Review

Date: 2026-05-12

Branch: `claude-implementation/c-phase-46-ai-helper-gate-review`

## Decision

```text
AI helper live-call gate
|
+-- Core model seam exists
|   +-- IModelAdapter / AnthropicModelAdapter
|   +-- approval required before credential read
|   +-- Windows Credential Manager target only
|   +-- local model-call.json audit
|   `-- tests cover fake/provider-blocked paths
|
+-- Shell runtime path remains default-disabled
|   +-- ShellCoordinator does not instantiate AnthropicModelAdapter
|   +-- PetTaskAdapterPreviewDispatcher does not call PetModelSummaryService
|   +-- no live model-call UI approval path is wired
|   `-- no model credentials were read or written in this phase
|
`-- Gate decision: HOLD
    +-- safe as dormant infrastructure
    +-- not ready for first user live-call approval yet
    `-- needs explicit capability flag + first-call consent UI wiring first
```

The current code is safe to keep because it is dormant by default, but it is **not ready to approve a first live model call**. The missing product seam is an explicit Shell-level capability flag and consent flow that wires `PetModelSummaryService` only after the user deliberately enables it.

No live model calls were made during this phase.

## Scope Reviewed

- `vnext/src/Wevito.VNext.Core/IModelAdapter.cs`
- `vnext/src/Wevito.VNext.Core/AnthropicModelAdapter.cs`
- `vnext/src/Wevito.VNext.Core/HelperAllowlistEvaluator.cs`
- `vnext/src/Wevito.VNext.Core/PetModelSummaryService.cs`
- `vnext/src/Wevito.VNext.Core/ModelConsentNoticeBuilder.cs`
- `vnext/src/Wevito.VNext.Core/PetCommandBarService.cs`
- `vnext/src/Wevito.VNext.Core/PetTaskAdapterPreviewDispatcher.cs`
- `vnext/src/Wevito.VNext.Shell/ShellCoordinator.cs`
- `vnext/tests/Wevito.VNext.Tests/AnthropicModelAdapterTests.cs`
- `vnext/tests/Wevito.VNext.Tests/HelperAllowlistEvaluatorTests.cs`
- `vnext/tests/Wevito.VNext.Tests/LethalTrifectaTests.cs`
- `vnext/tests/Wevito.VNext.Tests/ModelConsentNoticeBuilderTests.cs`
- `vnext/tests/Wevito.VNext.Tests/PetAgentContractTests.cs`
- `vnext/tests/Wevito.VNext.Tests/PetCommandBarServiceTests.cs`

## Current Safety Shape

```text
PET TASKS preview
|
+-- ShellCoordinator
|   `-- PetTaskAdapterPreviewDispatcher.BuildPreview(...)
|       +-- localDocs / spriteAudit / petState / etc.
|       `-- no model summary append
|
`-- dormant Core seam
    `-- PetModelSummaryService.AppendIfAllowedAsync(...)
        +-- requires injected IModelAdapter
        +-- requires PreviewReady result
        +-- requires HelperAllowlistEvaluator approval
        +-- requires approvedForModelCall=true to reach provider
        `-- appends summary only, no mutation
```

The important distinction: `PetModelSummaryService` can enrich a preview result if someone injects a model adapter and passes approval, but the current Shell path does not do that.

## Helper Roster And Permission Boundaries

The helper roster remains capped at three helpers:

| Helper | Species | Role | Runtime purpose |
| --- | --- | --- | --- |
| Scout | frog | `ResearchHelper` | docs/research style tasks |
| Inspector | pigeon | `SpriteReviewHelper` | sprite QA and pet-state review |
| Builder | rat | `ChecklistHelper` | code/proof/checklist planning |

Existing tests keep this boundary in place:

- `PetCommandBarServiceTests.BuildInitialState_CapsActiveHelpersAtThree`
- `PetCommandBarServiceTests.BuildDefaultRoster_UsesStableThreeHelperNamesAndSpecies`
- `PetCommandBarServiceTests.AddHelper_RejectsWhenRosterAlreadyHasThreeHelpers`
- `PetAgentContractTests.TaskCard_KeepsPetRoutingSeparateFromToolPolicy`

Personality/routing remains separate from permission. A pet name or role can route the task card, but tool policy and model allowlist decisions are evaluated separately.

## Model Allowlist Matrix

Current default model-summary capabilities:

| Helper role | Helper name | Tool family | Untrusted external | Private local data | Sends network | Gate result |
| --- | --- | --- | --- | --- | --- | --- |
| `ResearchHelper` | Scout | localDocs | yes | no | yes | allowed if explicitly wired/approved |
| `ResearchHelper` | Scout | codeReview | yes | no | yes | allowed if explicitly wired/approved |
| `SpriteReviewHelper` | Inspector | spriteAudit | no | yes | yes | allowed if explicitly wired/approved |
| `SpriteReviewHelper` | Inspector | petState | no | yes | yes | allowed if explicitly wired/approved |
| `ChecklistHelper` | Builder | codePatchPlan | no | yes | yes | allowed if explicitly wired/approved |
| `ChecklistHelper` | Builder | codeReview | no | yes | yes | allowed if explicitly wired/approved |

The default matrix intentionally avoids the lethal trifecta:

```text
blocked condition = untrusted external input + private local data + network send
```

Covered by:

- `HelperAllowlistEvaluatorTests.FindLethalTrifectaViolations_DefaultMatrixHasNone`
- `LethalTrifectaTests.Evaluate_BlocksCapabilityCombiningUntrustedPrivateAndNetwork`

The model allowlist also denies execute mode entirely:

```text
HelperPermissionMode.Execute -> denied
```

This keeps model output as a read-only summary layer, not an executor.

## Credential And Consent Review

Credential lookup is implemented only in `WindowsCredentialManagerModelCredentialStore`.

Target:

```text
Wevito/anthropic/api-key
```

Observed behavior:

- If `ApprovedForModelCall` is false, `AnthropicModelAdapter` blocks before reading credentials.
- If approval is true but no key exists, it blocks without provider call.
- If approval and a fake test key exist in tests, it calls the fake handler and writes `model-call.json`.
- Audit records include provider, model, pet id/name, helper role, tool family, args hash, decision, block reason, latency, and timestamp.
- Audit records do not store the raw API key.

Covered by:

- `AnthropicModelAdapterTests.SuggestAsync_BlocksWithoutApprovalBeforeReadingCredentials`
- `AnthropicModelAdapterTests.SuggestAsync_BlocksWhenCredentialManagerHasNoKey`
- `AnthropicModelAdapterTests.SuggestAsync_CallsProviderAfterApprovalAndCredential`
- `ModelConsentNoticeBuilderTests.BuildAnthropicNotice_ExplainsProviderDataAndCredentialStorage`

Consent notice currently says the user task, helper name/role, tool family, tool summary, trusted context, and wrapped untrusted snippets may be sent. It also points to Anthropic privacy information and the local credential/audit paths.

## External Provider Policy Check

Official Anthropic Privacy Center pages checked on 2026-05-12:

- [How long do you store my organization's data?](https://privacy.anthropic.com/en/articles/7996866-how-long-do-you-store-my-organization-s-data)
- [Can you delete data that I sent via API?](https://privacy.anthropic.com/en/articles/7996875-can-you-delete-data-that-i-sent-via-api)
- [Zero data retention products](https://privacy.anthropic.com/en/articles/8956058-i-have-a-zero-data-retention-agreement-with-anthropic-what-products-does-it-apply-to)

Current public guidance says Anthropic API inputs/outputs are normally deleted within 30 days, API data is not used for training unless an agreement says otherwise, and zero-data-retention requires a separate approved arrangement. This should still be rechecked immediately before any first live-call approval because provider terms can change.

## Issues Found

### P1 - No explicit Shell capability flag yet

The phase plan refers to a `pet_model_adapter` capability flag, but the current Shell path has no explicit runtime flag location. Safety currently comes from the model seam not being wired into Shell, not from a user-visible toggle.

Recommendation: before any live-call approval, add a clearly named capability flag, likely under app settings or a dedicated PET TASKS provider settings surface:

```text
pet_model_adapter = disabled by default
```

The flag should be read by Shell before constructing any real `IModelAdapter`.

### P1 - No first-call consent UI path yet

`ModelConsentNoticeBuilder` contains the consent text, but Shell does not present it to the user or persist a first-call approval decision.

Recommendation: first-call flow should show:

- provider name,
- exact data sent,
- credential target,
- local audit path,
- provider retention/training summary,
- privacy link,
- one-time or per-call approval choice.

### P2 - Default Anthropic model name is stale-prone

`AnthropicModelAdapter` defaults to `claude-3-5-sonnet-latest`. That may be acceptable as a placeholder, but model names drift. Before enabling live calls, choose an explicit current model and document why.

### P2 - Allowlist is Core-only, not provider-settings visible

The deny-wins allowlist is implemented and tested, but not surfaced in the UI. Before a user enables model calls, the UI should show which helpers can ask the model to summarize which tool outputs.

### P3 - Shell and PetCommandBarService duplicate helper family lists

`ShellCoordinator.BuildAllowedToolFamilies` and `PetCommandBarService.BuildAllowedToolFamilies` are close but not identical. This does not create a live-call risk today, but it is a maintenance smell before expanding agent capability.

## Recommended Next Step

Do **not** approve a first live model call yet.

Next implementation should be a narrow, review-gated phase:

```text
C-PHASE 46.5 - Model Capability Flag And Consent UI
|
+-- add explicit disabled-by-default setting
+-- show consent notice from ModelConsentNoticeBuilder
+-- show allowlist matrix in plain language
+-- require per-call approval for first pilot
+-- wire PetModelSummaryService only when enabled
+-- use fake adapter in tests
`-- stop before any real provider call
```

First real call, when separately approved later, should be the smallest low-risk pilot:

```text
Scout -> localDocs -> preview summary only -> no execute -> no mutation
```

Avoid using `codeReview` or any sprite/private project data for the first provider call. Start with a deliberately harmless local-docs prompt and inspect the generated `model-call.json` audit.

## Validation To Run For This Phase

```powershell
dotnet build .\vnext\Wevito.VNext.sln
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
```

No PET TASKS model probe should be run in this phase because there is no Shell live-call approval path and no model capability flag should be enabled.
