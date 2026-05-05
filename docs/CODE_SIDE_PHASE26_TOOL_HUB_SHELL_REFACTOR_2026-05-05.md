# Code-Side Phase 26 Tool Hub Shell Refactor - 2026-05-05

## Goal

Make the PET TASKS popup feel like a compact, single-entry Tool Hub instead of a debug-like helper panel.

Phase 26 followed the Phase 25 information architecture:

```text
Ask your pets
   |
   v
safety/capability line
   |
   v
helper roles
   |
   v
current task + next action
   |
   v
preview/report artifact
```

## Files Changed

- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Shell\ToolPopupWindow.xaml`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Shell\ToolPopupWindow.xaml.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\tools\probe-vnext-pet-tasks.ps1`

Supporting docs:

- `C:\Users\fishe\.codex\worktrees\36d6\wevito\docs\WEVITO_TOOL_HUB_INFORMATION_ARCHITECTURE_2026-05-05.md`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\docs\WEVITO_TOOL_AGENT_UI_MASTER_PLAN_2026-05-05.md`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\docs\TRANSLATION_AND_AUDIO_TOOL_RESEARCH_PLAN_2026-05-05.md`

## What Changed

- Renamed the helper popup title from `Wevito Helpers` / `Pet Helpers` to `Wevito PET TASKS` / `PET TASKS`.
- Moved the PET TASKS command textbox to the top of the helper surface.
- Replaced long intro copy with compact `Ask your pets` framing.
- Preserved existing automation IDs:
  - `PetCommandTextBox`
  - `PetCommandSubmitButton`
  - `PetTaskCapabilityText`
  - `PetWellbeingSnapshotText`
  - `PetTaskQueueComboBox`
  - `PetTaskApproveButton`
  - `PetTaskPreviewButton`
  - `PetTaskCancelButton`
- Added `PetTaskNextActionText` so the UI always tells the user what to do next.
- Updated helper role labels to simpler Tool Hub terms:
  - `Inspector - sprite QA`
  - `Builder - plans`
  - `Scout - research`
- Extended the capability line to explicitly label translation calls and audio changes as locked.
- Updated the live PET TASKS probe to:
  - find the renamed `Wevito PET TASKS` window,
  - assert `PetTaskNextActionText`,
  - record both initial and post-preview next-action text.

## Boundaries Preserved

- No sprite generation.
- No sprite import.
- No runtime/source PNG mutation.
- No prop anchor edits.
- No browser automation.
- No translation network calls.
- No audio control.
- No build/proof execution through PET TASKS.
- Safe Debug publish used `-SkipAssetPrep`.

## Validation

```text
dotnet build .\vnext\Wevito.VNext.sln
```

Result: passed, `0` warnings, `0` errors.

```text
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
```

Result: passed `90 / 90`.

```text
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
```

Result: passed.

Live PET TASKS probes:

```text
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\probe-vnext-pet-tasks.ps1 -TaskText "review goose baby female blue sprites" -ExpectedToolFamily spriteAudit -SkipBuild
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\probe-vnext-pet-tasks.ps1 -TaskText "summarize docs for the PET TASKS plan" -ExpectedToolFamily localDocs -SkipBuild
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\probe-vnext-pet-tasks.ps1 -TaskText "check pet state" -ExpectedToolFamily petState -SkipBuild
```

Results:

- `spriteAudit` passed: `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\pet-task-probes\20260505-130720-360-0e1c0d54\summary.json`
- `localDocs` passed: `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\pet-task-probes\20260505-130747-092-90efa10b\summary.json`
- `petState` passed: `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\pet-task-probes\20260505-130807-903-df134d0d\summary.json`

Probe evidence:

- Initial next-action text: `Next: prepare a task, then preview a report before anything can run.`
- Post-preview next-action text: `Next: open the artifact/report, then approve, revise, or cancel.`
- Target sprite row hashes unchanged.

## Audit

Pass.

- Correctness: preserved PET TASKS behavior and existing preview adapters.
- Maintainability: kept changes localized to popup presentation and probe assertions.
- Safety: no mutation/execution capability was added.
- UX: command input is now first, safety is visible, and the user gets a next-action cue.
- Runtime validation: build, tests, safe publish, and live probes passed.
- Visual QA: automated UI probe verified the renamed window and required controls. No screenshot capture was added in this phase.

## Next Phase Recommendation

Proceed to Phase 27: capture contracts and policy.

Do not implement screenshot/clip capture before defining:

- capture request/result contracts,
- approved capture scopes,
- artifact folder policy,
- privacy rules,
- no-mutation behavior,
- proof labeling rules.
