# Gemini Prop And UI Prompt Pack

Use these prompts to generate simple reusable game art that can be downloaded, staged in `incoming_sprites`, and cleaned up locally.

Prompt design goals:
- self-contained
- no assumption that Gemini knows the project name
- focused on isolated props, simple boards, or small grouped sets
- easy to crop into individual assets later

Global rule for every prompt in this file:
- when the prompt says `board`, it means a simple evenly spaced sprite layout only
- it does NOT mean a literal wooden board, cutting board, tray, table, shelf, platter, or display surface
- unless a prompt explicitly asks for a habitat component cluster, every object should be an isolated sprite on transparent background
- do not place all objects on one shared surface

## Recommended Order

1. Egg hatch lifecycle
2. Environments set A
3. Environments set B
4. Food set: omnivore and scavenger
5. Food set: herbivore and grazer
6. Food set: birds and seed eaters
7. Food set: predator and reptile
8. Water and feeding containers
9. Medicine and care items
10. Toys and enrichment set A
11. Toys and enrichment set B
12. Utility and shelter props
13. Status effect icons
14. Large action buttons
15. Small UI icon refresh

## Save Name Suggestions

- `egg-hatch-lifecycle.png`
- `environments-set-a.png`
- `environments-set-b.png`
- `food-set-omnivore.png`
- `food-set-herbivore.png`
- `food-set-birds.png`
- `food-set-predator.png`
- `containers-set.png`
- `care-items-set.png`
- `toy-set-a.png`
- `toy-set-b.png`
- `utility-props-set.png`
- `status-effects-set.png`
- `action-buttons-set.png`
- `ui-icons-set.png`

## 1. Egg Hatch Lifecycle

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one clean sprite layout reference for a small 2D desktop virtual pet game showing a neutral egg hatching sequence.

The egg should be a plain base egg that can later be recolored locally into multiple color variants.
Do not bake in a strong species identity.
Do not add text labels.

Board rules:
- transparent background preferred
- one single board only
- simple evenly spaced sprite layout only, not a literal object
- 6 cells arranged in one row
- equal spacing between cells
- each stage clearly separated
- no decorative frame
- no poster layout
- no extra objects besides the egg stages
- no wooden board
- no tray
- no table
- no shelf

Stages from left to right:
1. intact egg
2. slightly cracked egg
3. larger crack
4. long crack but still not opened
5. cracked open egg with visible shell break
6. cracked empty shell with nothing inside

Style:
- pixel art
- crisp edges
- slightly moody, cute, vulnerable desktop-pet tone
- readable at small size
- simple clean silhouette
- base egg should be neutral enough to tint into red, orange, yellow, blue, indigo, and violet later

Important:
- do not include a baby animal in this image
- this board is only the egg and shell sequence
- keep the egg shape consistent across stages
- make the cracking progression visually clear

Return only the generated image.
```

## 2. Environments Set A

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one clean sprite layout reference for a small 2D desktop virtual pet game showing isolated habitat environment components only.

Board rules:
- transparent background strongly preferred
- one single board only
- 2 columns x 3 rows
- 6 total environment thumbnails
- each environment component clearly separated
- no text labels
- no decorative poster frame
- no sky
- no ground backdrop painting
- no room wallpaper
- no large scenic background
- no animals anywhere in the image
- only environment pieces and habitat props

Generate these 6 isolated habitat component sets:
1. rat habitat components: cozy nest, bedding pile, scraps, little hide box
2. crow habitat components: branch perch, twig cluster, shiny trinket stash
3. fox habitat components: den opening, brush pile, forest floor props
4. snake habitat components: warm flat stone, grass clump, low brush, basking spot
5. deer habitat components: meadow grass tufts, stump, flower patch
6. frog habitat components: reeds, lily pad cluster, damp rock, puddle edge props

Style:
- pixel art
- crisp edges
- slightly moody, cozy, vulnerable desktop-pet tone
- keep each slot like a prop cluster, not a full illustrated scene
- keep layouts readable and crop-friendly

Return only the generated image.
```

