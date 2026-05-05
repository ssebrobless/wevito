# Wevito AI Pet, Sprite, And Tooling Phase Roadmap

Date: 2026-05-05

Purpose: consolidate the code-side review, visual-side handoffs, Sprite Workflow lessons, Dibu/creative-ai architecture, The-Workinator helper patterns, OpenClaw/MCP-style tool orchestration, Hatch Pet provenance discipline, and current web research into one phased plan for discussion.

This is a draft plan, not implementation approval. It does not authorize new visual generation, runtime PNG rewrites, broad automation, or pet-agent actions.

## System Shape

```text
╔══════════════════════════════════════════════════════════════════════════════╗
║ Wevito Target Architecture                                                 ║
╠════════════════════╦═════════════════════════════════════════════════════════╣
║ Godot pet runtime  ║ living pets, visuals, habitat, interaction proof       ║
║ vNext core         ║ durable simulation truth: needs, habits, personality   ║
║ vNext shell/tools  ║ desktop helper UI, dev tools, task cards, diagnostics  ║
║ Pet Command Bar    ║ user text input for up to three named pet helpers      ║
║ Sprite Workflow V2 ║ source/candidate/proof/apply/rollback for sprite rows  ║
║ Creative Lab       ║ reviewed examples, preference labels, offline evals    ║
║ Tool Broker        ║ permissioned local tools, logs, approvals, guardrails   ║
║ AI adapters        ║ OpenAI/API/local-model adapters behind stable schemas   ║
╚════════════════════╩═════════════════════════════════════════════════════════╝
```

The clean version of Wevito is a layered pet companion, not a tangled automation app. Pets should feel alive and helpful, but tool use, sprite mutation, model training, and file writes must stay visible, reversible, and logged.

The product fantasy is intentionally asymmetric: the visible animals should have simple, readable pet personalities, while the helper layer behind them can be a serious AI-agent/tool system. Do not overbuild Papilionem-style inner psychology for Wevito pets; use Papilionem as inspiration for boundaries and continuity, not as a target complexity level.

## Research Summary By Aspect

```text
╔════════════════════════╦══════════════════════════════╦══════════════════════╗
║ Aspect                 ║ Research-backed direction    ║ Wevito use          ║
╠════════════════════════╬══════════════════════════════╬══════════════════════╣
║ AI task execution      ║ tools + structured schemas   ║ task cards          ║
║ Agent safety           ║ guardrails + tracing         ║ visible audit log   ║
║ Pet helper UI          ║ named helpers + command bar  ║ 3 active pet agents ║
║ Image generation       ║ edit/generate candidates     ║ candidate lane only ║
║ Sprite workflow        ║ source/proof/apply/rollback  ║ Workflow V2         ║
║ Pet simulation         ║ truth feeds AI, not replaced ║ vNext/Godot parity  ║
║ Creative learning      ║ evals before training        ║ Creative Lab        ║
║ Local tooling          ║ adapter + broker boundaries  ║ safe desktop tools  ║
║ Game integration       ║ animation/prop manifests     ║ Godot proof first   ║
╚════════════════════════╩══════════════════════════════╩══════════════════════╝
```

Research references:

