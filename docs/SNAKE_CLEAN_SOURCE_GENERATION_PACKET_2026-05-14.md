# Snake Clean Source Generation Packet

```
goal
|
+-- generate clean source rows only
|   +-- no runtime PNG mutation yet
|   +-- no procedural/local fake art
|   `-- no broad snake rewrite
|
+-- review source rows
|   +-- full body in every frame
|   +-- consistent age/gender silhouette
|   +-- transparent/checkerboard-removable background
|   `-- no detached chunks, crop, boxed pixels, or afterimage trails
|
`-- apply later
    +-- import accepted rows
    +-- dry-run exact target scope
    +-- backup + hash + rollback drill
    `-- post-proof contact sheet/dev-controller audit
```

## Purpose

This packet is the exact generation brief for the remaining snake rows that are not safe to repair from existing source art.

The previous source packet proved that the repo does not currently contain clean source rows for these targets. Some alternate motion/care boards exist, but they are either the wrong pose family for the current low-profile snake direction or were blocked because one or more source frames were partial fragments.

This packet should be used to create new clean source rows. It does not approve applying anything to `sprites_runtime`.

## Source References To Attach

Attach these references when generating or manually repairing the rows:

- Current runtime evidence/contact sheet: `vnext/artifacts/snake-blocked-source-packet-20260514/snake-blocked-source-contact-sheet.png`
- Current source packet report: `docs/SNAKE_BLOCKED_SOURCE_PACKET_2026-05-14.md`
- Baby source identity reference: `incoming_sprites/snake baby.png`
- Teen source identity reference: `incoming_sprites/snake-teen.png`
- Adult source identity reference: `incoming_sprites/snake-adult.png`
- Existing snake runtime folder for style/scale comparison: `sprites_runtime/snake/`

## Global Art Rules

- Pixel-art-adjacent Wevito sprite style, not glossy illustration.
- Thick dark readable outline, limited palette, clean hard-edged transparency.
- Body should look like a snake, not a worm, dog, lizard, or eel.
- No fake PNG/checkerboard/white square background baked into the animal.
- No detached pixels, afterimages, motion trails, shadows, text, labels, or UI.
- No frame should be cropped by the canvas.
- All frames in a row must look like the same individual animal.
- Avoid size pulsing: the snake may stretch for slither motion, but the body mass should remain consistent.
- Blue source is the first review target. Other colors should be derived only after the blue row is accepted.
- For snake movement, favor low-profile slither/extension over upright coil bobbing unless the target explicitly describes a resting/eating pose.

## Required Source Rows

### 1. `snake / baby / female / blue / sad`

Frame count: `2`

Prompt:

```text
Create a clean two-frame pixel-art sprite row for Wevito: snake, baby, female, blue color variant, sad state.

Use the current baby snake identity: tiny low-profile body, rounded head, dark outline, blue palette, simple readable pixels. Frame 00 should show the baby snake drooping slightly with a tired/sad posture. Frame 01 should be a subtle second sad pose with the same complete body and same scale. No tears unless attached to the face and very small. No floating symbols, shadows, floor, text, labels, checkerboard, or background.

Every frame must show the complete snake body. Do not crop the head or tail. Do not create detached tail fragments. Keep transparent-background-friendly edges and no loose pixels.
```

Acceptance:

- `sad_00` and `sad_01` are both complete full-body baby snakes.
- `sad_01` is not a tail-only fragment.

### 2. `snake / baby / female / blue / sick`

Frame count: `4`

Prompt:

```text
Create a clean four-frame pixel-art sprite row for Wevito: snake, baby, female, blue color variant, sick state.

Use the current baby snake identity: tiny low-profile body, rounded head, dark outline, blue palette, simple readable pixels. The row should show the baby snake looking weak and sick through subtle posture changes: lowered head, curled body, small wobble, or tired eyes. Keep all four frames full-body and consistent. No floating symbols, detached droplets, shadows, floor, text, labels, checkerboard, or background.

Every frame must show the complete snake body. Do not crop the head or tail. Do not produce any partial-body frame. Keep transparent-background-friendly edges and no loose pixels.
```

