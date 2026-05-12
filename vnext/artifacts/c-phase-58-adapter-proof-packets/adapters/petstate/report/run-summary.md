# PET TASKS Pet State Report

TaskCard: `009a9cbe-97d2-43c6-bc4c-0e2f3bf15274`
Intent: review pet state

# Pet Debug Truth Report

Generated UTC: 2026-05-12T20:57:38.5470230+00:00
Mode: Pinned

## Pets

| Pet | State | Visible | Expected | Wellbeing | Drive | Emotion | Summary |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Rat 1 | Home | Idle | Idle | Watch | SelfMaintenance | Relief | Rat 1 should be watched for thirst. |
| Crow 2 | Home | Idle | Idle | Watch | SelfMaintenance | Relief | Crow 2 should be watched for thirst. |
| Fox 3 | Home | Happy | Idle | Watch | SelfMaintenance | Relief | Fox 3 should be watched for thirst. |

## Actions

| Action | Enabled | Reason |
| --- | --- | --- |
| Feed | True | A pet is hungry or has a nutrition-related condition. |
| Water | True | A pet can use water. |
| Rest | True | A pet is tired, away from home, or has exhaustion. |
| Play | True | A pet can benefit from affection, comfort, or fitness play. |
| Groom | True | A pet can benefit from grooming or a related condition treatment. |
| Bath | True | A pet is dirty enough or has a bath-treatable condition. |
| Medicine | True | A pet has low health, sickness, or a medicine-treatable condition. |
| Doctor | True | A pet has low health or an active condition. |
| Home | False | All pets are already home. |

## Findings

- No debug-truth findings.

## Safety

This adapter only wrote markdown/JSON artifacts in a new pet-task folder. It did not mutate pet state, runtime PNGs, source boards, prop anchors, shared assets, or visual-side candidate/proof folders.
