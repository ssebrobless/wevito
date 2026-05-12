# PET TASKS Screen Capture Preview

Generated: 2026-05-12T20:59:15.7786456+00:00
TaskCard: `6dbd495d-b58d-4cfe-8d1d-d480d39cbf2e`

## Summary

- The request appears to ask for a window screenshot. Future execution should prefer the Wevito window when possible.
- Proof surface: offline preview report only; this is not a packaged/live capture proof.
- Captured screen: False
- Recorded screen: False
- Did mutate files: False
- Approval gate: RUN APPROVED is required before any screenshot artifact can be created.
- Recording gate: short no-audio Wevito-window clips stay separately approval-gated; preview mode never records.

## Capabilities

- WindowScreenshot: ApprovalRequired, risk Medium, approval BeforeExecution. Execution target: capture only the Wevito window.
- RegionScreenshot: ApprovalRequired, risk Medium, approval BeforeExecution. Execution target: capture an explicitly selected region or the saved last region.
- Screenshot: ApprovalRequired, risk High, approval BeforeExecution. Full-desktop screenshots can expose private information and must remain explicitly approval-gated.
- ScreenRecording: ApprovalRequired, risk Medium, approval BeforeExecution. Execution target: 5-10 second Wevito-window-only MP4 proof clip, no audio.
- GifRecording: Blocked, risk High, approval HandOffRequired. GIF/video capture waits for the recording gate; preview mode never records.

## Safety Notes

- No screenshot was captured.
- No screen recording was started.
- No clipboard, desktop, project file, or sprite asset was changed.
- Future screenshot execution should prefer Wevito-window or selected-region scope over full desktop.
- Short proof clips are Wevito-window-only, no-audio, approval-gated, and capped at 10 seconds.
