# Code-Side Phase 38: Audio Assist Windows Status Inspection

Generated: 2026-05-05

## Shape

```text
PET TASKS audioAssist preview
   │
   ▼
AudioAssistPreviewAdapter
   │
   ▼
IAudioEndpointStatusReader
   │
   ├── tests ──▶ fake endpoint status
   │
   └── live  ──▶ Windows Core Audio default render endpoint
                 │
                 ├── master volume %
                 └── mute state

Read only. No volume change. No mute change. No asset mutation.
```

## What Changed

- Added `AudioEndpointStatus` to PET TASKS contracts.
- Added a fakeable `IAudioEndpointStatusReader` seam.
- Added `WindowsAudioEndpointStatusReader` using Windows Core Audio COM APIs for default output endpoint inspection.
- Updated `AudioAssistPreviewAdapter` so reports include endpoint availability, volume percentage, mute state, endpoint id, and detail.
- Updated audioAssist tests to inject a fake endpoint reader instead of depending on the local machine audio device.

## Current Runtime Behavior

- `check audio volume` now creates a PET TASKS report with the actual Windows default output volume/mute state when available.
- The report remains dry-run/read-only.
- `SetVolume`, `Mute`, and `Unmute` remain approval-gated future actions.
- External volume boosters/equalizers remain blocked/handoff-only.

## Validation

- Focused audio/parser/dispatcher/contract tests: passed `40 / 40`.
- `dotnet build .\vnext\Wevito.VNext.sln`: passed with `0` warnings and `0` errors.
- Full vNext tests: passed `136 / 136`.
- Safe Debug publish with `-SkipAssetPrep -SkipTests`: passed.
- Live `audioAssist` PET TASKS UI probe: passed.
- Probe confirmed target sprite row hashes were unchanged.

## Live Probe Result

- Probe summary: `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\pet-task-probes\20260505-143255-067-9a9f88c0\summary.json`
- PET TASKS report: `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\pet-tasks\20260505-183308-audioassist-audioassist\run-summary.md`
- Reported default output volume: `100%`
- Reported muted: `False`
- Audio changed: `False`
- Sprite row hashes unchanged: `True`

## Next Safe Step

Add an explicitly approval-gated execution adapter for normal Windows volume/mute changes only. Keep it capped to normal endpoint volume, never install drivers/APOs/equalizers, and keep external boost workflows as guidance/handoff.
