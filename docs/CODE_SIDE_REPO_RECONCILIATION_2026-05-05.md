# Code-Side Repo Reconciliation

Date: 2026-05-05

Phase: 0 - Coordination And Gate Reconciliation

Purpose: reconcile the current main repo, the active code-side worktree, visual-side docs, accepted optional endpoint state, and read-only validation signals before any further code-side implementation or asset mutation.

This report does not authorize runtime/source PNG mutation, broad cleanup, sprite generation, import, or worktree/main reconciliation by overwrite.

## Current Shape

```text
╔══════════════════════════════════════════════════════════════════════╗
║ Phase 0 Truth Map                                                   ║
╠══════════════════════╦═══════════════════════════════════════════════╣
║ main repo             ║ optional-rich, visually active, asset gate red║
║ worktree 36d6         ║ base-runtime clean, tests green, optional gap ║
║ visual thread         ║ continuing cleanup/classification work        ║
║ code thread           ║ should own contracts, gates, tests, runtime   ║
║ Claude Design         ║ prototype lane only, no runtime mutation      ║
╚══════════════════════╩═══════════════════════════════════════════════╝
```

Main repo:

```text
C:\Users\fishe\Documents\projects\wevito
branch: main
commit: dd520908a
status: ahead 1, very dirty
```

Active code-side worktree:

```text
C:\Users\fishe\.codex\worktrees\36d6\wevito
branch: detached HEAD
commit: dd520908a
status: dirty, but far smaller than main
```

## Commands Run

```text
git -C C:\Users\fishe\Documents\projects\wevito status --short --branch
git -C C:\Users\fishe\.codex\worktrees\36d6\wevito status --short --branch
git rev-parse --abbrev-ref HEAD / --short HEAD for both repos
python .\tools\report_runtime_canvas_mismatches.py --output .\vnext\artifacts\runtime-canvas-mismatches-phase0-main-20260505.json --markdown .\docs\RUNTIME_CANVAS_MISMATCHES_PHASE0_MAIN_2026-05-05.md
python C:\Users\fishe\Documents\projects\wevito\tools\report_runtime_canvas_mismatches.py --runtime-root C:\Users\fishe\.codex\worktrees\36d6\wevito\sprites_runtime --output C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\runtime-canvas-mismatches-phase0-worktree36d6-20260505.json --markdown C:\Users\fishe\Documents\projects\wevito\docs\RUNTIME_CANVAS_MISMATCHES_PHASE0_WORKTREE36D6_2026-05-05.md
dotnet test .\vnext\Wevito.VNext.sln -c Debug --no-restore --no-build
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests -StepTimeoutSeconds 60
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests -StepTimeoutSeconds 240
dotnet test .\vnext\Wevito.VNext.sln -c Debug --no-restore
python .\tools\audit_sprite_contract.py --output .\vnext\artifacts\sprite-contract-phase0-main-20260505.json
python C:\Users\fishe\Documents\projects\wevito\tools\audit_sprite_contract.py --runtime-root C:\Users\fishe\.codex\worktrees\36d6\wevito\sprites_runtime --output C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\sprite-contract-phase0-worktree36d6-20260505.json
python .\tools\audit_optional_animation_readiness.py --output .\vnext\artifacts\optional-readiness-phase0-main-20260505.json --markdown .\docs\OPTIONAL_READINESS_PHASE0_MAIN_2026-05-05.md
python C:\Users\fishe\Documents\projects\wevito\tools\audit_optional_animation_readiness.py --runtime-root C:\Users\fishe\.codex\worktrees\36d6\wevito\sprites_runtime --output C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\optional-readiness-phase0-worktree36d6-20260505.json --markdown C:\Users\fishe\Documents\projects\wevito\docs\OPTIONAL_READINESS_PHASE0_WORKTREE36D6_2026-05-05.md
```

Generated report artifacts:

