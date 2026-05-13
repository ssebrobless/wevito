# C-PHASE 73 Local Training / Tuning Pipeline

## Goal

Create the first local-first tuning scaffold so Wevito can improve retrieval,
routing, and prompt configuration from reviewed local datasets without hidden
training, hosted AI, web access, or uncontrolled mutation.

## Scope

- Added `LocalTrainingPlanService` for reportable `train_plan` manifests.
- Added `LocalTuningRunner` for guarded dry-run/apply/rollback tuning writes.
- Added `RerankHead`, `RouterConfig`, and `PromptConfigStore`.
- Added LoRA handoff scripts under `tools/` that are plan-only in this phase.
- Extended learning eval with `EvaluateAgainst(...)`.
- Extended `PetMemoryStore` with in-process rerank-head application.
- Added audit packet kind constants for `train_plan`, `tuning_apply`, and
  `tuning_rollback`.

## Safety Boundaries

- No hosted AI calls.
- No web access.
- No model downloads.
- No Python or external training launch.
- LoRA remains blocked by default through `tuning_lora_enabled=false`.
- Dry-run tuning writes only under `vnext/artifacts/`.
- Apply tuning is constrained to `vnext/content/` and backs up existing bytes.
- Eval regression beyond tolerance triggers rollback.
- Rollback verifies byte-exact sha256 restoration for existing files.
- KillSwitch blocks planning and tuning execution.

## Implemented Stages

- `73A RetrievalRerank`: deterministic rerank-head records and memory result
  reordering.
- `73B Routing`: router config records with helper-role hints.
- `73C PromptConfig`: prompt template, top-k, and threshold config store.
- `73D LoraPilot`: plan-only handoff scripts; no runtime execution path.

## Validation

- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "Training|Tuning|Rerank"` passed: 12 / 12.
- `dotnet build .\vnext\Wevito.VNext.sln` passed.
- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build` passed: 385 / 385.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests` passed.

## Next Phase

C-PHASE 74 should generalize guarded code/asset mutation using the same posture:
exact scope, dry-run, backup hash, rollback, post-proof, and no hidden execution.
C-PHASE 73 should not be auto-continued because it opens the local-learning
surface and needs user review before mutation capabilities expand.
