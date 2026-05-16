# Wevito Non-Interruption + Pet-Sim Polish Phase Plan: C-PHASE 121 through 124

Date: 2026-05-15
Author: Claude (Opus 4.7), commissioned by `ssebrobles@gmail.com`
Audience: Codex at medium reasoning effort
Companion files:
- `docs/DECISION_LEDGER_2026-05-15.md`
- `docs/CLAUDE_MASTER_PLAN_2026-05-15.md`
- `docs/CLAUDE_REFRAME_PHASES_106_THROUGH_116_PLAN_2026-05-15.md`
- `docs/CLAUDE_GAP_PHASES_97_THROUGH_105_PLAN_2026-05-15.md`

## 0. Purpose

These four phases close two specific concerns raised after the main grilling
session:

1. **Non-interruption beyond Q18/Q19 coverage** (C-PHASE 121-122).
   Q18 isolation + Q19 coexistence cover the big triggers (fullscreen,
   RAM cascade, app list). Phases 121-122 add the finer-grained
   guarantees: process priority classes, disk I/O budgets, audio
   discipline, focus-theft prevention beyond fullscreen, multi-monitor
   behavior, Windows Game Mode awareness, Codex compile throttling, and
   long-running-work scheduling.
2. **Pet sim polish** (C-PHASE 123-124).
   The pet sim is functional today and isolated from AI workload, but
   no phase actively improves pet behaviors, animation smoothness,
   particle effects, mouse reactivity, or pet-AI awareness. Phases
   123-124 land these as low-priority polish that ships *after* the
   sprite rungs (117-120) clean up the canonical art.

These phases extend the master plan (`CLAUDE_MASTER_PLAN_2026-05-15.md`)
and assume the same hard invariants apply.

## 0.1 Hard Invariants (carried forward)

```text
- Pet sim experience (visuals + gameplay) MUST NOT be affected by AI workload.
- Pet game has 1 GB reserved RAM minimum; AI cascade never touches it.
- KillSwitch / audit ledger / evidence packets remain load-bearing.
- Every new packet kind in PlainLanguageExplainer.KnownPacketKinds.
- No hosted-AI calls; LocalOnly runtime mode.
```

---

## C-PHASE 121 — Process Priority & Resource Throttling

**Goal:** Ensure wevito's background work never starves the user's
foreground applications of CPU, RAM, GPU, disk I/O, or network bandwidth.
Set all wevito processes to BelowNormal priority by default with explicit
boosting only on user-facing tasks. Throttle Codex compile/test cycles.
Honor Windows Game Mode.

**Scope:**

- Add `ProcessPriorityManagerService` in `Wevito.VNext.Core`:
  - On wevito startup, sets all wevito-spawned processes (shell, broker,
    Godot, Python sidecar, Ollama child, Codex CLI invocations) to
    `ProcessPriorityClass.BelowNormal` via `Process.PriorityClass = ...`.
  - Exposes `void BoostForForeground(string processName, TimeSpan duration)`:
    temporarily raises priority to `Normal` for 5s after a foreground-chat
    user turn. Auto-decays back to BelowNormal.
  - Refuses to set `AboveNormal` / `High` / `RealTime` — those classes are
    reserved for the user's applications.
- Add `DiskIoBudgetService`:
  - Polls disk write rate every 5s via WMI counters
    (`Win32_PerfFormattedData_PerfDisk_LogicalDisk`).
  - When user input detected in last 30s (via `GetLastInputInfo`), caps
    wevito's combined write rate at 20 MB/s by signaling cooperative
    yields to writers (audit ledger flush, soak driver heartbeat,
    artifact writes).
  - Long-running writes (LoRA training corpus emit, image-gen batch
    output) consult this service and pause writes for 1s when budget
    exceeded.
- Add `CodexCompileThrottleService`:
  - Detects when Codex CLI is mid-phase via the phase-status file.
  - When phase is in `running` state AND user is active (`GetLastInputInfo`
    < 30s), sets `MSBUILDPROCESSORCOUNT=2` env var before launching
    `dotnet build` / `dotnet test`. When user idle > 5 min, sets
    `MSBUILDPROCESSORCOUNT=6` (leaving 2 cores headroom).
  - Emits `codex_compile_throttled` packet on each launch with the
    chosen processor count.
