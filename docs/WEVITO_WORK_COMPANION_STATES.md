# Wevito Work Companion States

Updated: 2026-05-04

This document captures the Phase 1 planning vocabulary for PC-event-driven pet
states. It is documentation only. Adding enum values, runtime transitions,
desktop hooks, animations, or UI behavior is Phase 4 work.

The goal is to let Wevito become a useful work companion without turning the
runtime into a separate minigame. PC events should map to readable pet states,
and each proposed state must degrade cleanly to an existing animation until the
new animation exists.

```text
PC/work event
  -> semantic pet state
  -> optional authored animation
  -> existing fallback animation
```

## Additive State Set

These states are additive. They do not replace the existing base families.

| Proposed state | Purpose | Fallback before Phase 4 |
| --- | --- | --- |
| `waving` | Acknowledges a helpful capture, greeting, or attention event. | `happy` |
| `jumping` | Celebrates successful completion or accepted repair. | `happy` |
| `failed` | Shows an error, rejected result, or invalid input. | `sad` or `sick` |
| `waiting` | Shows patient background work or pending user/tool response. | `idle` or `sleep` |
| `review` | Shows focused inspection, scanning, or quality review. | `idle` or `walk` |

State-specific visuals should remain small and sprite-native. Do not add text,
UI panels, floating icons, detached effects, guide marks, shadows, or scenery to
the pet frames.

## Event Mapping

| PC or workflow event | Proposed pet state | Existing fallback | Notes |
| --- | --- | --- | --- |
| Link captured into a shelf or queue | `waving` | `happy` | The pet acknowledges that the capture worked. |
| Clipboard content is invalid or unsupported | `failed` | `sad` | Use for rejected URLs, missing files, or parse failures. |
| Opening a link or launching a related tool | `running` | `walk` | Runtime may already have walk; no new enum needed for generic motion. |
| Tool popup or provider page is waiting for user input | `waiting` | `idle` | Should feel patient, not broken. |
| Sprite repair scanner is evaluating frames | `review` | `idle` or `walk` | Focused inspection state for QA or visual review. |
| Sprite repair applied successfully | `jumping` | `happy` | Small celebration after accepted apply. |
| Sprite repair escalated or rejected | `failed` | `sad` | Use when manual intervention is needed. |
| Focus session is active | `waiting` | `sleep` or `idle` | Quiet background presence for deep work. |
| Build/test/process is running | `running` | `walk` | Show productive motion without implying success yet. |

## Runtime Guidance

Phase 1 should only document the vocabulary. Later implementation should keep the
runtime rules simple:

- Events choose semantic states.
- Semantic states choose authored animations when available.
- Missing authored animations fall back to existing base families.
- Fallbacks must be deterministic and non-blocking.
- Work-companion states should never require provider output at runtime.

## Visual Guidance

The proposed states should follow the same restrictions adopted from Hatch Pet:

| State | Visual direction | Avoid |
| --- | --- | --- |
| `waving` | Show a raised limb or head/body gesture. | Wave marks, floating lines, symbols. |
| `jumping` | Show body height/pose change. | Shadows, dust, impact bursts, floor cues. |
| `failed` | Show deflated posture or small attached expression detail. | Red X marks, detached smoke, floating punctuation. |
| `waiting` | Show calm idle variation, glance, blink, or small bounce. | UI clocks, labels, progress bars. |
| `review` | Show focused lean, blink, head tilt, or careful posture. | Magnifying glasses, papers, code, UI panels unless already part of the pet identity. |

## Phase Boundary

Do not add these enum values in Phase 1. Do not wire PC events in Phase 1. Do not
create new runtime animation assets in Phase 1.

Phase 4 may add the enum/state names, event-routing rules, and authored animation
fallbacks after the generation contract and QA workflow are stable.
