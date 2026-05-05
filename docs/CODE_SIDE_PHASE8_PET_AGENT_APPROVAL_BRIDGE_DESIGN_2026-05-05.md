# Code-Side Phase 8 Pet Agent Approval Bridge Design - 2026-05-05

## Purpose

Define the next bridge before implementation so pet helpers can eventually act like small AI agents without bypassing user consent, auditability, or Wevito's visual/runtime safety boundaries.

This is design-only. No execution adapter was implemented in this phase.

## Architecture Shape

```text
USER TEXT
   │
   ▼
PET TASKS command bar
   │
   ▼
TaskIntent ──▶ ToolPolicyDecision
   │              │
   ▼              ▼
TaskCard      allowed / approval / blocked
   │
   ▼
Durable queue: CompanionState.TaskCards
   │
   ├── Draft ───────────────┐
   │                        ▼
   ├── WaitingForApproval ─▶ User approval gate
   │                        │
   │                        ▼
   ├── Approved ───────────▶ Adapter dispatcher
   │                        │
   │                        ├── localDocs       read-only
   │                        ├── spriteAudit     read-only / no mutation
   │                        ├── checklist       write docs only after approval
   │                        ├── proofCapture    screenshot/report only
   │                        ├── basket          link-bin only
   │                        └── buildProof      build/probe only after approval
   │
   ├── Running ────────────▶ Audit log + visible companion state
   │
   ├── Reviewing ─────────▶ result summary, proof path, failure reason
   │
   ├── Done
   ├── Blocked
   ├── Failed
   └── Cancelled
```

## Status Transition Rules

```text
Draft
  ├── safe read-only policy ─────────▶ Approved or WaitingForApproval
  ├── medium/high policy ───────────▶ WaitingForApproval
  ├── blocked/risky policy ─────────▶ Blocked
  └── user cancels ─────────────────▶ Cancelled

WaitingForApproval
  ├── user approves ────────────────▶ Approved
  ├── user rejects ─────────────────▶ Cancelled
  └── policy changes/stale target ──▶ Blocked

Approved
  ├── dispatcher accepts ───────────▶ Running
  └── adapter unavailable ──────────▶ Blocked

Running
  ├── adapter finishes ─────────────▶ Reviewing
  ├── adapter fails recoverably ────▶ Failed
  └── user stops task ──────────────▶ Cancelled

Reviewing
  ├── validation passes ────────────▶ Done
  ├── validation fails ─────────────▶ Failed
  └── human review needed ──────────▶ WaitingForApproval
```

## Adapter Gate Requirements

Every executable adapter must provide:

- `ToolFamily` match against a current `ToolPolicy`.
- `DryRunPreview` for medium/high-risk tasks before execution.
- `ApprovedRootPaths` enforcement for any filesystem access.
- `AuditLogPath` written before external effects begin.
- `ResultSummary` and explicit failure reason.
- Cancellation support.
- No silent mutation of `sprites_runtime`, source boards, manifests, game content, docs, or build outputs unless the task card is explicitly approved for that effect.

## Initial Adapter Sequencing

1. `localDocs`: read-only doc lookup/summarization. Lowest risk.
2. `spriteAudit`: non-mutating sprite/report audit only. No PNG writes.
3. `proofCapture`: screenshot/contact-sheet/proof collection only.
4. `checklist`: docs/checklist draft writes after approval.
5. `basket`: link-bin organization only.
6. `buildProof`: build/test/probe after approval, with log paths.
7. Visual generation/import/apply adapters: deferred until visual-side production-safe manifest/provenance/apply workflow is complete.

## Required UI Before Execution

The `PET TASKS` panel needs these controls before any adapter can run:

- selected queued card detail,
- `Approve` for `WaitingForApproval`,
- `Cancel` for `Draft`, `WaitingForApproval`, `Approved`, and `Running`,
- clear explanation of why a task is blocked,
- dry-run preview for any non-read-only action,
- audit/proof path link after completion.

## Safety Boundaries

- Pet personality can influence presentation, tone, and helper routing, not permissions.
- Tool policy controls permissions.
- User approval controls execution.
- Adapter contracts control side effects.
- Audit logs are required before side effects.
- Visual generation/import/apply remains paused until the visual/code production workflow is explicitly approved.

## Open Design Questions

- Whether task cards should remain in `CompanionState` JSON long-term or move into normalized SQLite tables once execution history grows.
- Whether approval should be per task card, per tool family, or per temporary session.
- Whether pet-helper "learning" should use reviewed examples only, or also aggregate anonymous task outcomes.
- Whether browser/Gemini/Claude Design adapters should live inside Wevito, outside Wevito as helper tools, or remain manual handoff workflows.

## Recommended Next Implementation Phase

Implement approval-state editing without execution:

- select a queued task card,
- cancel task card,
- mark `WaitingForApproval` card as `Approved`,
- persist status transitions,
- do not run adapters yet.
