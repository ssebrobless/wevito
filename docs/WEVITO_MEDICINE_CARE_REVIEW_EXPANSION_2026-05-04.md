# Wevito Medicine/Care Review Expansion

Updated: 2026-05-04

This is Phase 4 of `docs/WEVITO_VISUAL_REMAINING_PHASE_PLAN_2026-05-04.md`.
It expands the existing medicine/care mapping into a visual readiness decision
packet.

It does not authorize runtime code changes, content changes, sprite edits, new
generation, import, or runtime PNG mutation.

## Boundary

```text
Phase 4
  |
  +-- review existing care assets
  +-- score icon-scale readiness
  +-- score scene-object readiness
  +-- confirm condition-to-asset mapping
  +-- identify first-class candidates for future content work
  +-- identify cleanup needs
  |
  +-- no new medicine generation
  +-- no content/runtime changes
  +-- no sprite edits
```

## Review Artifacts

```text
vnext/artifacts/visual-review/20260504-medicine-care-expansion/
  +-- medicine-care-scale-readiness-sheet.png
  +-- medicine-care-condition-map-sheet.png
  +-- medicine-care-pet-context-sheet.png
  +-- medicine-care-expansion-summary.md
  +-- manifest.json
```

These are non-mutating review artifacts. The sheets place existing PNGs at
review scale only. Source files are unchanged.

## Asset Readiness

| Asset | Icon scale | Scene scale | Decision |
| --- | --- | --- | --- |
| `first_aid_kit` | `pass` | `pass` | Best broad medicine fallback. |
| `medicine_dropper` | `pass` | `pass` | Best gentle small-pet illness visual. |
| `pill_bottle` | `pass` | `pass` | Clear ongoing medication visual. |
| `thermometer` | `warning` | `pass` | Strong diagnosis visual, but thin at toolbar scale. |
| `bandage_roll` | `warning` | `pass` | Good injury/recovery visual, but needs label/context at small scale. |
| `grooming_brush` | `warning` | `pass` | Good hygiene/comfort visual, not generic medicine. |
| `soap_bottle` | `pass` | `pass` | Clear hygiene/skin-care visual. |
| `towel` | `warning` | `pass` | Good comfort/rest prop, weak as standalone medicine icon. |
| `syringe` | `doctor_only` | `warning` | Readable but too severe for casual care. |

## Recommended Visual Families

```text
medicine
  -> first_aid_kit
  -> medicine_dropper
  -> pill_bottle

diagnosis
  -> thermometer
  -> first_aid_kit

hygiene
  -> grooming_brush
  -> soap_bottle
  -> towel

physical recovery
  -> bandage_roll
  -> first_aid_kit
  -> towel

doctor / high severity
  -> syringe
  -> thermometer
  -> first_aid_kit
```

## Condition Visual Map

This confirms the mapping from
`docs/WEVITO_MEDICINE_CARE_VISUAL_MAPPING_2026-05-04.md`.

| Condition | Primary visual | Secondary visual | Review status |
| --- | --- | --- | --- |
| `respiratoryProblems` | `medicine_dropper` | `thermometer` | `mapped` |
| `parasites` | `grooming_brush` | `soap_bottle` | `mapped` |
| `dentalProblems` | `pill_bottle` | `first_aid_kit` | `mapped` |
| `sheddingIssues` | `grooming_brush` | `towel` | `mapped` |
| `jointStiffness` | `first_aid_kit` | `bandage_roll` | `mapped` |
| `skinInfections` | `soap_bottle` | `medicine_dropper` | `mapped` |
| `viralSusceptibility` | `thermometer` | `medicine_dropper` | `mapped` |
| `dentalOvergrowth` | `pill_bottle` | `grooming_brush` | `mapped` |
| `footProblems` | `bandage_roll` | `first_aid_kit` | `mapped` |
| `obesity` | `first_aid_kit` | `thermometer` | `mapped` |
| `malnutrition` | `medicine_dropper` | `first_aid_kit` | `mapped` |
| `depression` | `towel` | `grooming_brush` | `mapped` |
| `anxiety` | `towel` | `medicine_dropper` | `mapped` |
| `jointPain` | `bandage_roll` | `first_aid_kit` | `mapped` |
| `exhaustion` | `towel` | `thermometer` | `mapped` |
| `injury` | `bandage_roll` | `first_aid_kit` | `mapped` |

No condition currently requires new art before it can be visually represented.

## First-Class Content Candidates

These are future content/code-side candidates only. Do not add them from the
visual thread.

| Future content id | Asset | Priority | Why |
| --- | --- | ---: | --- |
| `care-liquid-medicine` | `medicine_dropper` | 1 | Strong gentle medicine identity. |
| `care-pills` | `pill_bottle` | 2 | Distinct ongoing medication identity. |
| `care-bandage` | `bandage_roll` | 3 | Useful for injury, foot, joint, and recovery states. |
| `care-thermometer` | `thermometer` | 4 | Good diagnostic/sick-check moment. |
| `care-grooming-brush` | `grooming_brush` | 5 | Useful for hygiene, shedding, parasites, comfort. |
| `care-bath-soap` | `soap_bottle` | 6 | Clear hygiene/skin-care support. |
| `care-comfort-towel` | `towel` | 7 | Comfort/rest/recovery support. |

The current runtime/content layer exposes broad `care-medicine` and
`care-doctor` entries. These candidates should wait for code-side planning.

## Cleanup Notes

No asset requires immediate repainting or regeneration.

| Asset | Cleanup note | Severity |
| --- | --- | --- |
| `thermometer` | Pair with label or sick/doctor context at toolbar scale. | `warning` |
| `bandage_roll` | Can read as a generic roll unless paired with injury/recovery text. | `warning` |
| `grooming_brush` | Works best in hygiene/grooming contexts, not medicine. | `warning` |
| `towel` | Good comfort prop, weak standalone medical meaning. | `warning` |
| `syringe` | Keep doctor-only/high-severity to avoid alarming normal care. | `policy` |

## Decision

```text
medicine/care visual state
  -> not missing
  -> under-mapped
  -> ready for future content planning using existing assets
  -> no new generation needed yet
```

The best short-term visual set is:

```text
primary medicine: first_aid_kit, medicine_dropper, pill_bottle
diagnosis: thermometer
injury/recovery: bandage_roll
hygiene/comfort: grooming_brush, soap_bottle, towel
doctor-only: syringe
```

## Phase 4 Status

```text
Phase 4: complete
new generation needed: no
asset mutation approved: no
runtime/content changes approved: no
next visual phase: Phase 5 habitat loadout visual review expansion
```
