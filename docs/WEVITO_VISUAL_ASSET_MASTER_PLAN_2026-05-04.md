# Wevito Visual Asset Master Plan

Updated: 2026-05-04

This plan expands the current optional-animation work into a full visual asset
plan for Wevito. It covers pet color variants, current sprite cleanup,
medicine/care assets, items, environmental objects, habitat objects, eggs, UI,
and the asset workflow lessons adopted from Hatch Pet.

It is a planning document only. It does not request generation, import, runtime
code changes, sprite edits, or build/test runs.

## Direction

```text
visual work
  |
  +-- pet identity
  |   +-- species / age / gender
  |   +-- six egg-selected color variants
  |   +-- base + optional animation quality
  |
  +-- care and gameplay objects
  |   +-- food / water / medicine / doctor
  |   +-- toys / enrichment / ball interactions
  |
  +-- habitat presentation
  |   +-- species environments
  |   +-- beds / shelters / perches / hides
  |   +-- depth / occlusion / contact shadows
  |
  +-- UI and feedback
      +-- action icons
      +-- status effects
      +-- egg hatch lifecycle
      +-- companion-state visuals
```

The plan is deliberately staged. We should first prove what we already have,
then clean and map existing assets, then generate or author only the pieces that
are still missing.

## Current Truth

The project is not empty on assets. The better description is:

```text
asset-rich, mapping-partial, quality-not-yet-fully-proven
```

Confirmed during this pass:

| Area | Current state |
| --- | --- |
| Pet species | 10 species in content and runtime. |
| Ages | baby, teen, adult. |
| Genders | female, male. |
| Color variants | red, orange, yellow, blue, indigo, violet. |
| Runtime variant folders | 360 expected, 0 missing. |
| Shared runtime items | 81 item/object PNGs. |
| Care/medicine art | 9 care PNGs plus medicine/doctor icons. |
| Environment art | 12 runtime environment PNGs. |
| Habitat-like objects | Present in toys/utility groups. |
| Egg source board | Present. |

The main visual challenge is now coordination:

```text
existing art
  -> canonical inventory
  -> quality review
  -> content mapping
  -> runtime/display proof
  -> only then generation or cleanup
```

## Work Lanes

```text
Lane A: inventory and mapping
  purpose: know what exists and what each asset means

Lane B: color variant QA
  purpose: prove all six egg colors look intentional

Lane C: non-generation cleanup
  purpose: refine current art without new image generation

Lane D: optional animation polish
  purpose: repair cloned/reused prop-contact animation rows

Lane E: habitat and object staging
  purpose: make environments feel usable, not just decorative

Lane F: medicine/care visual system
  purpose: make health treatment assets clear and varied
```

## Lane A - Inventory And Mapping

Goal: turn the current asset-rich tree into a reliable source of truth.

Inputs:

- `docs/WEVITO_ASSET_INVENTORY_CHECKLIST_2026-05-04.md`
- `docs/SPRITE_SOURCE_OF_TRUTH.md`
- `vnext/content/species.json`
- `vnext/content/items.json`
- `vnext/content/environments.json`
- `vnext/content/conditions.json`
- `sprites_shared_runtime`
- `incoming_sprites`

Tasks:

| Task | Output | Risk |
| --- | --- | ---: |
| Confirm every shared runtime PNG has an intended gameplay/category role. | Asset role table. | Low |
| Separate content items from decorative/environment props. | Content-vs-decor matrix. | Low |
| Mark each asset as `ready`, `needs_review`, `needs_cleanup`, or `unmapped`. | Review queue. | Low |
| Record source board provenance for shared assets. | Hatch-style provenance notes. | Low |
| Identify duplicate-looking or unused assets. | Cleanup candidate list. | Low |

Decision rule:

```text
do not delete or regenerate just because an asset is unmapped
first decide whether it is content, decor, UI, status, or archive/reference
```

## Lane B - Color Variant QA

Goal: restore confidence in the six egg-selected pet color variants.

Canonical colors:

```text
red
orange
yellow
blue
indigo
violet
```

Current structural state:

```text
10 species x 3 ages x 2 genders x 6 colors = 360 runtime variant folders
0 folders missing in this pass
```

What still needs proof:

| Question | Why it matters |
| --- | --- |
| Does every color read as the intended egg color? | Egg outcome needs clear identity. |
| Does recoloring preserve species silhouette and markings? | Pets should not become generic color blobs. |
| Do colors stay consistent across animations? | Animation flicker can feel like visual bugs. |
| Do colors stay readable on all environments? | Strong colors can clash or disappear. |
| Are male/female and age differences preserved? | Palette work must not flatten identity. |

Recommended process:

