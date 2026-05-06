# Claude Implementation Risk Register

Date: 2026-05-05
Author: Claude master review
Companion to: `CLAUDE_FULL_WEVITO_IMPLEMENTATION_PLAN_2026-05-05.md`

## How To Read This Doc

Each row is a risk: a thing that can go wrong during implementation. The fields are:

```text
Risk        Short label
Severity    blocker | high | medium | low
Lane        runtime | visual | tool | learning | release | repo
Phase(s)    Where the risk is most likely to fire
Mitigation  What to do to reduce the chance of it firing
Rollback    What to do if it fires
Blocking?   Yes if Codex must stop and ask before proceeding
```

Severity heuristic:
- **blocker**: cannot ship without resolving
- **high**: likely to cost a day if it fires
- **medium**: an afternoon
- **low**: a fix-as-found

## Top Risks (Read First)

These are the five risks that most justify a stop-and-ask checkpoint. Codex must explicitly verify these before merging the relevant phase to `main`.

| # | Risk | Phase(s) | Severity |
|---|---|---|---|
| 1 | First PNG mutation through Sprite Workflow V2 corrupts a runtime row | C-PHASE 22, 23 | blocker |
| 2 | First real model call leaks user data to a provider that trains on it | C-PHASE 26 | blocker |
| 3 | First real `buildProof` execution runs a command not on the allowlist | C-PHASE 19 | high |
| 4 | Capture surface accidentally captures sensitive Wevito UI (credentials, billing) | C-PHASE 11, 12, 13 | high |
| 5 | Habitat manifest replaces hardcoded layout but renderer regresses | C-PHASE 6 | high |

Each top risk is also covered in the full register below with full mitigation and rollback.

## Full Register

### R01 — Sprite Workflow V2 apply corrupts a runtime row

- Severity: **blocker**
- Lane: visual
- Phase(s): C-PHASE 22, 23
- Description: V2 apply replaces runtime PNGs by hash. A bug in the apply path could corrupt a row, drop transparency, write to the wrong species/frame, or skip the backup step.
- Mitigation:
  - Same-volume staging + backup + `File.Move(overwrite:true)` only.
  - Reject apply if `sprites_runtime` and `.staging` are on different volumes.
  - Pre-apply: hash each existing file (BLAKE3); refuse to apply if any candidate hash equals an existing-file hash (unchanged candidate = waste).
  - Post-apply: re-run `audit_sprite_contract.py` and `report_runtime_canvas_mismatches.py`. If new errors, auto-rollback.
  - Limit to one row per apply call.
  - First real apply (C-PHASE 23 drop_ball pilot) is a stop-and-ask checkpoint.