- Add Windows Game Mode awareness:
  - Polls `HKEY_CURRENT_USER\Software\Microsoft\GameBar\AllowAutoGameMode`
    + foreground app's Game Mode status via the GameInput / GameBar API.
  - When Game Mode active (anywhere on system), `CoexistenceTriggerService`
    (C-PHASE 111) receives an additional trigger; wevito enters Quiet.
  - Emits `game_mode_detected` / `game_mode_cleared` packets.
- Add CPU/GPU/RAM reserve enforcement:
  - Extends `RuntimeBudgetMeter.TryReserve` to verify:
    - At least 50% of total CPU is "user-available" (i.e., not occupied
      by wevito processes) before granting any non-foreground reservation.
    - At least 50% of total GPU utilization is below 80% (free for user)
      before image-gen reservation.
    - At least 4 GB of total RAM is free before background reservation.
  - These are *user-protection floors*; if the user's apps are pushing
    the system, wevito refuses to start new work.
- Settings UI in `ToolPopupWindow`:
  - "Settings" tab grows a "Resource Throttling" section.
  - Toggles: Enable BelowNormal process priority (default on), Enable disk
    I/O budget (default on), Enable Game Mode honor (default on), Codex
    parallel-build cap when user active (slider 1-4, default 2).
- New packet kinds: `process_priority_set`, `process_priority_boosted`,
  `disk_io_budget_throttled`, `codex_compile_throttled`,
  `game_mode_detected`, `game_mode_cleared`,
  `user_protection_floor_blocked_reservation`.

**Pattern:** Follow `WindowsPowerHandler.cs` for the
Windows-API-poll + packet-emit pattern. Follow `RuntimeBudgetMeter.cs`
for the reservation-with-floor pattern.

**Files likely touched:**

```text
vnext/src/Wevito.VNext.Core/RuntimeBudgetMeter.cs                    (floor enforcement)
vnext/src/Wevito.VNext.Core/CoexistenceTriggerService.cs              (Game Mode trigger)
vnext/src/Wevito.VNext.Core/PlainLanguageExplainer.cs
vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml
vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml.cs
vnext/src/Wevito.VNext.Broker/Program.cs                              (apply priority at spawn)
tools/run-codex-loop.ps1                                              (invoke throttle service)
vnext/content/tool_definitions.json
```

**New files:**

```text
vnext/src/Wevito.VNext.Core/ProcessPriorityManagerService.cs
vnext/src/Wevito.VNext.Core/DiskIoBudgetService.cs
vnext/src/Wevito.VNext.Core/CodexCompileThrottleService.cs
vnext/src/Wevito.VNext.Core/GameModeDetectorService.cs
vnext/tests/Wevito.VNext.Tests/ProcessPriorityManagerServiceTests.cs
vnext/tests/Wevito.VNext.Tests/DiskIoBudgetServiceTests.cs
vnext/tests/Wevito.VNext.Tests/CodexCompileThrottleServiceTests.cs
vnext/tests/Wevito.VNext.Tests/GameModeDetectorServiceTests.cs
vnext/tests/Wevito.VNext.Tests/RuntimeBudgetMeterUserProtectionFloorTests.cs
docs/C_PHASE121_PROCESS_PRIORITY_RESOURCE_THROTTLING_2026-05-15.md
```

**Tests:**

- `ProcessPriorityManagerServiceTests.AllWevitoProcessesSetToBelowNormalOnStart`
- `ProcessPriorityManagerServiceTests.RefusesAboveNormalAndHigher`
- `ProcessPriorityManagerServiceTests.BoostDecaysAfter5Seconds`
- `ProcessPriorityManagerServiceTests.RespectsKillSwitch`
- `DiskIoBudgetServiceTests.CapsAt20MbpsWhenUserActive`
- `DiskIoBudgetServiceTests.NoLimitWhenUserIdle5min`
- `DiskIoBudgetServiceTests.EmitsThrottlePacketOnLimitHit`
- `CodexCompileThrottleServiceTests.Sets2CoresWhenUserActive`
- `CodexCompileThrottleServiceTests.Sets6CoresWhenUserIdle`
- `CodexCompileThrottleServiceTests.EmitsThrottlePacketOnLaunch`
- `GameModeDetectorServiceTests.DetectsGameModeFromRegistry`
- `GameModeDetectorServiceTests.SignalsCoexistenceTriggerOnDetect`
- `RuntimeBudgetMeterUserProtectionFloorTests.RefusesReservationBelowFloor`
- `RuntimeBudgetMeterUserProtectionFloorTests.AllowsForegroundEvenBelowFloor` — foreground bypasses floor (chat reply for user must always work).
- `PlainLanguageExplainerTests.CoversAllResourceThrottlingKinds`

