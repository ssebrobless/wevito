# Rat Authored Animation Pilot

Updated: 2026-03-11

```text
╔══════════════════════ Rat Pilot Workflow ══════════════════════╗
║ canonical source pose                                          ║
║   base-pose.png                                                ║
║       ▼                                                        ║
║ editable frame board                                           ║
║   editable-board.png                                           ║
║       ▼                                                        ║
║ image edit prompt                                              ║
║   improve idle / walk only                                     ║
║   preserve uploaded rat identity                               ║
║       ▼                                                        ║
║ import edited board                                            ║
║   import_authored_animation_board.py                           ║
║       ▼                                                        ║
║ expand colors                                                  ║
║   propagate_authored_colors.py                                 ║
║       ▼                                                        ║
║ runtime + visual harness                                       ║
╚═════════════════════════════════════════════════════════════════╝
```

## Purpose

These prompts are for the first authored-animation pilot using the canonical uploaded rat sprites.

Do not redesign the character. The goal is to animate the exact uploaded rat look more convincingly than the synthesized runtime motion.

## Files To Use

Adult male:
- base pose: `C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\authoring-packs\rat\adult\male\base-pose.png`
- editable board: `C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\authoring-packs\rat\adult\male\editable-board.png`
- runtime reference: `C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\authoring-packs\rat\adult\male\runtime-reference-blue.png`

Adult female:
- base pose: `C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\authoring-packs\rat\adult\female\base-pose.png`
- editable board: `C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\authoring-packs\rat\adult\female\editable-board.png`
- runtime reference: `C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\authoring-packs\rat\adult\female\runtime-reference-blue.png`

Apply the same pattern later to teen and baby.

## Global Edit Rules

- image only
- no text response
- no explanations
- preserve transparent-looking sprite content inside each frame area
- do not change the rat’s design identity
- keep the exact same side-facing orientation
- keep pixel art crisp
- no blur
- no anti-aliasing
- do not repaint the entire board in a new style
- keep the rat centered consistently in every frame
- keep feet and tail contact stable relative to the ground plane
- male stays slightly larger and bolder
- female stays slightly smaller and calmer

## Prompt: Adult Male Idle + Walk Pass

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

You are editing an existing pixel-art animation board for a small desktop virtual pet game.

Use the uploaded canonical rat source pose as the design anchor.
Preserve the exact uploaded rat identity, proportions, face, ears, tail, outline weight, and general shading style.
Do not redesign the rat.

You are editing the uploaded labeled frame board.
Keep the board layout, frame positions, and labels intact.
Do not remove labels.
Do not add new labels.
Do not add extra frames.
Do not change the board dimensions.

Only improve the motion in these frames:
- idle_00
- idle_01
- idle_02
- idle_03
- walk_00
- walk_01
- walk_02
- walk_03
- walk_04
- walk_05

Leave all other frames visually unchanged for now.

Animation goals:
- idle should be subtle, alive, and readable
- use tiny breathing, head, whisker, ear, and blink changes only
- walk should read as a grounded side-view rat walk
- feet placement should alternate clearly
- body weight should shift naturally
- tail should follow the motion slightly but stay controlled
- avoid jitter
- keep the baseline stable

Style goals:
- pixel art only
- crisp edges
- no blur
- no anti-aliasing
- slightly moody, cute, vulnerable tone
- preserve the original uploaded rat appearance exactly

Important:
- do not make the motion exaggerated
- do not squash or stretch wildly
- do not make the rat bounce unnaturally
- do not alter the untouched frames

Return only the edited image.
```

## Prompt: Adult Female Idle + Walk Pass

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

You are editing an existing pixel-art animation board for a small desktop virtual pet game.

Use the uploaded canonical rat source pose as the design anchor.
Preserve the exact uploaded rat identity, proportions, face, ears, tail, outline weight, and general shading style.
Do not redesign the rat.

You are editing the uploaded labeled frame board.
Keep the board layout, frame positions, and labels intact.
Do not remove labels.
Do not add new labels.
Do not add extra frames.
Do not change the board dimensions.

Only improve the motion in these frames:
- idle_00
- idle_01
- idle_02
- idle_03
- walk_00
- walk_01
- walk_02
- walk_03
- walk_04
- walk_05

Leave all other frames visually unchanged for now.

Animation goals:
- idle should be subtle, calm, and alive
- use tiny breathing, head, whisker, ear, and blink changes only
- walk should read as a grounded side-view rat walk
- feet placement should alternate clearly
- body weight should shift naturally
- tail should follow the motion slightly but stay controlled
- avoid jitter
- keep the baseline stable

Style goals:
- pixel art only
- crisp edges
- no blur
- no anti-aliasing
- slightly moody, cute, vulnerable tone
- preserve the original uploaded rat appearance exactly
- female should stay slightly smaller and calmer than male

Important:
- do not make the motion exaggerated
- do not squash or stretch wildly
- do not make the rat bounce unnaturally
- do not alter the untouched frames

Return only the edited image.
```

## After Edit

Import the edited board back into authored overrides:

```powershell
python .\tools\import_authored_animation_board.py --board "<edited-board.png>" --output-dir ".\sprites_authored\rat\adult\male\blue" --species rat --age-stage adult
python .\tools\propagate_authored_colors.py --source-dir ".\sprites_authored\rat\adult\male\blue" --output-root ".\sprites_authored"
```

Then validate with:

```powershell
python .\tools\render_runtime_sprite_previews.py --sprite-root .\sprites_runtime --authored-root .\sprites_authored --output-root .\vnext\artifacts\sprite-previews-authored-check
powershell -ExecutionPolicy Bypass -File .\tools\capture-vnext-visuals.ps1 -Configuration Debug
```
