# C-PHASE 197: Pet-Sim Completion Baseline

Date: 2026-06-12
Branch: `claude-implementation/c-phase-197-pet-sim-completion-baseline`
Plan: `docs/CLAUDE_C_PHASE196_PLUS_PET_SIM_COMPLETION_PLAN_2026-06-12.md` §6.2
Prompt: `docs/CLAUDE_C_PHASE196_PLUS_CODEX_MEDIUM_PROMPTS_2026-06-12.md` §2

## Goal

One authoritative, machine-readable baseline of the runtime sprite estate that
C-PHASE 198–203 consume. Read-only: no sprite mutation, no production source change.
The only new code is `tools/build_pet_sim_completion_baseline.py`, a read-only report
builder.

## Headline Numbers

| Metric | Value |
| --- | --- |
| Required contract errors | 0 (C-130 base intact) |
| Runtime variants | 360/360 (10 species × 3 ages × 2 genders × 6 colors) |
| Optional-family targets | 2,520 (360 × 7) |
| Optional targets fallback-only | 2,156 (the six ball families everywhere except 4 pilot rows) |
| Optional targets runtime-complete | 364 (drink 360 + goose/baby/female/blue play_ball, hold_ball, pickup_ball, drop_ball) |
| Prop-anchor-supported targets | 0 (`sprites_runtime/_metadata/prop_anchors.json` absent — C-200 adds it) |
| Loose (uncontracted) files | 1,944 |
| — `groom` (uncontracted_live) | 1,440 across 360 variants — REGISTER in C-201 |
| — `play` (uncontracted_dead) | 504 across 126 variants — DELETE in C-201 (D1) |
| Source→runtime quality flagged rows | 25 of 60 species/age/gender rows (see Quality section) |

## Key Finding: `groom` is live but uncontracted

`groom` (4 frames, present on all 360 variants) appears in neither
`EXPECTED_ANIMATIONS` nor `OPTIONAL_EXPANDED_ANIMATIONS` in
`tools/audit_sprite_contract.py`. It is NOT dead: the shell's primary animation-id
path is `CurrentAnimationState.ToString().ToLowerInvariant()`
(`Wevito.VNext.Shell/SpriteAssetService.cs`), so `PetAnimationState.Groom` renders
`groom_*` frames whenever they exist; the `Groom => "happy"` entry in
`GetFallbackAnimationId` is fallback-only. Consequence: C-PHASE 201 must add
`"groom": 4` to `EXPECTED_ANIMATIONS` BEFORE landing the stray-family pin, or the pin
would flag 1,440 live files. The prompts doc §6 was amended accordingly in this PR.

`play` is confirmed dead: `PetAnimationState` has no `Play` member, and the Play
action maps to `animationState=happy` + `optionalAnimationFamily=play_ball`
(`vnext/content/actions.json`). The 504 `play_*` files are unreachable.

## Authored-Verified Coverage (files per species × family)

| Species | idle | walk | Everything else |
| --- | --- | --- | --- |
| rat, crow, fox, snake, deer, frog, pigeon | 144 | 216 | 0 |
| raccoon | 72 | 72 | 0 |
| squirrel | 0 | 0 | 0 |
| goose | 0 | 0 | 0 |

C-PHASE 202's scope (goose, squirrel, raccoon locomotion) is confirmed by
measurement. No care/expression family has authored-verified art anywhere — input to
the C-PHASE 203 triage.

## Quality

`audit_source_to_runtime_quality.py` ran over the full matrix (idle/walk vs canonical
source poses). Flagged rows: 25 of 60 species/age/gender rows. Flag distribution: edge-touch 18, walk:halo-noise 9, too-large 8, walk:too-large 8, walk:fragmented 6, walk:edge-touch 6, halo-noise 1. Worst clusters: frog (fragmented + halo-noise across all ages), goose (edge-touch across all ages), deer/pigeon (too-large). Per-row flags are embedded in
`baseline.json` `variant_rows[].quality_flags` and feed the C-PHASE 203 triage queue
builder.

## Artifacts (local, gitignored root `vnext/artifacts/c-phase-197-pet-sim-baseline/`)

- `baseline.json` / `baseline.md` — the merged baseline (360 variant rows)
- `sprite_contract.json` — contract audit (errors: 0)
- `optional_readiness.json` / `.md` — 2,520-target readiness audit
- `source_to_runtime_quality.json` — quality audit summary copy
- `contact-sheets/` — all 10 species runtime contact sheets

## Determinism

`build_pet_sim_completion_baseline.py` was run twice; outputs are byte-identical
except the single top-level `generated_utc` field (verified by comparing with that
field stripped).

## Safety Boundaries

- No file under `sprites_runtime/`, `sprites_authored/`, `sprites_authored_verified/`,
  or `vnext/content/` was modified.
- No asset prep was run; the stable release lock is untouched.
- The builder refuses output paths outside `vnext/artifacts/`.
- No hosted AI, no local model, no network access, no SelfImprovement-surface change.

## Stop-Gate Checklist

- [x] Contract error_count is 0 (track would have halted otherwise).
- [x] No sprite or content file modified (`git status` clean apart from tool + docs).
- [x] Builder writes only under `vnext/artifacts/`.
- [x] Determinism verified across two runs.

## Consumers

- C-198: pilot row optional-family status.
- C-199/200: per-variant `missing_optional_families`.
- C-201: `loose_file_inventory.files` (classification `uncontracted_dead` = deletion
  list; `uncontracted_live` = groom registration).
- C-202: authored-verified coverage gaps.
- C-203: `quality_flags` + authored coverage.