**Validation commands:**

```powershell
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "ProcessPriority|DiskIoBudget|CodexCompileThrottle|GameModeDetector|UserProtectionFloor|PlainLanguage"
dotnet build .\vnext\Wevito.VNext.sln
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
```

**Artifacts:**

```text
%LOCALAPPDATA%/Wevito/audit/process-priority-events.jsonl
%LOCALAPPDATA%/Wevito/audit/disk-io-throttle-events.jsonl
%LOCALAPPDATA%/Wevito/audit/game-mode-events.jsonl
docs/C_PHASE121_PROCESS_PRIORITY_RESOURCE_THROTTLING_2026-05-15.md
```

**Stop gates:**

- Stop if any wevito process is set to `AboveNormal` or higher.
- Stop if disk I/O budget can be exceeded by > 5 MB/s for > 30s sustained.
- Stop if Codex compile uses all 8 cores while user is active.
- Stop if Game Mode detection misses a known-Game-Mode process (smoke
  test against known game executable).
- Stop if user-protection floor blocks a foreground task (foreground
  must always be allowed).
- Stop if any new packet kind is missing from `PlainLanguageExplainer`.

**Rollback:** revert PR; processes use OS default priority; Codex compile
uses all cores; Game Mode is ignored.

**Commit/PR:**

- Branch: `claude-implementation/c-phase-121-process-priority-resource-throttling`
- PR title: `C-PHASE 121: Process priority + resource throttling (BelowNormal default, disk I/O budget, Codex compile throttling, Game Mode honor)`.
- Phase report: `docs/C_PHASE121_PROCESS_PRIORITY_RESOURCE_THROTTLING_2026-05-15.md`.

**Auto-continue?** **No.** Touches OS-level priorities; user should review.

---

## C-PHASE 122 — Notification, Focus, Audio & Workspace Discipline

**Goal:** Wevito never steals user focus, never plays sound unprompted,
never spams notifications, and respects multi-monitor / workspace
boundaries. Notifications defer to "next idle moment" when user is busy.

**Scope:**

- Add `NotificationPolicyService` in `Wevito.VNext.Core`:
  - Intercepts every notification request (Stop cards, phase-complete,
    benchmark-regression, audit-ledger-warning, etc.).
  - Defers notification if any of these are true:
    - User typing in any application (keystroke in last 5s)
    - Foreground app is fullscreen
    - Coexistence trigger is active
    - DnD is active
  - Queued notifications fire on next "idle moment" (no input for 10s
    AND no triggers active).
  - Emergency notifications (kill switch, crash) bypass the queue but
    still don't steal focus.
  - Emits `notification_deferred` / `notification_delivered` packets.
- Add `FocusDisciplineService`:
  - Wraps every `Window.Show()` / `Show()` call in shell.
  - Default: `ShowActivated=false` so windows don't steal focus.
  - `ToolPopupWindow` only takes focus if user clicked the overlay icon.
  - `FirstLaunchWizardWindow` (C-PHASE 115) is the one exception — takes
    focus on first launch.
  - Stop cards open in background; ambient badge on `HomePanelWindow`
    indicates "X stopped phases need review."
  - Emits `focus_protected` packet on each window-show where focus theft
    was prevented.
- Add `AudioOutputPolicyService`:
  - Default: wevito produces no audio. Even pet sound effects are off
    until user explicitly enables in Settings.
  - `pet_sound_effects_enabled` setting (default false).
  - If enabled, pet sounds (feed/pet/groom/bathe) play only on user-
    triggered events (feed click, etc.), never on autonomous behavior.
  - Voice output (TTS) is *banned* in v1 — covered explicitly by a hard
    invariant in `tool_definitions.json`.
  - Emits `audio_played` packet on every sound (so user can audit).