- OpenAI Responses API supports multimodal, stateful, tool-using workflows with built-in and custom tools: `https://platform.openai.com/docs/api-reference/responses/retrieve`
- OpenAI recommends Responses for new agent-like projects and notes built-in tools such as web search, file search, computer use, code interpreter, and image generation: `https://platform.openai.com/docs/guides/responses-vs-chat-completions`
- OpenAI Structured Outputs provides schema-constrained JSON outputs useful for task cards, manifests, repair queue entries, and QA decisions: `https://platform.openai.com/docs/guides/structured-outputs`
- OpenAI function calling is the correct pattern when a model needs to invoke application functions: `https://platform.openai.com/docs/guides/function-calling`
- OpenAI image generation/editing supports transparent backgrounds, image inputs, multi-turn editing, and high input fidelity: `https://platform.openai.com/docs/guides/image-generation`
- OpenAI Code Interpreter can run Python in a sandbox and process images/files, which is useful for QA and contact-sheet/eval helpers: `https://platform.openai.com/docs/guides/tools-code-interpreter/`
- OpenAI Computer Use is beta, screenshot/action-loop based, and explicitly risky in authenticated or high-stakes environments: `https://platform.openai.com/docs/guides/tools-computer-use`
- OpenAI Agents SDK supports tools, handoffs, guardrails, and tracing: `https://platform.openai.com/docs/guides/agents-sdk`, `https://openai.github.io/openai-agents-python/tools/`, `https://openai.github.io/openai-agents-js/guides/guardrails/`, `https://openai.github.io/openai-agents-python/tracing/`
- OpenAI model optimization guidance emphasizes evals, prompt iteration, and fine-tuning only after measurement: `https://platform.openai.com/docs/guides/fine-tuning`
- OpenAI supervised fine-tuning docs explicitly recommend evals before investing in fine-tuning: `https://platform.openai.com/docs/guides/supervised-fine-tuning`
- Hugging Face Diffusers supports LoRA as a lightweight fine-tuning method with smaller portable weights: `https://huggingface.co/docs/diffusers/v0.27.2/en/training/lora`
- Hugging Face Diffusers training scripts are examples that need adaptation to the use case: `https://huggingface.co/docs/diffusers/training/overview`
- Hugging Face Datasets can create image datasets via image-folder style loading: `https://huggingface.co/docs/datasets/v2.18.0/en/image_dataset`
- Godot AnimatedSprite2D supports SpriteFrames, animation state, `flip_h`, frame changes, and animation finished/looped signals: `https://docs.godotengine.org/en/4.4/classes/class_animatedsprite2d.html`
- Godot CanvasItem supports Z-index and Y-sorting for 2D depth/layering work: `https://docs.godotengine.org/en/4.1/classes/class_canvasitem.html`
- Godot command-line export/headless workflows support deterministic build/proof automation: `https://docs.godotengine.org/en/4.4/tutorials/editor/command_line_tutorial.html`
- Godot ResourceLoader/FileAccess distinction matters for packaged runtime assets and user data: `https://docs.godotengine.org/en/4.6/classes/class_resourceloader.html`, `https://docs.godotengine.org/en/4.3/classes/class_fileaccess.html`
- FastAPI BackgroundTasks can support simple local backend tasks, but complex long jobs still need visible status and stronger lifecycle control: `https://fastapi.tiangolo.com/tutorial/background-tasks/`
- Tauri sidecars can embed external binaries for local desktop tools, and Tauri capabilities define desktop command permissions: `https://tauri.app/develop/sidecar/`, `https://v2.tauri.app/es/security/capabilities/`
- OpenClaw docs emphasize typed tools/plugins and a local tool surface: `https://docs.openclaw.ai/tools`
- OpenClaw security docs are a reminder that local agent execution needs hard boundaries: `https://docs.openclaw.ai/security`
- MCP standardizes tools/resources/prompts for model-tool integration, but security and consent remain implementor responsibilities: `https://modelcontextprotocol.io/specification/2024-11-05/index`, `https://docs.anthropic.com/en/docs/mcp`
- Hatch Pet's skill design strongly supports canonical references, row manifests, deterministic assembly, QA contact sheets, preview videos, and not fabricating visual outputs with code: `https://raw.githubusercontent.com/openai/skills/main/skills/.curated/hatch-pet/SKILL.md`
- Claude Design can create interactive prototypes, presentations, and design artifacts from chat plus screenshots/assets/code references, then export/share handoff artifacts: `https://support.claude.com/en/articles/14604416-get-started-with-claude-design`
- Claude Design design systems can extract reusable colors, typography, components, and layout patterns from focused product references; use small curated Wevito context rather than uploading broad repo state: `https://support.claude.com/en/articles/14604397-set-up-your-design-system-in-claude-design`
- OpenAI's current Agents guidance fits a code-owned Wevito orchestration model: the application should own tools, approvals, state, and specialist handoffs while agent surfaces propose and execute only through those boundaries: `https://developers.openai.com/api/docs/guides/agents`
- OpenAI guardrail guidance supports wrapping tool calls with pre/post validation, which maps directly to Wevito's task-card and tool-broker permission gates: `https://openai.github.io/openai-agents-js/guides/guardrails/`
- OpenAI agent eval guidance supports measuring pet-agent behavior through traces, graders, datasets, and eval runs before expanding autonomy: `https://developers.openai.com/api/docs/guides/agent-evals`
- OpenAI model optimization guidance reinforces evals and prompt iteration before fine-tuning, which keeps pet "ML capability" staged and review-driven: `https://developers.openai.com/api/docs/guides/model-optimization`

