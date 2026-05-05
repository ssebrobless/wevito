# Code-Side Phase 4 Runtime Contract Hardening

Date: 2026-05-04

Worktree:

`C:\Users\fishe\.codex\worktrees\36d6\wevito`

Purpose:

Make a small deterministic runtime-contract hardening pass after the Phase 3 validation sweep. No sprite PNGs, visual generation/import paths, Godot scripts, or broad runtime architecture were changed.

## Scope

```text
Phase 4 hardening
|
+-- Compare current runtime contract seams
|   +-- SpriteRuntimeCoverageTests
|   +-- report_runtime_canvas_mismatches.py
|   +-- build-vnext skip/test behavior
|
+-- Apply only narrow guardrail
|   +-- reporter now flags non-alpha PNG color types as invalid
|
+-- Revalidate
    +-- reporter fail-on-mismatch
    +-- focused SpriteRuntimeCoverageTests
    +-- full vNext tests
```

## Change Made

Updated:

`C:\Users\fishe\.codex\worktrees\36d6\wevito\tools\report_runtime_canvas_mismatches.py`

What changed:

- The reporter already checked for bad PNG headers.
- `SpriteRuntimeCoverageTests` already required PNG color type `4` or `6`, which means alpha-capable PNGs.
- The reporter now also treats non-alpha PNG color types as invalid frames.
- The markdown label and `--fail-on-mismatch` help text now say invalid or non-alpha PNGs are failures.

Why:

This aligns the Python reporter with the C# runtime coverage test so the fast non-mutating report catches the same kind of alpha regression before the test suite has to explain it.

## Commands Run

### Runtime Canvas Reporter

Command:

```powershell
python tools\report_runtime_canvas_mismatches.py --runtime-root sprites_runtime --output vnext\artifacts\runtime-canvas-contract-20260504-phase4-hardening.json --markdown vnext\artifacts\runtime-canvas-contract-20260504-phase4-hardening.md --fail-on-mismatch
```

Result:

- Passed.
- `2880` sequences checked.
- `10800` frames checked.
- `0` sequence canvas mismatches.
- `0` missing/count mismatches.
- `0` invalid or non-alpha PNGs.
- `3852` legacy fixed-canvas diagnostic mismatches.

Artifacts:

- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\runtime-canvas-contract-20260504-phase4-hardening.json`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\runtime-canvas-contract-20260504-phase4-hardening.md`

### Focused Sprite Runtime Coverage

Command:

```powershell
dotnet test .\vnext\Wevito.VNext.sln -c Debug --no-restore --filter "FullyQualifiedName~SpriteRuntimeCoverageTests"
```

Result:

- Passed.
- `2 / 2` tests passed.

### Full vNext Tests

Command:

```powershell
dotnet test .\vnext\Wevito.VNext.sln -c Debug --no-restore
```

Result:

- Passed.
- `26 / 26` tests passed.

### Whitespace/Syntax Diff Check

Command:

```powershell
git diff --check -- tools/report_runtime_canvas_mismatches.py
```

Result:

- Passed.

## Phase 4 Audit Result

Pass.

The active runtime sprite contract is now enforced consistently by both the C# runtime coverage test and the Python non-mutating reporter:

- Required runtime rows must exist at expected counts.
- Each exact animation sequence must have stable frame dimensions.
- PNGs must have valid headers.
- PNGs must be alpha-capable.
- Legacy fixed-canvas mismatches remain diagnostic only unless `--fail-on-canonical-mismatch` is requested.

## Deferred / Not Changed

- Did not update old Gemini import docs or `import_gemini_sprite_block.py` references to `28x24`; that belongs to visual/import workflow planning, not this runtime-contract hardening pass.
- Did not alter the large runtime PNG payload.
- Did not touch Godot gameplay scripts or Gemini automation helper tools.
- Did not stage any files.

## Recommended Next Phase

Proceed to Phase 5: Code-Side Visual Readiness Check.

Suggested scope:

1. Read the current visual-side docs and current code-side reports.
2. Confirm what code seams are ready for visual-side contact sheets and future asset replacement.
3. Identify readiness gaps for habitat zones, prop anchors, body condition, ghost rendering, and forced dev scenarios.
4. Do not implement broad gameplay or visual-generation changes without explicit coordination.