- Add `MultiMonitorService`:
  - Tracks which monitor the pet is on; persists to setting
    `pet_preferred_monitor`.
  - Pet stays on its monitor across restarts.
  - When user moves windows between monitors, pet doesn't follow unless
    user explicitly drags pet.
  - When pet's preferred monitor is disconnected, pet falls back to
    primary monitor.
- Add `CursorReactivityService` (subtle, opt-in):
  - Pet can look toward cursor when cursor approaches within N pixels
    (default 200).
  - Setting `pet_cursor_reactivity_enabled` (default true).
  - Reactivity is *visual only* — pet doesn't follow cursor, just turns
    head toward it occasionally.
  - Emits `pet_cursor_reactivity_fired` packet (rate-limited to once per
    10s to avoid log spam).
- Add `TrayIconDisciplineService`:
  - Wevito's system tray icon doesn't flash, doesn't badge, doesn't
    animate unless user-explicit-enable.
  - Right-click menu: Quiet, DnD, Show wevito, Exit.
- Settings UI in `ToolPopupWindow`:
  - "Settings" tab grows "Notifications & Discipline" section.
  - Toggles: Defer notifications during user activity (on), Allow focus
    theft (off — read-only), Pet sounds (off), Pet cursor reactivity (on),
    Tray icon animation (off).
- New packet kinds: `notification_deferred`, `notification_delivered`,
  `focus_protected`, `audio_played`, `pet_cursor_reactivity_fired`,
  `multi_monitor_preference_set`.

**Pattern:** Follow `FocusStealCounter.cs` for the focus-event tracking
pattern. Follow `LiveStatusFeed.cs` for the notification-queue pattern.

**Files likely touched:**

```text
vnext/src/Wevito.VNext.Shell/ShellCoordinator.cs                       (intercept window shows)
vnext/src/Wevito.VNext.Shell/HomePanelWindow.xaml.cs                   (badge for deferred Stop cards)
vnext/src/Wevito.VNext.Core/PlainLanguageExplainer.cs
vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml
vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml.cs
vnext/content/tool_definitions.json
scripts/pet.gd                                                          (Godot: cursor reactivity)
```

**New files:**

```text
vnext/src/Wevito.VNext.Core/NotificationPolicyService.cs
vnext/src/Wevito.VNext.Core/FocusDisciplineService.cs
vnext/src/Wevito.VNext.Core/AudioOutputPolicyService.cs
vnext/src/Wevito.VNext.Core/MultiMonitorService.cs
vnext/src/Wevito.VNext.Core/CursorReactivityService.cs
vnext/src/Wevito.VNext.Core/TrayIconDisciplineService.cs
vnext/tests/Wevito.VNext.Tests/NotificationPolicyServiceTests.cs
vnext/tests/Wevito.VNext.Tests/FocusDisciplineServiceTests.cs
vnext/tests/Wevito.VNext.Tests/AudioOutputPolicyServiceTests.cs
vnext/tests/Wevito.VNext.Tests/MultiMonitorServiceTests.cs
vnext/tests/Wevito.VNext.Tests/CursorReactivityServiceTests.cs
docs/C_PHASE122_NOTIFICATION_FOCUS_AUDIO_WORKSPACE_DISCIPLINE_2026-05-15.md
```

**Tests:**

- `NotificationPolicyServiceTests.DefersWhenUserTyping`
- `NotificationPolicyServiceTests.DefersDuringFullscreen`
- `NotificationPolicyServiceTests.DeliversOnNextIdleMoment`
- `NotificationPolicyServiceTests.EmergencyBypassesQueue`
- `NotificationPolicyServiceTests.EmergencyDoesNotStealFocus`
- `FocusDisciplineServiceTests.WindowShowsWithoutFocus`
- `FocusDisciplineServiceTests.UserClickOpensWithFocus`
- `FocusDisciplineServiceTests.FirstLaunchWizardTakesFocus`
- `AudioOutputPolicyServiceTests.NoSoundByDefault`
- `AudioOutputPolicyServiceTests.UserTriggeredSoundsPlayWhenEnabled`
- `AudioOutputPolicyServiceTests.TtsIsBanned`
- `MultiMonitorServiceTests.PetStaysOnPreferredMonitor`
- `MultiMonitorServiceTests.FallsBackToPrimaryWhenMonitorDisconnected`
- `CursorReactivityServiceTests.RateLimitedToOncePer10Seconds`
- `CursorReactivityServiceTests.OnlyTriggersWithinProximity`
- `PlainLanguageExplainerTests.CoversAllDisciplineKinds`

