# Code-Side Phase 15: PET TASKS Target Extraction

Date: 2026-05-05

## Scope

Make PET TASKS dry-run previews more useful by extracting safe target hints from helper-pet commands.

```text
User text
   |
   +-- "review goose baby female blue sprites"
   |       -> goose\baby\female\blue
   |
   +-- "summarize docs in C:\...\docs"
           -> explicit local path
```

This phase is still preview-only. It does not enable execution, visual generation, sprite import, Godot proofing, or runtime/source PNG mutation.

## Files Changed

- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Core\PetCommandParser.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Core\SpriteAuditPreviewAdapter.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\tests\Wevito.VNext.Tests\PetCommandParserTests.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\tests\Wevito.VNext.Tests\SpriteAuditPreviewAdapterTests.cs`

## Behavior

- `PetCommandParser` now extracts quoted or bare Windows absolute paths into `TargetPathsOrAssets`.
- Sprite review/audit commands now extract simple row phrases:
  - species
  - age: `baby`, `teen`, `adult`
  - gender: `female`, `male`
  - color: `red`, `orange`, `yellow`, `blue`, `indigo`, `violet`
- Example extracted row:
  - `goose baby female blue`
  - becomes `goose\baby\female\blue`
- `SpriteAuditPreviewAdapter` now resolves relative targets underneath approved sprite roots only.
- Relative targets that escape approved roots, such as `..\outside`, are blocked.

## Tests Added

- Parser extracts `goose\baby\female\blue` from a sprite audit command.
- Parser extracts a quoted Windows docs path from a local docs command.
- Sprite audit resolves a relative row target inside the approved root.
- Sprite audit blocks a relative target that escapes the approved root.

## Validation

- `dotnet build vnext\Wevito.VNext.sln` passed with `0` warnings.
- `dotnet test vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build` passed `77 / 77`.

## Audit Notes

- This is intentionally conservative and only recognizes exact simple row phrases.
- It does not infer incomplete sprite rows yet, such as species-only or species/age-only.
- It does not enumerate all colors or all genders from a single command.
- It does not apply or mutate any visual files.

## Next Safe Step

Add a tiny PET TASKS preview smoke/probe script that can exercise the parser/dispatcher/adapter path from the command line and write into a throwaway `vnext\artifacts\pet-tasks` folder. Keep it non-mutating and separate from Godot/package proofing.