## 3. Environments Set B

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one clean sprite layout reference for a small 2D desktop virtual pet game showing isolated habitat environment components only.

Board rules:
- transparent background strongly preferred
- one single board only
- 2 columns x 2 rows plus 2 smaller utility habitat slots if needed, but keep the board clean and crop-friendly
- no text labels
- no decorative poster frame
- no sky
- no ground backdrop painting
- no room wallpaper
- no large scenic background
- no animals anywhere in the image
- only environment pieces and habitat props

Generate these isolated habitat component sets:
1. pigeon habitat components: rooftop ledge, urban perch, pebble cluster
2. raccoon habitat components: crate hideout, alley clutter props, scavenger stash
3. squirrel habitat components: branch, stump, acorn pile, leaf litter
4. goose habitat components: pond bank props, shallow water edge pieces, grass tuft cluster
5. generic indoor pet room components: rug, cushion, small shelf, window ledge props
6. generic calm-night components: moonlit stone, lantern, soft ground props

Style:
- pixel art
- crisp edges
- slightly moody, cozy, vulnerable desktop-pet tone
- keep each slot like a prop cluster, not a full illustrated scene
- keep layouts readable and crop-friendly

Return only the generated image.
```

## 4. Food Set: Omnivore And Scavenger

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one clean sprite layout reference for a small 2D desktop virtual pet game showing isolated food item sprites for omnivores and scavengers.

Board rules:
- transparent background preferred
- one single board only
- simple evenly spaced sprite layout only, not a literal object
- 3 columns x 3 rows
- 9 food items total
- one item per slot
- no text labels
- no decorative frame
- no wooden board
- no cutting board
- no tray
- no table
- no shared plate under all items
- every food item should be isolated on transparent background

Include these food items:
1. berry cluster
2. sliced apple
3. nut pile
4. grain mix
5. bread crumbs
6. cooked meat chunk
7. fish scrap
8. egg
9. mixed scavenger snack bowl

Usefulness:
- should work across rat, crow, fox, raccoon, and squirrel where appropriate
- keep silhouettes clear and reusable

Style:
- pixel art
- crisp edges
- slightly moody, cozy game tone
- clean isolated objects

Return only the generated image.
```

## 5. Food Set: Herbivore And Grazer

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one clean sprite layout reference for a small 2D desktop virtual pet game showing isolated herbivore and grazer food item sprites.

Board rules:
- transparent background preferred
- one single board only
- simple evenly spaced sprite layout only, not a literal object
- 3 columns x 3 rows
- 9 food items total
- one item per slot
- no text labels
- no decorative frame
- no wooden board
- no cutting board
- no tray
- no table
- no shared plate under all items
- every food item should be isolated on transparent background

Include these food items:
1. clover bunch
2. leafy greens
3. pond greens
4. berry cluster
5. root vegetable slice
6. flower petals / edible blossoms
7. hay bundle
8. grain bundle
9. herbivore food bowl

Usefulness:
- should work across deer, goose, and general plant-feeding pets

Style:
- pixel art
- crisp edges
- slightly moody, cozy game tone
- clean isolated objects

Return only the generated image.
```

## 6. Food Set: Birds And Seed Eaters

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one clean sprite layout reference for a small 2D desktop virtual pet game showing isolated bird-food item sprites.

Board rules:
- transparent background preferred
- one single board only
- simple evenly spaced sprite layout only, not a literal object
- 3 columns x 3 rows
- 9 food items total
- one item per slot
- no text labels
- no decorative frame
- no wooden board
- no cutting board
- no tray
- no table
- no shared plate under all items
- every food item should be isolated on transparent background

Include these food items:
1. seed pile
2. sunflower seeds
3. mixed grain
4. berry cluster
5. bug treat
6. fish morsel
7. shiny trinket snack reward
8. hanging feeder seed mix
9. bird food bowl

Usefulness:
- should work across crow, pigeon, and goose with some overlap

Style:
- pixel art
- crisp edges
- slightly moody, cozy game tone
- clean isolated objects

Return only the generated image.
```

