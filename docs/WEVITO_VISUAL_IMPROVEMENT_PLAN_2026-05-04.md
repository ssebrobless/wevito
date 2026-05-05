# Wevito Visual Improvement Plan

Updated: 2026-05-04

This is the visual-side working plan for Wevito after reviewing the Hatch Pet
skill, Claude's adoption plan, the Phase 1 animation contract docs, the Phase 2
helper tools, the expression-quality audit, and the code-side high-reasoning
plan.

Scope boundary:

- This thread owns visual direction, QA judgment, prompt strategy, review
  criteria, and future generation batch planning.
- The code-side thread owns runtime implementation, deterministic reliability,
  build/probe failures, and vNext code changes.
- Do not start new visual generation until the code-side thread clears the
  deterministic reliability gates.
- Do not edit `.sprite-workflow/**`, Sprite Workflow App docs, dirty sprite
  import tools, runtime code, Godot scripts, or sprite assets from this visual
  planning pass.

## Current Visual State

```text
WEVITO VISUAL STATUS
|
+-- Base pet identity
|   +-- source truth: incoming_sprites/
|   +-- runtime contract: transparent 28x24 PNG frames
|   +-- 10 species x 3 ages x 2 genders x 6 colors
|
+-- Base animation families
|   +-- idle / walk / eat / happy / sad / sleep / sick / bathe
|   +-- expression corruption class was previously found and repaired
|   +-- remaining risk is subjective polish, not obvious coverage failure
|
+-- Optional action families
|   +-- drink / play_ball / hold_ball / pickup_ball / drop_ball
|   +-- carry_ball_walk / carry_ball_run
|   +-- authored-complete and packaged-validated
|   +-- biggest visual gap: generic overlay/reseed poses instead of bespoke grips
|
+-- Hatch Pet lessons now adopted in docs/tools
    +-- manifest-first generation contract
    +-- provider-neutral provenance and hash recording
    +-- layout guides
    +-- contact sheet + preview as required proof
    +-- identity drift as blocker
```

The important visual truth is that Wevito is no longer mainly a missing-assets
problem. It is a visual-approval problem. The next gains come from better
judgment gates and targeted expressive pose upgrades, not broad replacement.

## Visual Priorities

| Priority | Area | Current issue | Desired improvement | Start now? |
| ---: | --- | --- | --- | --- |
| 1 | Grip / hold / pickup poses | Stable but generic bodies with runtime ball overlay. | Species-specific beak, mouth, paw, or body contact that reads as true grip. | Plan only |
| 2 | Prop contact truth | Ball/water props can be valid but visually float or slide. | Prop should feel attached to the right anatomy through the whole loop. | Plan only |
| 3 | Identity preservation | AI can drift face, silhouette, markings, or age proportions. | Every generated row must preserve canonical species/age/gender identity. | Yes, as rubric |
| 4 | Motion readability | Some motions can pass frame count but read as static, teleporting, or stiff. | Preview loop must make the verb readable at game scale. | Yes, as rubric |
| 5 | Work-companion states | Waving/jumping/failed/waiting/review are useful but unauthored. | Later authored states should be subtle, sprite-native, and tool-state readable. | Plan only |
| 6 | Habitat grounding | Future polished pets need stronger contact/depth context. | Pets and props should feel grounded without shadows/effects baked into sprite frames. | Defer |
| 7 | Creator pipeline | "Hatch a Wevito" is compelling but far beyond current stability. | External/custom pet pack pipeline with provenance and QA. | Park |

## Visual Rule Stack

Any future visual candidate must pass these layers, in order:

```text
1. Contract
   +-- right species / age / gender / color / family
   +-- expected frame count
   +-- transparent 28x24 output after import

2. Identity
   +-- same body plan
   +-- same face language
   +-- same age proportions
   +-- no species drift

3. Motion
   +-- verb reads in the preview loop
   +-- no teleporting prop
   +-- no repeated static row unless intentionally idle-like

4. Artifact
   +-- no checkerboard, guide marks, UI, labels, text
   +-- no white/black box, matte, chroma fringe, detached specks
   +-- no edge clipping or box-fitting

5. Runtime Proof
   +-- contact sheet reviewed
   +-- preview reviewed
   +-- packaged/runtime proof reviewed
   +-- warnings documented, errors rejected
```

Do not treat `complete` as `approved`. Do not treat `valid PNGs` as `good
animation`.

## First Visual Improvement Lane

The first real visual generation lane should be bespoke grip-pose authoring.
It should wait until the code-side reliability gates are healthy.

Recommended pilot:

| Field | Value |
| --- | --- |
| Species | `goose` |
| Age | `baby` |
| Gender | `female` |
| Source color | `blue` |
| Family | `hold_ball` |
| Why this pilot | Goose has been heavily iterated, beak grip is visually legible, and `hold_ball` is currently stable but not expressive. |

Pilot success means:

- The ball reads as held by the beak/body, not pasted over the sprite.
- The goose remains recognizably the same baby female goose.
- The row keeps the 4-frame hold structure.
- The pose does not create cropped wings, resized body, or face drift.
- Contact sheet and preview are both visually convincing before import.

Do not start with all species. Do not start with all ball families. A one-row
pilot is the safest way to prove the workflow.

## Family-Specific Visual Criteria

