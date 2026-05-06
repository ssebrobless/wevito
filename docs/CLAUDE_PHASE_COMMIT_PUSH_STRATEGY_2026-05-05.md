# Claude Phase Commit / Push Strategy

Date: 2026-05-05
Author: Claude master review
Companion to: `CLAUDE_FULL_WEVITO_IMPLEMENTATION_PLAN_2026-05-05.md`

## Purpose

Tell Codex exactly when to commit, when to push, what branch to use, what message to write, and when to stop and ask. This is binding.

## Branch Strategy

```text
main                    ── always green; only fast-forward merges
  │
  └── claude-implementation/c-phase-N-<slug>
       ├── commit 1
       ├── commit 2
       └── ...
```

Rules:

1. **Never commit directly to `main`.** Every phase opens a new branch off `origin/main`.
2. Branch name: `claude-implementation/c-phase-N-<short-slug>`. Lower-kebab-case. Examples:
   - `claude-implementation/c-phase-0-baseline`
   - `claude-implementation/c-phase-1-action-animation-prop-contract`
   - `claude-implementation/c-phase-22-sprite-workflow-v2-apply-rollback`
3. **Always rebase, never merge sideways.** Before opening a PR, `git fetch origin && git rebase origin/main`.
4. **Never force-push `main`.** If a force-push is somehow attempted, refuse.
5. **Never force-push a branch that already has a PR open** unless the user explicitly asks. If you must rebase, push with `--force-with-lease`, never plain `--force`.
6. **One branch per phase.** Do not bundle two phases on one branch unless the user explicitly asks for parallel-related-work.
7. **Branch lifetime ≤ 1 working session.** A phase that takes longer than a session must commit progress, push, and resume next session on the same branch.

## Commit Cadence

A phase commits at logical checkpoints. The minimum is one commit; the maximum is whatever keeps each commit reviewable on its own.

Recommended commit boundaries within a phase:

```text
1. contracts/types       (new records, no behavior change yet)
2. content/data          (new JSON/data, schema-only)
3. core logic            (services, adapters, parsers)
4. shell/UI              (XAML, view-models)
5. tests                 (focused tests for the new behavior)
6. docs                  (updated dashboard percentages, new phase note)
```

If a phase doesn't have one of these layers, skip the commit. Don't pad.

## Commit Message Format

Use conventional commits prefixed with the phase id when useful.

```text
<type>(<scope>): <imperative summary>

<optional body explaining why if non-obvious>

[Phase: C-PHASE N]
```

Types:
- `feat` — new behavior
- `fix` — bug fix
- `refactor` — no behavior change
- `test` — test-only changes
- `docs` — doc-only changes
- `chore` — tooling, scripts, build, baseline snapshots

Scopes (Wevito conventions):
- `contracts`, `core`, `shell`, `broker`, `content`, `godot`, `tools`, `docs`, `test`, `phase0`, `phaseN`

Examples:

```text
feat(contracts): add AnimationFamily, PropOverlayKind, ActionVisualIntent

The new types decouple simulation Action from visible AnimationFamily
and from PropOverlayKind. Defaults preserve current behavior so existing
action-driven tests pass unchanged.

[Phase: C-PHASE 1]
```

```text
test(core): cover auto-care visible animation routing

[Phase: C-PHASE 2]
```

```text
chore(phase0): capture baseline validation snapshot

[Phase: C-PHASE 0]
```

## Push Cadence

Push at every phase checkpoint. Specifically:

1. **First push** when the branch has at least one commit and a draft of the work — even if not finished. This makes the branch visible to the user.
2. **Push after each commit boundary** (contracts, content, core, shell, tests, docs).
3. **Final push** before opening the PR.

Push command:

```bash
git push -u origin claude-implementation/c-phase-N-<slug>
```

If the branch already exists upstream and you've rebased: `git push --force-with-lease`.

Never `git push --force`.

## Pull Request Rules

When the phase work is complete and validated:

1. `gh pr create --base main --head claude-implementation/c-phase-N-<slug>`.
2. Title: `C-PHASE N: <phase title>`.
3. Body template:

```markdown
## Summary
- <one bullet per commit boundary>

## Validation
- `dotnet build` — passed
- `dotnet test --no-build` — passed (X / X)
- `tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests` — passed
- Live PET TASKS probe: <result, with summary path>
- Other phase-specific checks: <list>

## Files
- `vnext/...`
- `vnext/...`
- `tools/...`
- `docs/...`

## Risks Touched
- <link to relevant risk register entry, e.g. R01, R05>

## Stop-and-Ask?
- Yes / No (per `CLAUDE_FULL_WEVITO_IMPLEMENTATION_PLAN_2026-05-05.md`)

## Phase
C-PHASE N (`<phase title>`)
```

