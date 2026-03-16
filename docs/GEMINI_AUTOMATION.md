# Gemini Automation

Updated: 2026-03-12

```text
╔══════════════════ Gemini Automation Flow ══════════════════╗
║ handoff folder                                             ║
║   1-base-pose.png                                          ║
║   2-editable-board.png                                     ║
║   3-runtime-reference-blue.png                             ║
║   4-prompt.txt                                             ║
║        │                                                   ║
║        ▼                                                   ║
║ playwright persistent profile                              ║
║   logged into Gemini                                       ║
║   opened to target chat                                    ║
║        │                                                   ║
║        ▼                                                   ║
║ upload files ▶ paste prompt ▶ send ▶ download edited PNG   ║
╚═════════════════════════════════════════════════════════════╝
```

## One-Time Setup

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\setup-gemini-automation.ps1
```

This installs:
- local `playwright` dependency under `tools\gemini_automation`
- bundled Chromium browser

## Recommended Browser

Use the dedicated Gemini automation browser instead of your normal Chrome session:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\launch-gemini-debug-browser.ps1
```

This launches a separate Chrome profile at:
- `.codex-cache\chrome-debug-profile`

with a debug port on:
- `http://127.0.0.1:9333`

Log into Gemini there once and keep using that same browser for sprite authoring automation.

## Prepare Handoff Files

```powershell
python .\tools\prepare_gemini_handoff.py --species rat
```

The main automation wrapper also does this automatically before each run, so you do not need to call it separately unless you want to inspect the files yourself.

## First Run

Use setup mode once so the automation can attach to the dedicated Gemini browser and confirm the target chat is open.

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\run-gemini-dedicated.ps1 -Species rat -Age adult -Gender male -OpenBrowser -SetupOnly -KeepOpen
```

Then:
1. log into Gemini in the opened browser if needed
2. open the exact target chat
3. rerun the automation without `-SetupOnly`

## Automated Run

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\run-gemini-dedicated.ps1 -Species rat -Age adult -Gender male
```

Optional:
- `-ChatUrl "<exact gemini chat url>"`
- `-CdpUrl "http://127.0.0.1:9222"`
- `-SendOnly`
- `-KeepOpen`

## Live Logged-In Chrome Flow

If Gemini is already open in your normal logged-in Chrome session, use the live-session driver instead of the dedicated browser:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\drive-live-gemini.ps1 -Species rat -Age adult -Gender male
```

This path:
- uses the already-open `Google Gemini - Google Chrome` window
- pastes the single consolidated `1-upload-pack.png` directly from the clipboard into the prompt area
- sets the prepared prompt text
- sends the message
- waits for generation
- clicks `Download full size image`
- moves the downloaded PNG into the matching handoff folder

## Batch Roster Run

To prepare all species packs and handoffs:

```powershell
python .\tools\export_all_authoring_packs.py
python .\tools\prepare_all_gemini_handoffs.py
```

To run the full live Gemini batch with per-variant import and recolor:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\batch-drive-live-gemini.ps1 -ImportAfterEach -SkipExisting
```

This writes a resumable log under:
- `vnext\artifacts\gemini-batch`

Coverage can be audited with:

```powershell
python .\tools\report_authored_sprite_coverage.py
```

## Output

If the automated download succeeds, the edited image is saved to:

`incoming_sprites\gemini_handoff\<species>\<age>\<gender>\5-save-edited-board-here`

## Notes

- This uses a dedicated persistent browser profile under:
  - `.codex-cache\chrome-debug-profile`
- That browser/profile is intentionally separate from your normal Chrome profile.
- If you want to automate an already-open Chrome window, Chrome must have been started with a remote debugging port and you must pass `-CdpUrl`.
- A normal existing Chrome window cannot be attached by Playwright after the fact.
- The automation is best-effort because Gemini's UI can change.
- The most stable path is:
  - one persistent dedicated debug browser
  - one stable target chat
  - one variant per run