```text
1. build no-edit contact sheets
   species / age / gender with all six colors side by side

2. score each set
   pass / needs palette adjustment / needs source review

3. isolate palette problems
   color too dark, too close to outline, hue drift, contrast loss, marking loss

4. repair one species+age+gender set first
   prove recolor workflow before broad palette edits

5. only then batch color cleanup
```

First color QA target:

| Target | Reason |
| --- | --- |
| `goose / baby / female` all six colors | Already the first animation pilot; beak/body contrast makes palette issues obvious. |

Color work stop rules:

- stop if source board identity changes
- stop if palette cleanup changes frame canvas or crop
- stop if a recolor makes gender/age harder to read
- stop if the code-side thread reports runtime asset gates are unstable

## Lane C - Non-Generation Cleanup

Goal: refine current assets without asking a generator for new art.

Use Hatch Pet-style discipline:

```text
cleanup input
  -> manifest/provenance
  -> deterministic report
  -> contact sheet
  -> preview where motion exists
  -> review decision
  -> apply or reject
```

Cleanup categories:

| Category | What to look for | First action |
| --- | --- | --- |
| Canvas/crop consistency | mixed sizes, jitter, edge touching | Use `WEVITO_CANVAS_NORMALIZATION_VISUAL_GUIDE_2026-05-04.md`; fix per-sequence stability without shrinking natural motion. |
| Alpha/background residue | stray matte, floor patch, boxed pixels | Review contact sheets and alpha masks. |
| Palette consistency | hue drift across frames/colors | Build palette diff summaries. |
| Icon readability | tiny icons too vague at UI size | Review actual display scale. |
| Object grounding | props float or do not sit in scene | Add object-zone plan before art edits. |
| Sprite identity drift | species/age/gender changes | Manual review; never auto-fix identity. |

Important constraint:

```text
no broad cleanup that rewrites runtime PNGs until the mixed-canvas sequences are
reviewed under the per-sequence normalization rule
```

Recommended first cleanup queue:

| Order | Target | Why |
| ---: | --- | --- |
| 1 | Color contact sheets | No mutation, high information. |
| 2 | Care/medicine icon readability | Small set, important gameplay surface. |
| 3 | Habitat object scale review | Beds/shelters must fit pets. |
| 4 | Optional animation prop-contact review | Existing docs already define priority. |
| 5 | Environment/pet contrast review | Requires scene screenshots later. |

## Lane D - Optional Animation Polish

Goal: continue the current optional-family plan with better visual priority.

Current priority from `docs/WEVITO_OPTIONAL_ANIMATION_VISUAL_AUDIT_2026-05-04.md`:

```text
pickup_ball
  cloned from play in all inspected species

drop_ball
  cloned from pickup in most inspected species

hold_ball
  idle-cloned for frog, pigeon, raccoon, squirrel, goose
```

Recommended order:

```text
1. goose hold_ball endpoint
2. goose pickup_ball transition
3. goose drop_ball transition
4. goose carry_ball_walk/run review
5. goose drink review only if concrete issue appears
6. expand by failure type and grip anatomy
```

Code-side reliability gates are now green in the separate code-side worktree.
This lane should still wait for manifest/provenance/apply workflow readiness
before any generation, import, or runtime/source PNG mutation.

## Lane E - Habitat And Object Staging

Goal: make environments, beds, shelters, perches, and props feel like usable
places instead of flat decorations.

Current useful assets:

| Object type | Existing candidates |
| --- | --- |
| Beds/mats | `blanket_mat`, `hay_bed`, `moss_bed`, `nest_bed`, `cloth_mat` |
| Shelters/hides | `crate_hideout`, `log_shelter`, `tunnel_hide` |
| Perches/basking | `branch_perch`, `stump_perch`, `rock_basking_spot` |
| Scene dressing | `moss_patch`, `pebble_cluster`, `stick_bundle`, `lantern`, `wooden_sign` |
| Species containers | `pond_dish`, `seed_tray`, `water_bowl`, `hanging_feeder` |

Needed habitat contract:

```text
habitat object
  |
  +-- asset id
  +-- default species/environment use
  +-- screen position
  +-- depth band
  +-- occlusion role
  +-- interaction zone
  |   +-- sleep
  |   +-- hide
  |   +-- eat
  |   +-- drink
  |   +-- perch
  |   +-- play
  |
  +-- pet contact point
  +-- object shadow/grounding policy
```

Planning tasks:

| Task | Output |
| --- | --- |
| Choose default bed/shelter per species. | Species habitat loadout table. |
| Define object scale classes. | tiny, small, medium, large scene objects. |
| Define object interaction zones. | Sleep/eat/drink/play/perch/hide anchors. |
| Define depth bands. | Backdrop, far prop, pet, near occluder, UI. |
| Define review screenshot set. | One species per object class first. |

