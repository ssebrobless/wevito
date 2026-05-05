# Code-Side Phase 23: Local Docs Report Artifacts

Date: 2026-05-05

## Scope

Harden the existing `localDocs` PET TASKS preview adapter so it follows the same artifact convention as `spriteAudit` and `petState`.

```text
PET TASKS command
   |
   v
localDocs preview adapter
   |
   +-- reads approved docs only
   +-- writes local-docs-preview-report.json
   +-- writes run-summary.md
   +-- returns AuditLogPath
   |
   v
PET TASKS popup can show "Preview report ready"
```

## Files Changed

- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Contracts\PetAgentContracts.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Core\LocalDocsPreviewAdapter.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\tests\Wevito.VNext.Tests\LocalDocsPreviewAdapterTests.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\tests\Wevito.VNext.Tests\PetTaskAdapterPreviewDispatcherTests.cs`

## Implemented

- Added `LocalDocPreviewEntry`.
- Added `LocalDocsPreviewReport`.
- Writes `local-docs-preview-report.json`.
- Writes `run-summary.md`.
- Returns `AuditLogPath`.
- Blocks artifact roots that are not under a `pet-tasks` artifact folder.
- Continues to block execute mode.
- Continues to block targets outside approved `localDocs` roots.
- Continues to mutate no source docs or visual assets.

## Validation

- `dotnet build .\vnext\Wevito.VNext.sln` passed with `0` warnings.
- Focused `LocalDocsPreviewAdapterTests` passed `4 / 4`.
- Full vNext tests passed `90 / 90`.
- Safe Debug publish passed with `-SkipAssetPrep -SkipTests`.
- Live PET TASKS `localDocs` UI probe passed:
  - `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\pet-task-probes\20260505-123738-099-1e20daec\summary.json`
  - report: `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\pet-tasks\20260505-163752-localdocs-summarizedocs\run-summary.md`

## Audit Result

Pass. The three active no-mutation preview adapters now all have report-artifact behavior:

- `localDocs`
- `spriteAudit`
- `petState`

## Next Safe Step

Continue code-side PET TASKS hardening with read-only capability visibility or user-facing clarity. Do not add mutation, import, generation, build/proof execution, or browser automation without a separate approval gate.
