# PET TASKS Code Patch Plan

Generated: 2026-05-12T20:58:10.4597997+00:00
TaskCard: `61c49304-3e58-4645-8637-2fe51994ac08`
Requested change: plan a code fix in vnext

## Summary

- Candidate files: 40
- Did mutate code: false
- Scope: Prepare a reversible patch for "plan a code fix in vnext" using the approved code/script roots only. Candidate surface: 1 C#, 7 GDScript, 2 JSON, 13 PowerShell, 17 Python.

## Candidate Files

- `game_manager.gd`: GDScript, 1027 lines, 34340 bytes
- `main.gd`: GDScript, 19 lines, 288 bytes
- `main_scene.gd`: GDScript, 5772 lines, 217695 bytes
- `pet.gd`: GDScript, 684 lines, 22053 bytes
- `pet_data.gd`: GDScript, 122 lines, 3761 bytes
- `signal_bus.gd`: GDScript, 7 lines, 205 bytes
- `sound_manager.gd`: GDScript, 180 lines, 5875 bytes
- `analyze-vnext-flicker.py`: Python, 201 lines, 7213 bytes
- `assert-stable-baseline.ps1`: PowerShell, 45 lines, 1757 bytes
- `audit_optional_animation_readiness.py`: Python, 369 lines, 15247 bytes
- `audit_source_to_runtime_quality.py`: Python, 356 lines, 13058 bytes
- `audit_sprite_contract.py`: Python, 327 lines, 12385 bytes
- `authored_motion_specs.py`: Python, 147 lines, 8926 bytes
- `batch-drive-live-gemini.ps1`: PowerShell, 121 lines, 4267 bytes
- `bootstrap_sprite_pipeline.py`: Python, 313 lines, 13823 bytes
- `build-desktop-bridge.ps1`: PowerShell, 41 lines, 1117 bytes
- `build-release.ps1`: PowerShell, 409 lines, 16887 bytes
- `build-vnext.ps1`: PowerShell, 202 lines, 7372 bytes
- `build_sprite_source_manifest.py`: Python, 379 lines, 15018 bytes
- `capture-basket-link.ps1`: PowerShell, 16 lines, 511 bytes
- `capture-vnext-visuals.ps1`: PowerShell, 781 lines, 26739 bytes
- `capture-vnext-window.ps1`: PowerShell, 379 lines, 13077 bytes
- `clean_runtime_pet_frames.py`: Python, 135 lines, 4492 bytes
- `clean_shared_sprite_assets.py`: Python, 720 lines, 25743 bytes
- `compose_sprite_audit_board.py`: Python, 114 lines, 3752 bytes
- `copy-gemini-prompt.ps1`: PowerShell, 64 lines, 1897 bytes
- `desktop-hardening-probe.ps1`: PowerShell, 474 lines, 17071 bytes
- `desktop_bridge\Program.cs`: C#, 450 lines, 14835 bytes
- `drive-live-gemini.ps1`: PowerShell, 772 lines, 25116 bytes
- `dump-runtime-state.ps1`: PowerShell, 16 lines, 503 bytes

## Proposed Steps

- 1. Confirm current behavior: Use the report candidate files to inspect the smallest relevant code path before editing. Risk: Low.
- 2. Choose narrow write scope: Pick the minimum files required for the fix and avoid unrelated dirty work. Risk: Low.
- 3. Patch vNext code/UI: Apply the smallest C#/XAML change and add or update focused tests for the behavior. Risk: Medium.
- 4. Patch Godot behavior: Keep Godot changes isolated and validate controls or runtime behavior with a targeted proof. Risk: Medium.
- 5. Patch tooling: Keep scripts dry-run/no-mutation by default and preserve explicit safety checks. Risk: Medium.
- 6. Validate safely: Run focused tests first, then broader tests/builds with -SkipAssetPrep while visual cleanup is active. Risk: Low.
- 7. Record outcome: Write the result, artifacts, and residual risk to the implementation checklist or a phase report. Risk: Low.

## Validation Plan

- `dotnet build .\vnext\Wevito.VNext.sln`
- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build`
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests`
- `Run a targeted Godot/package proof for the touched behavior before calling the patch done.`
- `Run touched scripts in dry-run/no-mutation mode where available.`

## Rollback Plan

- Use git diff to review every touched code file before applying.
- Keep edits scoped so each file can be manually reverted without touching visual assets.
- If runtime proof fails, stop and revert only the patch files from this task.
- Do not use git reset --hard or checkout unrelated dirty files.

## Safety Gates

- No runtime/source PNG mutation.
- No visual generation or sprite import.
- No prop-anchor edits.
- No build asset preparation while visual cleanup is active; use -SkipAssetPrep.
- No destructive filesystem or git commands.
- Implementation requires explicit approval after this plan.

## Safety

This adapter only wrote markdown/JSON artifacts in a new pet-task folder. It did not edit code, run commands, mutate assets, or touch visual-side candidate/proof folders.
