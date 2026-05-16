# Wevito Sprite Rung Phase Plan: C-PHASE 117 through 120

Date: 2026-05-15
Author: Claude (Opus 4.7), commissioned by `ssebrobles@gmail.com`
Audience: Codex at medium reasoning effort
Companion files:
- `docs/DECISION_LEDGER_2026-05-15.md`
- `docs/CLAUDE_MASTER_PLAN_2026-05-15.md`

## 0. Purpose

These four phases implement the actual sprite-cleanup ladder per the
S-decisions (S-A, S-A3, S-B5, S-F3, S-G6, S-S5, S-P5, S-11a/b/c/d).
This is where the Gemini-cleanup-loop is broken: instead of cleaning
all 3,360 frames in the cloud, we curate ~55 canonical templates
locally, palette-conform them, and propagate to all 6 color variants.

## 0.1 Inputs

- 30 sprite palette grammars (C-PHASE 99) declaring slot semantics +
  natural canonical palette + 6 rainbow variants per (species, age).
- General fallback grammar (C-PHASE 100) for edge cases.
- Image generation runtime (C-PHASE 98) — used in Rung 2 for CLIP
  embedding via ONNX.
- Chat UI + tool registry (C-PHASE 107 + 109) for bookmark integration.

## 0.2 Outputs

After Rung 4: all 60 cells (10 species × 3 ages × 2 genders) have
clean canonical templates per S-A3 frame counts (walk = 4, others = 2),
palette-conformed per S-P5, propagated to all 6 color variants. Game
displays clean sprites. LoRA training (C-PHASE 97) has a clean corpus.

---

## C-PHASE 117 — Sprite Rung 1: Golden-Seed UI + Heuristic Prefilter

**Goal:** Build the in-wevito UI where the user reviews heuristically-
prefiltered candidate frames per (species, age, gender, animation) cell
and marks goldens. Implements S-S5.

**Scope:**

- Add `SpriteTemplateCandidatePoolService`:
  - For each (species, age, gender, animation) cell, walks all four
    source trees (`sprites/`, `sprites_authored/`, `sprites_runtime/`,
    `incoming_sprites/`).
  - Runs heuristic prefilter (extends `report_sprite_visual_quality.py`):
    - canvas size match
    - alpha-edge cleanliness threshold
    - palette quantization (≤ 8 unique colors after the conformer)
    - silhouette continuity vs cell siblings
    - no Gemini checkerboard remnants (`clean_shared_sprite_assets.py`
      shape detector)
  - Returns top 5-8 survivors per cell as `SpriteCandidate` records.
- Add `GoldenSeedingService`:
  - Stores user-selected goldens in `sprites_authored/_golden_index.json`
    (per S-S5 sidecar JSON storage).
  - Schema: `{ species, age, gender, animation, frame_index, source_path,
    selected_at_utc, selected_by }`.
  - Append-only (mirror `AuditLedgerService` pattern in JSON form: new
    entries never overwrite; older entries marked `superseded=true`).
- Add `GoldenSeedingPanel` to `ToolPopupWindow`'s Creative Lab tab:
  - "Sprite Templates" sub-tab.
  - Cell selector (species → age → gender → animation chain).
  - Candidate grid (~5-8 frames at 4× scale).
  - Keyboard-first: arrow keys navigate cells, number keys pick candidate,
    space marks-and-advance.
  - Progress badge: "Day 1: 7 of 60 idle cells seeded."
- Heuristic prefilter Python tool extension:
  - `tools/build_sprite_candidate_pool.py` (new) — produces the JSON
    candidate manifests consumed by the C# service.
- New packet kinds: `sprite_candidate_pool_built`,
  `sprite_golden_seeded`, `sprite_golden_superseded`.

**Pattern:** Follow `LearningLabBundleService.cs` for the manifest +
sidecar JSON storage; follow `CreativeLearningLabWindow.xaml.cs` for the
review-grid UI pattern.

**Files likely touched:**

```text
vnext/src/Wevito.VNext.Shell/CreativeLearningLabWindow.xaml
vnext/src/Wevito.VNext.Shell/CreativeLearningLabWindow.xaml.cs
vnext/src/Wevito.VNext.Core/PlainLanguageExplainer.cs
vnext/content/tool_definitions.json
```

**New files:**

```text
vnext/src/Wevito.VNext.Core/SpriteTemplateCandidatePoolService.cs
vnext/src/Wevito.VNext.Core/SpriteCandidate.cs
vnext/src/Wevito.VNext.Core/GoldenSeedingService.cs
vnext/src/Wevito.VNext.Core/GoldenIndexEntry.cs
vnext/src/Wevito.VNext.Shell/GoldenSeedingPanel.xaml
vnext/src/Wevito.VNext.Shell/GoldenSeedingPanel.xaml.cs
tools/build_sprite_candidate_pool.py
vnext/tests/Wevito.VNext.Tests/SpriteTemplateCandidatePoolServiceTests.cs
vnext/tests/Wevito.VNext.Tests/GoldenSeedingServiceTests.cs
vnext/tests/Wevito.VNext.Tests/GoldenSeedingPanelTests.cs
docs/C_PHASE117_SPRITE_RUNG_1_GOLDEN_SEED_UI_2026-05-15.md
```

