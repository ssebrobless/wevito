# C-PHASE 77: Activity Dashboard And Live Status UX

## Goal

Make Wevito's current helper activity visible at a glance without opening Settings, while preserving the local-first safety posture.

## Scope

- Added a read-only live status feed over the append-only audit ledger.
- Added plain-language packet labels for known helper packet kinds.
- Added a roam-band status banner that never triggers work and never raises focus.
- Added a home-panel Stop Everything button that only activates the kill switch; re-enable remains Settings-only.
- Reworked the Settings activity panel recent rows to use privacy-safe explanations instead of raw summaries.

## Implemented

- `LiveStatusFeed` builds daily or custom-window snapshots from audit-ledger metadata only.
- `PlainLanguageExplainer` maps known packet kinds and emits `WarnUnknownPacketKind` through shell trace logging for unknown kinds.
- `OverlayStatusBannerView` shows the last packet kind/time and daily preview/approval/mutation counts.
- The banner hides in quiet, pet-only, fullscreen-auto-quiet, and idle states, while Stop Everything remains visible.
- `live_status_poll_seconds` is hydrated with default `10` and clamps to a minimum of `5` seconds.
- A smoke sample was written to `vnext/artifacts/pet-tasks/20260513-activity-live-status-smoke/snapshot-sample.json`.

## Safety Boundaries

- No hosted model calls.
- No network calls.
- No asset or sprite mutation.
- No new execution modes.
- No task message bodies are displayed in live status.
- Stop Everything activation writes an audit row and blocks helper work; deactivation still requires the Settings checkbox.

## Validation

- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "LiveStatus|PlainLanguage|ActivitySummary"`
- `dotnet build .\vnext\Wevito.VNext.sln`
- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build`
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests`

## Notes

- The roam banner is rendered inside the existing click-through/no-activate roam band, so this phase intentionally keeps the banner non-focus-stealing. Settings remains reachable through the home-panel Settings button.
- The Activity panel remains evidence-ledger-based and does not expose private task text.

## Next Phase

C-PHASE 78 can build on this by using the live-status feed as the visible runtime health surface for deeper autonomous activity review.