**Validation commands:**

```powershell
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "NotificationPolicy|FocusDiscipline|AudioOutputPolicy|MultiMonitor|CursorReactivity|PlainLanguage"
dotnet build .\vnext\Wevito.VNext.sln
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
```

**Artifacts:**

```text
%LOCALAPPDATA%/Wevito/audit/notification-events.jsonl
%LOCALAPPDATA%/Wevito/audit/focus-protected-events.jsonl
%LOCALAPPDATA%/Wevito/audit/audio-played-events.jsonl
docs/C_PHASE122_NOTIFICATION_FOCUS_AUDIO_WORKSPACE_DISCIPLINE_2026-05-15.md
```

**Stop gates:**

- Stop if any window can steal focus without explicit user click
  (FirstLaunchWizard excepted).
- Stop if any audio plays without user-trigger source.
- Stop if TTS is implemented anywhere (banned in v1).
- Stop if pet leaves preferred monitor on its own.
- Stop if cursor reactivity rate-limit can be bypassed.
- Stop if any new packet kind is missing from `PlainLanguageExplainer`.

**Rollback:** revert PR; windows show with default focus behavior;
notifications fire immediately; pet may drift between monitors.

**Commit/PR:**

- Branch: `claude-implementation/c-phase-122-notification-focus-audio-workspace-discipline`
- PR title: `C-PHASE 122: Notification, focus, audio & workspace discipline`.
- Phase report: `docs/C_PHASE122_NOTIFICATION_FOCUS_AUDIO_WORKSPACE_DISCIPLINE_2026-05-15.md`.

**Auto-continue?** **Yes.** Defensive defaults; no new mutation surface;
no new model surface; UI-only changes.

---

## C-PHASE 123 — Pet Visual Polish (Post-Sprite-Cleanup)

**Goal:** Once sprite rungs 117-120 land clean canonical art, polish the
pet's visual presentation: smooth animation transitions, position
interpolation, idle micro-behaviors, particle effects, sub-pixel scaling
quality. Pet looks alive without bumping AI workload.

**Depends on:** Sprite Rung 4 (C-PHASE 120) having shipped clean sprites
for all animations. This phase is meaningless against the current
Gemini-imperfect sprites.

**Scope:**

- Add to `scripts/pet.gd` (Godot):
  - **Animation transition blending.** Currently `idle` → `walk` swaps
    instantly. Add a 3-frame cross-fade using `Tween` on `modulate.a`
    between layered AnimatedSprite2D nodes.
  - **Position interpolation.** Currently pet movement is per-tick teleport
    on grid. Add `Tween` interpolation between positions over the tick
    duration. Pet moves smoothly between grid cells.
  - **Idle micro-behaviors.** Every 8-12 seconds of `idle`, randomly
    trigger one of: blink (close eyes 1 frame, re-open), look-around
    (head turn left or right 4 frames), ear-twitch (top sprite layer
    shifts 1 pixel for 2 frames), tail-wag (bottom sprite layer pulses
    1 pixel for 4 frames). Each triggers via state machine; no
    side-effects on stats.
  - **Particle effects.** Add a `ParticleEffectsLayer` Node2D as
    sibling to the pet sprite:
    - `sleep` animation: 3 "z" particles fade in/out above pet's head.
    - `happy` animation: 4 small heart particles bubble up from pet.
    - `eat` animation: 2 small crumb particles fall from pet's mouth.
    - `bathe` animation: 6 small bubble particles drift up around pet.
    - `sad` animation: 2 small teardrop particles fall from pet's eyes.
    - `walk` animation: 1 dust particle puff behind pet every 3rd frame.
  - **Sub-pixel scaling.** Switch the pet's `texture_filter` from
    `Linear` to `Nearest` (already set per `project.godot` config) but
    add a `Camera2D` with `position_smoothing` enabled so the world
    feels less jittery when the user moves the window.
  - **Window shake reaction.** When user grabs and shakes the window
    (detected via rapid window-position deltas), pet plays `sad` for
    2s with extra teardrop particles. Whimsical bug; not a stat hit.
