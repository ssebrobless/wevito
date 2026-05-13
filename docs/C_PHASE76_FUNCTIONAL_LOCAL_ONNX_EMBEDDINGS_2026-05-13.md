# C-PHASE 76 Functional Local ONNX Embeddings

## Goal

Replace the placeholder ONNX embedding path with a real local inference backend
while preserving deterministic hashing fallback whenever model, tokenizer, or
session setup is unavailable.

## Scope

- Added `OnnxEmbeddingBackend` for local tokenizer loading, ONNX Runtime session
  execution, mean pooling, and L2 normalization.
- Updated `OnnxTextEmbeddingService` to use the backend through a lazy safe-load
  path while keeping cache/copy semantics and single-line degrade auditing.
- Updated the local embedder installer to require explicit model, tokenizer, and
  optional vocab URLs plus SHA-256 verification for every downloaded file.
- Documented bge-micro-v2 local install expectations.
- Marked `pet_memory` with `embeddingProviderWhenOnnxActive=bge-micro-v2`.
- Added tiny test fixture files that do not contain real model weights and do
  not open the network.
- Added backend/service tests for deterministic vectors, distinct-vector
  behavior, missing model/tokenizer fallback, corrupt backend fallback, L2
  normalization, dimensions, cache behavior, and existing memory-dimension
  migration coverage.

## Safety Boundaries

- No model weights were downloaded or committed.
- No test performs network access.
- Missing or corrupt model/tokenizer/session state degrades to
  `HashingTextEmbeddingService`.
- Degrade audit is emitted once per service instance, not once per embed call.
- `Embed` does not throw for missing/corrupt runtime state.
- Hashing fallback remains 16 dimensions; ready ONNX backend reports 384
  dimensions.
- `PetMemoryStore` continues to rebuild fresh databases when embedding
  dimensions change instead of silently re-embedding old rows.

## Validation

- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "Embedding|PetMemory"` passed: 22 / 22.
- `dotnet build .\vnext\Wevito.VNext.sln` passed.
- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build` passed: 411 / 411.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests` passed.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\install-local-embedder.ps1` safely refused to run without explicit URL/hash input.
- `tools\install-local-embedder.ps1` local `file:///` smoke passed with explicit SHA-256 values for `model.onnx`, `tokenizer.json`, and `vocab.txt`.

## Artifacts

- Ignored local evidence packet:
  `vnext/artifacts/pet-tasks/20260513-embedding-warmup/warmup-report.json`
- Ignored local summary:
  `vnext/artifacts/pet-tasks/20260513-embedding-warmup/run-summary.md`

## Residual Notes

The production backend is wired for real ONNX Runtime execution, but real
`bge-micro-v2` model/tokenizer files are still opt-in and must be installed by
the explicit SHA-verified installer. The checked-in `model.onnx` under tests is
a placeholder fixture used only with injected fake backends.

## Next Phase

C-PHASE 77 can improve the visible activity dashboard and live status UX. This
phase is stop-gated because it introduced the first real local ML inference
backend path.