4. If the phase is a stop-and-ask phase, mark the PR as **draft** and write a comment: `Draft because: <reason>. Awaiting human approval before merge.`
5. Do not auto-merge. The user merges. If the user wants Codex to merge after approval, they will say so explicitly.
6. Squash-merge only. Set `gh pr merge --squash` when the user approves.
7. Delete the branch after merge: `gh api -X DELETE /repos/<owner>/<repo>/git/refs/heads/claude-implementation/c-phase-N-<slug>` (or rely on GitHub's auto-delete-after-merge if set).

## Stop-and-Ask Phases (No Auto-Merge)

These phases must be opened as **draft** PRs and explicitly approved by the user before merging to `main`:

| Phase | Reason |
|---|---|
| C-PHASE 6 | First habitat renderer change with screenshots needing visual-side review |
| C-PHASE 13 | Recording (privacy) |
| C-PHASE 19 | First real `buildProof` command execution |
| C-PHASE 22 | First PNG mutation through Sprite Workflow V2 |
| C-PHASE 23 | First optional-animation row pilot through V2 |
| C-PHASE 26 | First real model API call |
| C-PHASE 30 | Release tag |

For these, after pushing the branch, Codex writes a single message to the user:

```text
C-PHASE N is ready for review.

Branch: claude-implementation/c-phase-N-<slug>
PR:     <gh pr url>

This is a stop-and-ask phase. I will not merge until you approve.

Validation summary:
- <bullets>

Open questions:
- <bullets, if any>
```

Then Codex stops and waits.

## Stop-and-Ask Conditions (Mid-Phase)

Codex must also stop and ask if any of these fire mid-phase:

1. C-PHASE 0 baseline check fails any item.
2. Any existing test starts failing.
3. A protected sprite hash changes outside an approved V2 apply.
4. `prop_anchors.json` or `sprites_runtime/_metadata/` shows a diff.
5. A capture, translation, audio, or model-call adapter is about to make its first real network/system call this checkout.
6. A TFM bump cascades to multiple projects.
7. A NuGet package needs to be added.
8. `build-vnext.ps1` would run without `-SkipAssetPrep` before C-PHASE 30.
9. The user's `git status` shows uncommitted work that wasn't there at C-PHASE 0.
10. A phase wants to add a capability that would create the lethal trifecta for any one helper.
11. `prop_anchors.json`, `sprites_authored_verified/`, or content under `sprites_runtime/_metadata/` would be modified.
12. A merge conflict appears in `sprites_runtime`, `vnext/content`, or `prop_anchors.json`.

When stopping mid-phase: push the current branch, write a short markdown note explaining state and the question, and ask. Don't half-finish work; commit what's working, leave the rest as TODO comments and tests with `[Skip]` markers.

## Validation Before Push

Every push of new work must be preceded by, at minimum:

```text
git status                        # confirm clean of unrelated changes
dotnet build .\vnext\Wevito.VNext.sln
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
```

Every PR-opening push must additionally pass the phase's validation list from the master plan. Don't open a PR with red.

If a push is for in-progress work (e.g. pause-and-resume), include `[wip]` in the commit message and don't open a PR yet:

```text
chore(phase5): wip - habitat manifest schema in progress

[Phase: C-PHASE 5] [wip]
```

## What `main` Looks Like After Each Phase

After C-PHASE N merges, `main` should:

1. Build green.
2. Pass all tests.
3. Pass safe Debug publish.
4. Pass every PET TASKS probe relevant to existing tools.
5. Have the dashboard updated for the phase.
6. Have a phase note appended to `docs\EXECUTION_BOARD.md` (or create that file under "Claude Implementation Plan" section).

## Conflict Resolution

If `git rebase origin/main` shows a conflict:

1. **Sprite files**: `sprites_runtime/`, `sprites_authored*`, `sprites_shared_runtime/` — STOP. Do not auto-resolve. Ask the user.
2. **Content JSON**: `vnext/content/*.json` — manually merge, prefer the older parent's structure if both sides changed the same field.
3. **Code in `vnext/src` and `vnext/tests`**: usually safe to resolve manually; rerun the focused test for the affected adapter to confirm.
4. **`docs/`**: prefer the newer doc; if both sides updated the dashboard, integrate both diffs.
5. **`prop_anchors.json`**: STOP. Visual-side-owned. Ask.

If conflict is in any "STOP" file, push the branch with conflict markers intact (or `git add` resolution as `--ours`/`--theirs` with intent), then ask the user.

## Tagging

Tags are only created in C-PHASE 30. No earlier phase tags `main`.

C-PHASE 30 tag flow:

```bash
git checkout main
git pull --ff-only
git tag -a v0.1.0-claude-rollup -m "Wevito Claude rollup release"
git push origin v0.1.0-claude-rollup
```

If the user wants a different tag name, ask before tagging.

## Hooks

Wevito does not currently use mandatory pre-commit hooks. If the user adds one mid-implementation, Codex must respect it. Never bypass with `--no-verify` unless the user explicitly says so.

## Force-Push Policy

Allowed only with `--force-with-lease`, only on a Claude implementation branch (never `main`), only after a clean rebase, never on a branch the user is reviewing without telling them first.

## Author / Co-Authorship

When Codex commits, follow Wevito's existing convention. Codex is doing the work; co-authorship line if used should match the host's existing pattern. The user can configure this; Codex should not invent a co-authorship line.

## Sample Sequence — C-PHASE 1

For reference, here is a complete C-PHASE 1 commit/push timeline:

```text
1. git fetch origin
2. git checkout -b claude-implementation/c-phase-1-action-animation-prop-contract origin/main
3. <edit Contracts/Models.cs and ContentContracts.cs>
4. dotnet build
5. dotnet test (focused)
6. git add vnext/src/Wevito.VNext.Contracts/Models.cs vnext/src/Wevito.VNext.Contracts/ContentContracts.cs
7. git commit -m "feat(contracts): add AnimationFamily, PropOverlayKind, ActionVisualIntent  [Phase: C-PHASE 1]"
8. git push -u origin claude-implementation/c-phase-1-action-animation-prop-contract

9. <edit vnext/content/actions.json>
10. dotnet test (focused)
11. git add vnext/content/actions.json
12. git commit -m "feat(content): hint optional families for water and play  [Phase: C-PHASE 1]"
13. git push

14. <edit Core/PetSimulationEngine.cs>
15. dotnet test (focused)
16. git add vnext/src/Wevito.VNext.Core/PetSimulationEngine.cs
17. git commit -m "feat(core): emit ActionVisualIntent from simulation  [Phase: C-PHASE 1]"
18. git push

19. <edit Shell/SpriteAssetService.cs>
20. dotnet test (focused)
21. git add vnext/src/Wevito.VNext.Shell/SpriteAssetService.cs
22. git commit -m "feat(shell): SpriteAssetService loads optional family folders with fallback  [Phase: C-PHASE 1]"
23. git push

24. <add tests>
25. dotnet test (focused, then full)
26. git add vnext/tests/Wevito.VNext.Tests/ActionVisualIntentTests.cs vnext/tests/Wevito.VNext.Tests/SpriteAssetServiceOptionalFamilyTests.cs
27. git commit -m "test: cover action/intent/family resolution  [Phase: C-PHASE 1]"
28. git push

29. <update dashboard>
30. git add docs/WEVITO_PROJECT_COMPLETION_DASHBOARD_2026-05-05.md
31. git commit -m "docs: bump game-core completion in dashboard  [Phase: C-PHASE 1]"
32. git push

33. dotnet build && dotnet test --no-build && build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
34. gh pr create --base main --head claude-implementation/c-phase-1-action-animation-prop-contract --title "C-PHASE 1: Action / Animation / Prop unified contract" --body-file <body.md>
35. <wait for human approval>
36. gh pr merge --squash
37. git checkout main && git pull --ff-only
```

## Sample Sequence — C-PHASE 22 (Stop-and-Ask)

```text
1-32. Same as above but for C-PHASE 22 work.
33. dotnet build && dotnet test --no-build && build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
34. gh pr create --base main --head claude-implementation/c-phase-22-sprite-workflow-v2-apply-rollback --draft --title "C-PHASE 22 [DRAFT]: Sprite Workflow V2 backup-apply + rollback + post-proof" --body-file <body.md>
35. Send user message:

   "C-PHASE 22 is ready for review.

   Branch: claude-implementation/c-phase-22-sprite-workflow-v2-apply-rollback
   PR:     <url>

   This is a stop-and-ask phase. I will NOT merge until you approve.

   This phase enables real PNG mutation in sprites_runtime through V2.
   Backup, hash verification, post-apply auto-rollback are all in place.
   No real apply has been performed yet — that is C-PHASE 23.

   Validation summary:
   - 144/144 tests pass
   - One full apply/rollback dry-run on a test row
   - Hashes verified before/after

   Question:
   - Approved to mark non-draft and merge?"

36. <wait>
37. After user approves, mark non-draft, squash-merge.
```

## End

For the master plan and per-phase prompts, see `CLAUDE_FULL_WEVITO_IMPLEMENTATION_PLAN_2026-05-05.md` and `CLAUDE_CODEX_MEDIUM_PHASE_PROMPTS_2026-05-05.md`.
