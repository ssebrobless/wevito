# Wevito Docs Index

This folder is an audit trail plus an implementation map. Older docs are kept for provenance; newer docs define the current local-AI direction.

```text
docs/
  |
  +-- current decision source
  |     DECISION_LEDGER_2026-05-15.md
  |     CLAUDE_MASTER_PLAN_2026-05-15.md
  |     CLAUDE_REFRAME_PHASES_106_THROUGH_116_PLAN_2026-05-15.md
  |
  +-- current phase reports
  |     C_PHASE106_...
  |     C_PHASE107_...
  |     ...
  |
  +-- sprite and visual workflow
  |     AUTHORED_ANIMATION_WORKFLOW.md
  |     REUSABLE_SPRITE_PIPELINE_PLAYBOOK.md
  |     VISUAL_* and WEVITO_VISUAL_* docs
  |
  +-- code-side runtime/tool history
  |     CODE_SIDE_* docs
  |     C_PHASE0 through C_PHASE85c docs
  |
  +-- preserved historical planning
        CLAUDE_*_2026-05-04.md
        CLAUDE_*_2026-05-05.md
        CLAUDE_*_2026-05-12.md
```

## Read First For Current Work

1. `DECISION_LEDGER_2026-05-15.md`
2. `CLAUDE_MASTER_PLAN_2026-05-15.md`
3. `CLAUDE_REFRAME_PHASES_106_THROUGH_116_PLAN_2026-05-15.md`
4. The latest `docs/C_PHASE<N>_*.md` report for the phase you are extending

## Frozen Audit Trail

Do not rewrite historical phase reports. In particular, C-PHASE 0 through C-PHASE 85c documents are retained as evidence of the earlier pet-first and tool-hub work.

If a newer phase supersedes an old plan, add a new report instead of editing the old one.

## Naming Guidance

- `C_PHASE*.md`: implementation reports and blockers.
- `CLAUDE*.md`: plans or prompts produced by Claude for Codex execution.
- `CODE_SIDE*.md`: code-side audit, planning, or visual-thread handoffs.
- `WEVITO_VISUAL*.md`: visual-side planning and QA documents.
- `*_BUG_*` or `BUG_BOARD.md`: issue tracking surfaces.