## 7. Food Set: Predator And Reptile

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one clean sprite layout reference for a small 2D desktop virtual pet game showing isolated predator and reptile food item sprites.

Board rules:
- transparent background preferred
- one single board only
- simple evenly spaced sprite layout only, not a literal object
- 3 columns x 3 rows
- 9 food items total
- one item per slot
- no text labels
- no decorative frame
- no wooden board
- no cutting board
- no tray
- no table
- no shared plate under all items
- every food item should be isolated on transparent background

Include these food items:
1. meat chunk
2. fish
3. egg
4. mouse prey icon
5. frog-safe bug cup
6. worm pile
7. lizard-safe protein bowl
8. fox meal plate
9. reptile food tray

Usefulness:
- should work mainly for snake, frog, fox, and partly crow/raccoon

Style:
- pixel art
- crisp edges
- slightly moody, cozy game tone
- clean isolated objects

Return only the generated image.
```

## 8. Water And Feeding Containers

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one clean sprite layout reference for a small 2D desktop virtual pet game showing reusable feeding and watering container sprites.

Board rules:
- transparent background preferred
- one single board only
- simple evenly spaced sprite layout only, not a literal object
- 3 columns x 3 rows
- 9 items total
- one item per slot
- no text labels
- no decorative frame
- no wooden board
- no tray
- no table
- each container should be isolated on transparent background

Include these items:
1. water bowl
2. shallow water dish
3. feeding plate
4. seed tray
5. hanging feeder
6. small pond dish
7. storage jar
8. treat cup
9. simple pet dish set

Style:
- pixel art
- crisp edges
- slightly moody, cozy game tone
- clean isolated objects

Return only the generated image.
```

## 9. Medicine And Care Items

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one clean sprite layout reference for a small 2D desktop virtual pet game showing isolated medicine and care item sprites.

Board rules:
- transparent background preferred
- one single board only
- simple evenly spaced sprite layout only, not a literal object
- 3 columns x 3 rows
- 9 items total
- one item per slot
- no text labels
- no decorative frame
- no wooden board
- no tray
- no table
- each item should be isolated on transparent background

Include these items:
1. pill bottle
2. medicine dropper
3. syringe
4. bandage roll
5. first-aid kit
6. thermometer
7. grooming brush
8. soap / shampoo bottle
9. towel or wash cloth

Style:
- pixel art
- crisp edges
- slightly moody, cozy game tone
- clean isolated objects

Return only the generated image.
```

## 10. Toys And Enrichment Set A

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one clean sprite layout reference for a small 2D desktop virtual pet game showing isolated toy and enrichment sprites.

Board rules:
- transparent background preferred
- one single board only
- simple evenly spaced sprite layout only, not a literal object
- 3 columns x 3 rows
- 9 items total
- one item per slot
- no text labels
- no decorative frame
- no wooden board
- no tray
- no table
- each item should be isolated on transparent background

Include these items:
1. ball
2. chew toy
3. rope toy
4. bell toy
5. branch perch
6. tunnel hide
7. digging tray
8. mirror / shiny trinket
9. leaf pile / forage mat

Style:
- pixel art
- crisp edges
- slightly moody, cozy game tone
- clean isolated objects

Return only the generated image.
```

## 11. Toys And Enrichment Set B

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one clean sprite layout reference for a small 2D desktop virtual pet game showing isolated enrichment and comfort object sprites.

Board rules:
- transparent background preferred
- one single board only
- simple evenly spaced sprite layout only, not a literal object
- 3 columns x 3 rows
- 9 items total
- one item per slot
- no text labels
- no decorative frame
- no wooden board
- no tray
- no table
- each item should be isolated on transparent background

Include these items:
1. nest bed
2. moss bed
3. hay bed
4. little crate hideout
5. stump perch
6. log shelter
7. rock basking spot
8. memory / memorial object
9. cozy blanket mat