- `C:\Users\fishe\Documents\projects\wevito\docs\RUNTIME_CANVAS_MISMATCHES_PHASE0_MAIN_2026-05-05.md`
- `C:\Users\fishe\Documents\projects\wevito\docs\RUNTIME_CANVAS_MISMATCHES_PHASE0_WORKTREE36D6_2026-05-05.md`
- `C:\Users\fishe\Documents\projects\wevito\docs\OPTIONAL_READINESS_PHASE0_MAIN_2026-05-05.md`
- `C:\Users\fishe\Documents\projects\wevito\docs\OPTIONAL_READINESS_PHASE0_WORKTREE36D6_2026-05-05.md`
- `C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\runtime-canvas-mismatches-phase0-main-20260505.json`
- `C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\runtime-canvas-mismatches-phase0-worktree36d6-20260505.json`
- `C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\sprite-contract-phase0-main-20260505.json`
- `C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\sprite-contract-phase0-worktree36d6-20260505.json`
- `C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\optional-readiness-phase0-main-20260505.json`
- `C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\optional-readiness-phase0-worktree36d6-20260505.json`

## Validation Results

```text
╔════════════════════════════════╦═══════════════════╦══════════════════╗
║ Check                          ║ Main repo          ║ Worktree 36d6    ║
╠════════════════════════════════╬═══════════════════╬══════════════════╣
║ Required runtime sequences     ║ 2880               ║ 2880             ║
║ Required runtime frames        ║ 10800              ║ 10800            ║
║ Mixed-canvas required rows     ║ 456                ║ 0                ║
║ Missing/count rows             ║ 0                  ║ 0                ║
║ Invalid/non-alpha PNG rows     ║ 0                  ║ 0                ║
║ Source boards                  ║ 30 / 30            ║ 30 / 30          ║
║ Supporting inputs              ║ 17 / 17            ║ 17 / 17          ║
║ Runtime variant dirs           ║ 360 / 360          ║ 360 / 360        ║
║ Runtime total frame count      ║ 23040 / 10800      ║ 10800 / 10800    ║
║ Optional readiness             ║ pass, 2520 / 2520  ║ pass, 2520 / 2520║
║ vNext tests                    ║ fail, 53/54 pass   ║ pass, 26/26      ║
║ Debug publish, skip asset prep ║ pass with 240 sec  ║ not rerun        ║
╚════════════════════════════════╩═══════════════════╩══════════════════╝
```

Important nuance: the optional readiness script passed against both contexts, but direct file inspection shows the worktree runtime folder does not contain the accepted `goose / baby / female / blue / hold_ball` runtime PNGs. Treat the worktree as base-runtime clean, not optional-runtime complete.

## Accepted Endpoint Check

Accepted endpoint:

```text
sprites_runtime/goose/baby/female/blue/hold_ball_00.png
sprites_runtime/goose/baby/female/blue/hold_ball_01.png
sprites_runtime/goose/baby/female/blue/hold_ball_02.png
sprites_runtime/goose/baby/female/blue/hold_ball_03.png
```

Main repo state:

```text
hold_ball_00.png 60x59 SHA256 E2CAC548EB4652EF77FE872AF927E2E0E07D0CC42837BEBAA5B595366AD1333A
hold_ball_01.png 60x59 SHA256 1E847D80D35FD0CF6E5BF7A0A1AA8218C8DB223957AF08856019D7418D2779D2
hold_ball_02.png 60x59 SHA256 8B6FB4322A6EE10A19E6E48E31A58A292D66F43E6BC7481ECC1CF4D89B4789DD
hold_ball_03.png 60x59 SHA256 859EA85F5A36325F7CC0E15A1E945BC040FEEF8D1A7EB5178EE46DDF55B6760A
```

Status:

- Present in main working tree.
- Currently untracked in main git status.
- Absent from the code-side worktree runtime folder.
- Must be protected from broad cleanup, normalization, overwrite, or worktree-to-main replacement until Phase 1 defines the ownership/apply contract.

## Main Findings

