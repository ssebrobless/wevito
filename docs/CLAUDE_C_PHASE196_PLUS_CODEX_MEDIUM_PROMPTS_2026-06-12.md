# Codex Medium-Reasoning Phase Prompts — Pet-Sim Completion Track: C-PHASE 196–205

Date: 2026-06-12
Audience: Codex CLI (medium reasoning effort)
Predecessor plan: `docs/CLAUDE_C_PHASE196_PLUS_PET_SIM_COMPLETION_PLAN_2026-06-12.md`

This file contains ten copy-paste prompts. **None is approved for implementation until
the owner gives the explicit go-ahead for the pet-sim completion track and resolves
decisions D1–D3 (plan §5).** After go-ahead: §1 and §2 run first, in order. §5 (C-200)
additionally requires the owner's per-species approval of the C-199 contact sheets.
§8 (C-203) additionally requires owner approval of the triage queue before its apply
batches run.

User pre-step before §1: merge PR #298 (C-PHASE 195).

---

## §0 — Common Header (paste into every phase)

```text
You are Codex CLI at medium reasoning effort, executing one C-PHASE of the Wevito
pet-sim completion track.

Working repo: C:\Users\fishe\Documents\projects\wevito

Pre-flight gate (run before any change; halt on any failure):
1) Sync: git fetch origin && git checkout main && git pull --ff-only origin main
2) Verify clean tree: git status --porcelain (must be empty)
3) Verify main contains C-PHASE 195: git merge-base --is-ancestor d7a5e916a HEAD
   (d7a5e916a = squash-merge of PR #298 onto main on 2026-06-12; PR #298 was
   squash-merged, so the branch SHA ee4aa82f9 is NOT an ancestor of main — anchor on
   the squash commit)
4) Build: dotnet build .\vnext\Wevito.VNext.sln
5) Tests: dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
   (expected full count: derive from the C-PHASE 196 report once it exists; 1715 at planning time)
6) Vnext build: powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
7) Diff check: git diff --check
If any pre-flight step fails: STOP. Do not patch around it. Open a Stop card and exit.

Hard invariants (every phase in this track):
- Pet-sim track only. DO NOT touch the apply-runner family, SupervisedImprovementLoop,
  InvariantViolationWatchdog, watchdog observer wiring, caller-allowlist pin tests,
  maturity clocks, or anything under the SelfImprovement namespace. No new
  ScanAndEmit hosts.
- No hosted AI as Wevito's runtime brain. No new System.Net.Http runtime use. The
  Gemini handoff lane is authoring-time only and human/driver operated; its outputs
  enter the repo only through the existing import + verification tools.
- No silent network access. No new sockets.
- NEVER run asset prep. NEVER pass -AllowAssetPrepAfterStable. Always build with
  -SkipAssetPrep. The stable release lock (vnext/content/stable_release_lock.json)
  stays in place and unmodified.
- Every sprite mutation: exact scope declared in the phase prompt, dry-run, backup,
  sha256 evidence, apply, post-proof (audit_sprite_contract.py error_count=0 AND
  report_runtime_canvas_mismatches.py mismatch/missing/invalid all 0), rollback
  script, user-visible report. Mutation outside the declared scope = stop gate.
- KillSwitchService.IsActive() honored by every new or touched stateful service.
- AuditLedgerService remains append-only. No UPDATE / DELETE FROM / DROP TABLE SQL.
- Every new audit packet kind appears in PlainLanguageExplainer.KnownPacketKinds with
  a plain-language sentence. Every new capability flag defaults bool.FalseString in
  CapabilityFlagInventory.
- Pets remain visually normal pet-sim characters. No AI-task animation overlays.
- Ball policy: runtime overlay only. NO ball pixels baked into any sprite frame.
- Runtime sprite contract: 28x24 RGBA. Required families idle4 walk6 eat4 happy4 sad2
  sleep2 sick4 bathe4 (+ drink4 groom4 present matrix-wide). Optional families and
  frame counts: play_ball 6, hold_ball 4, pickup_ball 4, drop_ball 4,
  carry_ball_walk 6, carry_ball_run 6 (tools/audit_sprite_contract.py
  OPTIONAL_EXPANDED_ANIMATIONS is the source of truth).
- Where a validation step pins a focused test filter with a minimum count, derive the
  minimum from the actual [Fact]/[Theory] count in the named test file at the current
  anchor SHA. Never copy a count from a prior phase report.
- Codex never auto-merges Auto-continue=No phases. Every phase here is Auto-continue=No.
- Never touch .codex\worktrees. Never kill IDE/foreign processes.

Validation (run before opening the PR; halt on any failure):
A) Focused test filter (per-phase, below)
B) dotnet build .\vnext\Wevito.VNext.sln
C) dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
D) powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
E) git diff --check
F) Sprite phases only: python .\tools\audit_sprite_contract.py --output <artifact-root>\sprite_contract.json  (error_count must be 0)
G) Sprite phases only: python .\tools\report_runtime_canvas_mismatches.py --output <artifact-root>\runtime_canvas.json --markdown <artifact-root>\runtime_canvas.md --fail-on-mismatch

History append (every phase, after PR opens):
Append one line to docs/codex-phase-history.jsonl matching the existing shape:
{"phase_id":"C-PHASE NNN","timestamp_utc":"<ISO8601 UTC, 7 fractional digits, Z>","branch":"claude-implementation/c-phase-NNN-<slug>","pr_url":"https://github.com/ssebrobless/wevito/pull/<N>"}

End-of-phase report block (post in the PR body and final message):
C-PHASE NNN: GREEN|RED
Anchor: <base branch/tag> @ <sha>
PR: <url>
Build: PASS|FAIL (warnings, errors)
Full test: PASS|FAIL (passed/total; expected total)
<focused suites: name count/count>
Sprite contract: error_count=<n> | N/A
Runtime canvas: mismatch=<n> missing=<n> invalid=<n> | N/A
Optional readiness: <complete>/<targets> | N/A
build-vnext: PASS|FAIL
git diff --check: PASS
git status: clean
Mutation scope honored: YES|NO (declared: <scope>)
Files touched under .codex\worktrees: NONE
IDE/foreign processes killed: NONE
KnownPacketKinds count: <n> (delta from 160 explained)
Auto-continue: No

PR convention: one PR per phase; branch + title per phase section; PR body links the
plan section, this prompt section, and the phase report doc.
```

