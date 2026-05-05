# Wevito Tool-Agent UI Master Plan - 2026-05-05

## Goal

Turn Wevito into a simple pet-simulator front end for powerful helper tools without exposing the user to a complicated tool dashboard.

The user should experience this:

```text
Ask a pet for help
   |
   v
Pet creates a task card
   |
   v
Pet previews or runs the safest available tool
   |
   v
User gets one clear result
   |
   v
User accepts, revises, or asks for the next step
```

Not this:

```text
Open many tabs
   |
   v
Pick from many technical tools
   |
   v
Configure paths, modes, backends, manifests
   |
   v
Guess what happened
```

## Core UX Principle

Every complex system must collapse into one of these simple user-facing task types:

```text
Review
Capture
Fix
Create
Code
Research
Organize
Explain
```

The pet can internally route to `spriteAudit`, `localDocs`, `petState`, capture tools, code tools, or future creative tools, but the user should only need to type a natural request.

## One Entry Point

The primary UI should be:

```text
PET TASKS
   |
   +-- "Ask your pets..."
   |
   +-- Active helper pets
   |
   +-- Current task card
   |
   +-- Result card
   |
   +-- Review / Approve / Revise / Open Artifact
```

Secondary UI should only appear when needed:

- capture selector,
- report viewer,
- code review diff,
- sprite proof contact sheet,
- creative lab sample board.

## Lessons From The-Workinator

Source project:

`C:\Users\fishe\Documents\projects\work-folder\The-Workinator`

Useful Workinator ideas:

- Single local app shell with all helpers under one roof.
- Task-specific folders.
- Knowledge-base folder.
- Task intake turns messy instructions into structured requirements.
- Capture tools use task-based file naming.
- Recording/crop/trim are attached to the active task.
- Coding workspace has language selection, scan/decode, structure preview, focused error context, snippets, formatting preview, and diff preview.
- Final output is a task packet with missing-item warnings and QA checks.

How Wevito should adapt this:

- Use task cards instead of Workinator tabs as the main organizing unit.
- Put artifacts under `vnext\artifacts\pet-tasks\<timestamp-slug>\`.
- Keep the helper-pet UI small and friendly.
- Do not surface full Workinator-style modules unless a task needs them.
- Use pet names/roles to make routing understandable without making permissions depend on personality.

## Tool Families

### Already Implemented Preview Tools

```text
spriteAudit
   reads sprite rows
   writes markdown/json report
   does not mutate PNGs

petState
   reads current pet state
   writes wellbeing/debug-truth report
   does not mutate gameplay

localDocs
   reads approved docs
   writes markdown/json summary
   does not mutate docs
```

### Planned Tools

```text
captureProof
   screenshot / proof capture
   starts with Wevito-window capture only

codeReview
   scan code, summarize structure, detect likely issues
   first version is read-only report

codePatchPlan
   propose patch plan only
   no file edits from PET TASKS at first

creativeLab
   style study, prompt packet, reference board, generation plan
   no generation/import initially

spriteWorkflow
   manifest, provenance, contact sheet, candidate review
   no apply/mutation until explicit approval pipeline exists

assetInventory
   scan items, sprites, habitats, food, medicine, UI art
   report-only first

buildProof
   run build/test/probe
   approval required

browserResearch
   web/search or local browser tasks
   approval required, no accounts/secrets by default

translateText
   translate pasted/user-provided text to a selected language
   provider-routed, artifact-backed, approval-gated for network calls

audioAssist
   inspect/control PC audio volume safely
   boost/equalizer work is approval-gated and external/APO-aware
```

## Pet Roles

Keep roles friendly and simple:

```text
Scout
   review / research / local docs

Inspector
   sprite audit / pet state / QA reports

Builder
   coding / build proof / implementation planning
