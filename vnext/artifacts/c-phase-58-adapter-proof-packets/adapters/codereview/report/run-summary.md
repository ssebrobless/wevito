# PET TASKS Code Review Report

Generated: 2026-05-12T20:57:54.3673173+00:00
TaskCard: `da69a744-e791-4bdd-8b4b-40a65be0f4f4`
Intent: review the code in Wevito.VNext.Core

## Summary

- Files scanned: 80
- Findings: 200
- Did mutate code: false

## Files

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

## Findings

- `game_manager.gd` line 143: trailing_whitespace - Line ends with whitespace.
- `game_manager.gd` line 146: trailing_whitespace - Line ends with whitespace.
- `game_manager.gd` line 155: trailing_whitespace - Line ends with whitespace.
- `game_manager.gd` line 158: trailing_whitespace - Line ends with whitespace.
- `game_manager.gd` line 161: trailing_whitespace - Line ends with whitespace.
- `game_manager.gd` line 164: trailing_whitespace - Line ends with whitespace.
- `game_manager.gd` line 170: trailing_whitespace - Line ends with whitespace.
- `game_manager.gd` line 173: trailing_whitespace - Line ends with whitespace.
- `game_manager.gd` line 182: trailing_whitespace - Line ends with whitespace.
- `game_manager.gd` line 185: trailing_whitespace - Line ends with whitespace.
- `game_manager.gd` line 188: trailing_whitespace - Line ends with whitespace.
- `game_manager.gd` line 191: trailing_whitespace - Line ends with whitespace.
- `game_manager.gd` line 204: trailing_whitespace - Line ends with whitespace.
- `game_manager.gd` line 221: trailing_whitespace - Line ends with whitespace.
- `game_manager.gd` line 226: trailing_whitespace - Line ends with whitespace.
- `game_manager.gd` line 229: trailing_whitespace - Line ends with whitespace.
- `game_manager.gd` line 233: trailing_whitespace - Line ends with whitespace.
- `game_manager.gd` line 276: trailing_whitespace - Line ends with whitespace.
- `game_manager.gd` line 280: trailing_whitespace - Line ends with whitespace.
- `game_manager.gd` line 283: trailing_whitespace - Line ends with whitespace.
- `game_manager.gd` line 286: trailing_whitespace - Line ends with whitespace.
- `game_manager.gd` line 295: trailing_whitespace - Line ends with whitespace.
- `game_manager.gd` line 298: trailing_whitespace - Line ends with whitespace.
- `game_manager.gd` line 301: trailing_whitespace - Line ends with whitespace.
- `game_manager.gd` line 304: trailing_whitespace - Line ends with whitespace.
- `game_manager.gd` line 370: trailing_whitespace - Line ends with whitespace.
- `game_manager.gd` line 374: trailing_whitespace - Line ends with whitespace.
- `game_manager.gd` line 377: trailing_whitespace - Line ends with whitespace.
- `game_manager.gd` line 392: trailing_whitespace - Line ends with whitespace.
- `game_manager.gd` line 395: trailing_whitespace - Line ends with whitespace.
- `game_manager.gd` line 399: trailing_whitespace - Line ends with whitespace.
- `game_manager.gd` line 403: trailing_whitespace - Line ends with whitespace.
- `game_manager.gd` line 436: trailing_whitespace - Line ends with whitespace.
- `game_manager.gd` line 448: trailing_whitespace - Line ends with whitespace.
- `game_manager.gd` line 467: trailing_whitespace - Line ends with whitespace.
- `game_manager.gd` line 473: trailing_whitespace - Line ends with whitespace.
- `game_manager.gd` line 476: trailing_whitespace - Line ends with whitespace.
- `game_manager.gd` line 483: trailing_whitespace - Line ends with whitespace.
- `game_manager.gd` line 489: trailing_whitespace - Line ends with whitespace.
- `game_manager.gd` line 491: trailing_whitespace - Line ends with whitespace.

## Suggested Tests

- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build`
- `Run the relevant script/probe manually in dry-run or no-mutation mode.`

## Safety

This adapter only wrote markdown/JSON artifacts in a new pet-task folder. It did not edit code, run commands, mutate assets, or touch visual-side candidate/proof folders.