## Claude Design Prototype Lane

Claude Design is useful for Wevito as a UI/UX prototyping lane, not as a sprite generator or runtime implementation source.

```text
Claude Design
  │
  ├── prototypes
  │     ├── Sprite Workflow V2 no-scroll console
  │     ├── vNext shell/home/action panel cleanup
  │     ├── pet helper task cards
  │     └── Creative Learning Lab dashboard
  │
  ├── design system
  │     ├── colors
  │     ├── typography
  │     ├── cards/buttons/status chips
  │     └── compact desktop layout rules
  │
  └── handoff artifacts
        ├── screenshots
        ├── standalone HTML/PDF/ZIP exports if useful
        └── implementation notes for Codex/code-side
```

Use it before UI-heavy implementation, especially for surfaces that previously became confusing or scroll-heavy. Do not use it to mutate `sprites_runtime`, source boards, game code, or secret-bearing project data.

Dedicated usage plan:

```text
C:\Users\fishe\Documents\projects\wevito\docs\WEVITO_CLAUDE_DESIGN_USAGE_PLAN_2026-05-05.md
```

## Pet Command Bar And Three Helper Agents

The pet-agent helper idea should enter Wevito through a named command bar, not through hidden background automation.

```text
Pet Command Bar
  │
  ├── user text input
  │     ├── "@Milo check the sprite audit"
  │     ├── "Bean, make a QA checklist"
  │     └── "Who should handle this?"
  │
  ├── active helper roster
  │     ├── Pet helper 1: name, mood, role, status
  │     ├── Pet helper 2: name, mood, role, status
  │     └── Pet helper 3: name, mood, role, status
  │
  ├── structured task intent
  │     ├── target pet
  │     ├── task kind
  │     ├── target files/docs/assets
  │     ├── risk level
  │     └── expected output
  │
  └── task card / tool broker
        ├── approval
        ├── execution
        ├── review
        ├── audit log
        └── learning feedback
```

The first implementation should support up to three active helper pets addressed by user-assigned names. The pets can personalize suggestions, explain status, and learn from reviewed outcomes, but they cannot bypass the tool broker, approval gates, or deterministic life-simulation owners.

Dedicated plan:

```text
C:\Users\fishe\Documents\projects\wevito\docs\WEVITO_PET_AGENT_TEXT_BAR_PLAN_2026-05-05.md
```

## Core Design Principles

```text
╔════════════════════════════════════════════════════════════════════════╗
║ Non-Negotiables                                                      ║
╠════════════════════════════════════════════════════════════════════════╣
║ Source art is never silently overwritten.                            ║
║ Candidates are not completed work until reviewed, proven, and applied.║
║ Pet life simulation truth feeds AI; AI does not replace it.           ║
║ Every helper action is a task card with status and audit log.         ║
║ Training happens offline from reviewed examples, not raw live logs.   ║
║ Browser/computer automation is last-resort, sandboxed, and visible.   ║
║ No broad sprite mutation without backup, hashes, proof, and rollback. ║
╚════════════════════════════════════════════════════════════════════════╝
```

## Phase 0: Coordination And Gate Reconciliation

Goal: make sure code-side, visual-side, and source-of-truth state are not lying to each other.

Scope:

- Reconcile the separate code-side green worktree with the main repo's current 456 mixed-canvas runtime rows.
- Protect the accepted `goose / baby / female / blue / hold_ball` endpoint.
- Freeze broad visual mutation until the proof/apply/rollback workflow is explicit.
- Confirm what visual-side is currently cleaning and whether it touches runtime rows, shared props, source boards, or docs only.

Why this phase exists:

- Current docs disagree depending on repo/worktree: code-side worktree previously reported `0` mixed-canvas rows, while the main repo audit found `456`.
- Any future tool that applies or audits sprites needs a truthful baseline.

Deliverables:

- `CODE_SIDE_REPO_RECONCILIATION_YYYY-MM-DD.md`
- Updated runtime canvas report.
- A short "safe to mutate?" status per asset lane.