```

The user can rename pets. Roles only affect routing and presentation. Permissions always come from policy.

## Phase Plan

### Phase 25: Tool Hub Information Architecture

Purpose:

Create the concrete UI map before adding more controls.

Deliverables:

- Define the Tool Hub sections.
- Define task card detail layout.
- Define result card layout.
- Define hidden advanced panels.
- Define which controls are always visible versus contextual.
- Create a no-code wireframe doc.

Success criteria:

- One obvious command entry point.
- No more than one primary result card visible.
- Advanced tool details are hidden by default.

### Phase 26: Tool Hub Shell Refactor

Purpose:

Reorganize existing PET TASKS popup into a clearer "Tool Hub" without adding new powers.

Deliverables:

- Rename/reshape "Pet Helpers" surface into "Tool Hub" or "Pet Tasks".
- Keep command bar at top.
- Show helper pets compactly.
- Show current task card.
- Show latest result path/report.
- Keep capability text.

Success criteria:

- User can understand what to type, what pet is assigned, what happened, and what to click next.

### Phase 27: Capture Contracts And Policy

Purpose:

Prepare screenshot/screen-capture safely.

Deliverables:

- Capture request/result contracts.
- Capture preset enum.
- Capture privacy level enum.
- Capture manifest schema.
- Policy rules:
  - Wevito window capture: low risk.
  - selected region: approval required.
  - foreground window: approval required.
  - full desktop: approval required.
  - recording: approval required.
  - external upload/share: blocked.

Success criteria:

- No screen capture yet.
- Tests prove risky capture requests are gated.

### Phase 28: Wevito Window Screenshot

Purpose:

First real capture capability, limited to Wevito itself.

Deliverables:

- Capture current Wevito window.
- Write `screenshot.png`, `manifest.json`, `run-summary.md`.
- Store under timestamped artifact folder.
- Add PET TASKS report path.

Success criteria:

- Visible/manual capture only.
- No full desktop capture.
- No recording.
- No upload.

### Phase 29: Asset Inventory Report Tool

Purpose:

Give visual-side and code-side a shared way to inspect game assets without mutation.

Deliverables:

- `assetInventory` adapter.
- Report sprite counts, item counts, missing expected groups, stale references.
- Optional sample contact sheet only when requested later.

Success criteria:

- Read-only.
- Useful enough to support visual cleanup without opening many folders manually.

### Phase 30: Code Review Preview Tool

Purpose:

Introduce coding capabilities safely.

Deliverables:

- `codeReview` task family.
- Approved-root read-only file scan.
- Language/file type detection.
- Structure summary.
- Likely issue list.
- Suggested tests.
- No edits.

Workinator ideas to reuse:

- language picker concept,
- structure preview,
- focused error context,
- diff preview pattern,
- snippets later.

Success criteria:

- Pet can answer "review this file/module" with a report.
- No file writes.
- No terminal command execution.

### Phase 31: Code Patch Plan Tool

Purpose:

Let pets propose implementation plans before editing.

Deliverables:

- `codePatchPlan` task family.
- Produces:
  - scope,
  - affected files,
  - proposed changes,
  - tests,
  - rollback plan.
- Still no edits from PET TASKS.

Success criteria:

- User can review a patch plan before asking Codex to implement.

### Phase 32: Build/Test Proof Adapter

Purpose:

Let approved pet tasks run safe build/test/probe commands.

Deliverables:

- `buildProof` execution adapter.
- Approval required.
- Command allowlist.
- Timeout.
- artifact log.
- status transitions.

Success criteria:

- Can run known safe commands like vNext tests or probes.
- Cannot run arbitrary shell.

### Phase 33: Creative Learning Lab Planning Surface

Purpose:

Bring creative-ai/Claude Design/Gemini sprite workflow ideas into Wevito without making a confusing art suite.

User-facing shape:

```text
Ask:
  "Make a reference packet for goose walk cleanup"

Result:
  one packet with references, style rules, prompt draft, checklist, and next action
```

Deliverables:

- `creativeLab` report adapter.
- Reference packet schema.
- Style-direction summary.
- Prompt packet output.
- Review checklist output.
- No image generation/import yet.

Success criteria:

- One result packet.
- No generator UI.
- No mutation.

### Phase 34: Sprite Workflow Planning Surface

Purpose:

Create a simplified, better replacement for the old sprite workflow app concepts.

User-facing shape:

```text
Ask:
  "Review baby goose idle and tell me what to fix"

Result:
  contact sheet / findings / candidate instructions / apply status
