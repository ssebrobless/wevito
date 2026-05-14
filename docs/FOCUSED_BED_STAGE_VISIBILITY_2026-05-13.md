# Focused Bed Stage Visibility - 2026-05-13

## Goal

Make focused Wevito easier to read while the larger sprite-art cleanup continues.

## Implemented

- Focused and pinned calm mode now places living pets in the side-by-side idle lineup immediately, without waiting for the simulation to mark each pet as settled at home.
- The focused home backdrop no longer paints decorative grass/patch/leaf clutter behind the pets.
- If the habitat manifest cannot be read, the shell fallback now uses the same simple three-bed stage instead of species-specific shelters, dishes, perches, or food props.

## Boundaries

- No sprite PNGs were edited.
- No source boards were edited.
- No prop anchors or habitat manifests were edited.
- This does not fix low-quality source art such as snake, squirrel, pigeon, crow, or fox frame issues. Those remain asset repair work.

## Validation

- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "HomePanel|ShellPresentation|HabitatLoadout"` passed: 24 / 24.
- `dotnet build .\vnext\Wevito.VNext.sln` passed.
- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build` passed: 583 / 583.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests` passed.