| Family | Must read as | Common failure | Reject if |
| --- | --- | --- | --- |
| `hold_ball` | Pet is holding the ball at a species-specific anchor. | Ball floats over idle pose. | Ball is not physically attached, or face/body identity changes. |
| `pickup_ball` | Pet reaches/lowers before the ball attaches. | Ball teleports from ground to mouth/paw. | The transition has no anticipatory pose. |
| `drop_ball` | Pet releases the ball from the hold point. | Ball pops away with no release posture. | Release reads as a missing frame or detached artifact. |
| `play_ball` | Pet reacts to the ball with visible play energy. | Ball is present but animal is static. | Motion does not read as play. |
| `carry_ball_walk` | Walk gait and ball anchor both remain stable. | Ball slides frame-to-frame. | Carry breaks gait or anchor. |
| `carry_ball_run` | Faster carry motion without shrinking or smearing. | Body gets box-fitted or speed-line-like. | Sprite is resized to fit action, or ball detaches. |
| `drink` | Head/mouth lowers toward a water source. | Bowl/water is decorative rather than contacted. | No mouth/head contact or body-size drift appears. |

## Work-Companion State Visual Direction

These should be authored later, after the runtime/code side is stable. They
should remain subtle and pet-native.

| State | Visual goal | Existing fallback | Avoid |
| --- | --- | --- | --- |
| `waving` | A tiny greeting or attention gesture. | `happy` | Floating wave marks, UI text, detached symbols. |
| `jumping` | Small celebratory lift and settle. | `happy` | Dust clouds, impact marks, shadows. |
| `failed` | Deflated/error posture that still feels like the pet. | `sad`/`sick` | Red Xs, punctuation, smoke, labels. |
| `waiting` | Patient, calm, background presence. | `idle`/`sleep` | Clocks, progress bars, tool icons. |
| `review` | Focused inspection/readiness pose. | `idle`/`walk` | Magnifying glasses, documents, code panels. |

Future visual-state order:

1. `waiting`
2. `failed`
3. `jumping`
4. `waving`
5. `review`

Reasoning: `waiting` and `failed` provide the most PC-utility readability; the
others are polish after the tool loop feels trustworthy.

## Prompt Rules For Future Grip-Pose Generation

Use this structure when the generation lane opens again:

```text
Create a Wevito <family> animation for <species> / <age> / <gender>.

Use the attached canonical source and verified runtime strip as identity locks.
Preserve the exact species identity, head shape, face design, age proportions,
outline weight, palette family, and silhouette.

Return exactly <frame_count> transparent sprite frames in <frame_count> slots.
Each runtime frame is 28x24 after import. Keep the pet centered with safe
padding. Do not crop or box-fit the animal.

The prop must be physically connected to the species-specific grip/contact
point. For birds, use beak contact. For mammals, use mouth/paw/body contact as
appropriate. The prop may not float, teleport, or detach between frames.

No text, labels, UI, speech bubbles, speed lines, dust, shadows, glows, motion
arcs, checkerboard, white background, black background, guide marks, borders, or
detached effects.
```

Provider prompts should be terse and literal. If the prompt becomes long enough
to invite decoration, split the visual references and the job instructions
instead of adding prose.

## Review Form

Use this mini form for each future visual candidate:

| Field | Notes |
| --- | --- |
| Run ID |  |
| Target | species / age / gender / color / family |
| Source references checked | canonical board, runtime strip, prop anchors, layout guide |
| Contact sheet reviewed | yes / no |
| Preview reviewed | yes / no |
| Identity result | pass / warning / fail |
| Motion result | pass / warning / fail |
| Prop contact result | pass / warning / fail |
| Artifact result | pass / warning / fail |
| Runtime proof result | pass / warning / fail |
| Decision | accept / accept with notes / repair / reject |
| Human note | one concrete reason |

## Waiting Conditions Before Generation

Do not begin visual generation until:

- code-side thread finishes or clearly scopes the deterministic reliability work
- `build-vnext.ps1` behavior is understood or isolated
- the non-mutating canvas mismatch report exists
- the vNext Settings Save probe failure is diagnosed
- Phase 2 helper tools are behavior-validated, not only syntax-compiled
- the user explicitly asks this visual thread to start a generation pilot

## Immediate Visual-Thread Work

While the code-side thread works, this thread can safely do:

1. Maintain this visual priority map.
2. Prepare generation prompts and review forms.
3. Build a no-generation pilot checklist for `goose / baby / female / hold_ball`.
4. Review existing contact sheets or preview artifacts if the user points to
   them.
5. Write handoff packets for Claude/Codex/other visual reviewers.

This thread should not:

- author or import new sprite frames
- run Gemini/OpenAI generation
- edit runtime code
- edit sprite workflow app files
- run broad sprite audits or packaged sweeps

## First Pilot Checklist

Before generating `goose / baby / female / blue / hold_ball`, prepare:

- manifest path and run id
- canonical goose baby female source reference
- current `hold_ball` runtime/contact-sheet reference
- current `idle` and `happy` references for identity comparison
- prop-anchor note for beak/body contact
- layout guide for 4 frames
- review form copied into the run summary
- rollback/backup location

Only after those are ready should generation begin.

## Bottom Line

The next visual improvement should be small, deliberate, and proof-heavy:
one grip-pose pilot, not a broad batch. Wevito already has coverage. The visual
win is making the most important rows look intentional, alive, and physically
connected while preserving each pet's identity.
