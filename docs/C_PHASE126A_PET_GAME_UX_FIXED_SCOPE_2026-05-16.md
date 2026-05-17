# C-PHASE 126a - Pet Game UX Fixed Scope

## Goal

Ship the fixed-scope pet-game UX repairs that were separated from C-PHASE 126b: starter egg parity, first-run sprite-cleanup task drafting, report-only sprite audit preview routing, and clearer action text.

## Scope

- Added `vnext/content/starter_eggs.json` as the shared ROYGBIV starter egg manifest.
- Refactored WPF `StarterEggCatalog` to one-shot load the manifest with the previous in-code catalog as fallback.
- Updated Godot `game_manager.gd` and `main_scene.gd` to read the starter egg manifest and map egg color to deterministic hidden species instead of randomizing selected eggs.
- Added an inline first-run choice panel under the WPF egg prompt with:
  - Just play with the pet
  - Help with sprite cleanup
  - Set up local AI runtime
- Wired "Help with sprite cleanup" into a local draft `TaskCard` using the existing `AgentTaskCardQueueService.AppendDraft` path.
- Added `first_run_choice_recorded` and `sprite_cleanup_help_card_drafted` packet kinds to the plain-language explainer.
- Added action clarity descriptions to `actions.json` and surfaced those descriptions in the action popup summary.
- Added a `sprite-cleanup-help` tool definition entry that stays report-only/no-mutation.

## Safety Boundaries

- No hosted AI calls.
- No model calls.
- No sprite/runtime PNG mutation.
- No asset prep.
- No new execution modes.
- PET TASKS sprite cleanup remains a draft/report-only preview surface.
- 126b items were intentionally not touched: fresh-save flow, focus/pinned/unfocused layout, and broader pet placement fixes.

## Validation

- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "StarterEgg|FirstRun|ActionMenu|ToolPopup|PlainLanguage"`: passed, 202/202.
- `dotnet build .\vnext\Wevito.VNext.sln`: passed.
- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build`: passed, 931/931.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests`: passed.
- `git diff --check`: passed.

## Notes

- The shared starter egg manifest currently keeps green visible but disabled with the message "Green runtime sprites are not installed yet."
- Godot now uses the same manifest for egg selection and selected-egg species mapping; this fixes the C-PHASE 125 drift where selected eggs could hatch random species.
- The existing PET TASKS preview path already calls the report-only adapter dispatcher for `spriteAudit`; this phase ensured onboarding creates a correctly scoped card for that path.

## Next Phase

C-PHASE 126b should handle fresh-save flow and focus/pinned/unfocused layout fixes, and each fix should reference the specific `BUG_BOARD.md` row produced in C-PHASE 125.