- Add to `vnext/src/Wevito.VNext.Contracts/PetAgentContracts.cs`:
  - `PetMicroBehavior` enum (`Blink`, `LookAround`, `EarTwitch`, `TailWag`).
  - `PetMicroBehaviorTriggered` IPC contract for audit purposes (low
    rate, ~once per 10s).
- Add to `Wevito.VNext.Core`:
  - `PetVisualPolishLogger` — receives micro-behavior IPC events and
    writes `pet_micro_behavior` packets (rate-limited to one per minute
    aggregate, not one per event, to avoid ledger pollution).
- Settings UI in `ToolPopupWindow`:
  - "Settings" tab grows "Pet Visual Polish" section.
  - Toggles: Animation blending (on), Position interpolation (on),
    Idle micro-behaviors (on), Particle effects (on), Window shake
    reaction (on).
- New packet kinds: `pet_micro_behavior`, `pet_particle_effect_played`
  (aggregated hourly, not per-particle).

**Pattern:** Follow `scripts/pet.gd`'s existing animation state machine
for the transition + micro-behavior additions. Follow standard Godot
particle patterns for the effects layer.

**Files likely touched:**

```text
scripts/pet.gd                                                           (transition blend, particles, micro-behaviors, shake reaction)
scripts/main_scene.gd                                                    (Camera2D smoothing)
scenes/main.tscn                                                         (add ParticleEffectsLayer)
vnext/src/Wevito.VNext.Contracts/PetAgentContracts.cs                    (new enum + IPC contract)
vnext/src/Wevito.VNext.Core/PlainLanguageExplainer.cs
vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml
vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml.cs
vnext/content/tool_definitions.json
```

**New files:**

```text
scripts/particle_effects_layer.gd
vnext/src/Wevito.VNext.Core/PetVisualPolishLogger.cs
vnext/tests/Wevito.VNext.Tests/PetVisualPolishLoggerTests.cs
docs/C_PHASE123_PET_VISUAL_POLISH_2026-05-15.md
```

**Tests:**

- `PetVisualPolishLoggerTests.RateLimitsToOncePerMinute`
- `PetVisualPolishLoggerTests.AggregatesEventCountsBeforeFlush`
- `PetVisualPolishLoggerTests.RespectsKillSwitch`
- `PlainLanguageExplainerTests.CoversPetMicroBehaviorKind`
- `PlainLanguageExplainerTests.CoversPetParticleEffectKind`
- Godot-side tests (`scripts/tests/pet_test.gd` if test framework exists):
  - `pet_blink_triggers_in_idle_state`
  - `pet_walk_dust_particle_appears`
  - `pet_happy_hearts_appear`
  - `pet_window_shake_triggers_sad_animation`
  - `position_interpolation_smooth_between_ticks`

**Validation commands:**

```powershell
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "PetVisualPolishLogger|PlainLanguage"
dotnet build .\vnext\Wevito.VNext.sln
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
```

If Godot test framework is available:

```powershell
godot --headless --path . --script scripts/tests/pet_test.gd
```

**Artifacts:**

```text
%LOCALAPPDATA%/Wevito/audit/pet-visual-polish.jsonl
docs/C_PHASE123_PET_VISUAL_POLISH_2026-05-15.md
```

**Stop gates:**

- Stop if pet animation FPS drops below 60 with all effects enabled
  (must verify via `PetFpsMonitorService` soak test from C-PHASE 110).
- Stop if particle effects cause GPU usage > 5% on idle pet.
- Stop if micro-behaviors log more than 1 packet per minute aggregate.
- Stop if position interpolation breaks pet's collision/boundary logic.
- Stop if window shake reaction can be triggered by AI work (must
  require real user input).
- Stop if any new packet kind is missing from `PlainLanguageExplainer`.

**Rollback:** revert PR; pet animations return to instant transitions;
no particles, no micro-behaviors.

**Commit/PR:**

- Branch: `claude-implementation/c-phase-123-pet-visual-polish`
- PR title: `C-PHASE 123: Pet visual polish (transitions, interpolation, micro-behaviors, particles)`.
- Phase report: `docs/C_PHASE123_PET_VISUAL_POLISH_2026-05-15.md`.

**Auto-continue?** **Yes.** Pure cosmetic enhancement; AI side unaffected;
no new mutation pathway.

---

## C-PHASE 124 — Pet Behavior + Pet ↔ AI Bidirectional Awareness

