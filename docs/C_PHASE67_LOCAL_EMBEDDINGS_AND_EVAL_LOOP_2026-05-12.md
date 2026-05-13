# C-PHASE 67: Local Embeddings And Eval Loop

## Goal

Add a local-first embedding seam and deterministic learning evaluation loop so reviewed Learning Lab promotions can be measured without hosted AI.

## Scope

- Added `OnnxTextEmbeddingService` with an opt-in ONNX model path and deterministic hashing fallback.
- Added an explicit local model folder for `bge-micro-v2` metadata only; no `.onnx` weights are committed.
- Added `tools/install-local-embedder.ps1`, which requires an explicit URL, expected SHA256, and `INSTALL` confirmation before any download.
- Made `PetMemoryStore` embedding dimensions configurable and protected existing memory DBs by renaming dimension-mismatched databases to `.legacy-<timestamp>.db`.
- Added `LearningEvalService` and eval records for recall@1, recall@3, MRR, p50 latency, and p95 latency.
- Added the regression sentinel: if recall@1 or MRR drops by more than 2%, the run records `regression=true` and does not promote the new baseline.

## Implemented

- `vnext/src/Wevito.VNext.Core/OnnxTextEmbeddingService.cs`
- `vnext/src/Wevito.VNext.Core/LearningEvalService.cs`
- `vnext/src/Wevito.VNext.Core/LearningEvalRecord.cs`
- `vnext/content/local-models/embeddings/bge-micro-v2/README.md`
- `vnext/content/local-models/embeddings/bge-micro-v2/.gitkeep`
- `tools/install-local-embedder.ps1`
- `vnext/tests/Wevito.VNext.Tests/OnnxTextEmbeddingServiceTests.cs`
- `vnext/tests/Wevito.VNext.Tests/LearningEvalServiceTests.cs`

## Safety Boundaries

- No hosted AI calls.
- No automatic model download.
- No committed model weights.
- Missing or unloadable ONNX model degrades to the hashing embedder and emits one degrade audit line.
- Eval uses promoted reviewed datasets only and does not make network calls.
- Memory dimension changes rebuild safely by moving the prior DB aside as `.legacy-<timestamp>.db`.

## Validation

- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "Embedding|LearningEval|PetMemory"`
- `dotnet build .\vnext\Wevito.VNext.sln`
- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build`
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests`

## Next Phase

C-PHASE 68 should add the audit ledger and activity reports so eval runs, scheduler proposals, and learning promotions flow into one append-only activity record. This phase intentionally stops for review before continuing because it introduces the ONNX Runtime dependency and embedding-dimension migration behavior.
