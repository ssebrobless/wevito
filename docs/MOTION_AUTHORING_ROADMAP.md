# Motion Authoring Roadmap

Updated: 2026-03-15

```text
╔══════════════════ Motion Authoring Flow ══════════════════╗
║ canonical source sheet                                    ║
║   incoming_sprites/<species-age>.png                      ║
║        ▼                                                  ║
║ focused motion pack export                                ║
║   locomotion / care / expression                          ║
║        ▼                                                  ║
║ Gemini motion pass                                        ║
║   direct download only                                    ║
║        ▼                                                  ║
║ extract focused board from Gemini pack                    ║
║        ▼                                                  ║
║ import family board to sprites_authored_verified          ║
║        ▼                                                  ║
║ propagate 6 color variants                                ║
║        ▼                                                  ║
║ runtime preview + live shell audit                        ║
╚════════════════════════════════════════════════════════════╝
```

## Family Packs

- `locomotion`
  - `idle_00..03`
  - `walk_00..05`
- `care`
  - `eat_00..03`
  - `sleep_00..01`
  - goal: head-down food/water usable poses plus true resting poses
- `expression`
  - `happy_*`
  - `sad_*`
  - `sick_*`
  - `bathe_*`

## Species Intent

- `snake`
  - idle coiled
  - walk fully extended slither inside each frame
- `frog`
  - crouch idle
  - real hop with hind-leg extension and landing
- `crow`, `pigeon`, `goose`
  - grounded bird hop/walk
  - occasional tiny balancing wing motion
- `fox`, `rat`, `deer`
  - real quadruped stepping
  - visible front/rear leg timing
- `raccoon`, `squirrel`
  - upright neutral pose
  - all-fours walk

## Current Pipeline Commands

Prepare focused Gemini packs:

```powershell
python .\tools\prepare_motion_gemini_handoff.py --family locomotion_idle --species snake crow fox frog pigeon raccoon squirrel rat deer goose
python .\tools\prepare_motion_gemini_handoff.py --family locomotion_walk_a --species snake crow fox frog pigeon raccoon squirrel rat deer goose
python .\tools\prepare_motion_gemini_handoff.py --family locomotion_walk_b --species snake crow fox frog pigeon raccoon squirrel rat deer goose
python .\tools\prepare_motion_gemini_handoff.py --family care --species snake crow fox frog pigeon raccoon squirrel rat deer goose
```

Drive Gemini for a focused pack against the already-open logged-in Gemini window:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\drive-live-gemini.ps1 -Species snake -Age adult -Gender male -Family locomotion_walk_a -AuthUser "wbogusz24@gmail.com"
```

Import a focused Gemini result:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\import-gemini-result.ps1 -Species snake -Age adult -Gender male -Family locomotion_walk_a
```

## Recommended Species Order

1. `snake`
2. `frog`
3. `crow`
4. `pigeon`
5. `fox`
6. `rat`
7. `deer`
8. `raccoon`
9. `squirrel`
10. `goose`

## Notes

- Direct Gemini download is required. Do not use public share-link images as the source for import.
- Focused packs are preferred over the old full-board workflow because they keep Gemini anchored on one motion family at a time.
- The current runtime can render variable frame sizes, so species like `snake` can use wider walk frames while still returning to neutral idle frames.
- The clean authored override lane now lives under `C:\Users\fishe\Documents\projects\wevito\sprites_authored_verified`.
- Gemini pack extraction now supports richer upload packs that include an optional approved-reference panel before the editable board.
- The snake locomotion handoff now seeds an approved slither reference automatically, which fixed the earlier coiled-walk failure on female/teen/baby snake packs.
- Blur-prone species now use split locomotion families:
  - `locomotion_idle`
  - `locomotion_walk_a`
  - `locomotion_walk_b`
- The smaller split packs materially improved sharpness for `pigeon` and avoided the hollow-body failure from the old denser locomotion boards.
- `tools\drive-live-gemini.ps1` now defaults to reusing the already-open logged-in Gemini Chrome window. It only opens a new Chrome window when explicitly run with `-AllowLaunchFallback`.
- The live shell now uses verified authored `idle` and `walk` frames by default when they exist, while still falling back to generated runtime for unfinished families. Set `WEVITO_VNEXT_DISABLE_AUTHORED_LOCOMOTION=1` to compare against runtime-only output.
- Completed verified locomotion coverage:
  - `snake`: 6/6 age+gender base variants
  - `frog`: 6/6 age+gender base variants
  - `crow`: 6/6 age+gender base variants
  - `pigeon`: 6/6 age+gender base variants
  - `rat`: 6/6 age+gender base variants
  - `fox`: 6/6 age+gender base variants
  - `deer`: 6/6 age+gender base variants
  - `raccoon`: adult male/female complete, teen male idle complete
- Latest locomotion family coverage from `tools\report_authored_sprite_coverage.py`:
  - `locomotion_idle`: 270 / 360 color variants complete
  - `locomotion_walk_a`: 264 / 360 color variants complete
  - `locomotion_walk_b`: 264 / 360 color variants complete
  - full combined `locomotion`: 264 / 360 color variants complete
- Current locomotion resume queue:
  - `raccoon`
    - `teen male`: `locomotion_walk_a`, `locomotion_walk_b`
    - `teen female`: `locomotion_idle`, `locomotion_walk_a`, `locomotion_walk_b`
    - `baby male`: `locomotion_idle`, `locomotion_walk_a`, `locomotion_walk_b`
    - `baby female`: `locomotion_idle`, `locomotion_walk_a`, `locomotion_walk_b`
  - `squirrel`
    - `adult male/female`: `locomotion_idle`, `locomotion_walk_a`, `locomotion_walk_b`
    - `teen male/female`: `locomotion_idle`, `locomotion_walk_a`, `locomotion_walk_b`
    - `baby male/female`: `locomotion_idle`, `locomotion_walk_a`, `locomotion_walk_b`
  - `goose`
    - `adult male/female`: `locomotion_idle`, `locomotion_walk_a`, `locomotion_walk_b`
    - `teen male/female`: `locomotion_idle`, `locomotion_walk_a`, `locomotion_walk_b`
    - `baby male/female`: `locomotion_idle`, `locomotion_walk_a`, `locomotion_walk_b`
