# Code-Side Phase 37: Audio Assist Status Preview

Generated: 2026-05-05

## Shape

```text
PET TASKS text
   │
   ▼
PetCommandParser
   │  audio / volume / mute / boost
   ▼
audioAssist task card
   │
   ▼
AudioAssistPreviewAdapter
   │
   ├──▶ markdown report
   └──▶ JSON report

No audio mutation. No sprite mutation. No provider/network call.
```

## What Changed

- Added the `audioAssist` PET TASKS tool family.
- Added audio action vocabulary for inspect, set volume, mute, unmute, boost guidance, and external enhancer handoff.
- Added a read-only `AudioAssistPreviewAdapter`.
- Routed audio/volume commands through PET TASKS preview dispatch.
- Added `audioAssist status` to the compact tool popup capability line.
- Kept all actual audio changes locked. The first report is policy/status only.

## Current Truth

- `audioAssist` can be assigned through the PET TASKS command bar.
- It writes a timestamped markdown + JSON report under `vnext\artifacts\pet-tasks`.
- It does not inspect the real Windows audio endpoint yet.
- It does not change volume, mute state, drivers, APOs, equalizers, or files.
- External volume-boost/enhancer work remains handoff-only.

## Validation

- Focused audio/parser/dispatcher/contract tests: passed `40 / 40`.
- `dotnet build .\vnext\Wevito.VNext.sln`: passed with `0` warnings and `0` errors.
- Full vNext tests: passed `136 / 136`.
- Safe Debug publish with `-SkipAssetPrep -SkipTests`: passed.
- Live `audioAssist` PET TASKS UI probe: passed.
- Probe confirmed target sprite row hashes were unchanged.

## Artifacts

- Probe summary: `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\pet-task-probes\20260505-142735-988-0763db87\summary.json`
- PET TASKS report: `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\pet-tasks\20260505-182749-audioassist-audioassist\run-summary.md`

## Next Safe Step

Add real read-only Windows endpoint inspection so `audioAssist` can report current master volume and mute state. Keep it non-mutating and test it behind a fakeable reader seam before any future set-volume/mute execution work.