Style:
- pixel art
- crisp edges
- slightly moody, cozy game tone
- clean isolated objects

Return only the generated image.
```

## 12. Utility And Shelter Props

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one clean sprite layout reference for a small 2D desktop virtual pet game showing isolated utility prop sprites and simple environment fillers.

Board rules:
- transparent background preferred
- one single board only
- simple evenly spaced sprite layout only, not a literal object
- 3 columns x 3 rows
- 9 items total
- one item per slot
- no text labels
- no decorative frame
- no wooden board
- no tray
- no table
- each item should be isolated on transparent background

Include these items:
1. small wooden sign
2. storage basket
3. seed sack
4. food crate
5. lantern
6. moss patch
7. pebble cluster
8. stick bundle
9. cloth mat

Style:
- pixel art
- crisp edges
- slightly moody, cozy game tone
- clean isolated objects

Return only the generated image.
```

## 13. Status Effect Icons

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one clean sprite layout reference for a small 2D desktop virtual pet game showing isolated status effect icons.

Board rules:
- transparent background preferred
- one single board only
- simple evenly spaced sprite layout only, not a literal object
- 4 columns x 2 rows
- 8 icons total
- one icon per slot
- no text labels
- no decorative frame
- no wooden board
- no tray
- no table
- every icon should be isolated on transparent background

Include these icons:
1. hungry
2. thirsty
3. sleepy
4. sick
5. happy
6. dirty
7. lonely
8. comforted / safe

Style:
- pixel art
- crisp edges
- same visual family as the pet art
- simple and readable at very small size

Return only the generated image.
```

## 14. Large Action Buttons

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one clean sprite layout reference for a small 2D desktop virtual pet game showing large action button sprites.

Board rules:
- transparent background preferred
- one single board only
- simple evenly spaced sprite layout only, not a literal object
- 3 columns x 3 rows
- 9 buttons total
- one button per slot
- buttons should be clearly separated
- text on the buttons is allowed in this prompt
- no wooden board
- no tray
- no table
- each button should be isolated on transparent background

Include these buttons:
1. Feed
2. Water
3. Rest
4. Play
5. Groom
6. Bath
7. Medicine
8. Doctor
9. Home

Style:
- pixel art
- crisp edges
- readable and simple
- same general visual family as the pet art
- slightly moody, cozy tone

Important:
- keep the text large and very legible
- avoid ornate typography
- make these feel like UI buttons, not signboards

Return only the generated image.
```

## 15. Small UI Icon Refresh

```text
Generate an IMAGE ONLY.

Do not answer with text.
Do not describe limitations.
Do not provide configuration data.
Do not provide JSON.
Do not provide a table.
Do not provide a written explanation.

Create exactly one clean sprite layout reference for a small 2D desktop virtual pet game showing small isolated UI icons in a matching visual style.

Board rules:
- transparent background preferred
- one single board only
- simple evenly spaced sprite layout only, not a literal object
- 5 columns x 4 rows
- 20 icons total
- one icon per slot
- no text labels
- no decorative frame
- no wooden board
- no tray
- no table
- every icon should be isolated on transparent background

Important naming rule:
- the names below are asset identifiers only
- do not draw text inside the icons
- do not draw underscore characters
- for entries like `food_meat` or `water_bowl`, interpret them as concepts only

Include these icons:
1. add
2. bathe
3. doctor
4. exercise
5. feed
6. food_meat = meat food icon
7. food_plant = plant food icon
8. food_salty = salty treat icon
9. food_sweet = sweet treat icon
10. ghost
11. groom
12. medicine
13. memoriam
14. minimize
15. callback / return home
16. rest
17. save
18. settings
19. water
20. water_bowl = bowl of water icon

Style:
- pixel art
- crisp edges
- readable at very small size
- same general visual family as the pet art
- slightly moody, cozy tone

Return only the generated image.
```
