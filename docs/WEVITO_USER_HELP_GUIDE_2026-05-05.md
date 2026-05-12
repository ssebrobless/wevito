# Wevito User Help Guide

Date: 2026-05-12
Status: release-candidate guide for the vNext helper shell and current Godot pet game.

## What Wevito Is

Wevito is a desktop pet companion with helper tools disguised as a small pet-sim overlay. The pets should remain visually normal pets: task work happens through task cards, reports, previews, and approval gates rather than special "work complete" animations.

## Install And Launch

### vNext Helper Shell

1. Build or unpack the vNext shell release.
2. Launch:

```powershell
.\vnext\artifacts\shell\Wevito.VNext.Shell.exe
```

3. The shell starts the helper broker, home panel, basket/tool popup surface, and PET TASKS command flow.

### Godot Desktop Pet

The Godot desktop export is not yet the recommended C-PHASE 30 release package. Use the Godot project/dev run path until `docs/C_PHASE30_RELEASE_BLOCKER_2026-05-12.md` is resolved.

## Core Controls

```text
Wevito controls
|-- global hotkeys
|   |-- Ctrl+Shift+P or Ctrl+Alt+P: toggle pinned overlay mode
|   |-- Ctrl+Shift+O or Ctrl+Alt+O: open basket/tool popup
|   |-- Ctrl+Shift+B or Ctrl+Alt+B: capture basket/clipboard helper action
|   `-- Ctrl+Shift+D or Ctrl+Alt+D: open dev tools/debug surface
|
|-- mouse
|   |-- click pet/home panel controls for care/actions
|   |-- use basket popup buttons for captured links/text
|   `-- pinned overlay should not steal focus from another foreground app
|
`-- PET TASKS
    |-- type a natural-language request
    |-- preview/report appears first
    `-- higher-risk execution needs explicit approval
```

If another program already owns a `Ctrl+Shift` hotkey, Wevito's broker can fall back to the matching `Ctrl+Alt` hotkey and records that in the broker trace.

## PET TASKS Basics

The command bar routes work to helper pets and tool families. Use simple task wording:

| Tool family | Example task |
| --- | --- |
| localDocs | `summarize the local docs` |
| spriteAudit | `review goose baby female blue sprites` |
| assetInventory | `inventory assets in sprites_runtime` |
| petState | `review pet state` |
| codeReview | `review the code in Wevito.VNext.Core` |
| codePatchPlan | `plan a code fix in vnext` |
| buildProof | `run a build proof` |
| translateText | `translate Hello goose to Spanish` |
| audioAssist | `boost my PC volume` |
| screenCapture | `screenshot the Wevito window` |

Avoid starting commands with a pet name unless that exact helper is present in the live roster. The parser treats `Name, do this` as explicit addressing and will block if the name is not a live helper.

## Helper Pets

Wevito currently uses a small helper roster. The exact live names come from seeded pets, but the intended roles are:

| Role | Purpose |
| --- | --- |
| SpriteReviewHelper | Sprite/art QA, reports, and no-mutation audit work. |
| ChecklistHelper | Project status, docs, release/checklist tasks. |
| ResearchHelper | Read-only research summaries and local docs. |

Pet personality affects tone/presentation, not permissions. A cute or eager pet does not get extra authority to mutate files, call models, capture screens, or run risky tools.

## Settings And Providers

### Translation

Translation is routed through the PET TASKS translation surface. Provider-backed execution should remain explicit and observable. If a provider API key is needed, store it through the approved credential path for that provider and keep provider disclosures visible.

### Audio

Audio assist should prefer safe Windows volume guidance and reversible settings. External boosters or APO-style system changes should be treated as higher-risk and approval-gated.

### Capture

Screenshot and future screen-recording features are privacy-sensitive. Wevito should label what is captured, where artifacts are written, and whether a proof is an offline approximation or a packaged/live proof.

### Model/AI

Live model calls are default-disabled. The model adapter consent flow and capability flag must be approved before any first real provider call.

## Privacy Disclosures

| Area | Disclosure |
| --- | --- |
| Capture | Screenshots or future recordings can include private desktop content. Use approval-gated capture and review saved artifact folders. |
| Translation | Provider translation can send text to an external service. Do not translate secrets or private data unless intended. |
| Audio | System audio changes can affect the entire PC. Prefer reversible volume changes and avoid unsafe boosting. |
| Model calls | AI providers can receive prompt content. Live calls remain disabled until explicitly enabled and approved. |
| Local artifacts | PET TASKS writes timestamped reports/artifacts under `vnext/artifacts/` by default. |

## Troubleshooting

### PET TASKS Probe Fails

Check the latest artifact folder under:

```text
vnext/artifacts/pet-task-probes/
```

Then inspect:

```text
vnext/artifacts/shell/Wevito.VNext.Shell.trace.log
vnext/artifacts/broker/Wevito.VNext.Broker.trace.log
```

### Pinned Overlay Does Not Respond

1. Confirm another app owns foreground focus.
2. Try the fallback hotkey: `Ctrl+Alt+P` or `Ctrl+Alt+O`.
3. Check broker trace for `hotkey-register` and `overlay-click`.
4. Re-run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\probe-vnext-pinned.ps1 -SkipBuild
```

### Sprite Canvas Or Runtime Contract Issues

Run:

```powershell
python .\tools\report_runtime_canvas_mismatches.py --output .\vnext\artifacts\runtime-canvas-mismatches.json --markdown .\vnext\artifacts\runtime-canvas-mismatches.md --fail-on-mismatch
python .\tools\audit_sprite_contract.py --output .\vnext\artifacts\sprite-contract.json
```

Do not run broad asset prep or sprite import unless that work is explicitly approved.

### Sprite Workflow Rollback

Approved Sprite Workflow V2 applies should always produce:

- backup-before-apply files
- pre/post hashes
- post-proof report
- rollback drill result

If any hash does not match the expected manifest, stop and document the blocker instead of applying.

### Release Packaging

The current release-candidate-safe vNext build path is:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Release -SkipAssetPrep
```

The Godot desktop export path is still blocked as of C-PHASE 30. See:

```text
docs/C_PHASE30_RELEASE_BLOCKER_2026-05-12.md
```

## Current Release Notes

- vNext Release build/test/publish is green with existing runtime assets.
- Runtime canvas contract was green in C-PHASE 29.
- Godot export needs further packaging work before a final tag.
- No live model call is part of this release candidate.