**Tests:**

- `SpriteTemplateCandidatePoolServiceTests.WalksAllFourSourceTrees`
- `SpriteTemplateCandidatePoolServiceTests.ReturnsTop5To8PerCell`
- `SpriteTemplateCandidatePoolServiceTests.FiltersCheckerboardArtifacts`
- `SpriteTemplateCandidatePoolServiceTests.FiltersOffPaletteFrames`
- `GoldenSeedingServiceTests.StoresGoldenInJsonSidecar`
- `GoldenSeedingServiceTests.SupersededEntriesPreserved`
- `GoldenSeedingServiceTests.RespectsKillSwitch`
- `GoldenSeedingPanelTests.KeyboardNavigationWorks`
- `GoldenSeedingPanelTests.MarkAndAdvanceWritesGolden`
- `GoldenSeedingPanelTests.ProgressBadgeUpdatesLive`
- `PlainLanguageExplainerTests.CoversSpriteCandidatePoolBuiltKind`
- `PlainLanguageExplainerTests.CoversSpriteGoldenSeededKind`

**Validation:** standard build/test/build-vnext + `python ./tools/build_sprite_candidate_pool.py --dry-run`.

**Stop gates:**
- Stop if prefilter passes frames containing checkerboard artifacts.
- Stop if golden index allows in-place edit of prior entries.
- Stop if UI lacks keyboard-first navigation.
- Stop if any cell shows fewer than 1 candidate (the prefilter must
  always return at least the best-available frame even if below quality
  threshold, with a `low_quality=true` flag).

**Rollback:** revert PR; goldens index remains; no UI to update it.

**Commit/PR:** branch `claude-implementation/c-phase-117-sprite-rung-1`,
title `C-PHASE 117: Sprite Rung 1 — Golden-seed UI + heuristic prefilter`.

**Auto-continue?** **No.** User must seed goldens manually; stop after
infrastructure ships.

---

## C-PHASE 118 — Sprite Rung 2: Image Embedding Scoring

**Goal:** Add CLIP-image embedding via ONNX so future candidates (from
LoRA or curation) can be scored against the seeded golden set. Implements
G6 similarity-to-golden gate.

**Depends on:** C-PHASE 117 (goldens exist) + C-PHASE 76 ONNX infrastructure.

**Scope:**

- Add `OnnxImageEmbeddingBackend` mirroring `OnnxEmbeddingBackend` from
  C-PHASE 76 (text); produces image embeddings via CLIP-image ONNX.
- Add `OnnxImageEmbeddingService` mirroring `OnnxTextEmbeddingService`:
  - Loads CLIP-image model from `vnext/content/local-models/image-embed/clip-vit-base/`
    via user-run `tools/install-image-embedder.ps1` (SHA-verified install).
  - Safe-degrades to `HashingImageEmbeddingService` (perceptual hash
    fallback) when model missing.
- Add `SpriteSimilarityService`:
  - `double ScoreCandidate(byte[] candidatePng, string cellId)`.
  - Reads the cell's goldens from `_golden_index.json`.
  - Embeds candidate + each golden; returns max cosine similarity.
  - Cached embeddings (idempotent).
