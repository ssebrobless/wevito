# Code-Side Phase 18: Game Interaction Readiness

Date: 2026-05-05

## Scope

Verify that the vNext game-side code is still ready for visual cleanup and future helper tooling after the PET TASKS work.

```text
safe build path
   |
   +-- build-vnext -SkipAssetPrep
   |
   v
runtime action probe
   |
   +-- care actions
   +-- settings/save
   +-- basket/link tools
   |
   v
small deterministic shell-text fix
```

No sprite asset preparation, regeneration, import, or visual mutation was run.

## Files Changed

- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Core\PetSimulationEngine.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\tests\Wevito.VNext.Tests\PetSimulationEngineTests.cs`

## Readiness Checks

### Build Script Policy

Reviewed `C:\Users\fishe\.codex\worktrees\36d6\wevito\tools\build-vnext.ps1`.

Important finding:

- safe code-side build/publish path is `-SkipAssetPrep`
- without `-SkipAssetPrep`, the script runs sprite cleaning/generation scripts against runtime asset folders
- visual-side cleanup should keep using the explicit skip flag unless asset regeneration is intentionally approved

### Runtime Action Probe

Ran:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\probe-vnext-actions.ps1 -Configuration Debug -SkipBuild -StartupDelayMs 2500
```

Passed summary:

- `doctor` action trace matched
- `medicine` action trace matched
- `bath` action trace matched
- `groom` action trace matched
- `feed` action trace matched through action option flow
- `water` action trace matched
- `play` action trace matched through action option flow
- `rest` action trace matched
- `home` returned the expected not-needed trace when already home
- settings toggles traced
- save traced
- basket paste/open/delete traced
- broker URL-open trace matched

Artifact:

- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\action-probes\20260505-015533\summary.json`

## Fix Applied

`PetSimulationEngine.DescribeAging` used a non-ASCII middle-dot separator. In this shell environment it could render as mojibake in PowerShell output.

Changed:

- from `lifePhase · pace`
- to `lifePhase - pace`

Added test:

- `DescribeAging_UsesAsciiSeparatorForShellText`

## Validation

- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -StepTimeoutSeconds 120` passed.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\probe-vnext-actions.ps1 -Configuration Debug -SkipBuild -StartupDelayMs 2500` passed.
- `dotnet build vnext\Wevito.VNext.sln` passed with `0` warnings.
- `dotnet test vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build` passed `79 / 79`.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests -StepTimeoutSeconds 120` passed and refreshed `vnext\artifacts\shell`.

## Audit Notes

- PET TASKS remains dry-run/report-only.
- No `Execute` run mode was enabled.
- No visual assets were regenerated or imported.
- No runtime/source PNG mutation was performed by this phase.
- Existing care/action functionality appears operational through the automated runtime probe.

## Next Safe Step

Produce a compact code-side handoff for the visual thread or continue with a deeper simulation design review if the user wants more code-side planning before visual cleanup lands.
