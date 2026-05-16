# C-PHASE 115 - Identity Rename + UX Language Pass

Branch: `claude-implementation/c-phase-115-identity-rename-ux-language-pass`

## Goal

Reframe Wevito as a local AI assistant with pet visuals as the cosmetic/gameplay surface, then remove misleading helper/pet task type names where the plan required a mechanical rename.

## Scope

- Implemented the C-PHASE 115 mechanical rename set for command-bar, parser, task-card queue, adapter dispatcher, and helper-profile contract names.
- Kept real pet-game classes unchanged: `PetActor`, `PetMemoryStore`, and `PetSimulationEngine`.
- Preserved the `Wevito.VNext.Contracts.PetAgentContracts.cs` file and namespace for API stability.
- Added local AI identity settings and first-launch setup state.
- Added a first-launch wizard surface with four steps.
- Added a top tab strip to the tool popup for Chat, Activity, Agents, Tools, Benchmarks, Creative Lab, and Settings.
- Rewrote top-level product docs to lead with the local AI assistant identity.

## Implemented

- `AiIdentityService` reads/writes `ai_identity_name`, defaulting to `Wevito`, and emits `ai_identity_set`.
- `FirstLaunchWizardStateService` tracks per-step completion, background choice, optional sprite-cleanup seed, and `first_launch_completed`.
- `FirstLaunchWizardWindow` provides the first-run wizard and can be rerun from Settings.
- `PlainLanguageExplainer` now covers `ai_identity_set`, `first_launch_step_completed`, and `first_launch_completed`.
- `README.md`, `WHAT_IS_WEVITO.md`, `docs/INDEX.md`, and `SPRITE_PIPELINE_KIT/README.md` now describe the AI-app pivot and safety shape.

## Safety Boundaries

- No hosted AI calls.
- No model weight downloads.
- No sprite mutation.
- No historical `C_PHASE0` through `C_PHASE85c` docs modified.
- No `PetActor`, `PetMemoryStore`, or `PetSimulationEngine` rename.
- KillSwitch is consulted before identity or first-launch services write settings.

## Validation

- Focused identity/language tests passed: `137 / 137`.
- `dotnet build .\vnext\Wevito.VNext.sln` passed with `0` warnings and `0` errors.
- Full vNext tests passed: `804 / 804`.
- Safe publish passed with `-SkipAssetPrep -SkipTests`.
- Stop-gate checks passed: no frozen `C_PHASE0` through `C_PHASE85c` docs were modified; `PetActor`, `PetMemoryStore`, `PetSimulationEngine`, and the contracts namespace were preserved; the legacy identity sweep only contains the deny-list in its own test file.

```powershell
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "IdentityRename|AiIdentity|FirstLaunch|PlainLanguage"
dotnet build .\vnext\Wevito.VNext.sln
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
```

## Next Phase

C-PHASE 115 is marked Auto-continue=Yes. If all validation and stop gates pass, continue to C-PHASE 116 on a fresh branch after this PR lands.
