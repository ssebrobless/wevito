# C-PHASE 143 - Self-Improvement Packet Taxonomy

Date: 2026-05-18
Branch: `claude-implementation/c-phase-143-self-improvement-packet-taxonomy`
Base: `5a042d2d652a883713ef26abeb062f063b232e93`

## Goal

Register the nine supervised self-improvement audit packet kinds from C-PHASE 142 so future phases can emit evidence in a stable, plain-language shape.

## Scope

Files added:

- `vnext/src/Wevito.VNext.Core/Audit/SelfImprovementPacketKinds.cs`
- `docs/C_PHASE143_SELF_IMPROVEMENT_PACKET_TAXONOMY_2026-05-18.md`

Files extended:

- `vnext/src/Wevito.VNext.Core/PlainLanguageExplainer.cs`
- `vnext/tests/Wevito.VNext.Tests/PlainLanguageExplainerTests.cs`

Files not touched:

- No packet producer services.
- No capability flags.
- No model adapters.
- No network surfaces.
- No mutation runners.
- No sprite, content JSON, or runtime asset files.

## Implemented

Added constants for:

- `self_improvement_proposal_drafted`
- `self_improvement_constitutional_reviewed`
- `self_improvement_dry_run_completed`
- `self_improvement_eval_completed`
- `self_improvement_apply_awaiting_approval`
- `self_improvement_apply_refused`
- `self_improvement_apply_completed`
- `self_improvement_rollback_verified`
- `self_improvement_maturity_clock_reset`

`PlainLanguageExplainer.KnownPacketKinds` now references those constants, and each kind has a user-readable sentence. Tests verify all nine constants are known, explained, and not duplicated as raw packet-kind string literals inside `PlainLanguageExplainer.cs`.

## Safety Boundaries

- No producer was added for any self-improvement packet.
- No file, asset, tool, model, training, or network mutation path was added.
- No capability flag was introduced or changed.
- No hosted AI or local model call was added.
- This phase only prepares taxonomy and plain-language explanations for later review-only/proposal phases.

## Validation

Passed:

- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "PlainLanguage|SelfImprovement"`: 239/239 passed.
- `dotnet build .\vnext\Wevito.VNext.sln`: passed with 0 warnings and 0 errors.
- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build`: 1071/1071 passed.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests`: passed.
- `git diff --check`: passed with only normal CRLF working-copy warnings.

## Stop-Gate Checklist

- [x] All nine packet kinds are present in `KnownPacketKinds`.
- [x] All nine packet kinds have plain-language sentences.
- [x] No test asserts actual production of a self-improvement packet.
- [x] `PlainLanguageExplainer.cs` references the nine kinds through `SelfImprovementPacketKinds` constants, not raw packet-kind literals.
- [x] Pre-flight and final validation commands passed.

## Next Phase

C-PHASE 144 is `ConstitutionalDecisionService v0`. Auto-continue is No, so C-PHASE 144 should not start until the user explicitly approves and provides the C-PHASE 144 prompt.
