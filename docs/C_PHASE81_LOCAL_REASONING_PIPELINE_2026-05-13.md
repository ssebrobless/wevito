# C-PHASE 81: Local Reasoning Pipeline

## Goal

Let Wevito synthesize local retrieved evidence into cited, reviewable text while staying local-first and refusing hosted AI providers.

## Scope

- Added `LocalReasoningService` to build role-aware prompts from retrieved chunks and trusted/untrusted context.
- Added `CitationEnforcer` to keep valid `[N]` cited sentences and replace unsupported sentences with `(needs citation)`.
- Added `LocalReasoningEvidencePacket` with prompt/response hashes, retrieved chunk ids, model id, citation coverage, and safety flags.
- Added default role-keyed prompt templates for Scout, Inspector, and Builder.
- Added optional reasoning hooks to local research and code-review preview adapters.
- Extended `ResearchPlannerService` so local reasoning can replace template synthesis when supplied.

## Implemented

- `LocalReasoningService` accepts a question, `RetrievalResult`, helper role, tool family, trusted context, untrusted context, and artifact root.
- The prompt embeds chunks as numbered local evidence blocks like `[1] <text>`.
- Untrusted context is wrapped with `PetModelSummaryService.WrapUntrusted`.
- The service calls only the injected local/deterministic adapter path and refuses hosted provider ids even if injected by mistake.
- Empty retrieval returns a deterministic "no local evidence" synthesis without calling the model.
- KillSwitch blocks the reasoning path and writes a blocked packet.
- `CitationEnforcer` computes `citation_coverage_ratio` and never throws.

## Safety Boundaries

- No hosted AI calls.
- No web/network calls.
- No source file mutation.
- No sprite/runtime asset mutation.
- No training or model-weight downloads.
- No uncited sentence is treated as supported; unsupported sentences are replaced with `(needs citation)`.
- LocalOnly/hosted refusal remains enforced at the service boundary.

## Validation

- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "LocalReasoning|Citation"`: passed, 7/7.
- `dotnet build .\vnext\Wevito.VNext.sln`: passed.
- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build`: passed, 487/487.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests`: passed.

## Next Phase

C-PHASE 82 should not start automatically. It is the golden eval / regression-gate phase and should begin only after this PR is reviewed and merged.
