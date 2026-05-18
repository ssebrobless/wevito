# C-PHASE 144 - ConstitutionalDecisionService v0

Date: 2026-05-18
Branch: `claude-implementation/c-phase-144-constitutional-decision-service-v0`
Base: `d06e5c39dac6f4fdf910a93f643ee9f466738238`

## Goal

Add the default-deny constitutional decision layer that future supervised self-improvement proposals must pass before becoming review packets.

## Scope

Files added:

- `vnext/src/Wevito.VNext.Core/SelfImprovement/ConstitutionalDecisionService.cs`
- `vnext/src/Wevito.VNext.Core/SelfImprovement/ConstitutionalDecisionInput.cs`
- `vnext/src/Wevito.VNext.Core/SelfImprovement/ConstitutionalDecisionOutcome.cs`
- `vnext/src/Wevito.VNext.Core/SelfImprovement/ConstitutionalRule.cs`
- `vnext/src/Wevito.VNext.Core/SelfImprovement/DefaultDenyRule.cs`
- `vnext/src/Wevito.VNext.Core/SelfImprovement/KillSwitchActiveRule.cs`
- `vnext/src/Wevito.VNext.Core/SelfImprovement/ScopeMustBeEnabledRule.cs`
- `vnext/src/Wevito.VNext.Core/SelfImprovement/NoNetworkInScopeRule.cs`
- `vnext/src/Wevito.VNext.Core/SelfImprovement/NoHostedAiRule.cs`
- `vnext/src/Wevito.VNext.Core/ShellCompositionRoot.cs`
- `vnext/tests/Wevito.VNext.Tests/ConstitutionalDecisionServiceTests.cs`
- `docs/C_PHASE144_CONSTITUTIONAL_DECISION_SERVICE_V0_2026-05-18.md`

Not touched:

- No proposal producer.
- No experiment registry.
- No audit packet writer.
- No apply path.
- No capability flag.
- No model adapter.
- No network, filesystem, sprite, or content surface.

## Implemented

`ConstitutionalDecisionService` evaluates a `ConstitutionalDecisionInput` through first-match-wins rules:

1. `KillSwitchActiveRule` -> `Blocked("kill_switch_active")`
2. `ScopeMustBeEnabledRule` -> `Blocked("scope_not_enabled")`
3. `NoHostedAiRule` -> `Blocked("hosted_ai_forbidden")`
4. `NoNetworkInScopeRule` -> `Blocked("network_not_allowed_for_scope")`
5. `DefaultDenyRule` -> `Blocked("no_experiment_kind_registered")` when the v0 registry is empty, otherwise `Blocked("default_deny_no_explicit_allow")`

`ShellCompositionRoot.CreateConstitutionalDecisionService` provides the default construction path. The repo did not already contain a `ShellCompositionRoot.cs`, so this phase added the smallest no-side-effect factory needed for later phases.

## Safety Boundaries

- The service is pure over inputs except reading `KillSwitchService.IsActive()`.
- No rule writes state.
- No rule calls a model adapter.
- No rule touches network or files.
- No allowlist entry exists by default.
- No service overload skips rules.
- Default outcome is blocked.
- The first audit packet writer remains deferred to C-PHASE 145.

## Validation

Passed:

- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "ConstitutionalDecision|PlainLanguage|SelfImprovement"`: 248/248 passed.
- `dotnet build .\vnext\Wevito.VNext.sln`: passed with 0 warnings and 0 errors.
- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build`: 1080/1080 passed.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests`: passed.
- `git diff --check`: passed.

## Stop-Gate Checklist

- [x] No rule has an allowlist entry.
- [x] No rule writes state.
- [x] No rule reaches a model adapter.
- [x] Default outcome is blocked.
- [x] Service exposes no overload that skips rules.
- [x] Validation commands passed.

## Next Phase

C-PHASE 145 is `Experiment registry v0 (empty initial registry)`. Auto-continue is No, so C-PHASE 145 should not start until the user explicitly approves and provides the C-PHASE 145 prompt.
