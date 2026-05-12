# PET TASKS Audio Boost Handoff

Generated: 2026-05-12T20:58:58.9126473+00:00
TaskCard: `cbbae9d5-b91c-4caa-98df-aff81f5dc500`

## Summary

- Wevito did not install software, edit APO/driver configuration, or request elevation.
- This guide only reports detected external tools and safe setup direction.

## Tool Status

### FxSound

- Status: NotInstalled
- Official URL: https://www.fxsound.com/
- Detail: External enhancer handoff only; Wevito does not install or configure FxSound.
- Evidence:
  - No FxSound registry/process/path evidence found.

### Equalizer APO

- Status: NotInstalled
- Official URL: https://sourceforge.net/projects/equalizerapo/
- Detail: APO handoff only; Wevito never edits Equalizer APO config.txt.
- Evidence:
  - No Equalizer APO config path evidence found.

## Setup Guidance

- FxSound: install only from the official FxSound site if you choose to use it. Reboot may be required for audio effects to settle.
- Equalizer APO: install from SourceForge, then configure manually at `C:\Program Files\EqualizerAPO\config\config.txt`. Wevito will not edit this file.
- Keep gain conservative. If audio distorts, reduce gain immediately.

## Safety Notes

- No installer was launched.
- No driver, APO, enhancer, or Equalizer APO config was modified.
- No elevation was requested.
- True system-wide boosting should be handled by explicit external tools, not silent Wevito automation.
- Safe-listening reminder: WHO guidance warns that high volume over time can damage hearing; keep levels modest.
- Loudness ceiling reminder: EBU R 128 style mastering typically uses a -1 dBTP true-peak ceiling to avoid clipping.