Do not implement renderer changes in this visual thread. The code-side plan
already has habitat grounding/depth work as a later lane.

## Lane F - Medicine And Care Visual System

Goal: make the health/care side feel intentional and readable.

Current issue:

```text
art layer has multiple medicine/care assets
content layer exposes mostly generic medicine + doctor
```

Existing care assets:

```text
bandage_roll
first_aid_kit
grooming_brush
medicine_dropper
pill_bottle
soap_bottle
syringe
thermometer
towel
```

Recommended mapping:

| Care/condition need | Visual candidates | Notes |
| --- | --- | --- |
| generic medicine | `first_aid_kit`, `pill_bottle` | Keep kit as broad action; pill bottle as specific medication. |
| liquid medicine | `medicine_dropper` | Good for small pets and gentle care. |
| doctor/vaccine | `syringe`, `first_aid_kit` | Use carefully; should not feel alarming by default. |
| injury/foot care | `bandage_roll`, `first_aid_kit` | Good for injury and foot problems. |
| fever/diagnosis | `thermometer` | Good for sick/viral/respiratory read. |
| hygiene-linked care | `soap_bottle`, `towel`, `grooming_brush` | Links to parasites, skin, shedding, cleanliness. |

Medicine plan:

```text
1. keep Medicine Kit as the simple top-level action
2. map specific conditions to secondary care visuals
3. review icons at actual UI scale
4. decide which assets become first-class content items
5. only generate new medicine art if an important treatment has no clear icon
```

First review target:

| Target | Reason |
| --- | --- |
| `medicine_dropper`, `pill_bottle`, `first_aid_kit`, `thermometer`, `bandage_roll` | Covers most health meanings without new art. |

## Egg And Hatch Visuals

Goal: make egg-selected species/color feel connected to the pet result.

Current assets:

- `incoming_sprites/egg-hatch-lifecycle.png`
- six color ids in every species record
- full six-color runtime folders for every species/age/gender

Needed visual proof:

| Requirement | First action |
| --- | --- |
| Egg colors correspond to palette ids | Create egg-color mapping table. |
| Hatch lifecycle reads cleanly | Review source board at target UI scale. |
| Hatch result matches selected color | Compare egg UI color to pet color contact sheet. |
| Rare/variant visual rules are clear | Defer until base six colors are proven. |

## Hatch Pet-Inspired Workflow Rules

Use these rules from the Hatch Pet research and Wevito contract docs:

| Rule | Wevito application |
| --- | --- |
| Shared written generation contract | No new generated asset without manifest/provenance. |
| Layout guide before generation | For sprite rows and source-board extraction. |
| Contact sheet required | For every visual batch, even no-generation cleanup. |
| Preview video required for motion | For every animation family repair. |
| Error vs warning rubric | Identity/canvas/background errors block import. |
| Small batches | One species/age/gender/family before broad expansion. |
| Review states | Accept/revise/reject before applying output. |

Non-generation refinement should follow the same review discipline:

```text
current asset
  -> inspect
  -> report
  -> contact sheet
  -> review decision
  -> deterministic cleanup only if safe
```

## Master Work Order

```text
Phase 0: docs and inventory
  status: in progress
  output: asset checklist + master visual plan

Phase 1: color QA setup
  output: all-six-color contact sheet plan for pets

Phase 2: medicine/care mapping
  output: condition-to-care-asset matrix

Phase 3: habitat object mapping
  output: species default beds/shelters/perches/props

Phase 4: non-generation cleanup queue
  output: deterministic reports and review sheets

Phase 5: optional animation visual pilot
  output: goose hold/pickup/drop set after code-side gates

Phase 6: selective asset generation
  output: only missing or failed assets, with manifest/provenance
```

## What To Do Next

The next safe visual-side tasks are docs/reporting tasks:

| Order | Task | Touches assets? |
| ---: | --- | --- |
| 1 | Create a palette QA plan/contact-sheet spec. | No |
| 2 | Create condition-to-medicine visual mapping. | No |
| 3 | Create species habitat loadout mapping. | No |
| 4 | Create cleanup queue criteria for existing shared assets. | No |
| 5 | Wait for code-side gates before PNG edits or generation. | No |

Do not begin broad recolor, cleanup, or generation yet. The runtime tree has the
expected six color variants structurally; the next question is whether those
variants look good and connect cleanly to egg selection.

## Success Criteria

This larger visual plan is successful when:

- every current asset has a known role or a deliberate archive/reference label
- every species has a documented default environment and habitat object set
- every condition has a medicine/care visual strategy
- every egg color has a verified pet color outcome
- cleanup can happen through deterministic review, not guesswork
- new generation happens only for specific missing/failed assets
- optional animation polish stays small, reviewable, and reversible