---

## §1 — C-PHASE 196: Main-sync product truth + BUG-005 disposition

```text
Start with the §0 common header. Run only after the user confirms PR #298 is merged.

Phase: C-PHASE 196 — Main-sync product truth + BUG-005 disposition
Branch: claude-implementation/c-phase-196-main-sync-product-truth

Goal:
Re-establish trustworthy main and disposition BUG-005 (Critical). Test/docs/evidence
only. No production source change of any kind.

Steps:
1) Pre-flight per §0 (step 3 must confirm ee4aa82f9 is an ancestor of main).
2) Record main SHA, full-suite pass count, and KnownPacketKinds count (must be 160)
   into vnext/artifacts/c-phase-196-main-truth/main_truth.json.
3) BUG-005 cold-cache repro, exactly per docs/BUG_BOARD.md BUG-005 detail:
   a) dotnet build-server shutdown
   b) Ensure no Wevito.VNext.* processes are running (report, do not kill foreign ones;
      if a Wevito shell you did not start is running, STOP and ask).
   c) Delete every bin\ and obj\ directory under vnext\
   d) dotnet build .\vnext\Wevito.VNext.sln
   e) dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
      --filter "FullyQualifiedName~ArtifactRenameApplyRunnerTests|FullyQualifiedName~ArtifactRenameRollbackRunnerTests"
      --logger "console;verbosity=normal"
      Expected minimum: derive from the [Fact] counts in the two test files at main HEAD.
   f) Then the full suite (no filter) to confirm overall green.
4) Disposition:
   - GREEN: edit docs/BUG_BOARD.md — move BUG-005 to Closed with resolution text citing
     the C-195 merge and the evidence paths under vnext/artifacts/c-phase-196-main-truth/;
     update the Summary counts; append a Session Note dated today.
   - RED: update BUG-005 Open entry with the fresh failure output paths, write the
     report doc with verdict RED, open the PR as Draft, and HALT THE TRACK. Do not
     proceed to C-PHASE 197.
5) Phase report: docs/C_PHASE196_MAIN_SYNC_PRODUCT_TRUTH_2026-MM-DD.md
   (state main SHA, full count, BUG-005 verdict + evidence paths, KnownPacketKinds=160).
6) History append per §0.

Files in scope (only these):
- docs/BUG_BOARD.md (edit)
- docs/C_PHASE196_MAIN_SYNC_PRODUCT_TRUTH_2026-MM-DD.md (new)
- docs/codex-phase-history.jsonl (one append)
- vnext/artifacts/c-phase-196-main-truth/** (new evidence)

Stop gates:
- Any production or test source file modified.
- Full suite count regressed vs the count reported in PR #298 (1715) without explanation.
- KnownPacketKinds != 160.
- BUG-005 repro red (Draft PR + halt, per step 4).

Validation: §0 A–E with A = the step-3e filter.
Commit message: C-PHASE 196: main-sync product truth + BUG-005 disposition (test/docs only)
PR title: C-PHASE 196: main-sync product truth + BUG-005 disposition
Auto-continue: No
```

---

## §2 — C-PHASE 197: Pet-sim completion baseline (sprite truth report)

