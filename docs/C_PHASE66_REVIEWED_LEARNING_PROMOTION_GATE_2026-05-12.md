# C-PHASE 66 Reviewed Learning Promotion Gate

Date: 2026-05-12
Branch: `claude-implementation/c-phase-66-reviewed-learning-promotion-gate`

## Goal

Let reviewed Creative Learning Lab bundles become local memory and versioned learning datasets only after explicit approval. This phase does not train a model and does not call hosted AI.

```text
C-PHASE 66 shape
|
+-- reviewed bundle exists
+-- approved learningPromotion task card required
+-- accept labels only
+-- write examples.jsonl
+-- write manifest with sha256
+-- write local memory rows with dataset provenance
`-- no training, no binary asset copy
```

## Implemented

- Added `LearningLabPromotionService`.
- Added `LearningDatasetManifest`.
- Added `LearningPromotionExampleRow`.
- Extended `PetMemoryStore` examples with:
  - `dataset_version`
  - `source_task_card_id`
- Added schema-safe column hydration for existing pet memory databases.
- Added `learning_promotion` metadata to `vnext/content/tool_definitions.json`.
- Added a Creative Learning Lab "Propose Promotion" button that explains the approval gate without mutating memory or datasets.
- Added tests for:
  - approval-required promotion
  - accepted-only filtering
  - manifest hash correctness
  - no binary asset copy
  - training/memory autopromotion constants remaining false

## Safety Boundaries

This phase does not:

- train or tune a model
- call hosted AI
- fetch the web
- promote rejected/revise/defer/blocked/unlabeled rows
- copy private binary assets
- allow memory writes without an approved `learningPromotion` task card

Promotion writes are explicit local mutations and are only allowed through `LearningLabPromotionService.Promote` after approval.

## Validation

```powershell
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "LearningLab|PetMemory|Promotion"
```

Result: PASS, 27 / 27.

Full phase validation:

```powershell
dotnet build .\vnext\Wevito.VNext.sln
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
```

Results:

- `dotnet build`: PASS, 0 warnings/errors.
- `dotnet test --no-build`: PASS, 331 / 331.
- `build-vnext -SkipAssetPrep -SkipTests`: PASS.

## Next Phase

C-PHASE 67 should add local embeddings and a deterministic learning/eval loop. It is a stop phase because it introduces an ONNX dependency and embedding-dimension migration behavior.

```text
next gate
|
+-- local embedding provider
+-- deterministic fallback
+-- eval metrics
+-- regression sentinel
`-- stop for user approval
```
