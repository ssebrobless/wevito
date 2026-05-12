# C-PHASE 54 Post-Stable Baseline Lock

Date: 2026-05-12

Branch: `claude-implementation/c-phase-54-post-stable-baseline-lock`

## Purpose

Protect the promoted `v0.1.0-desktop` stable baseline from accidental asset-prep regeneration while keeping the safe `-SkipAssetPrep` vNext build path available.

## Stable Release Lock

Lock file:

```text
vnext/content/stable_release_lock.json
```

Recorded stable baseline:

```text
stableTag: v0.1.0-desktop
stableAsset: WevitoDesktopPet-v0.1.0-desktop-win64.zip
stableSha256: c12a61d0ddd55bfa611bc14480dd298584e2e9d3849ea8e01064beb71c4efacc
sourceRcTag: v0.1.0-desktop-rc4
targetCommitish: 97285ad9887423fc42cc3b562087180ff0d8f90e
lockedRuntimeRoots: sprites_runtime, sprites_shared_runtime
```

Closed gates preserved by this phase:

```text
live_model_calls
broad_sprite_mutation
visual_generation_import
asset_prep_regeneration
prop_anchor_edits
all_color_propagation
screen_recording
external_audio_booster_control
automatic_training_data_promotion
```

## Build Guard

Guarded file:

```text
tools/build-vnext.ps1
```

New switch:

```powershell
-AllowAssetPrepAfterStable
```

Default behavior:

- `tools/build-vnext.ps1` fails before asset prep if `vnext/content/stable_release_lock.json` exists and `-SkipAssetPrep` is not passed.
- `-SkipAssetPrep` remains the normal safe post-stable build path.
- `-AllowAssetPrepAfterStable` exists only for future explicitly approved asset-prep phases.

Guard failure text:

```text
Stable release lock is present. Re-run with -SkipAssetPrep, or pass -AllowAssetPrepAfterStable only in an approved asset-prep phase.
```

## Baseline Assertion

New script:

```text
tools/assert-stable-baseline.ps1
```

Artifact output:

```text
vnext/artifacts/c-phase-54-post-stable-baseline-lock/
```

Generated proof files:

```text
runtime-canvas.json
runtime-canvas.md
sprite-contract.json
optional-readiness.json
optional-readiness.md
stable-baseline-summary.json
```

Baseline assertion summary:

| Check | Result |
| --- | --- |
| Stable tag is `v0.1.0-desktop` | PASS |
| Stable SHA256 matches promoted release | PASS |
| Runtime canvas audit | PASS |
| Sprite contract audit | PASS |
| Optional readiness audit | PASS |

Runtime canvas summary:

```text
checked_sequences=2880
checked_frames=10800
mismatch_count=0
canonical_mismatch_count=3852
missing_count=0
invalid_count=0
```

Sprite contract summary:

```text
source_boards_found=30 / 30
supporting_inputs_found=17 / 17
runtime_variant_dirs_found=360 / 360
runtime_frames_found=10818 / 10800
error_count=0
```

Optional readiness summary:

```text
passed=true
target_count=2520
authored_complete=0
runtime_prop_anchor_supported=0
fallback_only=2516
invalid_optional_art=0
error_count=0
```

The optional readiness result preserves the known post-stable caveat: optional action animation coverage is safe but mostly fallback-only.

## Validation

| Command | Expected | Result |
| --- | --- | --- |
| `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipTests` | FAIL with stable release lock message | PASS |
| `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests` | PASS | PASS |
| `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\assert-stable-baseline.ps1` | PASS | PASS |
| `dotnet build .\vnext\Wevito.VNext.sln` | PASS | PASS |
| `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build` | PASS | PASS, 280 / 280 |

Note: an initial parallel `dotnet test --no-build` attempt raced the fresh build output and reported the test DLL missing. The same command was rerun sequentially after the build completed and passed `280 / 280`.

## Mutation Scope

No sprites, source boards, runtime PNGs, shared runtime assets, release assets, prop anchors, model settings, or generated/imported art were modified.

Changed files:

```text
vnext/content/stable_release_lock.json
tools/assert-stable-baseline.ps1
tools/build-vnext.ps1
docs/C_PHASE54_POST_STABLE_BASELINE_LOCK_2026-05-12.md
```

## Next Phase

Next planned phase is C-PHASE 55, but it must start from a separate branch after this phase is reviewed and merged.
