# C-PHASE 173 — Self-improvement product-truth retest

## Goal

Add a no-production-code product-truth test layer for the current supervised self-improvement stack after C-PHASE 172.

## Scope

Added:

- `vnext/tests/Wevito.VNext.Tests/SelfImprovementProductTruthTests.cs`

Not touched:

- `vnext/src/**`
- `vnext/tools/**`
- historical phase reports
- capability defaults
- model, network, apply, mutation, and training surfaces

## Claims And Assertions

| Claim | Result | Evidence |
| --- | --- | --- |
| Held-out store lists no cases under an empty root. | Pass | `Held_out_store_lists_no_cases_under_empty_root` |
| In-distribution store lists no cases under an empty root. | Pass | `In_distribution_store_lists_no_cases_under_empty_root` |
| Supervised loop still refuses apply with `apply_runner_not_implemented_in_v0`. | Pass | `Supervised_loop_refuses_with_apply_runner_not_implemented_in_v0` |
| Every `_enabled` capability flag defaults to `False`. | Pass after blocker repair | `Every_enabled_capability_flag_defaults_to_false` |
| Eval gate runner returns NotApplicable when unset. | Pass | `Eval_gate_runner_returns_not_applicable_when_flag_unset` |
| Heuristic judge returns empty when unset. | Pass | `Heuristic_judge_returns_empty_when_flag_unset` |
| Eval coverage proposal scope writes nothing when unset. | Pass | `Eval_coverage_proposal_scope_writes_nothing_when_flag_unset` |
| Snapshot export signs by hashing the payload with an empty signature field, then inserts the hash. | Pass | `Snapshot_export_signature_uses_empty_signature_field_then_inserts_hash` |
| Replay harness does not write to the production audit ledger. | Pass | `Replay_harness_does_not_write_to_production_audit_ledger` |
| Proposal diff explainer blocks artifacts outside `vnext/artifacts/`. | Pass | `Proposal_diff_explainer_blocks_outside_vnext_artifacts` |
| Plain-language known packet kinds include every self-improvement packet kind. | Pass | `Plain_language_known_packet_kinds_contains_every_self_improvement_packet_kind` |
| Default local scoring provider is `NotConfiguredScoringProvider`. | Pass | `Local_scoring_provider_default_is_NotConfiguredScoringProvider` |
| Ollama loopback scoring provider refuses when flags are off. | Pass | `Ollama_loopback_scoring_provider_refuses_when_flags_off` |
| Capabilities and gates snapshot lists every capability inventory entry. | Pass | `Capabilities_and_gates_snapshot_lists_every_capability_flag_inventory_entry` |

## Drift Found And Repaired

The phase uncovered one product-truth drift against the prompt claim that every `_enabled` capability flag defaults to `False`.

These two capability inventory entries currently default to the empty string:

- `local_access_file_read_enabled`
- `local_tool_exec_enabled`

Both remained effectively off/unset, but the C-PHASE 173 assertion required the literal `False` default. After opening the initial Draft PR and documenting the drift, user approval to proceed resolved the blocker by changing only those two inventory defaults to `bool.FalseString`.

## Safety Boundaries

- No hosted AI call was added or made by product runtime code.
- No model call was added.
- No network surface was added.
- No training surface was added.
- No mutation or apply runner was added.
- The only capability default changes were `local_access_file_read_enabled` and `local_tool_exec_enabled`, normalized from empty string to `False`.
- The only production source change was `CapabilityFlagInventory.cs`.

## Validation

| Command | Result |
| --- | --- |
| `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "SelfImprovementProductTruth"` | Passed after blocker repair. |
| `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "SelfImprovementProductTruth|PlainLanguage|SelfImprovement"` | Passed after blocker repair. |
| `dotnet build .\vnext\Wevito.VNext.sln` | Passed, 0 warnings, 0 errors. |
| `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build` | Passed after blocker repair. |
| `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests` | Passed. |
| `git diff --check` | Passed. |

## Stop-Gate Checklist

- [x] A product-truth claim failed and was not masked.
- [x] User approved proceeding with the minimal blocker repair.
- [x] Production change was limited to the two capability inventory defaults.
- [x] No hosted AI call was observed or added.
- [x] No silent network, training, mutation, or apply behavior was added.
- [x] No capability flag was enabled.
- [x] PR can be marked ready after repaired validation passes.

## Next Phase

C-PHASE 174 — Apply-runner prerequisite checklist service — remains Auto-continue=No.

C-PHASE 174 can start after C-PHASE 173 is merged.
