# C-PHASE 113: Benchmark Case Curation UI

## Goal

Implement the Q17-Option-C benchmark curation path so Wevito can turn candidate evidence into reviewed benchmark cases without silently approving model output.

## Flow

```text
Chat answer ── bookmark ──▶ draft/chat/*.json ── review UI ── approve ──▶ approved/chat/*.json
Codex draft ──────────────▶ draft/<axis>/*.json ─ review UI ── reject ──▶ draft removed
Human hostile prompt ─────▶ adversarial form ───────────────────────────▶ approved/safety/*.json
```

## Implemented

- Added `BenchmarkCaseDraftService` for draft case creation, chat bookmarks, and human-authored adversarial approved cases.
- Added `BenchmarkCaseCurationStore` backed by append-only SQLite triggers at `%LOCALAPPDATA%/Wevito/audit/benchmark-curation.sqlite`.
- Added `BenchmarkCurationViewModel` plus `BenchmarkCurationPanel` in the Benchmarks surface.
- Wired assistant-message bookmark buttons in `ChatPanel` to open an inline expected-answer editor and create a draft case.
- Added packet-kind coverage for:
  - `benchmark_case_drafted`
  - `benchmark_case_bookmarked_from_chat`
  - `benchmark_case_approved`
  - `benchmark_case_rejected`
  - `benchmark_case_revised`

## Safety Boundaries

- Bookmark-from-chat creates draft cases only, never approved cases.
- Approved cases are never overwritten by draft creation.
- Approval moves a draft into `approved/<axis>/` and marks the approved file read-only.
- Rejection deletes only draft files and writes no approved case.
- Human adversarial cases require explicit hostile prompt and expected behavior text before they can be written.
- No hosted AI calls, no local model calls, no network access, and no sprite/runtime asset mutation were added.

## Validation

Run before merge:

```powershell
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "BenchmarkCase|BenchmarkCuration|ChatPanelBookmark|PlainLanguage"
dotnet build .\vnext\Wevito.VNext.sln
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
```

## Next Phase

C-PHASE 114 can harden the Codex loop now that benchmark cases can be curated by the user instead of silently seeded as authoritative.
