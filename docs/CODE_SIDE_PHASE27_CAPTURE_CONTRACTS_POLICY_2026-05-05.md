# Code-Side Phase 27 Capture Contracts And Policy - 2026-05-05

## Goal

Prepare screenshot and screen-capture support without performing any capture yet.

The phase adds the vocabulary and safety policy needed before Wevito can expose capture tools through PET TASKS.

```text
Capture request
   |
   v
CapturePolicyEvaluator
   |
   +-- allow low-risk Wevito-only still capture
   +-- require approval for region/window/desktop/recording
   +-- block external upload/share
```

## Files Changed

- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Contracts\PetAgentContracts.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Core\CapturePolicyEvaluator.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\tests\Wevito.VNext.Tests\PetAgentContractTests.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\tests\Wevito.VNext.Tests\CapturePolicyEvaluatorTests.cs`

Supporting research/planning:

- `C:\Users\fishe\.codex\worktrees\36d6\wevito\docs\SCREEN_CAPTURE_TOOLING_RESEARCH_PLAN_2026-05-05.md`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\docs\WEVITO_TOOL_HUB_INFORMATION_ARCHITECTURE_2026-05-05.md`

## Contract Additions

Enums:

- `CapturePreset`
- `CaptureTargetKind`
- `CaptureOutputKind`
- `CapturePrivacyLevel`

Records:

- `CaptureRegion`
- `CaptureRequest`
- `CapturePolicyDecision`
- `CaptureManifest`
- `CaptureResult`

## Policy Rules

```text
Wevito window still capture       -> allowed, low risk
Proof surface still capture       -> allowed, low risk
Selected region / last region     -> approval required, medium risk
Foreground window                 -> approval required, medium risk
Full desktop                      -> approval required, high risk
MP4/GIF recording                 -> approval required, medium risk
External upload/share             -> blocked
```

## Validation

```text
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "FullyQualifiedName~CapturePolicyEvaluatorTests|FullyQualifiedName~PetAgentContractTests"
```

Result: passed `15 / 15`.

```text
dotnet build .\vnext\Wevito.VNext.sln
```

Result: passed, `0` warnings, `0` errors.

```text
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
```

Result: passed `99 / 99`.

```text
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
```

Result: passed.

## Audit

Pass.

- Correctness: capture risk ladder matches the research plan and master plan.
- Safety: no screen capture, recording, upload, or mutation was implemented.
- Maintainability: capture contracts are separated from execution/backend implementation.
- Runtime compatibility: build, full tests, and safe publish passed.
- Visual-side coordination: no sprite or asset pipeline files were touched.

## Next Phase Recommendation

Proceed to Phase 28: Wevito-window screenshot only.

Phase 28 should stay narrow:

- only capture a Wevito-owned window/proof surface,
- write `screenshot.png`, `manifest.json`, and `run-summary.md`,
- store output under a new timestamped PET TASKS artifact folder,
- do not capture the full desktop,
- do not record video,
- do not upload/share.
