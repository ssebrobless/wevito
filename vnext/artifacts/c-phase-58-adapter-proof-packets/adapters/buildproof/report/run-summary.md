# PET TASKS Build Proof Plan

Generated: 2026-05-12T20:58:27.3061545+00:00
TaskCard: `f606ad30-1981-4a11-b462-58d76c5d91a2`
Intent: run a build proof

## Summary

- Commands planned: 4
- Commands run: false
- Did mutate files: false

## Command Plan

- 1. `dotnet build .\vnext\Wevito.VNext.sln` - Compile contracts, core, tests, broker, and shell before proofing. Requires approval: True. Skip asset prep: False.
- 2. `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build` - Run deterministic vNext tests against the compiled output. Requires approval: True. Skip asset prep: False.
- 3. `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests` - Publish the Debug vNext shell/broker without regenerating runtime sprites. Requires approval: True. Skip asset prep: True.
- 4. `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\probe-vnext-pet-tasks.ps1 -TaskText "review pet state" -ExpectedToolFamily petState -SkipBuild` - Optional live UI smoke proof after publish, still no asset mutation. Requires approval: True. Skip asset prep: False.

## Expected Artifacts

- Console output for build/test/publish commands.
- vnext\artifacts\shell\Wevito.VNext.Shell.exe after safe publish.
- vnext\artifacts\pet-task-probes\<timestamp>\summary.json for optional live probe.
- No runtime/source PNG changes.

## Safety Gates

- This adapter must not execute commands.
- Every build/proof command requires explicit user approval before a future execution adapter can run it.
- Use -SkipAssetPrep for vNext publish while visual cleanup is active.
- Do not regenerate sprites_runtime or source assets.
- Do not mutate runtime/source PNGs or visual-side candidate/proof folders.
- Stop if any command plan would require removing or overwriting unrelated dirty work.

## Stop Conditions

- Missing vNext solution, test project, or build script.
- Any proof path requires asset prep or sprite normalization.
- A live proof would need visual generation/import or PNG mutation.
- The working tree has conflicting code changes in the intended proof surface.

## Safety

This adapter only wrote markdown/JSON artifacts in a new pet-task folder. It did not run build commands, edit files, mutate assets, or touch visual-side candidate/proof folders.
