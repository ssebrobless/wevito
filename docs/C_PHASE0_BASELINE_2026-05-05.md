# C-PHASE 0 Baseline Validation

Date: 2026-05-06

Branch: `claude-implementation/c-phase-0-baseline`

## Summary

C-PHASE 0 captured the current baseline after the Claude roadmap docs were added to `main`.

The original Claude prompt expected `c1d0a5cc9`, but `origin/main` intentionally advanced to `246244699` after committing the five Claude roadmap documents. This phase uses `2462446992556386467b3d0f1359c1d2723f4760` as the current approved baseline.

Two prerequisite files were restored from commit `a07d475e1` because cleanup had dropped them even though C-PHASE 0 depends on them:

- `tools/audit_optional_animation_readiness.py`
- `vnext/content/optional_animation_families.json`

## Validation Results

| Check | Result | Notes |
| --- | --- | --- |
| Git baseline | PASS | `HEAD` equals `origin/main` at `2462446992556386467b3d0f1359c1d2723f4760` before phase artifacts. |
| vNext build | PASS | `dotnet build .\vnext\Wevito.VNext.sln` completed with `0 Warning(s)` and `0 Error(s)`. |
| vNext tests | PASS | `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build` passed `144 / 144`. |
| Safe Debug publish | PASS | `tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests` exited `0`. |
| Sprite contract audit | PASS | `30 / 30` source boards, `17 / 17` supporting inputs, `360 / 360` runtime variant dirs, `10800 / 10800` runtime frames, `0` errors. |
| Runtime canvas mismatch audit | PASS | `2880` sequences, `10800` frames, `0` mixed-canvas rows, `0` missing rows, `0` invalid rows. |
| Optional animation readiness | PASS with delta | Tool passed with `2520` targets and `0` errors, but current baseline is `complete_authored_count=0`, `fallback_only_count=2520`. Previous 2026-05-05 snapshot reported the inverse (`authored_complete=2520`, `fallback_only=0`). This should be investigated before phases that depend on optional authored families, especially C-PHASE 23. |
| PET TASKS probe matrix | PASS | All ten tool families exited `0` and emitted `status=PreviewReady` for the expected family in shell trace. |

## PET TASKS Probe Matrix

| Family | Task text | Extra flags | Result |
| --- | --- | --- | --- |
| `localDocs` | `summarize the local docs` | none | PASS |
| `spriteAudit` | `review goose baby female blue sprites` | none | PASS |
| `assetInventory` | `inventory assets in sprites_runtime` | none | PASS |
| `petState` | `review pet state` | none | PASS |
| `codeReview` | `review the code in Wevito.VNext.Core` | none | PASS |
| `codePatchPlan` | `plan a code fix in vnext` | none | PASS |
| `buildProof` | `run a build proof` | `-ApproveBeforePreview` | PASS |
| `translateText` | `translate Hello goose to Spanish` | `-ExpectExecuteEnabledAfterPreview` | PASS |
| `audioAssist` | `boost my PC volume` | none | PASS |
| `screenCapture` | `screenshot the Wevito window` | none | PASS |

## Artifact Paths

Primary baseline folder:

`vnext/artifacts/baseline-2026-05-05/`

Key files:

- `vnext/artifacts/baseline-2026-05-05/git-baseline.json`
- `vnext/artifacts/baseline-2026-05-05/build.txt`
- `vnext/artifacts/baseline-2026-05-05/tests.txt`
- `vnext/artifacts/baseline-2026-05-05/safe-publish.txt`
- `vnext/artifacts/baseline-2026-05-05/sprite-contract.json`
- `vnext/artifacts/baseline-2026-05-05/canvas.json`
- `vnext/artifacts/baseline-2026-05-05/canvas.md`
- `vnext/artifacts/baseline-2026-05-05/optional-readiness.json`
- `vnext/artifacts/baseline-2026-05-05/optional-readiness.md`
- `vnext/artifacts/baseline-2026-05-05/pet-task-probes-summary.json`
- `vnext/artifacts/baseline-2026-05-05/probe-localDocs.txt`
- `vnext/artifacts/baseline-2026-05-05/probe-spriteAudit.txt`
- `vnext/artifacts/baseline-2026-05-05/probe-assetInventory.txt`
- `vnext/artifacts/baseline-2026-05-05/probe-petState.txt`
- `vnext/artifacts/baseline-2026-05-05/probe-codeReview.txt`
- `vnext/artifacts/baseline-2026-05-05/probe-codePatchPlan.txt`
- `vnext/artifacts/baseline-2026-05-05/probe-buildProof.txt`
- `vnext/artifacts/baseline-2026-05-05/probe-translateText.txt`
- `vnext/artifacts/baseline-2026-05-05/probe-audioAssist.txt`
- `vnext/artifacts/baseline-2026-05-05/probe-screenCapture.txt`

PET TASKS probe detail folders were also created under:

`vnext/artifacts/pet-task-probes/`

## Follow-Up Notes

- The optional readiness delta is the only substantive baseline concern: current authored optional animation coverage is `0`, with all `2520` targets falling back at runtime.
- The restored optional readiness script and manifest are staged as prerequisites for this phase, not broad code changes.
- Earlier failed PET TASKS probe attempts were caused by probe text/flag mistakes. The final probe matrix above used parser-safe task text and the C-PHASE 0 flag set.