Validation:

- Full vNext test status is known and current.
- Runtime canvas mismatch report is current.
- Visual-side protected endpoints are listed.

Do not do:

- Do not normalize or rewrite runtime PNGs in this phase unless explicitly approved.
- Do not clean dirty workspace files.

## Phase 1: Canonical Contracts And Ownership Boundaries

Goal: define exactly what owns simulation, visuals, helper tools, learning, and proof.

Ownership map:

```text
╔════════════════════╦══════════════════════════════════════════════════╗
║ Owner              ║ Truth                                           ║
╠════════════════════╬══════════════════════════════════════════════════╣
║ vNext core         ║ durable pet identity, needs, habits, conditions ║
║ Godot runtime      ║ live game presentation and interaction proof    ║
║ Asset manifest     ║ path, hash, geometry, family, frame truth       ║
║ Sprite Workflow V2 ║ candidate/review/apply/rollback state          ║
║ Creative Lab       ║ reviewed examples and preference data           ║
║ Pet Command Bar    ║ user text input and named helper routing        ║
║ Tool Broker        ║ available tools, permissions, logs, approvals   ║
║ AI adapters        ║ model calls, structured outputs, tool proposals ║
╚════════════════════╩══════════════════════════════════════════════════╝
```

Life-sim boundaries:

- Drives/needs own hunger, hydration, energy, cleanliness, health pressure, rest, social, exploration, and play pressure.
- Emotion owns current channels like attachment, frustration, curiosity, rejection, comfort, agitation, exhaustion, and relief.
- Habits/routines own repeated behavior tendencies.
- Memory owns remembered outcomes, places, objects, interactions, care, and danger.
- Personality owns simple animal temperament and long-term bias, not temporary emotion or agent capability.
- AI consumes these fields to suggest task-card behavior; it must not mutate them directly.
- The advanced AI-agent capability belongs to the command/task/tool layers, not the pet's animal personality model.

Deliverables:

- `WEVITO_RUNTIME_CONTRACTS.md`
- `WEVITO_ACTION_ANIMATION_PROP_CONTRACT.md`
- `WEVITO_LIFE_SIM_OWNERSHIP_MAP.md`
- `WEVITO_PET_AGENT_TEXT_BAR_PLAN_2026-05-05.md`
- `WEVITO_PET_AGENT_COMMAND_CONTRACT.md`
- JSON schemas for sprite row, candidate, proof, tool task, and reviewed example.

Research incorporation:

- Use Structured Outputs for repair queue rows, task cards, and reviewed-example records.
- Use Structured Outputs for command-bar parsing into `TaskIntent`.
- Use function calling/tool schemas only when the model is invoking a real Wevito tool.
- Keep tool permissions separate from pet personality.
- Keep pet names and personality in life-sim/profile state; keep execution rights in Tool Broker.

Validation:

- Schema tests.
- No duplicate ownership of the same stat or behavior.
- No AI action can bypass a deterministic owner.

## Phase 1B: Claude Design System And Prototype Lane

Goal: use Claude Design to prototype the most confusing UI/tool surfaces before implementing them.

Scope:

- Create a Wevito design-system seed from focused screenshots, selected docs, and visual direction.
- Prototype Sprite Workflow V2 as a one-screen, no-scroll review/proof/apply console.
- Prototype the pet helper task-card/tool-broker dashboard.
- Prototype the pet command bar near tool sections, including three named helper pet cards.
- Prototype the Creative Learning Lab review dashboard.
- Export only reviewed design artifacts and implementation handoff notes.

Why this phase exists:

- The old sprite app failed partly because important work was hidden below scroll positions and inside cramped panels.
- Claude Design is well-suited to fast layout exploration, design systems, and handoff artifacts.
- Prototyping first reduces the chance that code-side builds another hard-to-watch tool.

Deliverables:

- Claude Design project/export links or local exports.
- A component map for each approved prototype.
- Implementation notes for which Wevito system owns each screen.

Validation:

- Prototype can be understood from one screen without mandatory scrolling.
- Work states are visible: queued, generating, reviewing, applied, awaiting review, failed.
- Pet helper states are visible: available, drafting, waiting for approval, running, reviewing, blocked, done.
- Any prototype that implies file mutation also shows approval, hashes, proof, and rollback.