```text
Start with the §0 common header. Run only after C-PHASE 196 merged GREEN.

Phase: C-PHASE 197 — Pet-sim completion baseline
Branch: claude-implementation/c-phase-197-pet-sim-completion-baseline

Goal:
One authoritative read-only baseline that C-PHASE 198–203 consume. No sprite mutation,
no production source change. New tooling is allowed only as a read-only report builder.

Steps:
1) Artifact root: vnext/artifacts/c-phase-197-pet-sim-baseline/
2) Run read-only audits into the artifact root:
   a) python .\tools\audit_sprite_contract.py --output ...\sprite_contract.json
   b) python .\tools\audit_optional_animation_readiness.py --output ...\optional_readiness.json --markdown ...\optional_readiness.md
   c) python .\tools\audit_source_to_runtime_quality.py (full matrix) > ...\source_to_runtime_quality.json
   d) python .\tools\export_pet_runtime_contact_sheets.py --runtime-root .\sprites_runtime --output-root ...\contact-sheets
3) New tool: tools/build_pet_sim_completion_baseline.py (read-only). It merges 2a–2c
   plus two new inventories into one baseline.json + baseline.md:
   - loose-file inventory: every file under sprites_runtime/<sp>/<age>/<gender>/<color>/
     whose family prefix is in neither EXPECTED_ANIMATIONS nor
     OPTIONAL_EXPANDED_ANIMATIONS (import both from tools/audit_sprite_contract.py;
     do not redefine). Expected at planning time: 504 play_* files across 126 variants
     (frog 36, goose 36, snake 36, rat 12, deer 6) — report actuals, do not hardcode.
   - authored-verified coverage matrix: per species x family, count of files under
     sprites_authored_verified/ (expected at planning time: locomotion only; goose and
     squirrel 0; raccoon partial — report actuals).
   baseline.json must carry per-variant rows with: missing optional families, loose
   files, quality scores, authored coverage. Deterministic output (stable ordering,
   no timestamps inside rows; one top-level generated_utc field is fine).
4) Phase report: docs/C_PHASE197_PET_SIM_COMPLETION_BASELINE_2026-MM-DD.md summarizing
   the headline numbers (optional-family gap count, loose-file count, worst quality
   rows, authored-coverage gaps) and naming the consuming phases.
5) History append per §0.

Files in scope:
- tools/build_pet_sim_completion_baseline.py (new, read-only builder)
- docs/C_PHASE197_PET_SIM_COMPLETION_BASELINE_2026-MM-DD.md (new)
- docs/codex-phase-history.jsonl (one append)
- vnext/artifacts/c-phase-197-pet-sim-baseline/** (new)

Stop gates:
- sprite_contract.json error_count > 0 (C-130 base regressed): Draft PR + HALT TRACK.
- Any file under sprites_runtime/, sprites_authored/, sprites_authored_verified/, or
  vnext/content/ modified.
- Builder writes anywhere outside the artifact root.
- Two consecutive builder runs produce different row content (nondeterminism).

Validation: §0 A–E; A = dotnet test ... --filter "FullyQualifiedName~Sprite" with the
minimum derived per §0 (existing sprite suites must stay green; no new C# tests
required this phase). Plus §0 F (already produced in step 2a).
Commit message: C-PHASE 197: pet-sim completion baseline (read-only sprite truth report)
PR title: C-PHASE 197: pet-sim completion baseline
Auto-continue: No
```

---

## §3 — C-PHASE 198: Ball-family pilot completion (goose/baby/female/blue)

```text
Start with the §0 common header. Run only after C-PHASE 197 merged GREEN and the
owner confirmed decision D3 (accept_candidate_for_apply_plan).

Phase: C-PHASE 198 — Ball-family pilot completion (goose/baby/female/blue)
Branch: claude-implementation/c-phase-198-ball-pilot-completion

Goal:
Make exactly one variant fetch-complete (all six ball families) and prove the fetch
sequence visually in the running app. This de-risks C-199/200.

Declared mutation scope (exact): sprites_runtime/goose/baby/female/blue/
carry_ball_walk_00..05.png and carry_ball_run_00..05.png — 12 ADDITIVE files. Nothing
else under sprites_runtime may change. The existing 60 files in that directory are
not modified.

Steps:
1) Verify the pilot's existing optional families (play_ball 6, hold_ball 4,
   pickup_ball 4, drop_ball 4) against audit_optional_animation_readiness.py.
2) carry_ball_walk: take the six candidate frames from
   vnext/artifacts/c-phase-56-optional-animation-pilot/goose-baby-female-blue-carry-ball-walk/candidate-frames/
   Verify each sha256 against the C-56 packet's target-manifest.json. Any mismatch:
   STOP (stop gate). Re-render a fresh comparison contact sheet of candidates vs the
   variant's current walk frames for the PR.
3) carry_ball_run: synthesize 6 frames deterministically from the variant's walk_00..05
   + the carry candidates (body pose only, no ball pixels, 28x24 RGBA). Document the
   transform in the phase report. Place candidates in the phase artifact root first.
4) Guarded apply (C-130 pattern), artifact root
   vnext/artifacts/c-phase-198-ball-pilot/:
   dry-run listing -> backup manifest (directory state + sha256 of all 60 existing
   files) -> copy in the 12 files -> post-proof: §0 F and G, plus
   audit_optional_animation_readiness.py showing 7/7 families complete for this
   variant -> write rollback.ps1 that deletes the 12 files and re-verifies the backup
   hashes.
5) Update vnext/artifacts/c-phase-56-optional-animation-pilot/.../decision-needed.md:
   chosen label accept_candidate_for_apply_plan, dated, pointing at this phase.
6) In-app proof: build the shell (per §0 D), launch it, then use DevControl to spawn
   the pilot variant and trigger the Play/fetch action. Capture each fetch stage
   (MoveToBall, Pickup, Hold, CarryWalk, CarryRun, Drop, ReturnIdle) via
   tools/capture-vnext-window.ps1 into the artifact root, plus a DevControl snapshot
   per stage showing the AnimationState. CarryWalk and CarryRun must show the real
   carry families (not Walk fallback) — confirm via the snapshot AnimationState.
7) Phase report: docs/C_PHASE198_BALL_PILOT_COMPLETION_2026-MM-DD.md with the
   verification table, transform description, capture index, and rollback path.
8) History append per §0.

Stop gates:
- Any C-56 candidate sha256 mismatch.
- Any write outside the declared 12-file scope or the artifact roots.
- Post-proof failure (contract, canvas, or readiness not 7/7 for the pilot).
- Step 6 shows a fallback family at CarryWalk or CarryRun stage.
- Ball pixels detected baked into any new frame.

Validation: §0 A–G; A = dotnet test ... --filter "FullyQualifiedName~PetSimulation|FullyQualifiedName~Sprite"
(minimum derived per §0).
Commit message: C-PHASE 198: ball-family pilot completion for goose/baby/female/blue (12 additive frames; fetch sequence proven in-app)
PR title: C-PHASE 198: ball-family pilot completion (goose/baby/female/blue)
Auto-continue: No
```