Acceptance:

- `sick_00..03` are all complete baby snake bodies.
- `sick_02` is not a partial body fragment.

### 3. `snake / teen / female / blue / happy`

Frame count: `4`

Prompt:

```text
Create a clean four-frame pixel-art sprite row for Wevito: snake, teen, female, blue color variant, happy state.

Use the current teen snake identity: medium-small low-profile body, dark outline, blue palette, slightly longer than baby, still clearly smaller than adult. The row should show a cheerful snake through a gentle head lift, tiny body bounce, or relaxed happy curve. Keep the body mass and length consistent across all four frames. No floating hearts, sparkles, symbols, shadows, floor, text, labels, checkerboard, or background.

Every frame must show the complete snake body. Do not crop the head or tail. Do not create a tiny partial first frame. Keep transparent-background-friendly edges and no loose pixels.
```

Acceptance:

- `happy_00..03` are all complete teen snake bodies.
- `happy_00` is not a partial body fragment.

### 4. `snake / adult / female / blue / idle`

Frame count: `4`

Prompt:

```text
Create a clean four-frame pixel-art sprite row for Wevito: snake, adult, female, blue color variant, idle state.

Use the current adult female snake direction: long low-profile body, not a tall upright coil. The row should be calm idle breathing: slight head/neck lift, subtle body curve shift, same body mass, same scale. This should read as a snake resting on the ground and ready to slither. Do not use an upright cobra/coiled pose for this row. No shadows, floor, text, labels, checkerboard, or background.

Every frame must show the complete snake body with head and tail visible. No grid fragments, no boxed partial source, no detached pixels.
```

Acceptance:

- `idle_00..03` are low-profile complete adult female snake bodies.
- No frame contains grid/box residue.

### 5. `snake / adult / female / blue / walk`

Frame count: `6`

Prompt:

```text
Create a clean six-frame pixel-art sprite row for Wevito: snake, adult, female, blue color variant, walk/slither state.

Use the current adult female snake direction: long low-profile body, dark outline, blue palette. Show real slithering by shifting S-curves through the body while the head faces the movement direction. The snake may extend and contract naturally, but body mass and scale should remain consistent. Avoid looking like a static image sliding. Do not use upright coil bobbing. No speed lines, shadows, floor, text, labels, checkerboard, or background.

Every frame must show the complete snake body with no cropped head or tail. No partial body fragments.
```

Acceptance:

- `walk_00..05` form a readable slither cycle.
- The snake faces movement direction and does not shrink/grow unnaturally.

### 6. `snake / adult / female / blue / eat`

Frame count: `4`

Prompt:

```text
Create a clean four-frame pixel-art sprite row for Wevito: snake, adult, female, blue color variant, eat state.

Use the current adult female snake identity: blue palette, dark outline, long snake body. The row should show eating in a simple readable way without adding a separate food prop: head dips forward, mouth/neck posture changes slightly, body remains grounded. A coiled pose is acceptable only if all frames are consistent with the adult female identity and not taller than the rest state. No loose food pixels, shadows, floor, text, labels, checkerboard, or background.

Every frame must show the complete snake body. Do not crop or fragment frame 02.
```

Acceptance:

- `eat_00..03` are complete adult female snake bodies.
- `eat_02` is not a partial body fragment.

### 7. `snake / adult / female / blue / happy`

Frame count: `4`

Prompt:

```text
Create a clean four-frame pixel-art sprite row for Wevito: snake, adult, female, blue color variant, happy state.

Use the current adult female snake identity: long low-profile body, blue palette, dark outline. Show a happy/relaxed snake through subtle head lift, gentle curve, or brighter expression. Keep scale and body mass consistent. No floating hearts, sparkles, symbols, shadows, floor, text, labels, checkerboard, or background.

Every frame must show the complete snake body. Do not create any partial final frame.
```

Acceptance:

- `happy_00..03` are all complete adult female snake bodies.
- `happy_03` is not a partial body fragment.