Do not do:

- Do not upload secrets, API keys, or broad private repo dumps.
- Do not treat generated prototype code as production truth.
- Do not generate or apply sprite PNGs through Claude Design.

## Phase 2: Main Asset Gate Repair

Goal: restore a truthful, test-passing main repo asset baseline.

Scope:

- Fix or reconcile the 456 mixed-canvas base animation rows.
- Preserve visual content; only deterministic transparent padding/canvas normalization should be considered.
- Keep source boards and accepted optional endpoints protected.

Deliverables:

- Updated runtime canvas report with `0` mixed rows.
- Full vNext test pass.
- Before/after contact sheets for touched species/families.

Validation:

- `python .\tools\report_runtime_canvas_mismatches.py`
- `dotnet test .\vnext\Wevito.VNext.sln -c Debug --no-restore`
- Visual spot-check for touched rows.

Risk:

- Medium, because runtime PNGs may be touched.

Approval needed:

- Yes, before mutation.

## Phase 3: Sprite Workflow V2 Read-Only Core

Goal: create a clean replacement for the confusing sprite app, starting read-only.

One-screen UI target:

```text
╔════════════════════════════════════════════════════════════════════╗
║ Sprite Workflow V2                                                ║
╠═══════════════╦════════════════════════════════════════════════════╣
║ Queue         ║ concrete rows: species / age / gender / color ... ║
║ Current Row   ║ source, runtime, candidate, proof, notes          ║
║ Review Strip  ║ source | runtime | candidate | animated preview   ║
║ Evidence      ║ hashes, geometry, validator findings, decision    ║
║ Actions       ║ dry-run apply | apply | rollback | export report  ║
╚═══════════════╩════════════════════════════════════════════════════╝
```

Scope:

- Import/read Wevito's runtime tree, optional families, prop anchors, and source paths.
- Generate contact sheets and GIF previews from existing files only.
- Produce concrete frame-scoped queues from validators and visual notes.
- Show source/runtime/candidate/proof in a compact no-scroll layout.
- Record decisions without mutating sprites.

Inspiration:

- `sprite-animation-workflow` gives the source-of-truth and focused-family vocabulary.
- Hatch Pet gives row manifests, QA contact sheets, preview videos, and packaging discipline.
- Dibu gives raw/cleaned/thumbnail/export lanes and metadata.
- The-Workinator gives task packet/checklist flow.

Deliverables:

- Read-only manifest importer.
- Read-only queue builder.
- Contact-sheet/preview generator.
- Markdown run summary exporter.

Validation:

- Import validates current Wevito tree without file mutation.
- Contact sheets match exact manifest paths.
- No "apply" code path exists yet or it is disabled by default.

## Phase 4: One-Row Apply, Rollback, And Proof

Goal: make runtime sprite mutation safe enough to trust.

Apply pipeline:

```text
candidate reviewed
      ▼
dry-run apply report
      ▼
backup current runtime row + hashes
      ▼
apply one row only
      ▼
run Godot proof
      ▼
human/code review
      ▼
accept endpoint OR rollback exact hashes
```

Scope:

- Add candidate lane support.
- Add backup-before-apply folder.
- Add one-row apply.
- Add exact rollback.
- Add Godot packaged/dev proof runner.
- Keep vNext proof optional until it supports optional animation families and prop overlays.

Validation:

- One-row dry-run reports exact files, hashes, and target paths.
- One-row real apply can be rolled back byte-for-byte.
- Godot proof confirms no double-ball, correct animation name, correct overlay contract.

Research incorporation:

- Use OpenAI Code Interpreter or local Python only for deterministic image analysis/reporting, not generating or fabricating visuals.
- Follow Hatch Pet's principle that deterministic scripts process already-generated visuals; they do not invent pet art.

## Phase 5: Unified Action, Animation, And Prop Contract

Goal: stop splitting visual action truth across Godot, vNext, and asset folders.

Current problem:

- Godot can drive optional families and prop overlays.
- vNext simulation has stronger pet personality but cannot yet request optional families or carried props cleanly.
- `water` maps to `eat` in vNext, while Godot has `drink`.
- `fetch_ball` is not yet a true pickup/hold/carry/drop sequence.

Target shape:

