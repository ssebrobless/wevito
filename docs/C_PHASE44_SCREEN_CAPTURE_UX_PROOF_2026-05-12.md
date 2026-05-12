# C-PHASE 44 - Screenshot And Capture UX Proof

Date: 2026-05-12
Branch: `claude-implementation/c-phase-44-screenshot-capture-ux-proof`

## Summary

C-PHASE 44 tightens the PET TASKS screenCapture wording so a user can tell the difference between a preview report and a real capture artifact.

```
PET TASKS screenCapture
        │
        ├── PREVIEW
        │     └── offline report only
        │         no screenshot, no recording, no mutation
        │
        └── RUN APPROVED
              └── local capture artifact only
                  no upload/share/clipboard/sprite mutation
```

## Changes

- Preview reports now explicitly label themselves as `offline preview report only`.
- Preview reports now state that `RUN APPROVED` is required before any screenshot artifact is created.
- Preview reports now state that preview mode never records.
- Execution summaries now label themselves as live Windows capture artifacts from the approved screenCapture path.
- Execution summaries now state the local-artifact-only/no-share boundary and whether the output is a recording.
- Tests lock down the new preview/execution report language.

## Probe Finding

An initial `screenCapture` PET TASKS probe without `-ApproveBeforePreview` failed because the task card correctly entered `WaitingForApproval`, leaving `PetTaskPreviewButton` disabled.

That is current intended behavior for screenCapture. The successful probe used:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\probe-vnext-pet-tasks.ps1 -TaskText "screenshot the Wevito window" -ExpectedToolFamily screenCapture -ApproveBeforePreview -SkipBuild
```

Successful probe summary:

- `vnext/artifacts/pet-task-probes/20260512-133038-923-9ae595a4/summary.json`
- Preview report: `vnext/artifacts/pet-tasks/20260512-173053-screencapture-screencapture/run-summary.md`
- Target sprite hashes unchanged: `true`
- No screenshot was captured during the preview probe.

## Boundaries

- No screenshot artifact was executed in this phase.
- No recording artifact was executed in this phase.
- No sprite/runtime PNGs changed.
- No shared assets changed.
- No asset-prep command was run.

## Validation

- `dotnet build .\vnext\Wevito.VNext.sln` passed.
- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build` passed: `280 / 280`.
- PET TASKS `screenCapture` preview probe passed with approval-before-preview.

## Follow-Up

- If we want an actual screenshot artifact proof, run the `screenCapture` task through `RUN APPROVED` in a separate approval-gated phase.
- Screen recording remains privacy-sensitive and should stay behind a separate explicit approval gate.
