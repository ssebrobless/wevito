# C-PHASE 159 - Held-Out Eval Case Schema

Date: 2026-05-19
Branch: `claude-implementation/c-phase-159-held-out-eval-case-schema`
Base: `c3089666ee08ddde01ed71cc1002e58e26591377`

## Goal

Define the held-out eval case schema, add a separate test-only seeding CLI, and lock every C-PHASE 151-158 self-improvement surface away from `IHeldOutEvalStore` / `HeldOutEvalStore` dependencies.

## Scope

Files added:

- `vnext/src/Wevito.VNext.Core/SelfImprovement/Eval/HeldOutEvalCase.cs`
- `vnext/src/Wevito.VNext.Core/SelfImprovement/Eval/HeldOutEvalCaseValidator.cs`
- `vnext/tools/Wevito.Tools.HeldOutSeed/Wevito.Tools.HeldOutSeed.csproj`
- `vnext/tools/Wevito.Tools.HeldOutSeed/Program.cs`
- `vnext/tests/Wevito.VNext.Tests/HeldOutEvalCaseSchemaTests.cs`
- `vnext/tests/Wevito.VNext.Tests/HeldOutEvalCaseSeedCliTests.cs`
- `docs/C_PHASE159_HELD_OUT_EVAL_CASE_SCHEMA_2026-05-19.md`

Files extended:

- `vnext/tests/Wevito.VNext.Tests/HeldOutEvalStoreVisibilityTests.cs`
- `vnext/Wevito.VNext.sln`
- `docs/codex-phase-history.jsonl`

Not touched:

- `vnext/src/Wevito.VNext.Core/SelfImprovement/Eval/HeldOutEvalStore.cs`
- `vnext/src/Wevito.VNext.Core/SelfImprovement/Eval/IHeldOutEvalStore.cs`
- No held-out case JSON files under `vnext/eval/held-out/`.
- No Shell/Core project references to `Wevito.Tools.HeldOutSeed`.
- No sprite assets, content JSON, model adapter, network surface, capability-flag default, or apply runner.

## Implemented

- Added `HeldOutEvalCase` with id, domain, prompt SHA-256, expected-kind SHA-256, authored timestamp, and optional notes.
- Added `HeldOutEvalCaseValidator` with checks for id/domain regexes, lowercase 64-character hashes, distinct prompt/expected hashes, valid authored timestamp, and notes length.
- Added `Wevito.Tools.HeldOutSeed` as a separate console project in `vnext/tools/`.
- The seed CLI supports `add --domain <X> --prompt-file <P> --expected-file <E> --root <R> [--id <ID>] [--notes <N>]`.
- The seed CLI computes hashes from the prompt and expected files, derives an id when omitted, validates the case, refuses duplicates, canonicalizes the output path under `--root`, and writes indented JSON.
- Extended visibility tests so these surfaces cannot grow `IHeldOutEvalStore` / `HeldOutEvalStore` constructor, method parameter, or return-type dependencies:
  - `OperationTimelineService`
  - `RefusedApprovalAggregateService`
  - `ApprovalCardDetailService`
  - `MaturityScoreboardService`
  - `InvariantViolationWatchdog`
  - `SupervisedImprovementLoop`
  - `CapabilityFlagAuditService`
  - `ReplayHarness`
  - `SpriteRepairBatchProposalScope`
- Added schema round-trip and invalid-shape tests.
- Added CLI tests for valid write, duplicate refusal, escape/id refusal, no network type dependency, and no runtime held-out store construction dependency.

## Safety Boundaries

- The seed CLI is a separate tools project and is not referenced by Shell/Core.
- The seed CLI makes no HTTP/network calls and does not reference `IHeldOutEvalStore` / `HeldOutEvalStore`.
- The held-out runtime store API is unchanged.
- No held-out case content was committed.
- The held-out store remains invisible to proposal loops, timelines, scoreboards, refusal aggregates, approval details, the invariant watchdog, replay harness, and sprite proposal scope.
- No audit packets, capability flags, model calls, apply pathways, or mutation pathways were added.

## Validation

Pre-flight passed before implementation:

- `git status`: clean after stashing planning docs.
- `dotnet build .\vnext\Wevito.VNext.sln`: passed.
- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build`: 1177/1177 passed.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests`: passed.
- `git diff --check`: passed.

Implementation validation:

- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "HeldOutEval|PlainLanguage|SelfImprovement"`: 262/262 passed.
- `dotnet build .\vnext\Wevito.VNext.sln`: passed.
- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build`: 1193/1193 passed.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests`: passed.
- `git diff --check`: passed with Git's existing LF-to-CRLF working-copy warning only.

## Stop-Gate Checklist

- [x] No forbidden surface takes `IHeldOutEvalStore` / `HeldOutEvalStore` in a signature.
- [x] `Wevito.Tools.HeldOutSeed` is not referenced from `Wevito.VNext.Shell` or `Wevito.VNext.Core`.
- [x] `HeldOutEvalStore.cs` was not modified.
- [x] `IHeldOutEvalStore.cs` was not modified.
- [x] No held-out case JSON file was committed under `vnext/eval/held-out/`.
- [x] Pre-flight commands passed.

## Next Phase

C-PHASE 160 is `In-distribution eval case store`. Auto-continue is No, so C-PHASE 160 should not start until the user explicitly approves and provides its prompt from `docs/CLAUDE_C_PHASE159_PLUS_CODEX_MEDIUM_PROMPTS_2026-05-19.md`.