---

## §4 — C-PHASE 199: Ball-family synthesis tool + full-matrix dry-run candidates

```text
Start with the §0 common header. Run only after C-PHASE 198 merged GREEN.

Phase: C-PHASE 199 — Ball-family synthesis tool + full-matrix dry-run candidates
Branch: claude-implementation/c-phase-199-ball-family-synthesis-candidates

Goal:
Production tool + reviewable candidates for all six ball families across the matrix.
ZERO writes to sprites_runtime/ in this phase.

Steps:
1) New tool: tools/generate_optional_ball_families.py
   - Inputs: --runtime-root (read-only), --out-root (must be under vnext/artifacts/),
     --species/--age/--gender/--color filters, --families filter.
   - For each target variant, synthesize body-pose frames for play_ball(6),
     hold_ball(4), pickup_ball(4), drop_ball(4), carry_ball_walk(6), carry_ball_run(6)
     from that variant's existing on-contract frames (walk/happy/idle bases +
     deterministic per-family transforms). Take the C-PHASE 198 transform for
     carry families as the reference implementation; document each family's transform
     in the tool docstring.
   - Contract: 28x24 RGBA, frame counts per OPTIONAL_EXPANDED_ANIMATIONS (import the
     dict from tools/audit_sprite_contract.py; do not redefine), NO ball pixels.
   - Deterministic: two runs over the same inputs produce byte-identical outputs.
   - Skips families a variant already has in runtime (the goose pilot) and records
     the skip in its manifest.
   - Writes a per-species manifest with sha256 per candidate frame.
2) Full-matrix dry-run into vnext/artifacts/c-phase-199-ball-candidates/<species>/...
   Expected volume: 360 variants x 30 frames minus the pilot's existing 30 — report
   actual counts in the manifest; do not hardcode.
3) Review sheets: one contact sheet per species (10 PNGs) under
   .../review-sheets/, each showing per age/gender/color rows: existing walk+happy
   reference frames alongside all six candidate families. The goose sheet must include
   the C-198 applied pilot row labeled as the visual reference.
4) Determinism proof: run the tool twice; diff the manifests' sha256 sets; record the
   result in the phase report.
5) Phase report: docs/C_PHASE199_BALL_FAMILY_SYNTHESIS_CANDIDATES_2026-MM-DD.md —
   transform table, candidate counts, review-sheet index, and an explicit
   "AWAITING PER-SPECIES OWNER APPROVAL BEFORE C-PHASE 200" section listing the 10
   species with empty approval checkboxes.
6) History append per §0.

Files in scope:
- tools/generate_optional_ball_families.py (new)
- docs/C_PHASE199_BALL_FAMILY_SYNTHESIS_CANDIDATES_2026-MM-DD.md (new)
- docs/codex-phase-history.jsonl (one append)
- vnext/artifacts/c-phase-199-ball-candidates/** (new)

Stop gates:
- Any write outside vnext/artifacts/c-phase-199-ball-candidates/.
- Any candidate frame failing 28x24 RGBA or its family frame count.
- Determinism proof fails.
- Ball pixels detected in any candidate.

Validation: §0 A–E; A = dotnet test ... --filter "FullyQualifiedName~Sprite"
(minimum derived per §0). F/G not required (no runtime mutation) but run F anyway to
prove the runtime tree is untouched (error_count=0, file census unchanged vs C-197).
Commit message: C-PHASE 199: ball-family synthesis tool + full-matrix dry-run candidates (artifact-only)
PR title: C-PHASE 199: ball-family synthesis tool + dry-run candidates
Auto-continue: No
```

---

## §5 — C-PHASE 200: Guarded matrix apply of six ball families