```text
ActionIntent
   ├─ simulation effects
   ├─ animation family request
   ├─ prop overlay request
   ├─ movement target
   └─ completion/fallback policy
```

Deliverables:

- `ActionIntent` / `ActionFamily` model.
- `AnimationFamily` model separate from emotional `PetAnimationState`.
- Prop overlay metadata for ball, bowl, food, medicine, care tools, habitat props.
- Action sequence controller for fetch and visible auto-drink.

Validation:

- Unit tests for action -> animation family -> prop requirements.
- Godot dev scenario for staged fetch.
- Movement facing still follows motion direction.

Research incorporation:

- Use Godot AnimatedSprite2D signals and frame controls for action sequence completion.
- Use CanvasItem/Y-sort/Z-index principles for props, pets, and habitat depth.

## Phase 6: Pet Life Simulation And Personality Parity

Goal: make pets feel alive because their behavior emerges from durable simulation truth, not random animation swaps.

Life-sim state families:

```text
drives
  ├─ food / hydration / rest / safety / cleanliness
  ├─ social / affection / play / exploration
  └─ routine / habitat / resource control

emotion
  ├─ attachment / comfort / curiosity / relief
  ├─ agitation / rejection / frustration / exhaustion
  └─ sickness / vulnerability / fear-like avoidance

memory
  ├─ care outcomes / object outcomes / place memories
  ├─ interaction outcomes / danger / routine memories
  └─ user preference memories
```

Scope:

- Decide whether vNext is source-of-truth simulation and Godot consumes/mirrors, or Godot remains game truth with parity rules.
- Add durable memory and routine data shapes.
- Add visible scenario/debug overlays.
- Add personality effects that alter action scoring, not permission boundaries.

Deliverables:

- Life-sim contract.
- Scenario presets for personality and condition states.
- Save/load persistence rules.
- Debug truth overlay plan.

Validation:

- Deterministic tests for habits, personality drift, condition outcomes, aging, death, and recovery.
- Visual proof scenarios for hungry, thirsty, playful, sick, tired, lonely, stubborn, affectionate, and curious pets.

Research incorporation:

- Follow the life-simulation rule: AI consumes current drives/emotions/memories/routines, but deterministic systems own truth.

## Phase 7: Habitat, Depth, Occlusion, And Grounding

Goal: make environments interactive and visually grounded.

Scope:

- Create habitat object manifest with object type, species compatibility, zone, anchor, depth band, occlusion mode, and contact shadow.
- Replace scattered hardcoded prop positions gradually.
- Add perch/enter/rest/drink/eat/use zones.
- Add simple contact shadows and depth rules.

Deliverables:

- `habitat_objects.json`
- `habitat_interaction_zones.json`
- One species habitat proof.
- Contact shadow policy.

Validation:

- Manifest schema tests.
- Godot proof: pet can enter/perch/use object and layer correctly.
- Screenshots/contact sheets for depth examples.

Research incorporation:

- Use Godot CanvasItem Y-sort/Z-index correctly for overlap.
- Keep prop zones data-driven so visual-side can review and code-side can test.

## Phase 8: Wevito Tool Broker And Task Cards

Goal: let pets help with PC/web/local tasks without unsafe autonomy.

This phase owns the first implementation of the pet command bar: a text input near the tool/task sections where the user can address up to three active helper pets by user-assigned name.

Tool broker shape:

```text
╔════════════════════════════════════════════════════════════╗
║ Tool Broker                                               ║
╠══════════════╦═════════════════════════════════════════════╣
║ Tool schema  ║ name, args, outputs, risk, permissions     ║
║ Command bar  ║ user text -> target pet -> TaskIntent      ║
║ Task card    ║ requested, waiting, running, review, done  ║
║ Guardrails   ║ pre-check, post-check, approval gates      ║
║ Audit log    ║ who/what/when/why/result                   ║
║ Pet surface  ║ visible animation/status/personality       ║
╚══════════════╩═════════════════════════════════════════════╝
```

Safe first tools:

- Save copied link to basket.
- Summarize clipboard into a note.
- Open a local doc.
- Create a QA checklist.
- Capture screenshot/proof image.
- Export a sprite review packet.
- Search local docs.

Deferred/risky tools:

