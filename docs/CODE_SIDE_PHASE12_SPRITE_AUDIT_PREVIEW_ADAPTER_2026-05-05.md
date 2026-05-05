# Code-Side Phase 12 Sprite Audit Preview Adapter - 2026-05-05

## Scope

Implemented the first no-mutation `spriteAudit` adapter in Core. It produces report-only artifacts and remains disconnected from the `PET TASKS` Shell run path.

## Visual-Side Inputs

Visual-side expectations were read from:

`C:\Users\fishe\Documents\projects\wevito\docs\VISUAL_SIDE_PET_TASKS_ADAPTER_EXPECTATIONS_2026-05-05.md`

Confirmed constraints applied in this phase:

- first proof surface is report-only,
- first adapter may be `localDocs` or non-mutating `spriteAudit`,
- `spriteAudit` minimum output is markdown plus JSON,
- write only new timestamped/pet-task artifact folders,
- do not write into existing visual-side review/candidate/proof folders,
- no runtime/source PNG mutation,
- no sprite import,
- no visual generation,
- no prop-anchor edits,
- no candidate apply,
- goose `drop_ball` apply/proof remains manual and outside `PET TASKS`.

## Shape

```text
TaskAdapterRequest
      |
      v
SpriteAuditPreviewAdapter
      |
      +-- validate dry-run report mode
      +-- validate tool family = spriteAudit
      +-- validate read-only policy
      +-- validate approved sprite roots
      +-- validate target paths stay inside roots
      +-- validate artifact root contains pet-tasks
      |
      +-- read PNG headers + hashes
      |
      v
pet-tasks artifact folder
      |
      +-- sprite-audit-report.json
      +-- run-summary.md
      +-- qa\          created, no contact sheet yet
```

## Files Changed

- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Contracts\PetAgentContracts.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Core\SpriteAuditPreviewAdapter.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\tests\Wevito.VNext.Tests\SpriteAuditPreviewAdapterTests.cs`

## Implemented

- Added `SpriteAuditFrameFinding`.
- Added `SpriteAuditReport`.
- Added `SpriteAuditPreviewAdapter`.
- The adapter writes:
  - `sprite-audit-report.json`,
  - `run-summary.md`,
  - an empty `qa\` folder reserved for future contact-sheet output.
- The adapter scans PNG headers for dimensions and hashes.
- The adapter flags basic structural header findings:
  - `invalid_png_signature`,
  - `invalid_png_header`,
  - `unreadable_png`,
  - `tiny_frame`,
  - `large_frame`.

## Guardrails

- Blocks non-dry-run mode.
- Blocks non-`spriteAudit` intent/policy pairs.
- Blocks non-read-only policy.
- Blocks missing approved roots.
- Blocks requested targets outside approved roots.
- Blocks artifact roots that are not under a `pet-tasks` folder.
- Blocks writing into animation candidate/proof-style folders by requiring `pet-tasks` and rejecting unsafe path segments.
- Does not edit PNGs.
- Does not edit source boards.
- Does not edit prop anchors.
- Does not run Godot, vNext, browsers, Gemini, Claude, or external tools.

## Validation

- `dotnet build vnext\Wevito.VNext.sln` passed.
- `dotnet test vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build` passed `67 / 67`.

Focused test coverage:

- writes markdown and JSON under a pet-task artifact folder,
- reads approved PNG targets,
- preserves PNG hash before/after report generation,
- blocks outside-root targets,
- blocks existing visual packet artifact roots,
- blocks execute mode.

## Audit Result

Pass. The adapter matches the visual-side safe lane: report-only, no sprite mutation, new artifact folder only.

## Known Limitation

Contact-sheet generation is not implemented in this Core adapter yet. The markdown report explicitly states this. This still satisfies the visual-side minimum output of markdown plus JSON; contact sheets should be added in a later visual-output phase with a deliberate image composition strategy.

## Next Safe Step

Add a dry-run adapter dispatcher that can select `localDocs` or `spriteAudit` preview adapters by `ToolFamily`, still without Shell execution wiring.