```text
Start with the §0 common header. Run only after C-PHASE 199 merged AND the owner has
approved species from the C-199 review sheets. Only approved species are in scope;
list them explicitly at the top of your working notes before starting.

Phase: C-PHASE 200 — Guarded matrix apply of six ball families
Branch: claude-implementation/c-phase-200-ball-family-matrix-apply

Goal:
Apply the approved C-199 candidates to sprites_runtime, one species batch at a time
(C-130 pattern), and pin ball families into the runtime coverage tests.

Declared mutation scope (exact): for each APPROVED species,
sprites_runtime/<species>/<age>/<gender>/<color>/{play_ball,hold_ball,pickup_ball,drop_ball,carry_ball_walk,carry_ball_run}_NN.png
— ADDITIVE only. No existing file is modified or deleted. Plus (one-time):
sprites_runtime/_metadata/prop_anchors.json (new file), and the sprite runtime
coverage test expectations under vnext/tests/.

Per-species batch procedure (artifact root
vnext/artifacts/c-phase-200-ball-apply/<NNN>-<species>/):
1) Verify candidate sha256s against the C-199 manifest for the species. Mismatch: STOP.
2) Dry-run listing of every file to be written; confirm zero collisions with existing
   files (the goose pilot's existing families are skips, recorded as such).
3) Apply (copy candidates in).
4) Post-proof: §0 F and G + audit_optional_animation_readiness.py shows all 216
   species ball targets (36 variants x 6 families) present + matrix sweep:
   powershell ...\tools\run-matrix-sweep.ps1 -ArtifactRoot <root>\cockpit-sweep -Species <species>
5) Batch summary.json + rollback.ps1 (deletes exactly the batch's written files).
6) A red post-proof: run rollback.ps1, verify restoration, write the batch RED summary,
   HALT the phase (do not start the next species), open the PR as Draft.

One-time, in the same PR (first batch):
- sprites_runtime/_metadata/prop_anchors.json: per-species default ball anchor entries
  in the schema audit_optional_animation_readiness.py reads
  (load_prop_anchor_metadata). Re-run the readiness audit to confirm
  runtime_prop_anchor_supported rises accordingly.
- Update the sprite runtime coverage test expectations (find via
  dotnet test --filter "FullyQualifiedName~Sprite" + the build-vnext coverage step) so
  the six ball families are ASSERTED present for approved species going forward.
  Derive new expected counts from the actual post-apply tree, not arithmetic.

Phase report: docs/C_PHASE200_BALL_FAMILY_MATRIX_APPLY_2026-MM-DD.md — per-batch
table (species, files written, post-proof results, rollback path), coverage-test
delta, readiness summary before/after.
History append per §0.

Stop gates:
- Any write to a non-approved species.
- Any overwrite of an existing runtime file.
- Any batch post-proof failure (after rollback: Draft PR + halt).
- Full suite red after coverage-test update.
- prop_anchors.json schema rejected by the readiness audit.

Validation: §0 A–G; A = dotnet test ... --filter "FullyQualifiedName~Sprite"
(minimum derived per §0, post-update). Full suite per §0 C must be green.
Commit message: C-PHASE 200: guarded matrix apply of six ball families (<N> species batches; additive; coverage tests pinned)
PR title: C-PHASE 200: ball-family matrix apply
Auto-continue: No
```

---

## §6 — C-PHASE 201: Loose `play_*` removal + contract stray-family pin

```text
Start with the §0 common header. Run only after C-PHASE 197 merged GREEN and the
owner confirmed decision D1 = remove. (If D1 = register instead: STOP and request a
revised prompt; do not improvise a registration.)

Phase: C-PHASE 201 — Loose play_* removal + contract stray-family pin
Branch: claude-implementation/c-phase-201-loose-play-removal-stray-pin

Goal:
Delete the dead generic play_* frames (unreachable by any action or engine path),
REGISTER the live-but-uncontracted groom family in the sprite contract, and pin the
contract so stray families can never silently accumulate again.

C-197 finding that amends this phase (2026-06-12): `groom` (4 frames, present
360/360) is in neither EXPECTED_ANIMATIONS nor OPTIONAL_EXPANDED_ANIMATIONS, but it
IS live: PetAnimationState.Groom resolves its primary animation id via
CurrentAnimationState.ToString().ToLowerInvariant() in
Wevito.VNext.Shell/SpriteAssetService.cs (the GetFallbackAnimationId "happy" entry is
fallback-only). `play` has no PetAnimationState member and no action maps to it
(actions.json play -> animationState happy + optionalAnimationFamily play_ball), so
play_* is dead. Therefore BEFORE the stray-family pin lands: add "groom": 4 to
EXPECTED_ANIMATIONS in tools/audit_sprite_contract.py. Without this, the pin would
flag 1,440 live groom files. The C-197 baseline.json loose-file inventory classifies
each loose file as uncontracted_live (groom -> register) vs uncontracted_dead
(play -> delete); the deletion list is the uncontracted_dead entries ONLY.

Declared mutation scope (exact): ONLY the files listed in the C-PHASE 197
baseline.json loose-file inventory (expected ~504 play_*.png across ~126 variants —
use the inventory, never a re-glob, as the deletion list). DELETE only; no file
content modified.

Steps (artifact root vnext/artifacts/c-phase-201-loose-play-removal/):
0) Register groom: add "groom": 4 to EXPECTED_ANIMATIONS in
   tools/audit_sprite_contract.py; re-run the contract audit (error_count must stay 0,
   now with groom counted as required 360/360).
1) Extract the deletion list from the C-197 baseline.json: the
   loose_file_inventory.files entries with classification == "uncontracted_dead"
   ONLY. Cross-check: every path exists, every filename starts with play_, count
   matches the baseline uncontracted_dead_count. Any divergence: STOP.
2) Backup: zip all listed files with relative paths + sha256 manifest.
3) Dry-run listing -> delete -> post-proof:
   a) §0 F and G (contract error_count=0; canvas clean)
   b) File census: runtime PNG count dropped by exactly the deletion-list count.
   c) Full test suite green (§0 C).
4) rollback.ps1: restores the zip and re-verifies hashes.
5) Stray-family pin (both layers):
   a) tools/audit_sprite_contract.py: add a strict check (default ON) that fails any
      runtime animation file whose family prefix is in neither EXPECTED_ANIMATIONS nor
      OPTIONAL_EXPANDED_ANIMATIONS.
   b) New test vnext/tests/Wevito.VNext.Tests/SpriteRuntimeStrayFamilyTests.cs that
      walks sprites_runtime and asserts the same — fails fast in CI without Python.
6) Phase report: docs/C_PHASE201_LOOSE_PLAY_REMOVAL_2026-MM-DD.md (deletion count,
   backup path, post-proof results, pin description).
7) History append per §0.

Stop gates:
- Deletion list diverges from the C-197 inventory by even one path.
- Any non-play_* file deleted or any file modified.
- Post-proof failure (rollback, Draft PR, halt).
- The new strict check/test fails on the post-deletion tree (would mean another stray
  family exists — report it, do not delete it in this phase).

Validation: §0 A–G; A = dotnet test ... --filter "FullyQualifiedName~SpriteRuntimeStrayFamily|FullyQualifiedName~Sprite"
(minimum derived per §0 including the new file's facts).
Commit message: C-PHASE 201: remove dead generic play_* frames (guarded, backed up) + stray-family contract pin
PR title: C-PHASE 201: loose play_* removal + stray-family pin
Auto-continue: No
```