- Browser automation on logged-in sites.
- Sending messages/emails.
- Shell/file writes outside approved project roots.
- Payments, purchases, account changes, credential handling.
- Long-running autonomous loops.

Research incorporation:

- Use function calling/structured tool schemas for deterministic local tools.
- Use Structured Outputs to parse command-bar text into task intents before any tool is considered.
- Use OpenAI Agents SDK-style guardrails/tracing concepts even if implemented locally.
- Use OpenClaw/MCP ideas for typed tools and skills, but treat skills/plugins as trusted code.
- Use Computer Use only for isolated, visible, sandboxed proof scenarios, not authenticated user work.

Deliverables:

- Tool registry schema.
- Pet command bar UI and parser.
- Three-helper active roster contract.
- Permission policy.
- Task card UI.
- Audit log.
- Initial safe tool set.

Validation:

- Unit tests for permissions and state transitions.
- Parser tests for explicit pet names, `@PetName`, unaddressed commands, and blocked commands.
- Manual proof that rejected risky actions do not execute.
- Logs contain enough detail to debug what happened.

## Phase 9: Pet Agent Pilot

Goal: make one named pet help with a small real task while staying obviously under user control, then expand to a maximum of three active helper pets after the one-pet pilot is safe.

Pilot examples:

- "Goose collects this visual QA note and opens the relevant checklist."
- "Crow saves this link into the Wevito basket."
- "Squirrel builds a sprite review packet from selected files."
- "Frog reminds me to review the habitat prop contact sheet later."
- "@Milo summarize this failed sprite audit and create a review task card."

Pet-agent rule:

```text
pet personality changes presentation
pet name routes the request
tool broker controls permission
task card controls state
audit log records action
human approves risky steps
```

Deliverables:

- One pet assigned to one task type.
- Three-helper roster disabled or read-only until the one-pet pilot passes.
- Status animation mapping: waiting, review, failed, waving/jumping success.
- Pet-specific phrasing/personality.
- Hard stop on unapproved write/send/automation actions.

Validation:

- End-to-end safe task proof.
- Proof that addressed commands route to the correct pet profile.
- No hidden file writes.
- No external messages.
- User-visible task card and completion log.

## Phase 10: Creative Learning Lab

Goal: let Wevito learn from art decisions without unstable live self-modification.

Scope:

- Store reviewed examples from sprite QA.
- Store reviewed examples from pet command-bar task outcomes.
- Capture issue labels, decision labels, source/candidate/proof paths, hashes, notes, and reviewer.
- Export/import reviewed example bundles.
- Add dataset versions and release notes.
- Add benchmarks before training.

Data families:

```text
command
  user request -> structured sprite target

pet_command
  named pet + user text -> TaskIntent + route decision

sprite_review
  source/runtime/candidate -> issue labels + decision

edit
  original frame + repair instruction -> accepted frame

preference
  option A / option B -> user choice + reason

pet_feedback
  task result + user feedback -> pet preference/memory update candidate

evaluation
  fixed prompts/cases -> expected pass/fail criteria
```

Research incorporation:

- Dibu's architecture already proves raw/cleaned/thumbnail/export lanes, interaction logs, dataset examples, and contribution bundles.
- OpenAI and Dibu both point to evals before fine-tuning.
- Pet-agent ML begins as reviewed preference memory and eval data; model fine-tuning waits until enough approved examples exist.
- Hugging Face Diffusers/LoRA can be explored after enough reviewed art data exists.

Deliverables:

- Reviewed example schema.
- Creative Learning Lab dashboard.
- Bundle export/import scripts.
- Evaluation benchmark skeleton.

Validation:

- No raw unreviewed logs enter training data.
- Bundle validation passes.
- Benchmark exists before any model/rule promotion.

## Phase 11: Local/Cloud AI Adapter Strategy

Goal: avoid hard-locking Wevito to one AI provider or one automation mode.

Adapter lanes:

```text
LLM adapters
  ├─ OpenAI Responses / Agents-style backend
  ├─ local LLM for command parsing later
  └─ mock/rule-based fallback for tests

Pet agent adapters
  ├─ command parser
  ├─ helper routing
  ├─ summarizer/planner
  └─ reviewed-memory retriever

Image adapters
  ├─ OpenAI image generation/editing candidate lane
  ├─ Gemini/manual-provider candidate lane
  ├─ local Diffusers/LoRA experiment lane
  └─ no-op/mock candidate lane for tests

Evaluator adapters
  ├─ deterministic validators
  ├─ model-assisted review labels
  └─ human review decisions
```