```

Deliverables:

- Unified sprite workflow task card type.
- Manifest/provenance fields.
- Candidate review packet.
- Apply/rollback plan only.
- No apply until explicit production pipeline is ready.

Success criteria:

- Visual work becomes easy to inspect.
- The user does not need to scroll through giant app surfaces.

### Phase 35: Controlled Mutation Gate

Purpose:

Only after report/proof surfaces are reliable, allow carefully approved changes.

Deliverables:

- Mutation approval UI.
- Backup-before-apply.
- Hash verification.
- Rollback button/plan.
- Apply manifest.
- Post-apply proof.

Success criteria:

- No silent mutation.
- Every changed file has provenance and rollback.

### Phase 36: Region Capture And Short Clips

Purpose:

Support visual QA and "show me what happened" clips.

Deliverables:

- region capture,
- last region,
- 5-10 second proof clips,
- privacy labels,
- optional GIF export later.

Success criteria:

- Works for sprite animation proof and game-behavior proof.
- Does not become a general surveillance recorder.

### Phase 37: Browser/Research Adapter

Purpose:

Add web research capability only after local tools are stable.

Deliverables:

- explicit approval,
- source logging,
- no account/secrets automation by default,
- markdown research packet.

Success criteria:

- Useful for docs/research.
- Not silently controlling browser sessions.

### Phase 38: Translation Contracts And Provider Router

Purpose:

Add translation as a simple PET TASKS tool without making the user choose technical providers first.

Deliverables:

- `translateText` task family.
- Translation request/result contracts.
- Provider status model for DeepL, Google Cloud Translation, Azure AI Translator, and LibreTranslate/self-hosted.
- Source language, target language, glossary/style fields.
- Provider router with no network call yet.

Success criteria:

- User can ask for translation and get a preview packet explaining what would happen.
- No text is sent to a provider yet.

### Phase 39: Translation Preview Adapter

Purpose:

Turn natural language translation requests into reviewable task cards.

Deliverables:

- Parse commands like `translate this to Spanish`.
- Estimate character count.
- Show target/source language.
- Show provider availability.
- Write preview artifact.

Success criteria:

- No external API call.
- No private text leaves the machine.

### Phase 40: Translation Execution Adapter

Purpose:

Allow approved translation through the first configured provider.

Deliverables:

- Approval-gated provider call.
- Backend-only credential access.
- Timestamped artifacts:
  - translated text,
  - JSON report,
  - markdown summary.

Success criteria:

- User explicitly triggers translation.
- Output is easy to review/copy.

### Phase 41: Translation Glossary And QA

Purpose:

Make translation safer for game/UI/code/project text.

Deliverables:

- Glossary support.
- Placeholder/markdown/code-block preservation checks.
- Glossary coverage report.

Success criteria:

- Translation result includes quality warnings before the user uses it.

### Phase 42: Audio Assist Contracts And Policy

Purpose:

Prepare PC audio helper tools safely.

Deliverables:

- `audioAssist` task family.
- Audio action kinds:
  - inspect volume,
  - set volume,
  - mute/unmute,
  - boost guide,
  - external enhancer handoff.
- Policy:
  - inspect is low risk,
  - volume changes require approval,
  - driver/APO install is blocked.

Success criteria:

- No audio is changed yet.

### Phase 43: Audio Status Preview

Purpose:

Give the user a safe first audio helper result.

Deliverables:

- Read current output device, master volume, mute state.
- Optional per-app sessions later.
- Write markdown/JSON report.

Success criteria:

- Read-only.
- No volume changes.

### Phase 44: Safe Volume Control

Purpose:

Let Wevito adjust normal Windows volume with explicit approval.

Deliverables:

- Approval-gated master volume set/mute/unmute.
- Maximum `100%`.
- Clear safety/feedback text.

Success criteria:

- No boost beyond Windows endpoint volume.

### Phase 45: External Boost Handoff

Purpose:

Support real system-wide boosting without building risky driver-level code.

Deliverables:

- Detect/help with FxSound or Equalizer APO.
- Generate setup guide.
- Open official docs/downloads only after approval.
- No install/config mutation.

Success criteria:

- User gets a safe path to real boost/equalizer functionality.

### Phase 46: Boost Preset Planning

Purpose:

Prepare safe gain/EQ suggestions without applying them silently.

Deliverables:

- Conservative preset text.
- Headroom/limiter warning.
- Device compatibility notes.

Success criteria:

- Plan only.
- User applies externally or via a future controlled mutation gate.

## Simplicity Rules

Every new tool must answer:

1. What is the one-sentence user request?
2. What is the single result artifact?
3. What is hidden by default?
4. What is the maximum allowed side effect?
5. What approval is required?
6. What proof shows it worked?

If a tool needs more than one visible panel at first, it is too complex.

## Recommended Immediate Next Step

Start Phase 25, then Phase 26.

Reason:

- The UI shape must be simple before adding capture, coding, creative lab, or sprite workflow tools.
- Existing PET TASKS works but is still a popup with accumulating controls.
- A clean Tool Hub shell gives every future capability a place to live.

Translation/audio research and implementation details are tracked in:

`C:\Users\fishe\.codex\worktrees\36d6\wevito\docs\TRANSLATION_AND_AUDIO_TOOL_RESEARCH_PLAN_2026-05-05.md`