---

## §7 — C-PHASE 202: Authored locomotion completion — goose, squirrel, raccoon

```text
Start with the §0 common header. Run only after C-PHASE 197 merged GREEN. This is a
cooperative phase: it has a planned human round-trip point and a clean blocked exit.

Phase: C-PHASE 202 — Authored locomotion completion: goose, squirrel, raccoon
Branch: claude-implementation/c-phase-202-authored-locomotion-completion

Goal:
Close the authored-verified locomotion gap pinned by C-197 (goose 0, squirrel 0,
raccoon partial) using the existing focused-family handoff lane.

Declared mutation scope (exact): sprites_authored/ + sprites_authored_verified/ +
sprites_runtime/ idle_* and walk_* files for goose, squirrel, raccoon only (raccoon:
only the rows C-197 lists as missing). Nothing else.

Stage 1 — Prepare (Codex):
1) python .\tools\prepare_motion_gemini_handoff.py --family locomotion --species goose squirrel raccoon
2) Record the prepared pack index into
   vnext/artifacts/c-phase-202-authored-locomotion/prepared-packs.json.
3) If the returned boards (Stage 2) are not yet available: commit Stage 1 only, write
   a phase_blocked audit row, mark the report "BLOCKED AT ROUND-TRIP", open the PR as
   Draft, and exit GREEN-blocked. Resume later on the same branch.

Stage 2 — Round-trip (owner or tools/batch-drive-live-gemini.ps1): outside Codex.

Stage 3 — Import + verify + apply (Codex, after boards are returned):
4) For each returned board: python .\tools\import_authored_family_board.py (per its
   own verification: identity preserved vs canonical source sheets, transparent
   background, 28x24 grid contract). Rejected boards go back to Stage 2; record each
   rejection reason.
5) python .\tools\propagate_authored_colors.py per accepted species/age/gender into
   the authored tree.
6) Guarded apply into sprites_runtime for the in-scope idle/walk rows: backup +
   sha256 manifest of every file to be OVERWRITTEN (this phase does overwrite) ->
   apply -> post-proof §0 F and G + matrix sweep per species -> rollback.ps1
   (restores backups, re-verifies hashes).
7) Re-run the authored-coverage matrix from C-197's builder; locomotion coverage for
   the three species must now be complete (or exactly match the accepted-board subset
   if some boards were rejected — state which).
8) Phase report: docs/C_PHASE202_AUTHORED_LOCOMOTION_COMPLETION_2026-MM-DD.md.
9) History append per §0.

Stop gates:
- Any import-tool identity/contract rejection silently overridden.
- Mutation outside the three species' idle/walk rows.
- Post-proof failure (rollback, Draft PR, halt).
- Any hosted-AI call from Codex itself (the round-trip is owner/driver-operated only).

Validation: §0 A–G; A = dotnet test ... --filter "FullyQualifiedName~Sprite"
(minimum derived per §0).
Commit message: C-PHASE 202: authored locomotion completion for goose/squirrel/raccoon (handoff round-trip + guarded apply)
PR title: C-PHASE 202: authored locomotion completion (goose, squirrel, raccoon)
Auto-continue: No
```

---

## §8 — C-PHASE 203: Quality triage queue + targeted authored refresh batches

