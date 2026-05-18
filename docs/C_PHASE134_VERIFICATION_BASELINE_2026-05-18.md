# C-PHASE 134 - Verification Baseline And Bug Board Live Retest

Date: 2026-05-18
Branch: `claude-implementation/c-phase-134-verification-baseline`
Base commit: `c6365a4fe06c7bc91314f3d23295d343c133b06e`
Scope: no code changes, no content JSON changes, no tests added.

## Verified True

- The synced repo tip is `c6365a4fe`, descended from C-PHASE 133 implementation commit `9b478282c`.
- Pre-flight validation passed before the live walkthrough: `dotnet build`, `dotnet test --no-build` at 993/993, `tools/build-vnext.ps1 -SkipAssetPrep -SkipTests`, and `git diff --check`.
- `tools/reset-wevito-vnext-profile.ps1` exists and uses the live save path `%LOCALAPPDATA%\WevitoVNext\wevito-vnext.db`.
- A dry-run reset receipt and a backup-first actual reset receipt were captured.
- The fresh shell launched from `vnext/src/Wevito.VNext.Shell/bin/Debug/net8.0-windows10.0.19041.0/Wevito.VNext.Shell.exe`.
- The first-launch wizard rendered, and selecting `Help with sprite cleanup` persisted a Draft PET TASKS card with `ToolFamily=spriteAudit`.
- The live WPF starter egg render showed exactly six v1 eggs: red, orange, yellow, blue, indigo, violet. Green was omitted.
- `vnext/content/starter_eggs.json` and Godot `scripts/game_manager.gd` fallback starter eggs agree on the same six v1 starter colors and deterministic species mapping.
- Spawning a baby female red fox via DevControl produced slot 0 with `speciesId=fox`, `lifeStage=baby`, `gender=female`, and `colorVariant=red`.
- DevControl action retest succeeded for `feed`, `water`, `rest`, `play`, `groom`, `bath`, `medicine`, `doctor`, and `home`.
- The previous action-feedback gap is fixed in the live DevControl path: `water` transitioned to `Drink`, `groom` transitioned to `Groom`, and `doctor` transitioned to `Doctor`.
- Local brain UI reported `BRAIN offline`, matching the Ollama loopback probe result.
- Product runtime audit rows captured during the walkthrough had no `did_use_hosted_ai=1` and no `did_use_network=1`.
- Sampled sprite PNG and sampled archived audit JSONL bytes were unchanged after autonomous scope toggle exercises.
- Both autonomous scope settings were persisted back to `False` at the end of the walkthrough.

## Drifted From Plan

- The prompt referred to `%LOCALAPPDATA%\WevitoVNext\audit\*.jsonl`, but the current live audit ledger is the append-only SQLite file at `%LOCALAPPDATA%\Wevito\audit\ledger.sqlite`, with adjacent JSONL artifacts for some non-ledger receipts. Evidence was captured from the SQLite ledger.
- The prompt expected one `autonomous_scope_tick` after enabling each scope. In the live build, the individual scope checkboxes can toggle, but the global autonomous beta gate remains off and the `Try the autonomous beta` affordance is disabled at `checks=7/11`. Result: `autonomous_scope_enabled_changed` rows were written, but no `autonomous_scope_tick`, `sprite_repair_triage_card_drafted`, or `audit_ledger_cleanup_summary` row fired during this phase.
- DevControl options still list `green` as a generic runtime color variant, but starter egg UI/content/Godot fallback correctly omit green for v1. This is not a BUG-003 regression because starter selection is the tested contract.

## Bug Status Table

| Bug | Status | Evidence |
|---|---|---|
| BUG-001 | Closed | `vnext/artifacts/c-phase-134-verification/reset-profile-dry-run.txt`; `vnext/artifacts/c-phase-134-verification/reset-profile-actual.txt` |
| BUG-002 | Closed | `vnext/artifacts/c-phase-134-verification/sqlite-state-after-help-choice.json`; `vnext/artifacts/c-phase-134-verification/devcontrol-snapshot-after-help-choice.json` |
| BUG-003 | Closed | `vnext/artifacts/c-phase-134-verification/egg-catalog-parity.json`; `vnext/artifacts/c-phase-134-verification/uia-tree-after-fresh-launch.json` |
| BUG-004 | Closed | `vnext/artifacts/c-phase-134-verification/devcontrol-red-fox-actions.json` |

## Autonomous Scope Exercise Summary

| Scope | Default State | Exercise Result | Evidence |
|---|---:|---|---|
| `sprite-repair-triage` | Off | Individual checkbox toggled on then off. Global autonomous beta remained off, so no tick fired. | `vnext/artifacts/c-phase-134-verification/scope-toggle-uia-events.json`; `vnext/artifacts/c-phase-134-verification/audit-scope-toggle-rows.json` |
| `audit-ledger-cleanup` | Off | Individual checkbox toggled on then off. Global autonomous beta remained off, so no tick fired. | `vnext/artifacts/c-phase-134-verification/scope-toggle-uia-events.json`; `vnext/artifacts/c-phase-134-verification/audit-scope-toggle-rows.json` |

