# Wevito Tool Hub Information Architecture - 2026-05-05

## Goal

Turn the helper-pet tool surface into one compact, low-friction entry point:

```text
Ask once -> pet helper chooses safe tool -> result card -> user decides next action
```

The user should not need to know whether the work is a sprite audit, local doc scan, code review, translation, capture, creative packet, or audio helper. That routing should be hidden behind the PET TASKS command bar and clear task cards.

## Current Problem

The existing PET TASKS surface is functional, but it is still arranged like a technical debug panel:

```text
╔════════════════════════════════════════════════════════════╗
║ PET TASKS today                                           ║
╠════════════════════════════════════════════════════════════╣
║ explanation text                                          ║
║ capability text                                           ║
║ helper 1 │ helper 2 │ helper 3                            ║
║ wellbeing snapshot                                        ║
║ task input + prepare                                      ║
║ status/result/queue/actions                               ║
╚════════════════════════════════════════════════════════════╝
```

This works for proofing but will not scale cleanly as we add capture, code-review, translation, creative-lab, sprite-workflow, and audio-assist tools.

## Target Shape

```text
╔════════════════════════════════════════════════════════════╗
║ PET TASKS                                      SAFE PREVIEW║
╠════════════════════════════════════════════════════════════╣
║ Ask your pets                                             ║
║ ┌──────────────────────────────────────────┐ ┌──────────┐ ║
║ │ @Scout translate this to Spanish...      │ │ Prepare  │ ║
║ └──────────────────────────────────────────┘ └──────────┘ ║
║ spriteAudit · petState · localDocs ready                  ║
║ locked: generation/import/mutation/build/browser/audio    ║
╠════════════════════════════════════════════════════════════╣
║ Helpers                                                   ║
║ ┌──────────────┐ ┌──────────────┐ ┌──────────────┐        ║
║ │ Scout        │ │ Inspector    │ │ Builder      │        ║
║ │ docs/review  │ │ QA/audit     │ │ code/proofs  │        ║
║ └──────────────┘ └──────────────┘ └──────────────┘        ║
╠════════════════════════════════════════════════════════════╣
║ Current Task                                              ║
║ ┌──────────────────────────────────────────────────────┐   ║
║ │ Assigned: Scout · Draft                              │   ║
║ │ Tool: localDocs · Policy: allowed preview            │   ║
║ │ Latest: draft_created                                │   ║
║ └──────────────────────────────────────────────────────┘   ║
║ [Preview] [Approve] [Cancel]                              ║
╠════════════════════════════════════════════════════════════╣
║ Result                                                    ║
║ ┌──────────────────────────────────────────────────────┐   ║
║ │ Report ready: run-summary.md                         │   ║
║ │ 0 files changed · 1 artifact folder created          │   ║
║ └──────────────────────────────────────────────────────┘   ║
║ [Open Artifact] [Copy Summary] [Revise Task]              ║
╚════════════════════════════════════════════════════════════╝
```

## Information Priority

1. **Task input**
   - The first thing visible should be the user's text bar.
   - Placeholder examples should use natural language:
     - `@Scout summarize the sprite docs`
     - `@Inspector audit goose baby female blue`
     - `@Builder review the shell popup code`
     - `translate this to Spanish`
     - `check my PC volume`

2. **Safety/capability line**
   - One short line should say what can run now.
   - One short line should say what is locked.
   - This protects the user from assuming PET TASKS can already mutate sprites or control the PC.

3. **Helper pet roles**
   - Keep exactly three visible helper cards.
   - Use simple role labels, not complex personality systems:
     - Scout: research, docs, summarization.
     - Inspector: sprite QA, pet state, visual reports.
     - Builder: code review, proof planning, tests later.

4. **Current task**
   - Show one task card as the active focus.
   - The queue should be present but secondary.
   - Avoid making the user choose from a dense list before understanding the latest result.

5. **Result artifact**
   - Every tool should converge on:
     - markdown summary,
     - JSON report,
     - optional contact sheet/screenshot/proof media,
     - artifact path.

## Tool Family Routing

```text
User text
   |
   v
PetCommandParser
   |
   +-- spriteAudit     -> no-mutation sprite report
   +-- petState        -> wellbeing/debug truth report
   +-- localDocs       -> approved-root doc summary
   +-- captureProof    -> screenshot/report, later clips
   +-- codeReview      -> read-only code issue report
   +-- codePatchPlan   -> implementation plan, no edits
   +-- buildProof      -> approved safe command runner
   +-- creativeLab     -> prompt/reference/checklist packet
   +-- spriteWorkflow  -> candidate review/apply plan only
   +-- translateText   -> translation preview/execution
   +-- audioAssist     -> audio status/control/boost handoff
```

## Simplicity Rules

- Do not add a separate tab per tool.
- Do not expose provider choice unless the user asks or setup is missing.
- Do not put generated/import/mutation controls beside preview-only tools.
- Do not make the user scroll to understand the current task.
- Keep advanced details collapsed or moved to artifact reports.
- The visible popup should answer:
  - What can I ask?
  - Which pet is doing it?
  - Is it safe?
  - What happened?
  - What is the next button?

## Phase 26 UI Refactor Scope

The next implementation phase should update only the helper popup layout and related probes.

In scope:

- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Shell\ToolPopupWindow.xaml`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Shell\ToolPopupWindow.xaml.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\tools\probe-vnext-pet-tasks.ps1`

Expected UI changes:

- Rename popup title from `Pet Helpers` to `PET TASKS`.
- Move the command textbox to the top of the helper surface.
- Replace long intro copy with compact `Ask your pets` copy.
- Preserve automation IDs used by probes:
  - `PetCommandTextBox`
  - `PetCommandSubmitButton`
  - `PetTaskCapabilityText`
  - `PetWellbeingSnapshotText`
  - `PetTaskQueueComboBox`
  - `PetTaskApproveButton`
  - `PetTaskPreviewButton`
  - `PetTaskCancelButton`
- Add an automation-visible result/next-action label if practical.
- Keep the surface read-only/non-mutating.

Out of scope:

- No new tool execution.
- No translation network calls.
- No audio control.
- No screenshot capture.
- No build command execution through PET TASKS.
- No sprite/art mutation.

## Validation Plan

After Phase 26:

```text
dotnet build .\vnext\Wevito.VNext.sln
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\probe-vnext-pet-tasks.ps1 -TaskKind spriteAudit -SkipBuild
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\probe-vnext-pet-tasks.ps1 -TaskKind localDocs -SkipBuild
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\probe-vnext-pet-tasks.ps1 -TaskKind petState -SkipBuild
```

Pass criteria:

- Build succeeds.
- Tests pass.
- Debug publish succeeds without asset prep.
- PET TASKS probes still find command box, capability text, wellbeing snapshot, and task controls.
- No runtime/source PNG changes are caused by this phase.

## Audit Notes

- This IA intentionally keeps `translateText` and `audioAssist` behind future phases. They are planned, but the UI should first become clear enough to host them safely.
- The helper pets remain simple animal-themed agents. Their AI/tool capability can be powerful, but their personalities should stay lightweight and game-like.
- The visual thread can keep sprite cleanup separate; this phase does not require visual-side coordination.
