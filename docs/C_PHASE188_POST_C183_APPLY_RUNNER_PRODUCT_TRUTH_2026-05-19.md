# C-PHASE 188 — Post-C-PHASE-183 Apply-Runner Product-Truth Retest

## Goal

Pin the post-C-PHASE-183 through C-PHASE-187 apply-runner surface with deterministic product-truth tests, without changing production source, adding runtime behavior, enabling flags, or introducing any new packet kind.

## Scope

- Added `vnext/tests/Wevito.VNext.Tests/PostC183ApplyRunnerProductTruthTests.cs`.
- Did not modify any file under `vnext/src/`.
- Did not modify any file under `vnext/tools/`.
- Did not modify `vnext/Wevito.VNext.sln`.

## Implemented

| Test claim | Assertion |
| --- | --- |
| `ProductTruth_artifact_rename_apply_runner_type_exists_in_apply_namespace` | Confirms `ArtifactRenameApplyRunner` still lives under `Wevito.VNext.Core.SelfImprovement.Apply`. |
| `ProductTruth_seven_apply_v0_packet_kinds_are_in_known_packet_kinds` | Confirms the seven apply-v0 packet kinds remain known to `PlainLanguageExplainer`. |
| `ProductTruth_seven_apply_v0_capability_flags_default_false` | Confirms the seven apply-v0 capability flags default to `False`. |
| `ProductTruth_supervised_loop_apply_runner_not_implemented_reason_unchanged` | Confirms the legacy supervised loop still refuses with `apply_runner_not_implemented_in_v0`. |
| `ProductTruth_legacy_apply_completed_constant_unchanged` | Confirms the legacy `self_improvement_apply_completed` constant is unchanged. |
| `ProductTruth_runner_type_appears_in_held_out_eval_store_visibility_forbidden_types` | Confirms the apply runner remains inside held-out visibility lockdown tests. |
| `ProductTruth_runner_only_writes_under_artifact_root` | Confirms the apply runner source does not reference user/global output roots. |
| `ProductTruth_apply_runner_activity_service_type_exists` | Confirms the read-only activity service exists in the apply namespace. |
| `ProductTruth_apply_runner_activity_service_uses_readonly_sqlite` | Confirms the activity service opens SQLite in read-only mode. |
| `ProductTruth_apply_runner_activity_service_no_insert_update_delete_sql` | Confirms the activity service source does not contain mutating SQL. |
| `ProductTruth_artifact_rename_rollback_runner_exists_with_symmetric_path_constraints` | Confirms rollback runner path symmetry and C-PHASE-187 extension support. |
| `ProductTruth_three_explicit_rollback_packet_kinds_in_known_kinds` | Confirms explicit rollback packet kinds remain known to `PlainLanguageExplainer`. |
| `ProductTruth_two_explicit_rollback_capability_flags_default_false` | Confirms rollback flags default to `False`. |
| `ProductTruth_mutation_scope_guard_throws_invalid_operation_on_out_of_scope_path` | Confirms out-of-scope mutation paths throw `InvalidOperationException`. |
| `ProductTruth_mutation_allow_list_contains_only_expected_eight_types` | Confirms the C-PHASE-186 mutation allow-list remains exactly eight types. |
| `ProductTruth_mutation_scope_audit_tests_pass_with_only_known_mutators` | Confirms the mutation audit tests still discover and allow only known mutators. |
| `ProductTruth_extended_artifact_types_flag_default_false` | Confirms the C-PHASE-187 extended artifact flag defaults to `False`. |
| `ProductTruth_runner_refuses_non_json_extension_when_flag_false` | Confirms `.txt`, `.md`, and `.svg` are refused while the extended flag is off. |
| `ProductTruth_runner_accepts_txt_md_svg_when_flag_true` | Confirms `.txt`, `.md`, and `.svg` can be renamed only after the extended flag is true. |
| `ProductTruth_runner_refuses_each_forbidden_extension_unconditionally` | Confirms the forbidden extension list is refused even when the extended flag is true. |

## Safety Boundaries

- No production source changes.
- No model call.
- No network call.
- No held-out or in-distribution eval content read.
- No new capability flag.
- No new audit packet kind.
- No new apply behavior.
- No UI change.
- No mutation beyond temporary files created by test fixtures under `%TEMP%`.

## Validation

| Command | Result |
| --- | --- |
| `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "PostC183ApplyRunnerProductTruth\|ArtifactRenameApply\|ArtifactRenameRollback\|MutationScope\|PlainLanguage\|SelfImprovement\|PostC174SelfImprovementBaseline"` | Passed: 500/500 |
| `dotnet build .\vnext\Wevito.VNext.sln` | Passed: 0 warnings, 0 errors |
| `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build` | Passed: 1629/1629 |
| `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests` | Passed |
| `git diff --check` | Passed |

## Stop-Gate Checklist

- [x] No production source file changed.
- [x] No baseline claim failed and was masked by relaxing the assertion.
- [x] No new capability flag default change was required.
- [x] Test class does not import `System.Net.Http` or `System.Net.Sockets`.
- [x] Test class does not reference held-out or in-distribution eval stores/cases.
- [x] Pre-flight and validation commands passed.

## Next Phase

No C-PHASE 189 is approved in the current C-PHASE 183+ plan. The next step is reviewer/user decision: either merge this product-truth retest and ask Claude for the next planned batch, or hold here for additional review of the first apply-runner prototype surface.
