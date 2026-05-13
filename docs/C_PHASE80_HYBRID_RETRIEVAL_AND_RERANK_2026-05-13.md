# C-PHASE 80: Hybrid Retrieval And Local Rerank Pipeline

## Goal

Give Wevito a local-first retrieval layer that can ingest approved text documents, retrieve relevant chunks with hybrid keyword + dense scoring, fuse the rankings, and rerank the result set without hosted AI, web access, or hidden file reads.

## Scope

- Added document chunk records with source path, sha256, byte offsets, embedding, and chunk timestamp.
- Extended `PetMemoryStore` with `pet_doc_chunks`, `pet_doc_chunks_fts`, and optional `vec_doc_chunks` schema support.
- Added `LocalDocumentIngestService` for approved-root, report-only document ingestion.
- Added `LocalRetrievalService` with dense search, keyword search, reciprocal-rank fusion, and deterministic rerank.
- Added `ReciprocalRankFusion`, `RetrievalChunk`, and `RetrievalResult` contracts.
- Added retrieval-aware optional context hooks for local research, code review, and asset inventory preview adapters.
- Added `local_retrieval` metadata to `tool_definitions.json`.

## Implemented

- `LocalDocumentIngestService` expands approved files/folders, calls `UnifiedPolicyService.EvaluateRead` for every file, skips denied/unreadable/binary files, writes a JSON manifest, and emits `local_doc_ingest` / `local_access_blocked` audit evidence.
- `PetMemoryStore` stores active document chunks, archives prior chunks for the same path when sha256 changes, and keeps FTS rows joined through active chunk state so stale chunks are not returned.
- `LocalRetrievalService` combines dense results and BM25/FTS keyword results with RRF (`k=60`) and applies `RerankHead` deterministically.
- `RerankHead.Apply` now supports retrieval chunks without changing its tuning/mutation path.
- KillSwitch returns an empty retrieval result and records a blocked audit row.

## Safety Boundaries

- No hosted AI calls.
- No web/network calls.
- No source file mutation.
- No model training or model-weight downloads.
- No runtime/source PNG mutation.
- No reads outside approved roots.
- Retrieval and ingestion are report-only local data operations.
- Rerank config remains read-only at runtime; tuning remains the separate guarded tuning path.

## Validation

- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "Retrieval|Chunk|Rerank|Fusion|DocIngest"`: passed, 8/8.
- `dotnet build .\vnext\Wevito.VNext.sln`: passed.
- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build`: passed, 480/480.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests`: passed.

## Next Phase

C-PHASE 81 should not start automatically. It is the first local reasoning/RAG surface and should begin only after this PR is reviewed and merged.
