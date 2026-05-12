# C-PHASE 42 PET TASKS UX Clarity

Date: 2026-05-12

Branch: `claude-implementation/c-phase-42-pet-tasks-ux-clarity`

## Goal

Make PET TASKS harder to misread as unrestricted automation while keeping the useful report/artifact flow easy to find.

## Change Shape

```text
PET TASKS wording
|
+-- before
|   +-- safe surface existed
|   +-- report buttons existed
|   `-- wording could still read like "run a task"
|
`-- after
    +-- "safe reports" header
    +-- PREPARE -> PREVIEW -> RUN APPROVED ladder
    +-- exact adapter names preserved for probes
    +-- RUN renamed to RUN APPROVED
    +-- result path explains report artifact timing
    `-- home tools hint says report-first/gated
```

## Files Changed

- `vnext/src/Wevito.VNext.Shell/HomePanelWindow.xaml.cs`
- `vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml`
- `vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml.cs`

## User-Facing Clarifications

- PET TASKS now says `Ask your pets (safe reports)`.
- The command flow now says `Prepare - preview report - run only if explicitly approved`.
- The text box example now avoids test-only pet names and uses `summarize the local docs`.
- The capability text keeps exact family names while separating safe/report-only, approval-gated, and locked capabilities.
- The execution button now says `RUN APPROVED`, not just `RUN`.
- The queue summary includes a `runnable` count so report-only reviewed cards do not look executable.
- Default next-action text explains that only a few reviewed cards can run.

## Validation

Build:

```powershell
dotnet build .\vnext\Wevito.VNext.sln
```

Result: `PASS`

Tests:

```powershell
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
```

Result: `PASS`, `278 / 278`

PET TASKS probes:

| Probe | Task text | Expected family | Result |
| --- | --- | --- | --- |
| localDocs | `summarize the local docs` | `localDocs` | PASS |
| spriteAudit | `review goose baby female blue sprites` | `spriteAudit` | PASS |

Probe artifacts:

- `vnext/artifacts/c-phase-42-pet-tasks-ux-clarity/localDocs-probe.txt`
- `vnext/artifacts/c-phase-42-pet-tasks-ux-clarity/spriteAudit-probe.txt`
- `vnext/artifacts/c-phase-42-pet-tasks-ux-clarity/probe-summary.json`

## Gates Preserved

- No live model calls.
- No sprite import/generation/PNG mutation.
- No prop-anchor edits.
- No browser automation.
- No desktop/region recording.
- No audio booster/driver/APO changes.
- No new execution adapter was enabled.

## Remaining Work

C-PHASE 43 should move to care, item, and habitat runtime mapping polish. PET TASKS can continue receiving visual polish later, but this phase closes the immediate clarity issue without expanding capability.

