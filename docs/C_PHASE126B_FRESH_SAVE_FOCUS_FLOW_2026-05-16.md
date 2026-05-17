# C-PHASE 126b: Fresh Save And Action Confirmation Fixes

## Goal

Close the remaining C-PHASE 125 product-truth bugs that were explicitly recorded in `docs/BUG_BOARD.md` and not covered by C-PHASE 126a:

- `BUG-001`: the documented fresh-profile reset target was stale because live state is stored in `%LOCALAPPDATA%\WevitoVNext\wevito-vnext.db`.
- `BUG-004`: successful DevControl care actions did not always produce distinct animation-state confirmation for `water`, `groom`, and `doctor`.

Focus / pinned / unfocused layout changes were not included in this PR because C-PHASE 125 did not create a `BUG_BOARD.md` row for that surface. The only layout-adjacent work here is preserving the existing fresh egg prompt flow and making action confirmation easier to verify through DevControl snapshots.

## Scope

Changed:

- `vnext/src/Wevito.VNext.Core/AppRepository.cs`
- `vnext/src/Wevito.VNext.Shell/ShellCoordinator.cs`
- `tools/reset-wevito-vnext-profile.ps1`
- `vnext/src/Wevito.VNext.Contracts/Models.cs`
- `vnext/src/Wevito.VNext.Contracts/DevControlContracts.cs`
- `vnext/src/Wevito.VNext.Core/PetSimulationEngine.cs`
- `vnext/src/Wevito.VNext.Shell/DevControlSnapshotBuilder.cs`
- `vnext/src/Wevito.VNext.Shell/SpriteAssetService.cs`
- `vnext/content/actions.json`
- `vnext/tests/Wevito.VNext.Tests/AppRepositoryTests.cs`
- `vnext/tests/Wevito.VNext.Tests/ActionVisualIntentTests.cs`
- `vnext/tests/Wevito.VNext.Tests/FreshSaveFlowTests.cs`
- `docs/BUG_BOARD.md`

Not changed:

- No PNGs.
- No sprite imports or generation.
- No hosted AI/model calls.
- No asset-prep mutation.
- No broad focus/pinned redesign without a matching bug row.

## Implemented

### BUG-001

- Added a single code-level source of truth for the live vNext save database:
  `%LOCALAPPDATA%\WevitoVNext\wevito-vnext.db`.
- Updated `ShellCoordinator` to consume the repository path constants instead of duplicating the database filename.
- Added `tools/reset-wevito-vnext-profile.ps1`, which backs up the live SQLite database before deletion so a fresh launch returns to the egg-choice profile.
- Added tests proving the default database path and the empty fresh-save state.

### BUG-004

- Added first-class action confirmation states:
  `Drink`, `Groom`, and `Doctor`.
- Updated action content so:
  `water -> Drink`, `groom -> Groom`, and `doctor -> Doctor`.
- Preserved visual safety by mapping those states back to available sprite families:
  `Drink` uses the `drink` action family when present and falls back to `eat`; `Groom` falls back to `happy`; `Doctor` falls back to `sick`.
- Extended DevControl pet slot snapshots with `ActionVisualFamily` and `LastActionId` so audits can distinguish the visible family and the action that caused it.

## Safety Boundaries

- Reset is manual and backup-first; it does not run automatically.
- Runtime sprite rendering remains fallback-safe if dedicated action art is missing.
- Existing PET TASKS and model capability flags remain unchanged.
- `AuditLedgerService` remains append-only.

## Validation

- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "StarterEgg|OnboardingSprite|ActionMenuClarity|FreshSaveFlow|PlainLanguage"`: passed, 189/189.
- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "ActionVisualIntent|AppRepository"`: passed, 12/12.
- `dotnet build .\vnext\Wevito.VNext.sln`: passed.
- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build`: passed, 941/941.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests`: passed.
- `git diff --check`: passed with line-ending normalization warnings only.

## Next Phase

After user review, the next planned phase is C-PHASE 127: the DevController visual QA matrix sweep. That phase should use these clearer action states and snapshots as evidence while still avoiding sprite mutation.