**Goal:** Smarter pet wandering/idle AI, cursor curiosity, and a clean
two-way data channel between the pet sim and the wevito AI so the AI
can reference pet state in chat ("Speedy looks hungry") and the user's
pet interactions are surfaced to the AI's context.

**Depends on:** C-PHASE 109 (tool registry) and C-PHASE 107 (chat UI) so
AI has somewhere to surface pet state to user.

**Scope:**

- Improve pet wandering AI in `scripts/pet.gd`:
  - Currently: random walk within bounds at fixed cadence.
  - New: stateful goal-based wandering. Pet has internal state for
    `current_goal` (one of `wander`, `seek_food_zone`, `seek_water_zone`,
    `rest_zone`, `follow_cursor`).
  - Goal selection based on dominant stat (hunger > 70 → seek_food_zone, etc.).
  - Pet visits "zones" (currently invisible map points; can be visible
    later) corresponding to interactions.
  - Pet returns to neutral wandering when goal satisfied.
- Add pet ↔ cursor curiosity:
  - When cursor enters pet's "personal space" (radius 80px), pet pauses
    wandering for 1-3 seconds, looks at cursor.
  - On further proximity (radius 40px), pet plays `happy` for 1s.
  - On cursor click on pet, fires existing pet interaction; no change.
- Add `PetStateTool` to the tool registry (per C-PHASE 109):
  - Tool name: `get_pet_state`
  - Args: `pet_slot` (0, 1, or 2 — defaults to 0)
  - Returns: JSON with pet's species, age, gender, color, current stats
    (hunger/happiness/energy/etc.), current animation, current goal,
    active conditions, recent user interactions in last 1 hour.
  - When AI is uncertain about pet state, calls this tool mid-chat.
- Add `PetInteractionLogger`:
  - Logs every user interaction with the pet (feed click, pet click,
    groom click, bathe click).
  - Writes `pet_interaction_logged` packets.
  - Logger is consulted by `get_pet_state` tool for the "recent
    interactions" field.
- Add AI proactivity prompts (carefully bounded):
  - When pet stat crosses critical threshold (hunger > 90, energy < 10,
    health < 30), AI receives a system-level "fyi" prompt in its next
    chat context: "Speedy's hunger is at 92% — you can mention this
    to the user."
  - AI doesn't auto-message the user; it can only mention pet state in
    response to user chat turns (no unprompted notifications).
  - Setting `ai_mentions_pet_state` (default true).
- Multi-pet interactions (if user has 2+ pets):
  - Pets can interact with each other when in proximity: play_together,
    groom_each_other, share_food_zone. Lightweight 2-3 second
    interactions. No stat side-effects for now.
- New packet kinds: `pet_goal_changed`, `pet_interaction_logged`,
  `pet_cursor_curiosity_fired`, `pet_state_tool_invoked`.

**Pattern:** Pet wandering: follow existing `pet.gd` state machine pattern.
Tool registration: follow C-PHASE 109's `ToolRegistry` pattern. AI
proactivity: follow Q22's `RetrievalAutomaticInjector` for the per-turn
context injection pattern.

**Files likely touched:**

```text
scripts/pet.gd                                                           (goal-based wandering, cursor curiosity)
scripts/main_scene.gd                                                    (zone definitions)
scripts/game_manager.gd                                                  (state-threshold notifications)
vnext/src/Wevito.VNext.Core/ToolRegistry.cs                              (register PetStateTool)
vnext/src/Wevito.VNext.Core/PetTaskAdapterPreviewDispatcher.cs           (consume pet state)
vnext/src/Wevito.VNext.Core/RetrievalAutomaticInjector.cs                 (inject pet state alerts)
vnext/src/Wevito.VNext.Core/PlainLanguageExplainer.cs
vnext/src/Wevito.VNext.Contracts/PetAgentContracts.cs                    (new state fields)
vnext/content/tool_definitions.json
```

**New files:**

```text
vnext/src/Wevito.VNext.Core/Tools/PetStateTool.cs
vnext/src/Wevito.VNext.Core/PetInteractionLogger.cs
vnext/src/Wevito.VNext.Core/PetStateContextInjector.cs
vnext/tests/Wevito.VNext.Tests/PetStateToolTests.cs
vnext/tests/Wevito.VNext.Tests/PetInteractionLoggerTests.cs
vnext/tests/Wevito.VNext.Tests/PetStateContextInjectorTests.cs
docs/C_PHASE124_PET_BEHAVIOR_PET_AI_AWARENESS_2026-05-15.md
```

