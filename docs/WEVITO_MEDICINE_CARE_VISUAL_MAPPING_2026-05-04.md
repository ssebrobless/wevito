# Wevito Medicine And Care Visual Mapping

Updated: 2026-05-04

This document maps existing medicine and care art to Wevito's condition and
treatment needs. It is intended to prevent unnecessary generation by using the
care assets that already exist.

It is docs-only. It does not request content changes, runtime code changes,
sprite edits, or new generated assets.

## Current Shape

```text
care visuals
  |
  +-- generic action icons
  |     +-- medicine
  |     +-- doctor
  |
  +-- care item sprites
        +-- bandage_roll
        +-- first_aid_kit
        +-- grooming_brush
        +-- medicine_dropper
        +-- pill_bottle
        +-- soap_bottle
        +-- syringe
        +-- thermometer
        +-- towel
```

Current content records expose only broad care entries:

| Content id | Display name | Icon id |
| --- | --- | --- |
| `care-medicine` | Medicine Kit | `medicine` |
| `care-doctor` | Doctor Call | `doctor` |

Interpretation:

```text
the art layer has multiple care tools
the gameplay/content layer currently treats care more generically
```

The next visual job is mapping, not generating.

## Existing Care Assets

Files in `sprites_shared_runtime/items/care`:

| Asset | Bytes | Visual role |
| --- | ---: | --- |
| `bandage_roll` | 494 | Injury wrap, foot care, physical recovery. |
| `first_aid_kit` | 443 | General medicine, emergency care, doctor support. |
| `grooming_brush` | 519 | Grooming, parasites, shedding, comfort care. |
| `medicine_dropper` | 513 | Liquid medicine, gentle illness treatment. |
| `pill_bottle` | 536 | Ongoing medication, common treatment. |
| `soap_bottle` | 445 | Bathing, hygiene-linked care. |
| `syringe` | 481 | Doctor/vaccine/injection-only treatment. |
| `thermometer` | 499 | Diagnosis, fever, sickness check. |
| `towel` | 572 | Bath recovery, comfort, drying, rest care. |

## Condition Mapping

Conditions from `vnext/content/conditions.json`:

| Condition | Category | Primary visual | Secondary visual |
| --- | --- | --- | --- |
| `respiratoryProblems` | innate | `medicine_dropper` | `thermometer` |
| `parasites` | innate | `grooming_brush` | `soap_bottle` |
| `dentalProblems` | innate | `pill_bottle` | `first_aid_kit` |
| `sheddingIssues` | innate | `grooming_brush` | `towel` |
| `jointStiffness` | innate | `first_aid_kit` | `bandage_roll` |
| `skinInfections` | innate | `soap_bottle` | `medicine_dropper` |
| `viralSusceptibility` | innate | `thermometer` | `medicine_dropper` |
| `dentalOvergrowth` | innate | `pill_bottle` | `grooming_brush` |
| `footProblems` | innate | `bandage_roll` | `first_aid_kit` |
| `obesity` | acquired | `first_aid_kit` | `thermometer` |
| `malnutrition` | acquired | `medicine_dropper` | `first_aid_kit` |
| `depression` | acquired | `towel` | `grooming_brush` |
| `anxiety` | acquired | `towel` | `medicine_dropper` |
| `jointPain` | acquired | `bandage_roll` | `first_aid_kit` |
| `exhaustion` | acquired | `towel` | `thermometer` |
| `injury` | acquired | `bandage_roll` | `first_aid_kit` |

Notes:

- `syringe` should remain doctor-only or high-severity. It is useful, but it can
  feel too intense as a casual medicine icon.
- `first_aid_kit` is the safest broad fallback.
- `medicine_dropper` is the best gentle-treatment visual for small pets.
- `grooming_brush`, `soap_bottle`, and `towel` should support hygiene-linked
  care rather than generic medicine.

## Treatment Visual Families

```text
medicine
  +-- pill_bottle
  +-- medicine_dropper
  +-- first_aid_kit

doctor
  +-- first_aid_kit
  +-- syringe
  +-- thermometer

hygiene
  +-- soap_bottle
  +-- grooming_brush
  +-- towel

physical recovery
  +-- bandage_roll
  +-- first_aid_kit
  +-- towel
```

## First-Class Content Candidates

Do not add these to content from this visual thread, but these are good
candidates for later code/content work.

| Candidate content id | Asset | Why |
| --- | --- | --- |
| `care-liquid-medicine` | `medicine_dropper` | Distinct from generic medicine kit. |
| `care-pills` | `pill_bottle` | Clear ongoing treatment. |
| `care-bandage` | `bandage_roll` | Injury/foot/joint visual. |
| `care-thermometer` | `thermometer` | Diagnosis and sick-state check. |
| `care-grooming-brush` | `grooming_brush` | Hygiene and comfort care. |
| `care-bath-soap` | `soap_bottle` | Bath/skin/parasite support. |
| `care-comfort-towel` | `towel` | Recovery and rest support. |

## Visual QA Criteria

Review these assets at two scales:

```text
1. toolbar/icon scale
2. habitat/object scale
```

Pass criteria:

- object is identifiable without text
- silhouette is distinct from other care tools
- icon does not read as food/toy
- treatment meaning is not too alarming for normal care
- palette works against current UI background
- object remains readable near pet sprites

Warning criteria:

- object is recognizable only with label support
- two care assets look too similar at small scale
- color blends into UI background
- item is readable but too large/small beside pet

Fail criteria:

- object meaning is unclear
- object looks like a weapon or unsafe tool in normal care context
- object has background/matte artifacts
- icon is illegible at toolbar scale
- asset cannot be distinguished from food or toy assets

## Review Order

```text
1. first_aid_kit
2. medicine_dropper
3. pill_bottle
4. thermometer
5. bandage_roll
6. grooming_brush
7. soap_bottle
8. towel
9. syringe
```

Reason:

- first five cover the most medical meanings
- hygiene assets are already linked to bath/groom actions
- syringe is useful but should be handled carefully

## Stop Rules

Stop care/medicine visual work if:

- mapping requires content/runtime changes before review can continue
- icons are too small to evaluate without UI screenshots
- any asset needs repainting rather than simple cleanup
- new medicine generation is proposed before existing care assets are reviewed

## Current Recommendation

Use the existing care art first.

```text
next visual step
  -> create a care asset contact sheet/review surface

not yet
  -> no new medicine generation
  -> no content item expansion from this thread
  -> no runtime treatment UI changes
```

The medicine/care set is not missing; it is under-mapped. Mapping and QA should
happen before any generation.
