# Screen Capture Tooling Research Plan - 2026-05-05

## Goal

Add screenshot and screen-capture capabilities to Wevito in a way that supports:

- visible proof of AI/pet-agent work,
- sprite/animation QA,
- tool-session evidence,
- user-shareable reports,
- future pet-agent task assignments such as "capture the issue and summarize it."

## Research Summary

### ShareX

Source: https://getsharex.com/

Useful patterns:

- Many capture modes: fullscreen, active window, monitor, region, last region, scrolling capture, screen recording, GIF recording.
- Strong post-capture workflow model.
- Custom workflow system.
- Hotkeys.
- Destination/export pipeline.

Wevito takeaway:

- Use a capture pipeline, not a one-off screenshot button.
- Every capture should create a timestamped artifact with metadata.
- Let PET TASKS eventually request capture presets like `capture current app`, `capture sprite proof`, or `capture last region`.

### OBS Studio

Sources:

- https://obsproject.com/
- https://obsproject.com/kb/obs-studio-overview
- https://obsproject.com/kb/sources-guide

Useful patterns:

- Scene/source model.
- Display capture, window capture, webcam/video sources, browser sources, image/text overlays.
- Hotkeys for start/stop recording and scene switching.
- Audio mixer and filters.
- Studio mode separates editing from live output.

Wevito takeaway:

- For Wevito, use simplified `CaptureScene` presets rather than a full OBS clone.
- Start with fixed scenes:
  - `wevito_window`
  - `current_foreground_window`
  - `sprite_review_region`
  - `proof_with_overlay_metadata`
- Treat overlays as sources: timestamp, pet name, task card id, report path, capture mode.

### Greenshot

Source: https://greenshot.org/

Useful patterns:

- Simple keyboard-driven capture.
- Full screen, window, fixed area, last area.
- Lightweight editor and annotation.
- Clipboard/file/export choices.
- Designed for low-friction daily use.

Wevito takeaway:

- We need a very simple first UX: "Capture", "Capture Region", "Capture Last Area".
- Add annotation later; do not block first implementation on a full editor.

### ScreenToGif

Source: https://www.screentogif.com/features

Useful patterns:

- Short region recordings.
- GIF/WebM-style proof loops.
- Integrated editor for trimming frames.
- Excellent fit for animation QA.

Wevito takeaway:

- This is the best model for sprite animation proof capture.
- First recording target should be short, bounded clips of the Wevito app/proof surface, not long desktop recordings.
- Save as MP4 first; GIF export can come after proofing works.

### Snagit

Source: https://www.techsmith.com/snagit/features/

Useful patterns:

- Capture presets.
- Scrolling capture.
- Step capture.
- Cursor editing.
- OCR/text recognition.
- Blur/redaction.
- Searchable library with tags and metadata.
- Share/export workflows.

Wevito takeaway:

- Build a capture library from the beginning.
- Every capture should know:
  - what window/region was captured,
  - which pet/task requested it,
  - which report/proof it belongs to,
  - whether it contains private screen content.
- Redaction/privacy needs to be a first-class design concern before broad desktop capture.

### Windows Snipping Tool And PowerToys

Sources:

- https://support.microsoft.com/windows/open-snipping-tool-and-take-a-screenshot-a35ac9ff-4a58-24c9-3253-f12bac9f9d44
- https://learn.microsoft.com/windows/powertoys/text-extractor

Useful patterns:

- Built-in Windows screenshot UX.
- OCR/text actions.
- Redaction/copy text flows.

Wevito takeaway:

- We should avoid surprising users with silent screen capture.
- User-visible capture consent and clear capture indicators matter.
- OCR can become a later helper tool for reading errors/docs from screenshots.

## Technical Options

### Option A: Native Windows Capture First

Source: https://learn.microsoft.com/windows/uwp/audio-video-camera/screen-capture

Use `Windows.Graphics.Capture` for screenshots/snapshots and possibly frame streams.

Pros:

- Modern Windows-supported API.
- Good fit for WPF/vNext.
- Can capture a display or application window.
- Suitable for snapshots and future short recording streams.

Cons:

- Requires OS support checks.
- May show Windows capture consent UI depending on capture target.
- More implementation work than shelling out to an existing tool.

Recommendation:

- Use this as the first real in-app capture backend.

### Option B: FFmpeg Capture Backend

Source: https://www.ffmpeg.org/ffmpeg-devices.html

Use FFmpeg for region/window/desktop recording via Windows capture devices such as `gdigrab`.

Pros:

- Excellent encoding support.
- Can create MP4/GIF/WebM pipelines.
- Good for recordings once paths/regions are known.

Cons:

- External dependency.
- Region/window capture can be brittle.
- More privacy/security concerns.

Recommendation:

- Add later as an optional backend for longer recordings or GIF exports.

### Option C: OBS Integration

Sources:

- https://obsproject.com/
- https://obsproject.com/kb/sources-guide

Use OBS as an external integration or inspiration for scenes/sources.

Pros:

- Industry-standard recording/streaming feature set.
- Strong scene/source architecture.
- Good for advanced user workflows.

Cons:

- Heavyweight.
- Not ideal for a simple first Wevito helper workflow.
- Requires external app and configuration.

Recommendation:

- Do not require OBS for core Wevito.
- Borrow its scene/source model for our internal capture presets.
- Consider optional OBS handoff/export later.

## Proposed Wevito Architecture

```text
PET TASKS / Tool Hub
   |
   +-- Capture request
   |     |
   |     +-- CapturePolicy
   |     |     +-- allowed target type
   |     |     +-- privacy level
   |     |     +-- approval required?
   |     |
   |     +-- CapturePreset
   |     |     +-- wevito window
   |     |     +-- current foreground window
   |     |     +-- selected region
   |     |     +-- last region
   |     |     +-- proof surface
   |     |
   |     +-- CaptureBackend
   |           +-- native Windows screenshot
   |           +-- future FFmpeg recording
   |           +-- future OBS handoff
   |
   +-- CaptureArtifact
         +-- screenshot.png / clip.mp4
         +-- manifest.json
         +-- run-summary.md
         +-- optional OCR text
         +-- optional redaction metadata
```

## Recommended Phases

### Phase 25: Capture Contracts And Policy

- Add capture request/result contracts.
- Add capture presets.
- Add privacy levels.
- Add artifact manifest schema.
- Add policy evaluator rules:
  - in-app Wevito capture: low risk
  - foreground window capture: approval required
  - full desktop capture: approval required
  - recording: approval required
  - external upload/share: blocked for now

### Phase 26: Local Capture Library UI

- Add a Tool Hub section for capture history.
- Show timestamp, mode, assigned pet, task card, artifact path.
- Add open-folder/open-report buttons later, but start with read-only listing.

### Phase 27: Native Wevito Window Screenshot

- Implement first backend for Wevito app/window capture only.
- Write:
  - `screenshot.png`
  - `manifest.json`
  - `run-summary.md`
- Keep it manual and visible.
- Do not add full desktop capture yet.

### Phase 28: PET TASKS Capture Preview Adapter

- Add `captureProof` or `screenCapture` task family.
- First mode is dry-run preview only.
- Then allow approved in-app capture execution only.
- Keep desktop/foreground recording gated.

### Phase 29: Region And Last-Region Capture

- Add user-selected region storage.
- Add last-region capture.
- Useful for watching a proof area or visual bug.

### Phase 30: Short Recording Proof Clips

- Add short bounded MP4 clips.
- Default max duration: 5-10 seconds.
- Target Wevito/proof window first.
- Add GIF export later.

### Phase 31: OCR And Redaction

- Add optional OCR extraction.
- Add redaction metadata and manual blur/redact workflows.
- Do not auto-upload or auto-share.

## Best First Implementation

Start with Phase 25.

Reason:

- Capture touches privacy and user trust.
- We need contracts and policy gates before any screen capture execution.
- It fits the current PET TASKS architecture and keeps the first implementation safe.

The first actual capture feature should be:

```text
Capture current Wevito window
   |
   +-- visible user action
   +-- no full desktop
   +-- no upload
   +-- no recording
   +-- timestamped artifact folder
   +-- manifest + markdown report
```

