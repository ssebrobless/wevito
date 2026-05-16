# C-PHASE 122 - Notification, Focus, Audio & Workspace Discipline

## Goal

Make Wevito calmer around the user's active PC work: notifications defer instead of interrupting, background windows do not steal focus, pet sounds are off by default, cursor reactivity is rate-limited, and workspace preferences have explicit local policy seams.

## Scope

- Added local policy services for notification deferral, focus discipline, audio output, multi-monitor preference, cursor reactivity, and tray-icon animation discipline.
- Added Settings toggles for notification deferral, pet sounds, cursor reactivity, tray icon animation, and a locked-off focus-theft control.
- Routed shell window showing through `FocusDisciplineService`; startup/background windows show without activation, while explicit user-opened tool surfaces can activate.
- Added a small Godot cursor-reactivity seam in `scripts/pet.gd` with a 200px proximity and 10-second rate limit.
- Marked `audioAssist` with `ttsPolicy=banned-in-v1` and `unpromptedAudioDefault=disabled`.
- Added all new C-PHASE 122 packet kinds to `PlainLanguageExplainer.KnownPacketKinds`.

## Safety Boundaries

- No hosted AI calls.
- No network access added.
- No sprite/runtime asset mutation.
- No TTS implementation; the audio policy rejects text-to-speech.
- Emergency notifications can deliver immediately but never request focus.
- First-launch wizard remains the only non-click focus exception.

## Stop-Gate Review

- Background window focus theft: false. Startup windows are shown with `ShowActivated=false`.
- Audio without user-trigger source: false. Sounds remain disabled by default and require user-triggered requests when enabled.
- TTS implemented anywhere: false. Only the ban policy and tests were added.
- Pet leaving preferred monitor on its own: false. Multi-monitor policy resolves preferred monitor or primary fallback without movement side effects.
- Cursor reactivity rate-limit bypass: false. C# policy and Godot seam both use a 10-second cooldown.
- Missing packet kind in explainer: false. All six new packet kinds are mapped.

## Validation

- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "NotificationPolicy|FocusDiscipline|AudioOutputPolicy|MultiMonitor|CursorReactivity|PlainLanguage"` passed: 177/177.
- `dotnet build .\vnext\Wevito.VNext.sln` passed with 0 warnings and 0 errors.
- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build` passed: 893/893.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests` passed.

## Next Phase

C-PHASE 123 can build on this by adding crash/session/power resilience, keeping the same default-deny posture and no-surprise user-interruption rules.