**Tests:**

- `PetStateToolTests.ReturnsAccuratePetStateForActiveSlot`
- `PetStateToolTests.FallsBackToSlot0WhenNoArgs`
- `PetStateToolTests.RespectsUnifiedPolicyService`
- `PetInteractionLoggerTests.AppendIsAppendOnly`
- `PetInteractionLoggerTests.RecentInteractionsRespectsTimeWindow`
- `PetStateContextInjectorTests.InjectsAlertForCriticalThreshold`
- `PetStateContextInjectorTests.DoesNotInjectAboveThreshold`
- `PetStateContextInjectorTests.RespectsUserDisabledSetting`
- Godot-side tests (if available):
  - `pet_seeks_food_zone_when_hungry`
  - `pet_returns_to_wander_when_goal_satisfied`
  - `pet_cursor_curiosity_radius_check`
- `PlainLanguageExplainerTests.CoversAllPetBehaviorKinds`

**Validation commands:**

```powershell
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "PetStateTool|PetInteractionLogger|PetStateContextInjector|PlainLanguage"
dotnet build .\vnext\Wevito.VNext.sln
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
```

**Artifacts:**

```text
%LOCALAPPDATA%/Wevito/audit/pet-interactions.jsonl
%LOCALAPPDATA%/Wevito/audit/pet-goals.jsonl
docs/C_PHASE124_PET_BEHAVIOR_PET_AI_AWARENESS_2026-05-15.md
```

**Stop gates:**

- Stop if `PetStateTool` returns data without going through `UnifiedPolicyService`.
- Stop if AI can auto-message user about pet state (must be reactive only).
- Stop if context injector pollutes context with stale state (must use
  freshness window).
- Stop if pet wandering breaks the existing in-bounds collision logic.
- Stop if cursor curiosity tracking exceeds 1 packet per 10s rate limit.
- Stop if any new packet kind is missing from `PlainLanguageExplainer`.

**Rollback:** revert PR; pet returns to random-walk wandering; AI loses
pet-state awareness; AI no longer mentions pet status.

**Commit/PR:**

- Branch: `claude-implementation/c-phase-124-pet-behavior-pet-ai-awareness`
- PR title: `C-PHASE 124: Pet behavior + pet ↔ AI bidirectional awareness`.
- Phase report: `docs/C_PHASE124_PET_BEHAVIOR_PET_AI_AWARENESS_2026-05-15.md`.

**Auto-continue?** **Yes.** Pet behavior enhancements + read-only tool;
no autonomy expansion; no mutation pathway.

---

## Sequencing Recommendation

Insert into the master plan execution sequence as follows:

```text
Pre-existing master plan order...
6.   C-PHASE 111                      User-PC coexistence policy
6.5. C-PHASE 121                      Process priority + resource throttling     <-- INSERTED
6.6. C-PHASE 122                      Notification, focus, audio & workspace      <-- INSERTED
7.   C-PHASE 101                      Household maintenance
... continues with existing order
17.  C-PHASE 118                      Sprite Rung 2: Image embedding scoring
18.  C-PHASE 119                      Sprite Rung 3: Palette conformer + propagation
... existing autonomy phases ...
36.  C-PHASE 120                      Sprite Rung 4: Remaining animations
37.  C-PHASE 123                      Pet visual polish                           <-- INSERTED (after sprites clean)
38.  C-PHASE 124                      Pet behavior + pet ↔ AI awareness          <-- INSERTED
```

The non-interruption phases (121-122) land *early* because they protect
the user during every subsequent phase. The pet-sim polish phases (123-124)
land *late* because they depend on clean sprites + chat UI + tool registry.

## Closing Notes

These four phases complete the non-interruption story (121-122) and
formally schedule the pet sim improvements (123-124) so they're not
forgotten in the AI-heavy buildout. Total phase count after these:
**37 new phases** (4 sprite rungs + 11 H4+ foundation + 9 gap + 11 reframe
+ 2 non-interruption + 2 pet-sim polish).

The hard invariants stand: pet sim never affected by AI workload; user
PC always usable; League and other gaming runs unaffected.
