# C-PHASE 175 - Post-174 self-improvement verification baseline

## Goal

Pin the C-PHASE 159-174 self-improvement safety surface at the current merged HEAD with deterministic test assertions. This phase is test-only and does not modify production source, tools, model paths, network paths, mutation paths, or apply behavior.

## Scope

Added:

- `vnext/tests/Wevito.VNext.Tests/PostC174SelfImprovementBaselineTests.cs`

Not touched:

- `vnext/src/**`
- `vnext/tools/**`
- `vnext/Wevito.VNext.sln`
- production capability defaults
- model, network, training, mutation, and apply surfaces

## Claims and Assertions

| Test | Expected outcome |
| --- | --- |
| `Baseline_eval_gate_manifest_lists_eleven_documented_gates` | `EvalGateManifest.Default().Gates` exactly matches the documented 11-gate order. |
| `Baseline_eval_gate_runner_enabled_setting_is_eval_gate_runner_v1_enabled` | `EvalGateRunner.EnabledSetting` remains `eval_gate_runner_v1_enabled`. |
| `Baseline_heuristic_judge_enabled_setting_is_heuristic_judge_enabled` | `HeuristicJudgeService.EnabledSetting` remains `heuristic_judge_enabled`. |
| `Baseline_heuristic_judge_registers_exactly_six_rules` | A hand-built awaiting-approval ledger fixture produces exactly 6 heuristic judge findings. |
| `Baseline_apply_runner_prereq_check_returns_exactly_ten_entries` | `ApplyRunnerPrerequisiteCheckService.Check(...)` returns exactly 10 entries when relevant flags are on. |
| `Baseline_apply_prerequisite_packet_kind_is_self_improvement_apply_prerequisite_check` | `SelfImprovementPacketKinds.ApplyPrerequisiteCheck` equals `self_improvement_apply_prerequisite_check` and is registered in `PlainLanguageExplainer.KnownPacketKinds`. |
| `Baseline_supervised_loop_apply_runner_not_implemented_reason_is_v0` | `SupervisedImprovementLoop.ApplyRunnerNotImplementedReason` remains `apply_runner_not_implemented_in_v0`. |
| `Baseline_local_scoring_request_only_exposes_prompt_sha_and_rubric` | `LocalScoringRequest` exposes `PromptSha256` and `Rubric`, without raw prompt, answer, rationale, or text fields. |
| `Baseline_not_configured_scoring_provider_default_is_refused` | `NotConfiguredScoringProvider.Score(...)` refuses with `local_scoring_provider_not_configured`; enabled setting remains `local_scoring_provider_enabled`. |
| `Baseline_ollama_loopback_scoring_provider_constants_are_pinned` | Ollama scoring constants remain `local_scoring_provider_ollama_enabled`, `127.0.0.1:11434`, and `qwen2.5:7b-instruct-q4_k_m`. |
| `Baseline_capability_inventory_contains_every_flag_named_above` | Capability inventory contains every baseline flag, and every `_enabled` flag defaults to `False`. |
| `Baseline_tool_projects_are_not_referenced_from_shell_or_tests` | The five standalone tool projects exist and are not referenced from Shell or Tests project files. |

## Safety Boundaries

- No production code changed.
- No file under `vnext/src/` changed.
- No file under `vnext/tools/` changed.
- No hosted AI call was added.
- No network call was added.
- No held-out eval content is read by product runtime.
- No new capability flag was added.
- No audit packet kind was added.
- No apply runner or mutation behavior was added.

## Validation

| Command | Result |
| --- | --- |
| `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "PostC174SelfImprovementBaseline|SelfImprovementProductTruth|PlainLanguage|SelfImprovement"` | Passed: 277/277 |
| `dotnet build .\vnext\Wevito.VNext.sln` | Passed |
| `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build` | Passed: 1343/1343 |
| `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests` | Passed |
| `git diff --check` | Passed |

## Stop-Gate Checklist

- [x] No production source file changed.
- [x] No baseline claim was masked by relaxing an assertion.
- [x] No new capability flag default change was required.
- [x] The new test class does not import `System.Net.Http`.
- [x] The new test class does not import `System.Net.Sockets`.
- [x] No model, network, training, mutation, held-out content read, or apply behavior was added.

## Next Phase

C-PHASE 176 - Loopback Ollama readiness probe - remains Auto-continue=No.