> **AMENDMENT R2 (2026-06-12, plan §10):** the C-199 review returned 0/10 approved,
> so C-203 now runs BEFORE C-200 and C-202-import. Preconditions become: C-197 AND
> C-199 AND C-201 merged (NOT 200/202). Stage 1's machine-built queue is REPLACED by
> the owner-seeded queue already at
> `vnext/artifacts/c-phase-203-quality-triage/triage-queue.json` (owner review is
> ground truth; machine flags under-detect — fox/raccoon/squirrel/crow are 0/6
> flagged but owner-reported). The phase splits into two lanes per plan §10.2:
> **C-203A** procedural cleanup (crow, goose, raccoon, rat, squirrel; branch
> `claude-implementation/c-phase-203a-procedural-cleanup`) and **C-203B** Gemini
> regeneration (deer, fox, frog, pigeon, snake; branch
> `claude-implementation/c-phase-203b-gemini-regen`), one PR each. C-203B's
> round-trip session also carries the C-202 locomotion boards (single owner driving
> session). Stage-2 batch machinery below applies unchanged to both lanes; every
> batch post-proof adds a BUG-007 color-conformance check (per-variant tint
> verification of required families against the variant's color id). After both
> lanes: run §11 (C-199R) before §5 (C-200).

```text
Start with the §0 common header. Run only after C-PHASE 197 AND 200 AND 202 merged.
The queue (Stage 1) needs explicit owner approval before any batch (Stage 2) runs.

Phase: C-PHASE 203 — Quality triage queue + targeted authored refresh batches
Branch: claude-implementation/c-phase-203-quality-triage-refresh

Goal:
Honest quality pass: triage every family row against a written rubric; refresh only
what falls below the bar, through the same guarded machinery.

Stage 1 — Triage queue (report-only):
1) New tool tools/build_sprite_quality_triage_queue.py (C-128 mold): consumes the
   C-197 baseline + C-200 post-apply readiness + C-202 coverage rerun; emits
   vnext/artifacts/c-phase-203-quality-triage/triage_queue.{json,md} with one row per
   species/age/gender/family below the quality bar. Each row: priority (P0 visual
   defect / P1 off-model or jarring motion / P2 synthesized-but-acceptable-candidate /
   P3 cosmetic), evidence (quality score + contact-sheet crop path), and repair lane
   (authored-refresh via handoff | tool-regenerate | accept-as-is recommendation).
   Write the rubric thresholds INTO the tool docstring and the queue header.
2) Phase report Stage-1 section + "AWAITING OWNER QUEUE APPROVAL" marker. Open the PR
   now (Draft) so the owner reviews the queue in-PR.

Stage 2 — Drain approved rows (after owner approval, same branch):
3) Group approved rows into batches (one species/family group per batch). Per batch,
   by lane:
   - authored-refresh: the C-202 Stage 1/3 procedure for that family
     (prepare_motion_gemini_handoff.py supports care/expression families per
     tools/authored_motion_specs.py; blocked-at-round-trip handling identical to C-202).
   - tool-regenerate: regenerate via the relevant existing tool, candidates to
     artifacts first, owner-visible contact sheet, then guarded apply.
   Every batch: declared scope, backup+sha256, post-proof §0 F and G + per-species
   matrix sweep, rollback.ps1, summary.json — the C-130 pattern, no exceptions.
4) Re-run the quality audit on drained rows; scores must clear the rubric bar or the
   batch is RED (rollback + halt for review).
5) Phase report: docs/C_PHASE203_QUALITY_TRIAGE_REFRESH_2026-MM-DD.md — queue stats,
   batch table, before/after scores.
6) History append per §0.

Stop gates:
- Any batch outside the owner-approved queue rows.
- Any apply without its batch backup/post-proof/rollback evidence.
- Post-refresh score below bar (rollback that batch, halt).
- Queue builder writes outside its artifact root.

Validation: §0 A–G; A = dotnet test ... --filter "FullyQualifiedName~Sprite"
(minimum derived per §0).
Commit message: C-PHASE 203: sprite quality triage queue + targeted refresh batches (rubric-gated)
PR title: C-PHASE 203: quality triage + targeted refresh
Auto-continue: No
```

---

## §9 — C-PHASE 204: Play/fetch wiring verification + all-action visual proof matrix

```text
Start with the §0 common header. Run only after C-PHASE 200 AND 201 merged.

Phase: C-PHASE 204 — Play/fetch wiring verification + all-action visual proof matrix
Branch: claude-implementation/c-phase-204-play-fetch-action-proof

Goal:
Prove the game plays as designed with the completed art. Test/evidence phase; the ONLY
allowed production-code change is fixing a Play->fetch wiring gap if one is found.

Steps:
1) Unit tests (new file vnext/tests/Wevito.VNext.Tests/FetchSequenceFamilyResolutionTests.cs):
   - ResolveFetchStageIntent returns PickupBall/HoldBall/CarryBallWalk/CarryBallRun/
     DropBall (no fallbacks) when availableOptionalFamilies contains the full set.
   - ResolveFetchStageIntent fallback chains still resolve correctly for an empty set
     (regression guard for variants the owner excluded in C-200, if any).
   - ResolveActionVisualIntent for the Play ActionDefinition (optionalAnimationFamily
     play_ball) resolves PlayBall with the Ball overlay.
2) Wiring audit (read first, fix only if broken): trace the Shell action path for
   Play — it must reach PetSimulationEngine.StartFetchSequence /
   AdvanceFetchSequence with the variant's actual available optional families (from
   SpriteAssetService discovery), not a hardcoded empty set. If a gap exists, fix it
   minimally, with a focused test. Document the finding either way in the report.
3) DevControl visual proof matrix (artifact root
   vnext/artifacts/c-phase-204-action-proof/): for each of the 10 species (one
   representative variant each — vary age/gender/color across species), drive all
   nine actions + a full fetch sequence; per step capture a window screenshot + a
   DevControl snapshot showing the AnimationState transition. Assert: every action's
   snapshot shows its mapped state (the BUG-004 class must stay fixed: water, groom,
   doctor included), and every fetch stage on ball-complete variants shows the real
   family, not a fallback.
4) Phase report: docs/C_PHASE204_PLAY_FETCH_ACTION_PROOF_2026-MM-DD.md — wiring
   verdict, proof-matrix table (species x action -> state observed), capture index.
5) History append per §0.

Stop gates:
- Any action success without its mapped AnimationState transition.
- Any fetch stage rendering a fallback family on a ball-complete variant.
- Production-code changes beyond a documented Play->fetch wiring fix.

Validation: §0 A–E; A = dotnet test ... --filter "FullyQualifiedName~FetchSequenceFamilyResolution|FullyQualifiedName~PetSimulation"
(minimum derived per §0 including the new file's facts).
Commit message: C-PHASE 204: play/fetch wiring verification + all-action visual proof matrix
PR title: C-PHASE 204: play/fetch wiring + action proof matrix
Auto-continue: No
```

---

## §10 — C-PHASE 205: RC Checklist v1 full sweep + playability verdict

```text
Start with the §0 common header. Run only after C-PHASE 202, 203, and 204 merged.

Phase: C-PHASE 205 — RC Checklist v1 full sweep + pet-sim playability verdict
Branch: claude-implementation/c-phase-205-rc-sweep-playability-verdict

Goal:
The exit gate for the pet-sim track. Evidence + docs only; no production source change.

Steps:
1) Run all 30 items of docs/RC_CHECKLIST_V1.md across its own matrix (1/2/3 pets;
   focused + unfocused; normal play / action tab open / priority modal open /
   save/load cycle), using the C-134 evidence pattern: DevControl snapshots + window
   captures per item under vnext/artifacts/c-phase-205-rc-sweep/<RC-NN>/.
   Items requiring human judgment (readability, overlap, flicker) get captures + a
   written PASS/FAIL with one-sentence reasoning each.
2) Failures: file bugs on docs/BUG_BOARD.md per its template (severity per the
   checklist's triage rubric), linked from the RC item row.
3) Verdict report: docs/C_PHASE205_PET_SIM_PLAYABILITY_VERDICT_2026-MM-DD.md —
   full 30-item table (PASS/FAIL + evidence path + bug ID), open-bug summary, and the
   explicit final line:
   PET SIM FULLY PLAYABLE: YES   (only if zero Critical/Major bugs open)
   PET SIM FULLY PLAYABLE: NO — blocked by <bug IDs>
4) If NO: list the proposed fix mini-phases (C-206+) in the report; do not implement
   them in this phase.
5) History append per §0.

Stop gates:
- Any production or sprite file modified.
- Verdict YES while any Critical/Major bug is open.

Validation: §0 A–E; A = full suite (no filter).
Commit message: C-PHASE 205: RC checklist v1 full sweep + pet-sim playability verdict
PR title: C-PHASE 205: RC sweep + playability verdict
Auto-continue: No
```

---

## §11 — C-PHASE 199R: Ball-candidate regeneration + second review round (Amendment R2)

```text
Start with the §0 common header. Run only after C-PHASE 203A AND 203B (incl. the
C-202 board import) merged. Report-only phase: no sprites_runtime mutation.

Phase: C-PHASE 199R — Ball-candidate regeneration + second review round
Branch: claude-implementation/c-phase-199r-candidate-regen

Goal:
The C-199 candidate set inherited source-art defects (owner review 2026-06-12:
0/10 approved). Sources are now repaired; regenerate candidates deterministically
and stage the second per-species approval gate for C-200.

Steps:
1) Delete the stale candidate tree under
   vnext/artifacts/c-phase-199-ball-candidates/candidates/ (artifacts are local;
   keep candidate-manifest.json renamed *.r1.json for provenance).
2) Re-run tools/generate_optional_ball_families.py over the full matrix into
   vnext/artifacts/c-phase-199r-ball-candidates/ — same flags as C-199, fresh
   manifest with per-frame sha256. Determinism spot-check via --hash-only twice.
3) Rebuild the 10 per-species review sheets (same layout; goose pilot row labeled).
   NOTE: the C-198 pilot (goose/baby/female/blue) was applied from PRE-repair
   sources — regenerate its 6 families too and include the diff in the report so
   the owner can decide whether the pilot gets re-applied in C-200's goose batch.
4) Phase report docs/C_PHASE199R_CANDIDATE_REGEN_2026-MM-DD.md with the per-species
   approval checklist ("AWAITING PER-SPECIES OWNER APPROVAL BEFORE C-PHASE 200").
   C-200's scope = exactly the species checked HERE (the C-199 checklist is void).
5) History append per §0.

Stop gates:
- Any write outside the two c-phase-199* artifact roots.
- Hash instability across the two --hash-only runs.
- Any source row consumed by synthesis still failing the contract audit or the
  BUG-007 color-conformance check (halt: repair lane missed a row).

Validation: §0 A–E; A = dotnet test ... --filter "FullyQualifiedName~Sprite"
(minimum derived per §0).
Commit message: C-PHASE 199R: regenerate ball candidates from repaired sources + second review gate
PR title: C-PHASE 199R: candidate regeneration + re-review gate
Auto-continue: No
```
