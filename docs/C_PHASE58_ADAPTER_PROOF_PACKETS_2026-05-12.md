# C-PHASE 58: Adapter Proof Packets

Date: 2026-05-12
Branch: `claude-implementation/c-phase-58-adapter-proof-packets`

## Scope

C-PHASE 58 added a repeatable PET TASKS adapter proof-pack runner. The runner exercises the current adapter families through the existing WPF probe surface and gathers durable evidence into one phase-specific artifact folder.

No model calls, screen recording, external audio booster control, sprite mutation, art generation, or runtime/source PNG changes were made.

## Outputs

- `tools/run-pet-task-adapter-proof-pack.ps1`
- `vnext/artifacts/c-phase-58-adapter-proof-packets/adapter-proof-summary.json`
- `vnext/artifacts/c-phase-58-adapter-proof-packets/adapter-proof-summary.md`
- `vnext/artifacts/c-phase-58-adapter-proof-packets/adapters/<family>/`

Each adapter folder contains a copied probe packet, report packet, shell trace copy, and probe output text when available.

## Adapter Coverage

| Family | Task text | Result |
| --- | --- | --- |
| `localDocs` | `summarize the local docs` | PASS |
| `spriteAudit` | `review goose baby female blue sprites` | PASS |
| `assetInventory` | `inventory assets in sprites_runtime` | PASS |
| `petState` | `review pet state` | PASS |
| `codeReview` | `review the code in Wevito.VNext.Core` | PASS |
| `codePatchPlan` | `plan a code fix in vnext` | PASS |
| `buildProof` | `run a build proof` | PASS |
| `translateText` | `translate Hello goose to Spanish` | PASS |
| `audioAssist` | `boost my PC volume` | PASS |
| `screenCapture` | `screenshot the Wevito window` | PASS |

## Implementation Notes

- The runner builds the vNext Shell once, then invokes `tools/probe-vnext-pet-tasks.ps1` with `-SkipBuild` for each adapter.
- `buildProof` uses `-ApproveBeforePreview`.
- `translateText` uses `-ExpectExecuteEnabledAfterPreview`.
- `screenCapture` also uses `-ApproveBeforePreview` because the current C-PHASE 57 UI correctly classifies screenshot requests as approval-only before preview. The proof still does not execute the capture action.
- `audioAssist` uses the boost request as a report/setup-guide handoff and verifies it does not enable execution for external booster control.
- The runner copies probe/report/trace evidence into the C-PHASE 58 folder, then removes transient probe and pet-task artifact folders created during the run.
- The runner stops leftover local `Wevito.VNext.Shell` / `Wevito.VNext.Broker` proof processes after each adapter so later builds do not hit locked DLLs.

## Validation

Commands run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\run-pet-task-adapter-proof-pack.ps1
dotnet build .\vnext\Wevito.VNext.sln
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
```

Results:

- Adapter proof pack: PASS, 10 / 10 families.
- vNext build: PASS.
- vNext tests: PASS, 286 / 286.

## Residuals

- The proof runner relies on the existing WPF automation probe, so it verifies the adapter route and preview/report surface rather than replacing full manual UI review.
- `translateText` proves the reviewed card can enable execution after preview, but this phase does not execute a provider call.
- `screenCapture` proves approval-to-preview only. It does not capture a screenshot or start any recording.