Hash guard:

| Sample | Path | Before SHA256 | After SHA256 | Equal |
|---|---|---|---|---:|
| Sprite PNG | `sprites_runtime/crow/adult/female/blue/bathe_00.png` | `5392A0CDBC7E93E2B443458CB83046BF88F339EF4E9C777F2B92B585947C2D1F` | `5392A0CDBC7E93E2B443458CB83046BF88F339EF4E9C777F2B92B585947C2D1F` | True |
| Audit JSONL | `%LOCALAPPDATA%/Wevito/audit/coexistence-events.jsonl` | `BF74B860AF7BF7864913940ED50ECD17FF5274499FE07E1F82D81F78A49CE106` | `BF74B860AF7BF7864913940ED50ECD17FF5274499FE07E1F82D81F78A49CE106` | True |

Final autonomous settings:

```json
{
  "runtime_autonomous_beta_enabled": "False",
  "autonomous_scope_sprite-repair-triage_enabled": "False",
  "autonomous_scope_audit-ledger-cleanup_enabled": "False"
}
```

## Local Brain Status

- Ollama probe: not reachable at `http://127.0.0.1:11434/api/tags`.
- Probe error: `WebException: Unable to connect to the remote server`.
- UIAutomation badge text: `BRAIN offline`.
- Heartbeat row: `local_brain_heartbeat`, status `Offline`, `did_use_network=0`, `did_use_hosted_ai=0`, `did_use_local_model=0`, `did_mutate=0`.
- Evidence: `vnext/artifacts/c-phase-134-verification/ollama-probe.json`, `vnext/artifacts/c-phase-134-verification/local-brain-badge-uia.json`, `vnext/artifacts/c-phase-134-verification/local-brain-heartbeat.json`.

## Evidence Artifacts

- `vnext/artifacts/c-phase-134-verification/01-home-after-fresh-launch.png`
- `vnext/artifacts/c-phase-134-verification/07-after-help-with-sprite-cleanup-choice.png`
- `vnext/artifacts/c-phase-134-verification/08-after-red-fox-actions.png`
- `vnext/artifacts/c-phase-134-verification/sqlite-state-after-help-choice.json`
- `vnext/artifacts/c-phase-134-verification/devcontrol-snapshot-after-help-choice.json`
- `vnext/artifacts/c-phase-134-verification/devcontrol-red-fox-actions.json`
- `vnext/artifacts/c-phase-134-verification/egg-catalog-parity.json`
- `vnext/artifacts/c-phase-134-verification/audit-rows-during-walkthrough.json`
- `vnext/artifacts/c-phase-134-verification/audit-network-hosted-flags-during-walkthrough.json`
- `vnext/artifacts/c-phase-134-verification/scope-hash-after-toggles.json`
- `vnext/artifacts/c-phase-134-verification/final-autonomous-settings.json`
- `vnext/artifacts/c-phase-134-verification/final-dotnet-build.log`
- `vnext/artifacts/c-phase-134-verification/final-dotnet-test-no-build.log`
- `vnext/artifacts/c-phase-134-verification/final-build-vnext-skip-asset-prep.log`
- `vnext/artifacts/c-phase-134-verification/final-git-diff-check.log`

## Validation

| Check | Result | Evidence |
|---|---:|---|
| `dotnet build .\vnext\Wevito.VNext.sln` | Pass | `vnext/artifacts/c-phase-134-verification/final-dotnet-build.log` |
| `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build` | Pass, 993/993 | `vnext/artifacts/c-phase-134-verification/final-dotnet-test-no-build.log` |
| `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests` | Pass | `vnext/artifacts/c-phase-134-verification/final-build-vnext-skip-asset-prep.log` |
| `git diff --check` | Pass | `vnext/artifacts/c-phase-134-verification/final-git-diff-check.log` |

## Stop-Gate Checklist

- [x] No hosted AI call observed from product runtime.
- [x] No silent network access observed.
- [x] No silent file mutation observed outside expected save/audit writes and evidence artifact writes.
- [x] No capability flag default changed.
- [x] AuditLedger remains append-only.
- [x] Both autonomous scopes are OFF at end of walkthrough.
- [x] All five pre-flight/final validation commands pass on the synced tip.
- [x] Shell launched cleanly from the fresh Debug build output.

## Next Phase

C-PHASE 135 remains blocked on user review because C-PHASE 134 is Auto-continue=No. Recommended C-PHASE 135 focus: autonomous scope UX should clearly explain that individual scope toggles do not execute while the global autonomous beta gate is off, and should provide a safe preview-only path that does not require persistent enablement.
