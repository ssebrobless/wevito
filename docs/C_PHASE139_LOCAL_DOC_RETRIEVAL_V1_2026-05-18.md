# C-PHASE 139 - Local Document Retrieval v1

## Goal

Add a default-off, local-only document retrieval surface using SQLite FTS5 and BM25 ranking. This is the first deterministic research/retrieval layer that does not depend on hosted AI, local model invocation, web access, embeddings, clipboard hooks, or hidden file/tool execution.

## Scope

- Added `LocalDocumentRetrievalService` and `LocalDocumentIndex` under `vnext/src/Wevito.VNext.Core/LocalRetrieval/`.
- Added `SettingKeys` for:
  - `local_document_retrieval_enabled=false`
  - `local_document_retrieval_root=%USERPROFILE%\Documents\WevitoLocalNotes`
  - `local_document_retrieval_max_file_bytes=4194304`
- Added a hidden-by-default `Local Docs` tab in `ToolPopupWindow`.
- Added a Settings checkbox that can reveal the `Local Docs` tab by flipping only the local retrieval capability.
- Added packet kinds to `PlainLanguageExplainer`:
  - `local_document_index_built`
  - `local_document_index_refused`
  - `local_document_query_completed`
- Added focused tests under `vnext/tests/Wevito.VNext.Tests/LocalRetrieval/`.

## Implemented Behavior

- Index path is isolated at `%LOCALAPPDATA%\WevitoVNext\local-doc-index\index.db`.
- Index database is separate from `wevito-vnext.db` and the audit ledger.
- Indexing is capability-gated and refuses while the flag is off.
- Build and query both consult `KillSwitchService` before doing work.
- Indexing reads only files under the configured root after `Path.GetFullPath` normalization.
- Supported file extensions are `.md`, `.txt`, and `.json`.
- Files over the configured size cap are skipped with `local_document_index_refused`.
- Unsupported extensions and outside-root paths are refused with evidence packets.
- Incremental indexing skips unchanged files by comparing SHA256 with the `files` table.
- Query uses the canonical parameterized SQL:
  `SELECT path, line_no, content, bm25(docs) AS score FROM docs WHERE docs MATCH @q ORDER BY bm25(docs) ASC LIMIT @n;`
- User query text is wrapped as a bound `@q` parameter, capped to `topN <= 50`, and never interpolated into SQL.
- Query rejects empty text and unbalanced double quotes.

## Safety Boundaries

- No hosted AI calls.
- No local model calls.
- No network access.
- No silent document indexing; the service is default-off and the UI tab is hidden until enabled.
- No writes outside the dedicated index database path.
- No mutation of user source documents.
- No sprite, content, audit-ledger, or app database mutation.

## Validation

- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "LocalDocumentRetrieval|LocalDocumentIndex|PlainLanguage|Settings"` passed: `232 / 232`.
- `dotnet build .\vnext\Wevito.VNext.sln` passed: `0` warnings, `0` errors.
- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build` passed: `1037 / 1037`.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests` passed.
- `git diff --check` passed.

## Stop-Gate Checklist

- [x] Index does not write outside `%LOCALAPPDATA%\WevitoVNext\local-doc-index\`.
- [x] Query path does not invoke any model adapter or network client.
- [x] Query path does not interpolate raw user input into SQL.
- [x] Capability flag remains default-off.
- [x] Index file is not co-located with the audit ledger or `wevito-vnext.db`.

## Next Phase

C-PHASE 140 can simplify the Tool Hub and incorporate Local Docs into a cleaner catalog/tab layout. This phase does not auto-continue.