- Rollback: V2 rollback button restores from `.backup\<row-id>-<ts>\`. Last resort: `git checkout -- sprites_runtime/<species>/<row>` (only works if the row was committed clean before apply, which is guaranteed by C-PHASE 22 design).
- Blocking? Yes. Stop and ask before merging C-PHASE 22 and C-PHASE 23.

### R02 — Model call leaks user data

- Severity: **blocker**
- Lane: tool
- Phase(s): C-PHASE 26
- Description: First helper-pet model call could send file contents, clipboard text, or pet-state data to a provider that trains on it.
- Mitigation:
  - Default model adapter = none. No provider is configured by default.
  - First-call consent dialog: provider name, what is being sent, retention, training policy. Same shape as translation consent.
  - Persist API keys in Windows Credential Manager (DPAPI), never in plaintext config.
  - Lethal-trifecta separation: Scout reads untrusted external content; Builder doesn't read untrusted; Inspector reads sprite-only data.
  - Any web/file content fetched as part of the tool is wrapped in `<untrusted>` tags before reaching the model.
  - Audit log records every call: provider, args hash, decision, latency.
  - Default provider should not train on input. (DeepL Pro: no. Anthropic API: no. OpenAI API: opt-out by default.)
- Rollback: revert C-PHASE 26 code. Delete API key from Credential Manager. Delete audit log if user requests.
- Blocking? Yes. Stop and ask before merging C-PHASE 26.

### R03 — buildProof executes off-allowlist command

- Severity: **high**
- Lane: tool
- Phase(s): C-PHASE 18, 19
- Description: A bug in `ProofExecutionAllowlistEvaluator` matches a command that includes shell composition, redirection, or a hard-blocked operation (`Remove-Item`, `git reset`, etc.).
- Mitigation:
  - Allowlist matches by exact-equality of executable + ordered argument list. No regex.
  - Hard-block list runs first; if a hard-blocked op is present anywhere in the argument list, refuse.
  - `build-vnext.ps1` only allowed with `-SkipAssetPrep` until C-PHASE 30.
  - Pre-run: snapshot protected sprite hashes. Post-run: re-hash; if changed, mark `MutationDetected` and stop.
  - Tests cover blocked composition, blocked destructive commands, blocked asset-prep.
  - Fake-runner stub phase (C-PHASE 18) before real runner (C-PHASE 19).
  - First real run = stop-and-ask checkpoint.
- Rollback: revert C-PHASE 19. Real runner can be turned off by reverting one feature flag.
- Blocking? Yes. Stop and ask before merging C-PHASE 19.

### R04 — Capture surface accidentally captures sensitive UI

- Severity: **high**
- Lane: tool
- Phase(s): C-PHASE 11, 12, 13
- Description: A screenshot of "Wevito window" picks up a sensitive child window (settings, credentials, billing). A region screenshot captures a sensitive area of the desktop. A proof clip records a credential prompt.
- Mitigation:
  - Set `WDA_EXCLUDEFROMCAPTURE` (`SetWindowDisplayAffinity`) on every Wevito sensitive surface (settings, credential dialogs, billing).
  - For Wevito-window capture: enumerate child HWNDs and skip excluded ones.
  - Default capture indicator (yellow border) stays on. Don't suppress unless user explicitly toggles + OS-level toggle is on.
  - Region/foreground/desktop blocked until C-PHASE 12 / 13.
  - Recording (C-PHASE 13): hard 10-second cap, Wevito window only, visible recording overlay drawn by Wevito itself.
  - Post-capture redaction is mandatory before any network egress. C-PHASE 11/12/13 ship with no network egress.
  - Document Recall behavior in privacy doc (C-PHASE 30).
- Rollback: revert capture phase. Delete artifact folder if it contains sensitive content.
- Blocking? Yes for C-PHASE 13 (recording). Stop and ask before merging.

### R05 — Habitat renderer regresses after manifest swap

- Severity: **high**
- Lane: runtime
- Phase(s): C-PHASE 5, 6
- Description: Replacing hardcoded prop layout in `HabitatLoadoutResolver` breaks existing rendering (props off-screen, wrong scale, missing).
- Mitigation:
  - C-PHASE 5 keeps hardcoded fallback as a safety net.
  - C-PHASE 6 only handles 5 pilot species; others continue to use fallback.
  - Capture before/after screenshots into `vnext\artifacts\c-phase-6-habitat-pilot\`.
  - Stop-and-ask checkpoint after screenshots for visual-side review.
  - Tests include `HabitatLoadoutResolverFallbackTests` confirming old behavior under fallback.
- Rollback: revert resolver to hardcoded path; manifest still loads but is ignored.
- Blocking? Yes. Stop and ask before merging C-PHASE 6.

### R06 — Optional animation regression breaks pilot row

- Severity: medium
- Lane: visual
- Phase(s): C-PHASE 1, 2, 3, 23
- Description: Adding action/animation/prop contract changes the way `goose / baby / female / blue / hold_ball` (the accepted pilot row) renders.
- Mitigation:
  - C-PHASE 1 contracts default to current behavior — `optionalAnimationFamily` is opt-in per action.
  - Tests assert that without the new field, the pet animation chain is identical to today.
  - Goose pilot row is added to `SpriteRuntimeCoverageTests` as a protected hash check.
- Rollback: revert C-PHASE 1 contracts. Pilot row hash stays unchanged.
- Blocking? No.

### R07 — Save/load schema breaks on lifecycle changes

- Severity: high
- Lane: runtime
- Phase(s): C-PHASE 28
- Description: Adding senior age stage / ghost variant / memorial spawn changes save schema. Old saves fail to load.
- Mitigation:
  - Save schema has a `version` field. Bump on schema change.
  - Migration test: load a recorded `v1` save and assert it round-trips through `v2` without error.
  - Migration is forward-only.
  - Fall back to `idle` and `adult` if any new field is missing on load.
- Rollback: revert C-PHASE 28. Migration code reads old saves but does not change file on disk if loaded under fallback.
- Blocking? No, but treat as a stop-and-ask if migration test fails.

### R08 — TFM bump cascades to other vNext projects

- Severity: medium
- Lane: tool
- Phase(s): C-PHASE 11
- Description: Bumping `Wevito.VNext.Shell` to `net8.0-windows10.0.19041.0` may force `Wevito.VNext.Broker` and `Wevito.VNext.AutomationRunner` to follow.
- Mitigation:
  - Audit project references before bump.
  - Only Shell needs WinRT projections.
  - Keep Broker and AutomationRunner on `net8.0` (or `net8.0-windows`) if possible.
- Rollback: revert TFM change in csproj.
- Blocking? Yes if it cascades. Stop and ask if Broker/AutomationRunner builds break.

### R09 — Probe automation IDs break

- Severity: medium
- Lane: tool
- Phase(s): C-PHASE 8, 9, 10
- Description: Tool Hub IA refactor changes automation IDs in `ToolPopupWindow.xaml`. Existing PET TASKS probes break.
- Mitigation:
  - Phase explicitly requires preserving every existing automation ID.
  - Add new IDs (`PetTaskResultPathText`, `PetTaskNextActionText`) without renaming old ones.
  - Run every existing probe (`localDocs`, `spriteAudit`, `assetInventory`, `petState`, `codeReview`, `codePatchPlan`, `buildProof`, `translateText`, `audioAssist`, `screenCapture`) before merging C-PHASE 8.
- Rollback: revert XAML.
- Blocking? No.

### R10 — Translation provider Free-vs-Pro confusion

- Severity: medium
- Lane: tool
- Phase(s): C-PHASE 14, 15
- Description: User configures DeepL Free thinking it's the "API Free" plan but actually pasted a key for the consumer "web/app Free" plan, which trains on input.
- Mitigation:
  - First-translation consent dialog explicitly distinguishes "DeepL API Free" vs "DeepL Web/App Free".
  - Provider readiness check verifies the key is an API key (different format than web-app keys).
  - Document in user help guide (C-PHASE 30).
- Rollback: user can revoke key in Credential Manager.
- Blocking? No.

### R11 — Equalizer APO setup guide is wrong for user's machine

- Severity: low
- Lane: tool
- Phase(s): C-PHASE 16, 17
- Description: Detection false-positive for Equalizer APO (registry leftover from prior install) — guide tells user to edit `config.txt` that doesn't exist.
- Mitigation:
  - Detection checks both registry AND config.txt presence.
  - If only one is present, mark status `Partial`; don't generate edit instructions, generate "verify install" instructions instead.
- Rollback: user can ignore the guide; nothing changed on disk.
- Blocking? No.

### R12 — sqlite-vec NuGet adds native dependency

- Severity: medium
- Lane: learning
- Phase(s): C-PHASE 27
- Description: sqlite-vec ships native binaries. Wevito's installer or unzipped distribution may not include the right binary for x64 / ARM64.
- Mitigation:
  - Stage 1: confirm sqlite-vec NuGet ships x64 + ARM64 Windows binaries.
  - Stage 2: add a startup self-test that initializes sqlite-vec; if fails, surface a "memory unavailable" status and disable memory-using tools.
  - Stage 3: Release packaging (C-PHASE 30) verifies binaries are bundled.
- Rollback: revert NuGet add. Helpers fall back to non-memory routing.
- Blocking? No, but C-PHASE 30 must verify before tagging release.

### R13 — Real `buildProof` execution mutates code or assets

- Severity: high
- Lane: repo
- Phase(s): C-PHASE 19
- Description: A whitelisted build command unexpectedly mutates code or assets — e.g. `dotnet build` regenerates a `.gitignore`-tracked file, or a probe writes outside the artifact root.
- Mitigation:
  - Pre-run: snapshot hash of every file under `vnext/src`, `vnext/tests`, `tools`, `scripts`, `sprites_*`, `vnext/content`.
  - Post-run: re-hash. If any tracked file changed, mark `MutationDetected` and refuse to merge.
  - Probe commands write only to `vnext/artifacts/`.
- Rollback: `git checkout -- <changed paths>` if mutation is benign; investigate otherwise.
- Blocking? Yes. Stop-and-ask if mutation detected.

### R14 — Recording captures Recall-protected content the user does not realize

- Severity: high
- Lane: tool
- Phase(s): C-PHASE 13
- Description: User records a 10-second proof clip; the clip captures a password manager flyout, banking app, or InPrivate browsing window the user did not consider.
- Mitigation:
  - Recording is Wevito-window only. Confirm via WGC `CreateForWindow(wevitoHwnd)` only.
  - Region recording = blocked until a future phase with explicit privacy controls.
  - Visible Wevito-drawn recording indicator (in addition to the OS yellow border).
  - Post-capture redaction step before any network egress (none in C-PHASE 13).
- Rollback: clip file is local-only; user can delete.
- Blocking? Yes. Stop-and-ask before merging C-PHASE 13.

### R15 — Visual-side and code-side branches diverge again

- Severity: medium
- Lane: repo
- Phase(s): all
- Description: Visual-side opens a parallel branch (e.g. `codex/visual-side-qa-20260506`) during Claude implementation; the two diverge in `sprites_runtime` or docs.
- Mitigation:
  - Phase commits are surgical — each phase touches a small fileset.
  - Sprite Workflow V2 (C-PHASE 20-23) is the only phase touching `sprites_runtime`. Schedule visual-side cleanup around it.
  - C-PHASE 0 baseline check captures the current `sprites_runtime` state.
  - Phase docs reference exact file paths; if visual-side moves files, the phase fails fast.
- Rollback: merge visual-side first if conflicts arise; do not auto-resolve.
- Blocking? No, but stop-and-ask if a phase merge has any conflict in `sprites_runtime`.

### R16 — Helper pet personality grants permission accidentally

- Severity: high
- Lane: tool / learning
- Phase(s): C-PHASE 9, 26, 27
- Description: A helper pet's "personality" or "role" string is accidentally used in policy decisions (e.g. "Builder is allowed to run buildProof because role = code"). This violates the rule: personality affects presentation, never permission.
- Mitigation:
  - Permission decisions live in `ToolPolicyEvaluator` and use only: tool family, action kind, risk level, approval state, current `permission_mode`.
  - `HelperPet.Role` is a string for UX; no policy code reads it.
  - Tests assert: every policy outcome is identical regardless of helper name/role.
- Rollback: not applicable; violation = bug, fix forward.
- Blocking? No, but a code review catch.

### R17 — Lethal trifecta combination in one helper

- Severity: high
- Lane: learning
- Phase(s): C-PHASE 26, 27
- Description: A helper accidentally gets all three: untrusted-content read, private-data access, outbound communication. Classic prompt-injection trap.
- Mitigation:
  - Per-pet allowlist enforces split: Scout reads untrusted; Builder doesn't read untrusted; Inspector reads sprite-only.
  - Tests assert that no single pet has all three of: `read.web|file|clipboard`, `read.private`, `network.send`.
  - Memory store writes are themselves a gated tool — even Scout must approval-gate writes that include untrusted-tagged content.
- Rollback: revert offending allowlist change.
- Blocking? Yes. Stop-and-ask if a phase wants to add a capability that would create the trifecta.

### R18 — Approval fatigue drives blanket approve

- Severity: medium
- Lane: tool
- Phase(s): C-PHASE 11, 19, 22, 26
- Description: Too many gates → user clicks Approve without reading → real risk gets through.
- Mitigation:
  - Tier aggressively: advisory tools (read-only `localDocs`, `spriteAudit`, `petState`) auto-run.
  - Validating gate (preview + diff) for writes inside Wevito's scratch space.
  - Blocking gate (explicit approve each time) only for: network calls, Wevito-window capture (after first approval per session, remember for 5 minutes), buildProof, V2 apply.
  - Escalating gate (typed confirmation) for: V2 first apply on a new row, real model call to a new provider, recording.
- Rollback: not applicable; UX issue.
- Blocking? No.

### R19 — Memory store corruption on crash

- Severity: medium
- Lane: learning
- Phase(s): C-PHASE 25, 27
- Description: SQLite databases corrupt on power loss / forced kill, especially with vector indexes.
- Mitigation:
  - WAL mode enabled. Small writes. Transaction-wrap.
  - Checkpoint on graceful shutdown.
  - Self-healing on startup: integrity check; if corrupt, rename file and start fresh; surface a "memory rebuilt" notification.
- Rollback: delete `learning_lab.db` and per-pet memory DB files.
- Blocking? No.

### R20 — Release build differs from Debug

- Severity: medium
- Lane: release
- Phase(s): C-PHASE 30
- Description: Release optimization triggers a code path bug not seen in Debug — e.g. a JIT difference, a `[Conditional("DEBUG")]` method that silently no-ops in Release.
- Mitigation:
  - C-PHASE 30 explicitly runs full test suite under `Release`.
  - Avoid `[Conditional("DEBUG")]` for behavior-affecting methods.
  - Release `build-vnext.ps1` invoked once with all checks.
- Rollback: revert; ship Debug-only initially if Release breaks.
- Blocking? Yes. Stop-and-ask if any test fails in Release.

### R21 — Asset-prep build regenerates runtime sprites mid-implementation

- Severity: high
- Lane: visual
- Phase(s): all (especially C-PHASE 18-23)
- Description: Someone runs `build-vnext.ps1` without `-SkipAssetPrep` while visual-side cleanup or V2 apply is in progress; runtime PNGs get regenerated.
- Mitigation:
  - Every phase's safe-publish command in this plan uses `-SkipAssetPrep`.
  - C-PHASE 18 allowlist hard-blocks `build-vnext.ps1` without `-SkipAssetPrep`.
  - Tests assert allowlist behavior.
  - Convention: until C-PHASE 30, never run asset prep.
- Rollback: V2 rollback if it happens during apply; otherwise `git checkout -- sprites_runtime/`.
- Blocking? Yes. Stop-and-ask if anyone proposes asset prep before C-PHASE 30.

### R22 — Pet animation accidentally driven by tool state

- Severity: medium
- Lane: runtime
- Phase(s): C-PHASE 8, 9, 26
- Description: After Tool Hub refactor or helper-pet additions, a tool state change causes a pet to play `wave`/`failed`/`review` animations, violating the under-the-hood rule from `CODE_SIDE_PHASE41_UNDER_THE_HOOD_PET_TASKS_BEHAVIOR_2026-05-05.md`.
- Mitigation:
  - That phase already removed the linkage. New phases must not reintroduce it.
  - Tests assert pet animation state is unaffected by `TaskCardStatus` transitions.
  - Code review: any phase touching `ShellCoordinator` re-checks the rule.
- Rollback: revert offending phase.
- Blocking? No.

### R23 — Three-helper roster cap silently exceeded

- Severity: low
- Lane: tool
- Phase(s): C-PHASE 9
- Description: A bug or future change adds a fourth active helper.
- Mitigation:
  - `PetCommandBarService.AddHelper()` rejects with explicit error if active count == 3.
  - Tests assert the cap.
- Rollback: revert.
- Blocking? No.

### R24 — Provenance hash collisions

- Severity: low
- Lane: visual
- Phase(s): C-PHASE 20, 22
- Description: BLAKE3 collision (theoretical; practically impossible) or, more realistically, a path collision where two manifest entries have the same `path` field by accident.
- Mitigation:
  - Manifest entries are keyed by full path including species/age/gender/color/family/frame.
  - Manifest writer asserts unique keys; refuses to write if duplicate.
- Rollback: regenerate manifest.
- Blocking? No.

### R25 — Translation glossary breaks code-block preservation

- Severity: low
- Lane: tool
- Phase(s): C-PHASE 14
- Description: Glossary substitution rewrites text inside a code block, breaking the round-trip.
- Mitigation:
  - QA pass detects code-block / placeholder / markdown drift before result is finalized.
  - Provider `formality` and `tag_handling` parameters used where supported (DeepL has `tag_handling: xml`, useful for protecting tags).
  - Glossary substitution skips text inside fenced blocks (` ``` `) and inline code (`` ` ``).
- Rollback: surface QA warning; user can revert to raw provider output.
- Blocking? No.

### R26 — Click-through / overlay layering breaks on Win11 24H2+

- Severity: low
- Lane: runtime
- Phase(s): all
- Description: WPF `WS_EX_LAYERED | WS_EX_TRANSPARENT` behavior changes in a future Windows update.
- Mitigation:
  - Existing behavior is well-tested; phases here don't change overlay layering.
  - C-PHASE 29 final QA includes overlay layering verification on the user's primary OS version.
- Rollback: not applicable; would require a Windows-specific fix.
- Blocking? No.

### R27 — Capture indicator (yellow border) is required by user policy

- Severity: low
- Lane: tool
- Phase(s): C-PHASE 11, 12, 13
- Description: Some users will want to suppress the yellow border for proof clips. Without OS-level toggle, suppression is silently ignored.
- Mitigation:
  - Detect OS-level toggle state via test capture. If suppression silently fails, surface a "border cannot be hidden" message and keep the in-app indicator visible.
- Rollback: not applicable.
- Blocking? No.

### R28 — Backup folder grows unbounded

- Severity: low
- Lane: visual
- Phase(s): C-PHASE 22
- Description: Every V2 apply writes a backup to `sprites_runtime\.backup\`. Over months, this fills disk.
- Mitigation:
  - V2 keeps last 50 backup folders. Older ones are auto-pruned with a confirmation dialog at next V2 launch.
  - Backup folder is on the same volume as runtime (per atomic-apply requirement).
- Rollback: not applicable.
- Blocking? No.

### R29 — Codex executes a phase with stale context

- Severity: medium
- Lane: repo
- Phase(s): all
- Description: A Codex thread runs a phase whose docs have been updated but Codex re-uses an older copy of the prompt.
- Mitigation:
  - Each Codex prompt starts with: "Read these files first" — explicit list.
  - Codex prompt includes a one-line check: confirm `git rev-parse HEAD` is reachable from `origin/main` and matches expected baseline.
  - Plan, prompts, and risk register share the same date stamp; new versions get new dated docs.
- Rollback: revert phase, redo with current prompt.
- Blocking? No, but check.

### R30 — `prop_anchors.json` accidentally edited

- Severity: high
- Lane: visual
- Phase(s): C-PHASE 1, 3, 5, 6
- Description: A phase that touches habitat/animation contracts inadvertently edits `sprites_runtime/_metadata/prop_anchors.json`, which is a visual-side-owned file.
- Mitigation:
  - Every phase's "Affected files" list is explicit. `prop_anchors.json` is never listed.
  - Pre-merge check: assert `git diff origin/main -- sprites_runtime/_metadata/` is empty.
- Rollback: `git checkout -- sprites_runtime/_metadata/prop_anchors.json`.
- Blocking? Yes. Stop-and-ask if any phase shows a diff in `_metadata/`.

## Risk Heatmap By Phase

```text
                          R01 R02 R03 R04 R05 R06 R07 R08 R09 R10 R11 R12 R13 R14 R15 R16 R17 R18 R19 R20 R21 R22 R23 R24 R25 R26 R27 R28 R29 R30
C-PHASE 0  Preflight        ·   ·   ·   ·   ·   ·   ·   ·   ·   ·   ·   ·   ·   ·   ·   ·   ·   ·   ·   ·   ·   ·   ·   ·   ·   ·   ·   ·   m   ·
C-PHASE 1  Anim contract    ·   ·   ·   ·   ·   m   ·   ·   ·   ·   ·   ·   ·   ·   m   ·   ·   ·   ·   ·   m   ·   ·   ·   ·   ·   ·   ·   m   m
C-PHASE 2  Auto-care        ·   ·   ·   ·   ·   m   ·   ·   ·   ·   ·   ·   ·   ·   m   ·   ·   ·   ·   ·   m   ·   ·   ·   ·   ·   ·   ·   m   m
C-PHASE 3  Staged fetch     ·   ·   ·   ·   ·   m   ·   ·   ·   ·   ·   ·   ·   ·   m   ·   ·   ·   ·   ·   m   ·   ·   ·   ·   ·   ·   ·   m   m
C-PHASE 4  Item content     ·   ·   ·   ·   ·   ·   ·   ·   ·   ·   ·   ·   ·   ·   m   ·   ·   ·   ·   ·   m   ·   ·   ·   ·   ·   ·   ·   m   ·
C-PHASE 5  Habitat schema   ·   ·   ·   ·   H   ·   ·   ·   ·   ·   ·   ·   ·   ·   m   ·   ·   ·   ·   ·   m   ·   ·   ·   ·   ·   ·   ·   m   H
C-PHASE 6  Habitat pilot    ·   ·   ·   ·   H   ·   ·   ·   ·   ·   ·   ·   ·   ·   m   ·   ·   ·   ·   ·   m   ·   ·   ·   ·   ·   ·   ·   m   H
C-PHASE 7  Habitat all      ·   ·   ·   ·   m   ·   ·   ·   ·   ·   ·   ·   ·   ·   m   ·   ·   ·   ·   ·   m   ·   ·   ·   ·   ·   ·   ·   m   ·
C-PHASE 8  Tool Hub UI      ·   ·   ·   ·   ·   ·   ·   ·   m   ·   ·   ·   ·   ·   m   ·   ·   ·   ·   ·   m   m   ·   ·   ·   ·   ·   ·   m   ·
C-PHASE 9  3-helper bar     ·   ·   ·   ·   ·   ·   ·   ·   m   ·   ·   ·   ·   ·   m   H   m   ·   ·   ·   m   m   l   ·   ·   ·   ·   ·   m   ·
C-PHASE 10 Artifact ctrls   ·   ·   ·   ·   ·   ·   ·   ·   m   ·   ·   ·   ·   ·   m   ·   ·   ·   ·   ·   m   ·   ·   ·   ·   ·   ·   ·   m   ·
C-PHASE 11 Wevito capture   ·   ·   ·   H   ·   ·   ·   m   m   ·   ·   ·   ·   ·   m   ·   ·   m   ·   ·   m   ·   ·   ·   ·   ·   l   ·   m   ·
C-PHASE 12 Region capture   ·   ·   ·   H   ·   ·   ·   ·   ·   ·   ·   ·   ·   ·   m   ·   ·   m   ·   ·   m   ·   ·   ·   ·   ·   l   ·   m   ·
C-PHASE 13 Proof clips      ·   ·   ·   H   ·   ·   ·   ·   ·   ·   ·   ·   H   H   m   ·   ·   m   ·   ·   m   ·   ·   ·   ·   ·   l   ·   m   ·
C-PHASE 14 Trans glossary   ·   ·   ·   ·   ·   ·   ·   ·   ·   m   ·   ·   ·   ·   m   ·   ·   m   ·   ·   m   ·   ·   ·   l   ·   ·   ·   m   ·
C-PHASE 15 Trans LibreTr    ·   ·   ·   ·   ·   ·   ·   ·   ·   m   ·   ·   ·   ·   m   ·   ·   m   ·   ·   m   ·   ·   ·   l   ·   ·   ·   m   ·
C-PHASE 16 Audio handoff    ·   ·   ·   ·   ·   ·   ·   ·   ·   ·   l   ·   ·   ·   m   ·   ·   ·   ·   ·   m   ·   ·   ·   ·   ·   ·   ·   m   ·
C-PHASE 17 Audio presets    ·   ·   ·   ·   ·   ·   ·   ·   ·   ·   l   ·   ·   ·   m   ·   ·   ·   ·   ·   m   ·   ·   ·   ·   ·   ·   ·   m   ·
C-PHASE 18 buildProof stub  ·   ·   H   ·   ·   ·   ·   ·   ·   ·   ·   ·   ·   ·   m   ·   ·   m   ·   ·   m   ·   ·   ·   ·   ·   ·   ·   m   ·
C-PHASE 19 buildProof real  ·   ·   H   ·   ·   ·   ·   ·   ·   ·   ·   ·   H   ·   m   ·   ·   m   ·   ·   m   ·   ·   ·   ·   ·   ·   ·   m   ·
C-PHASE 20 V2 readonly      ·   ·   ·   ·   ·   ·   ·   ·   ·   ·   ·   ·   ·   ·   m   ·   ·   ·   ·   ·   m   ·   ·   l   ·   ·   ·   ·   m   ·
C-PHASE 21 V2 import        ·   ·   ·   ·   ·   ·   ·   ·   ·   ·   ·   ·   ·   ·   m   ·   ·   ·   ·   ·   m   ·   ·   l   ·   ·   ·   ·   m   ·
C-PHASE 22 V2 apply         B   ·   ·   ·   ·   ·   ·   ·   ·   ·   ·   ·   ·   ·   m   ·   ·   m   ·   ·   m   ·   ·   l   ·   ·   ·   l   m   m
C-PHASE 23 drop_ball pilot  B   ·   ·   ·   ·   ·   ·   ·   ·   ·   ·   ·   ·   ·   m   ·   ·   m   ·   ·   m   ·   ·   l   ·   ·   ·   l   m   m
C-PHASE 24 Lab readonly     ·   ·   ·   ·   ·   ·   ·   ·   ·   ·   ·   ·   ·   ·   m   ·   ·   ·   m   ·   m   ·   ·   ·   ·   ·   ·   ·   m   ·
C-PHASE 25 Lab labels       ·   ·   ·   ·   ·   ·   ·   ·   ·   ·   ·   ·   ·   ·   m   ·   ·   m   m   ·   m   ·   ·   ·   ·   ·   ·   ·   m   ·
C-PHASE 26 Pet pilot        ·   B   ·   ·   ·   ·   ·   ·   ·   ·   ·   ·   ·   ·   m   H   H   m   ·   ·   m   m   ·   ·   ·   ·   ·   ·   m   ·
C-PHASE 27 Pet 3+memory     ·   ·   ·   ·   ·   ·   ·   ·   ·   ·   ·   m   ·   ·   m   H   H   m   m   ·   m   m   l   ·   ·   ·   ·   ·   m   ·
C-PHASE 28 Lifecycle        ·   ·   ·   ·   ·   ·   H   ·   ·   ·   ·   ·   ·   ·   m   ·   ·   ·   ·   ·   m   ·   ·   ·   ·   ·   ·   ·   m   ·
C-PHASE 29 Final QA         ·   ·   ·   ·   ·   ·   ·   ·   ·   ·   ·   ·   ·   ·   m   ·   ·   ·   ·   ·   m   ·   ·   ·   ·   ·   ·   ·   m   ·
C-PHASE 30 Release          ·   ·   ·   ·   ·   ·   ·   ·   ·   ·   ·   m   ·   ·   m   ·   ·   ·   ·   m   m   ·   ·   ·   ·   ·   ·   ·   m   ·

Legend: B = blocker, H = high, m = medium, l = low, · = not applicable
```

## Open Risk Decisions For The User

These are decisions Codex cannot make alone. The user must answer before C-PHASE N specified.

| Decision | Phase | Default if unanswered |
|---|---|---|
| Should `main` receive PRs directly, or is there a `next` integration branch? | All | PR to `main`, fast-forward only after green |
| Will visual-side run cleanup branches in parallel with Claude implementation? | All | Schedule visual-side around C-PHASE 20-23 |
| Which model provider is Wevito's first AI helper adapter? (Anthropic, OpenAI, local) | C-PHASE 26 | Anthropic Claude API (Opus 4.7 default) |
| Should sqlite-vec memory be on by default? | C-PHASE 27 | Off; user opts in |
| Recording (C-PHASE 13) — ship this milestone, or defer to post-release? | C-PHASE 13 | Defer to post-release if user is uncertain |
| Release tag version? | C-PHASE 30 | `v0.1.0-claude-rollup` |
| Is the user OK with Equalizer APO requiring a reboot, or should audio boost handoff be deferred? | C-PHASE 16 | Document the reboot, ship the handoff |

## Stop-And-Ask Summary

Codex must stop and ask the user before merging these phases:

- C-PHASE 6 (habitat pilot screenshots)
- C-PHASE 13 (recording — privacy)
- C-PHASE 19 (real buildProof execution)
- C-PHASE 22 (first PNG mutation)
- C-PHASE 23 (drop_ball pilot)
- C-PHASE 26 (first model call)
- C-PHASE 30 (release tag)

And before continuing if:

- Any test count regresses
- Protected sprite hash changes outside V2
- `prop_anchors.json` shows a diff
- Asset-prep is requested before C-PHASE 30
- Lethal-trifecta combination would form for one helper
- `build-vnext.ps1` runs without `-SkipAssetPrep`

# End

For the implementation plan, see `CLAUDE_FULL_WEVITO_IMPLEMENTATION_PLAN_2026-05-05.md`. For commit/push rules, see `CLAUDE_PHASE_COMMIT_PUSH_STRATEGY_2026-05-05.md`.