```text
Phase 0 findings
  │
  ├── F1 main/worktree divergence
  │     ├── main has optional-rich runtime assets
  │     └── worktree has green base runtime contract
  │
  ├── F2 main asset gate still red
  │     ├── 456 required base rows have mixed canvases
  │     └── SpriteRuntimeCoverageTests expects 80, sees 81
  │
  ├── F3 accepted endpoint is fragile
  │     ├── present in main
  │     ├── untracked
  │     └── absent in worktree
  │
  ├── F4 repo is very dirty
  │     ├── main includes huge artifacts/cache/source/runtime changes
  │     └── no cleanup/revert should happen casually
  │
  └── F5 build path is viable if asset prep is skipped
        ├── 60s shell publish timeout was too short
        └── 240s skip-asset-prep publish passed
```

## Safe-To-Mutate Status

| Lane | Status | Reason |
| --- | --- | --- |
| Planning/docs | Safe | Docs-only coordination is reversible and needed. |
| Claude Design prototypes | Safe with constraints | Use focused screenshots/docs only; no repo-wide upload, secrets, or asset mutation. |
| Build artifacts | Safe to regenerate | `build-vnext.ps1 -SkipAssetPrep -SkipTests` passed with a realistic timeout. |
| vNext contracts/tests | Plan first | Code-side can own this, but Phase 1 should define ownership before broad edits. |
| Main `sprites_runtime` base rows | Not safe yet | Main has 456 mixed-canvas required rows and active dirty visual work. |
| Main optional runtime rows | Not safe yet | Optional lane is present, but accepted endpoint is untracked and must be protected. |
| Source boards / `incoming_sprites` | Not safe yet | No generation/import/rewrite approval in Phase 0. |
| `sprites_shared_runtime` items/status/habitats | Not safe yet | Visual cleanup has modified/shared assets; code-side should not overwrite. |
| Worktree runtime assets | Not safe to copy over main | Worktree is base-clean but lacks optional accepted endpoint runtime files. |

## Phase 0 Audit

Phase goal: passed with concerns.

What passed:

- Current main and worktree state were inspected.
- Fresh runtime canvas reports were generated.
- Main and worktree validation difference was confirmed.
- Accepted hold-ball endpoint was located and hashed.
- Claude Design was added as a prototype lane in the roadmap before Phase 0 continued.

Concerns that block mutation:

- Main remains test-red due `SpriteRuntimeCoverageTests.RuntimeSpriteFrames_AreCanonicalTransparentPngs`, expected `80`, actual `81`.
- Main still has 456 mixed-canvas required runtime rows.
- The accepted hold-ball endpoint is present but untracked in main and absent from the worktree.
- Main repo has massive unrelated dirty state; do not clean, revert, or normalize without a scoped plan.

## Recommended Phase 1 Start

Phase 1 should define contracts and ownership before any mutation:

```text
Phase 1 contract targets
  │
  ├── source of truth per lane
  │     ├── main optional-rich runtime
  │     ├── worktree base-canvas clean runtime
  │     └── visual-side cleanup outputs
  │
  ├── protected endpoint list
  │     └── goose / baby / female / blue / hold_ball
  │
  ├── apply/proof/rollback contract
  │     ├── candidate manifest
  │     ├── before hashes
  │     ├── after hashes
  │     └── rollback restore
  │
  ├── build safety policy
  │     ├── default skip asset prep during proof
  │     └── explicit approval before asset prep
  │
  └── merge/reconciliation policy
        ├── never copy base-clean worktree over optional-rich main blindly
        └── preserve optional rows and accepted endpoints
```

Recommended next document:

```text
C:\Users\fishe\Documents\projects\wevito\docs\WEVITO_PHASE1_CONTRACT_OWNERSHIP_PLAN_2026-05-05.md
```

Phase 1 should not mutate sprites. It should produce the contract needed to safely decide whether Phase 2 normalizes main assets, imports worktree fixes, or reconciles via a new controlled apply path.
