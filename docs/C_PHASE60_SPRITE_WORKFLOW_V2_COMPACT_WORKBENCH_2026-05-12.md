# C-PHASE 60 Sprite Workflow V2 Compact Workbench

Date: 2026-05-12
Branch: `claude-implementation/c-phase-60-sprite-workflow-v2-compact`

## Outcome

C-PHASE 60 compacts Sprite Workflow V2 into a single selected-row workbench with four visible lanes:

```text
---------------- compact row workbench ----------------+
| capped queue | selected target summary               |
|              | + source authored/verified + runtime  |
|              | + candidate                + proof     |
|              | mutation gate: import -> dry-run ->    |
|              | exact row id -> exact one-row scope    |
+------------------------------------------------------+
```

## What Changed

- Capped the queue display at 120 rows and added queue count guidance so users filter instead of scrolling through the full project.
- Reworked the selected row area into equal source/runtime/candidate/proof lanes.
- Added the target summary line:
  - `species | age | gender | color | family | expected frames | status`
- Added an obvious report-only state before candidate import.
- Kept this phase free of generation, Gemini, browser, or automatic apply controls.
- Tightened apply enablement so Apply requires all gates:
  - selected row exists
  - candidate import exists and targets the selected row
  - dry-run manifest exists
  - user typed exact row id
  - dry-run proves exact mutation scope
  - no apply has already happened
- Added exact-scope checks for runtime overwrite, current runtime hash, candidate hash, frame-family prefix, runtime-row containment, and backup-folder containment.

## Safety Notes

- Window load reads manifests and may write contact-sheet cache artifacts under `vnext/artifacts/sprite-workflow-v2-cache/`.
- Window load does not mutate `sprites_runtime`.
- Apply remains a manual gated action. It is still backed by the existing backup/apply/rollback services.
- No PNGs, source boards, prop anchors, generation controls, or browser/Gemini controls were changed.

## Tests

Added/updated SpriteWorkflow tests for:

- exact row-id confirmation
- candidate + dry-run + exact-scope apply gate
- rejection when dry-run would add or target outside exact scope
- target summary line content

## Validation

- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "SpriteWorkflow"` passed: 20 / 20.
- `dotnet build .\vnext\Wevito.VNext.sln` passed: 0 warnings, 0 errors.
- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build` passed: 294 / 294.

## Mutation Statement

Changed files:

```text
vnext/src/Wevito.VNext.Shell/SpriteWorkflowV2Window.xaml
vnext/src/Wevito.VNext.Shell/SpriteWorkflowV2Window.xaml.cs
vnext/tests/Wevito.VNext.Tests/SpriteWorkflowApplyRollbackTests.cs
docs/C_PHASE60_SPRITE_WORKFLOW_V2_COMPACT_WORKBENCH_2026-05-12.md
```

No runtime/source sprite PNGs were modified.

## Next Phase

C-PHASE 61 can proceed separately on a fresh branch after this PR is merged. It should not add uncontrolled training or automatic promotion behavior.