Scope:

- Build stable adapter interfaces first.
- Keep pet-agent helper adapters behind `TaskIntent` and tool-broker schemas.
- Keep generation as candidate-only.
- Keep training offline.
- Prefer smaller, typed outputs over freeform agents.

Research incorporation:

- OpenAI image generation is useful for transparent-background candidate creation and multi-turn editing.
- OpenAI Responses/Agents-style infrastructure is useful for multi-step pet helper work, but Wevito should own state, approvals, and tools.
- Diffusers/LoRA is promising for future local sprite specialization, but only after datasets/evals exist.
- Dibu's adapter pattern should be reused conceptually.

Validation:

- Mock adapter tests.
- Provider output always enters candidate lane.
- No provider can write directly to runtime assets.

## Phase 12: Production Hardening, Packaging, And Release

Goal: make the system maintainable and safe to use day-to-day.

Scope:

- Build/publish scripts.
- Godot proof automation.
- vNext shell packaging.
- Sprite Workflow V2 packaging.
- Tool broker logs and export.
- Settings migration.
- Recovery and rollback docs.

Research incorporation:

- Godot headless/export command-line support should be used for repeatable proof builds.
- Tauri sidecar/capability patterns are relevant if Sprite Workflow V2 or the Creative Lab becomes a desktop app.
- FastAPI background tasks are suitable for light local work, but training/proof jobs should have visible job status like Dibu's `TrainingController`.

Validation:

- Clean build from repo.
- Packaged app launches.
- Proof scenarios run.
- Logs and rollbacks exist.
- User-facing docs explain safe usage.

## Sequencing Recommendation

```text
now
  ▼
Phase 0  reconcile state
  ▼
Phase 1  contracts
  ▼
Phase 1B Claude Design prototypes
  ▼
Phase 2  main asset gate
  ▼
Phase 3  Sprite Workflow V2 read-only
  ▼
Phase 4  one-row apply/rollback/proof
  ▼
Phase 5  action/animation/prop contract
  ▼
Phase 6  pet personality parity
  ▼
Phase 7  habitat grounding
  ▼
Phase 8  tool broker/task cards
  ▼
Phase 9  first pet-agent pilot
  ▼
Phase 10 Creative Learning Lab
  ▼
Phase 11 AI adapter expansion
  ▼
Phase 12 hardening/release
```

Parallel work that can safely continue:

- Visual-side manual sprite/asset cleanup if it avoids broad unproven mutation and protects accepted endpoints.
- No-edit contact sheets and visual QA.
- Docs and schema planning.
- Read-only audits.

Parallel work that should wait:

- New broad image generation.
- Applying many sprite rows.
- Local model fine-tuning.
- Browser/computer automation on real logged-in accounts.
- Pet agents that write/send/execute without explicit user approval.

## Highest-Risk Decisions To Make Deliberately

- Whether vNext or Godot becomes source-of-truth simulation.
- Where Sprite Workflow V2 lives: Wevito `tools/`, standalone app, or Sprite Workflow App replacement.
- Whether Dibu remains separate or becomes a shared creative orchestrator.
- How much AI tool use should be local-only vs API-backed.
- Whether any computer-use/browser automation is worth the risk.
- When visual-side cleanup should pause for code-side canvas reconciliation.

## Recommended Immediate Next Step

Start with Phase 0 and Phase 1 as a planning/inspection pass, add Phase 1B for the first Sprite Workflow V2 prototype if UI/tool work is next, then ask for explicit approval before Phase 2 mutates any runtime PNGs.

The practical next artifact should be:

```text
C:\Users\fishe\Documents\projects\wevito\docs\WEVITO_PHASE0_PHASE1_EXECUTION_PLAN_2026-05-05.md
```

That doc should identify the exact files, tests, reports, schemas, and no-touch asset lanes needed before the first implementation phase begins.

If Claude Design is used first, the practical companion artifact is:

```text
C:\Users\fishe\Documents\projects\wevito\docs\WEVITO_CLAUDE_DESIGN_USAGE_PLAN_2026-05-05.md
```