- Add `BookmarkFromChatTool` (called from C-PHASE 107's chat UI):
  - When user clicks 🔖 on a chat message containing an image, the tool
    invokes `SpriteSimilarityService.ScoreCandidate` against any matching
    grammar.
- New packet kinds: `image_embedding_computed`, `sprite_similarity_scored`.

**Pattern:** Mirror `OnnxTextEmbeddingService.cs` and
`HashingTextEmbeddingService.cs` exactly; image variant.

**Files / tests:** standard. Includes installer script for image embedder.

**Stop gates:**
- Stop if installer auto-downloads weights without SHA verification.
- Stop if fallback path breaks when model missing.
- Stop if scoring is non-deterministic for same inputs.

**Auto-continue?** **No.** First image-side ML inference.

---

## C-PHASE 119 — Sprite Rung 3: Palette Conformer + Color Propagation

**Goal:** Once goldens are seeded (C-PHASE 117) and palette grammars
declared (C-PHASE 99), produce the propagated 6-color variant set for
every seeded cell. Implements S-P5 + S-11a/b.

**Depends on:** C-PHASE 99 + C-PHASE 117.

**Scope:**

- Add `ColorVariantPropagationService`:
  - For each seeded cell, reads the canonical golden frame.
  - Reads the grammar's 6 variants from `pet-sprite-<species>-<age>.json`.
  - For each variant, runs `PaletteConformer.Remap` with the variant's
    body palette substituted (outline + eye stay canonical per P5a/b).
  - Writes propagated frames to `sprites_authored/<species>/<age>/<gender>/<color>/<animation>_<NN>.png`.
- Add `tools/propagate-canonical-to-variants.ps1`:
  - User-trigger or autonomous-via-experiment-runner.
  - Runs through `GuardedMutationService` (dry-run + apply + post-proof).
- Sprite Workflow V2 surface extended:
  - "Propagate Goldens" button per cell in Creative Lab → Sprite Templates
    sub-tab.
  - Per-cell status shows: "Seeded (red) / Propagated (5 variants pending) /
    Complete (all 6 variants)".
- **Authored → runtime promotion (critical for visible game updates):**
  - After variant propagation lands new files in `sprites_authored/`,
    invoke the existing `SpriteWorkflowApplyService.PromoteToRuntime`
    (or equivalent) which copies validated authored frames into
    `sprites_runtime/<species>/<age>/<gender>/<color>/<animation>_<NN>.png`.
  - The Godot pet game reads from `sprites_runtime/` — this is the path
    that makes new sprites visible in-game.
  - Promotion runs the same heuristic prefilter from C-PHASE 117 to
    refuse degraded frames; failures roll back per `GuardedMutationService`.
  - On promotion success, emit a `sprite_runtime_promoted` packet with
    cell id + variant count.
- **Godot reload behavior:**
  - Godot caches imported textures by file mtime. New mtime → re-import on
    next Godot startup. Re-import is automatic; no special Godot signal needed.
  - The build hot-swap wrapper (C-PHASE 95) restarts the pet game process
    as part of every phase-merge cycle, so the user sees updated sprites
    within ~30s of phase merge (the natural-pause wait + smoke + swap).
  - For mid-phase user-triggered propagation (clicking "Propagate Goldens"
    button), send IPC to Godot via existing broker pipe with
    `reload_sprites` signal; Godot reloads the sprite atlas without
    restarting.
- New packet kinds: `sprite_variant_propagated`, `sprite_propagation_run`,
  `sprite_runtime_promoted`, `sprite_atlas_reloaded`.

**Pattern:** Follow `propagate_authored_colors.py` for the algorithm
shape; follow `SpriteWorkflowApplyService.cs` for the mutation pathway.

**Files / tests:** standard.

**Stop gates:**
- Stop if propagation modifies outline or eye colors.
- Stop if propagation writes outside `sprites_authored/`.
- Stop if any variant fails post-proof verification.

**Auto-continue?** **Yes.** Mechanical color propagation; uses existing
guarded mutation; user already approved the canonical golden in Rung 1.

---

## C-PHASE 120 — Sprite Rung 4: Remaining 7-Animation Expansion

**Goal:** Repeat rungs 1-3 for the other 7 animations (walk, eat, happy,
sad, sleep, sick, bathe). After this, all 60 cells × 8 animations × 6
variants = 2,880 ship-ready frames (down from the original 3,360 due to
the 2-frame compression per S-A3).

**Depends on:** C-PHASE 117 + 118 + 119.

**Scope:**

- This phase is mostly process, not new code.
- Reuses existing `GoldenSeedingPanel`, `ColorVariantPropagationService`,
  etc., for the additional 7 animations × 60 cells = 420 cells to seed +
  propagate.
- Adds new aggregate progress tracking:
  - `SpriteTemplateCompletionReportService` — computes "% of cells
    complete per animation" for the digest UI (C-PHASE 105).
- Once all animations complete, retires `incoming_sprites/` to
  `sprites_archive/<ts>-pre-c120/` (still on disk, never deleted, just
  out of the way).
- Updates README to point to the new ship-ready sprite layout.
- New packet kind: `sprite_template_completion_report`.

**Pattern:** Reuses everything from prior rungs.

**Files / tests:** smaller — mostly aggregate reporting.

**Stop gates:**
- Stop if any animation falls below 50% cell completion before phase merges.
- Stop if `incoming_sprites/` is deleted instead of archived.

**Auto-continue?** **Yes.** Mechanical expansion + reporting.

---

## Closing Notes

After all 4 rungs land, the sprite cleanup loop is closed permanently:

- 60 canonical cells × 2-4 frames per animation × 8 animations = ~1,320
  hand-curated canonical frames (the work surface that breaks the Gemini
  loop).
- Propagation to 6 variants × 1,320 = ~7,920 ship-ready frames (most of
  which are mechanically derived from canonical).
- LoRA training corpus from C-PHASE 97 has clean source data.
- AI-generated future sprite candidates (C-PHASE 88 experiment kind) get
  scored via C-PHASE 118 similarity service.

The original 3,360-frame Gemini-imperfect set is preserved in
`sprites_archive/` for historical reference but no longer drives any
ship path.