### 8. `snake / adult / female / blue / sick`

Frame count: `4`

Prompt:

```text
Create a clean four-frame pixel-art sprite row for Wevito: snake, adult, female, blue color variant, sick state.

Use the current adult female snake identity: long low-profile body, blue palette, dark outline. Show sickness through drooping head, low energy curve, slight body slump, or tired eyes. Keep the snake fully visible and grounded. No floating symbols, detached droplets, shadows, floor, text, labels, checkerboard, or background.

Every frame must show the complete snake body. Do not create a partial first frame.
```

Acceptance:

- `sick_00..03` are all complete adult female snake bodies.
- `sick_00` is not a partial body fragment.

### 9. `snake / adult / female / blue / bathe`

Frame count: `4`

Prompt:

```text
Create a clean four-frame pixel-art sprite row for Wevito: snake, adult, female, blue color variant, bathe state.

Use the current adult female snake identity: long low-profile body, blue palette, dark outline. Show bathing/grooming as a subtle clean/wet wiggle or head/body wipe posture without adding separate water puddles or detached bubbles. The body should remain fully visible and consistent. No detached water chunks, shadows, floor, text, labels, checkerboard, or background.

Every frame must show the complete snake body. Do not fragment frame 01.
```

Acceptance:

- `bathe_00..03` are all complete adult female snake bodies.
- `bathe_01` is not a partial body fragment and has no detached water/noise chunk.

### 10. `snake / adult / male / blue / walk`

Frame count: `6`

Prompt:

```text
Create a clean six-frame pixel-art sprite row for Wevito: snake, adult, male, blue color variant, walk/slither state.

Use the current adult male snake direction: long low-profile body, slightly heavier/darker than female if needed, dark outline, blue runtime palette. Show real slithering by shifting S-curves through the body while the head faces the movement direction. The snake may extend and contract naturally, but body mass and scale should remain consistent. Do not use upright coil bobbing. No speed lines, shadows, floor, text, labels, checkerboard, or background.

Every frame must show the complete snake body with no cropped head or tail. No partial body fragments.
```

Acceptance:

- `walk_00..05` form a readable adult male slither cycle.
- No frame is a partial body fragment.

### 11. `snake / adult / male / blue / sick`

Frame count: `4`

Prompt:

```text
Create a clean four-frame pixel-art sprite row for Wevito: snake, adult, male, blue color variant, sick state.

Use the current adult male snake identity: long low-profile body, dark outline, blue runtime palette. Show sickness through drooping head, low energy curve, slight body slump, or tired eyes. Keep the snake fully visible and grounded. No floating symbols, detached droplets, shadows, floor, text, labels, checkerboard, or background.

Every frame must show the complete snake body. Do not create thin line fragments or partial middle frames.
```

Acceptance:

- `sick_00..03` are all complete adult male snake bodies.
- `sick_01` and `sick_02` are not line/body fragments.

## Import And Apply Instructions After Source Exists

1. Save generated/repaired source rows in a new timestamped source packet folder, not directly into `sprites_runtime`.
2. Produce a contact sheet comparing current runtime blue frames against candidate blue frames.
3. Review the candidate rows manually before mutation.
4. Run a dry-run apply that confirms exact target scope:
   - only `sprites_runtime/snake/<age>/<gender>/<color>/<family>_NN.png`
   - all six colors only for accepted rows
   - no other species, no source-board edits, no prop anchor edits
5. Apply with backup-before-apply and SHA-256 pre/post hashes.
6. Run rollback drill and re-apply if rollback succeeds.
7. Run:

```powershell
python .\tools\report_runtime_canvas_mismatches.py --output .\vnext\artifacts\<run>\runtime-canvas.json --markdown .\vnext\artifacts\<run>\runtime-canvas.md --fail-on-mismatch
python .\tools\audit_sprite_contract.py --output .\vnext\artifacts\<run>\sprite-contract.json
dotnet build .\vnext\Wevito.VNext.sln
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
```

8. Refresh the local main build only after merge.
