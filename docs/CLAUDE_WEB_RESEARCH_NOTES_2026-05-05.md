# Claude Web Research Notes

Date: 2026-05-05
Author: Claude master review
Purpose: capture external research findings that informed `CLAUDE_FULL_WEVITO_IMPLEMENTATION_PLAN_2026-05-05.md`. Each finding maps to a phase or decision.

This is a reference doc. Codex does not need to read it for any phase. The master plan and Codex prompts already encode the decisions.

## How To Read This Doc

```text
Topic
  ├── Finding (one paragraph)
  ├── 2026 truth (where current state differs from older docs)
  ├── Wevito decision
  └── Mapped phase
```

## 1. Windows Screen Capture (informs C-PHASE 11, 12, 13)

### Finding: Windows.Graphics.Capture (WGC) is the Microsoft-recommended modern path

WGC is the same API OBS, Teams, and Snipping Tool use. Originally Win10 1803; the desktop-callable surface (`GraphicsCaptureItemInterop::CreateForWindow`/`CreateForMonitor`) landed in 1903 / SDK 18362, which is the floor for Wevito.

Sources:
- https://learn.microsoft.com/en-us/uwp/api/windows.graphics.capture?view=winrt-26100
- https://github.com/microsoft/Windows.UI.Composition-Win32-Samples/tree/master/dotnet/WPF/ScreenCapture
- https://github.com/obsproject/obs-studio/blob/master/libobs-winrt/winrt-capture.cpp

### Finding: TFM matters

Use `net8.0-windows10.0.19041.0` (or higher) — WinRT projections are first-class on .NET 8, no extra `Microsoft.Windows.SDK.Contracts` needed. Most common bug is letting the `DispatcherQueueController` get GC'd, causing `InvalidCastException`. Source: https://learn.microsoft.com/en-us/answers/questions/5722908/best-practice-for-long-scrolling-screenshot-in-win

### Finding: PrintWindow + PW_RENDERFULLCONTENT is the legacy fallback only

Black on Chromium/Electron without the flag, slow with it, still flakes on D3D fullscreen and many UWP/XAML windows. Sources:
- https://groups.google.com/a/chromium.org/g/graphics-dev/c/LrpgdDg7p_8
- https://github.com/electron/electron/issues/21687

### Finding: Yellow capture indicator is by design

Drawn by the compositor, suppressible only via `GraphicsCaptureSession.IsBorderRequired = false` (Win11 22H2+) AND the user's `Settings → Privacy & security → Screenshot borders → Let apps turn off the screenshot border` toggle being on. Source: https://learn.microsoft.com/en-us/answers/questions/4172450/turn-off-the-yellow-screen-border-windows-11-uses

### Finding: `WDA_EXCLUDEFROMCAPTURE` is the official "do not capture" signal

`SetWindowDisplayAffinity(hwnd, WDA_EXCLUDEFROMCAPTURE)` (Win10 2004+). Recall, Snipping Tool, OBS, RDP, and your own WGC all honor it. Some Win11 driver bugs disrespect it. Per-window, not inherited by child HWNDs. Sources:
- https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setwindowdisplayaffinity
- https://www.meziantou.net/how-to-exclude-your-windows-app-from-screen-capture-and-recall.htm
- https://www.bleepingcomputer.com/news/security/signal-now-blocks-microsoft-recall-screenshots-on-windows-11/

### Finding: Recall (2025+) treats `WDA_EXCLUDEFROMCAPTURE` as the contract

Recall ships GA, snapshots desktop continuously. Honors the flag. Auto-skips frames containing password/credit-card patterns. Enterprise can disable via Intune/Purview — do not assume Recall is off. Sources:
- https://support.microsoft.com/en-us/windows/privacy-and-control-over-your-recall-experience-d404f672-7647-41e5-886c-a3c59680af15
- https://learn.microsoft.com/en-us/windows/client-management/manage-recall

### Finding: For recording, FFmpeg gdigrab is no longer recommended

`gdigrab` inherits all PrintWindow/BitBlt limits. `ddagrab` (Desktop Duplication API) is the modern FFmpeg path. Best 2026 .NET choice = WGC frame pool → Media Foundation H.264/HEVC sink writer (what Xbox Game Bar / Snipping Tool video mode use). Source: https://copyprogramming.com/howto/how-to-record-windows-screen-with-ffmpeg

### Finding: ShareX/Greenshot post-capture redaction

Neither tool implements OS-level exclude; they capture whatever DWM gives, then redact in their editor. Pattern Wevito should follow: capture → editor pixelate/blackout → upload (only with explicit confirmation). Source: https://getsharex.com/

### Wevito decisions (mapped to C-PHASE 11/12/13)

1. TFM = `net8.0-windows10.0.19041.0` for `Wevito.VNext.Shell`.
2. Default capture path = WGC system picker.
3. Keep PrintWindow + PW_RENDERFULLCONTENT only as a fallback for Win10 < 1903.
4. Yellow border on by default. Opt-in toggle to suppress, only enable if OS-level toggle is on.
5. Always set `WDA_EXCLUDEFROMCAPTURE` on Wevito's own settings/credential surfaces.
6. Recording = WGC → MF SinkWriter, not bundled FFmpeg. If FFmpeg ships for transcode, use `ddagrab`.
7. Post-capture redaction is mandatory before any network egress. C-PHASE 11/12/13 ship without network egress at all.
8. Document Recall behavior in privacy doc (C-PHASE 30).

## 2. Windows Audio APIs (informs C-PHASE 16, 17)

### Finding: NAudio is the C# binding choice

NAudio v2.3.0 (March 2026), .NET 6+/Framework 4.7.2+, works on .NET 8. Mature COM interop wrappers around `IMMDevice`, `IAudioEndpointVolume`, `IAudioSessionManager2`, `ISimpleAudioVolume`. CSCore has slowed; community migrated (Lively switched). CsWin32 (Microsoft.Windows.CsWin32 0.3.275) is fine for missing interfaces but doesn't include `IPolicyConfig` (the undocumented per-app default-device routing — what EarTrumpet uses). Sources:
- https://github.com/naudio/NAudio
- https://www.nuget.org/packages/NAudio/
- http://filoe.github.io/cscore/
- https://github.com/microsoft/CsWin32/issues/1105

### Finding: APO development is kernel-adjacent driver work, not an app feature

APOs ship as part of an audio driver package, require WHQL/HLK validation. Win11 wants `IAudioSystemEffects3` and CAPX. LFX/GFX APOs de-facto deprecated. Microsoft recommends a separate Hardware Support App (HSA) for user-facing settings. Sources:
- https://learn.microsoft.com/en-us/windows-hardware/drivers/audio/audio-processing-object-architecture
- https://learn.microsoft.com/en-us/windows-hardware/drivers/audio/windows-11-apis-for-audio-processing-objects

### Finding: Equalizer APO

v1.4.2 (Nov 2025), still actively maintained on SourceForge. 64-bit-precision fork `equalizerAPO64` exists. **Reboot required** at install and after Windows updates that replace the audio driver. Plain-text `config.txt` at `C:\Program Files\EqualizerAPO\config\`, easily generated/edited. No first-party C# wrapper — integration is "write a text file, optionally signal reload." Sources:
- https://sourceforge.net/projects/equalizerapo/
- https://sourceforge.net/p/equalizerapo/wiki/Configuration%20reference/
- https://sourceforge.net/p/equalizerapo/discussion/general/thread/3139195e12/

### Finding: FxSound

Open-source under GPL-3 since 2022; `fxsound2/fxsound-app` last updated April 2026. Installs MSI + a separate virtual audio driver. CLI flags exist but **runtime preset switching via CLI is unreliable** per user reports. Treat as "user installs and runs it." Sources:
- https://github.com/fxsound2/fxsound-app
- https://github.com/fxsound2/fxsound-driver
- https://forum.fxsound.com/t/changing-presets-via-command/4800

### Finding: EarTrumpet is the reference UX

C# + WPF (v2.0+ removed all C++). Centennial-packaged desktop app via Microsoft Store, but the UI is WPF XAML. Architecture worth copying: tray icon + flyout + mixer window, view-models subscribed to `IAudioSessionNotification` events dispatching to UI thread. Source: https://github.com/File-New-Project/EarTrumpet

### Finding: Hearing-safety guidelines

WHO safe-listening: 80 dB(A) for ≤40h/week, 75 dB for children; safe time halves per +3 dB. EBU R 128: program loudness target -23 LUFS, **true-peak ceiling -1 dBTP**. R 128 s2 is the streaming supplement. iZotope/FabFilter docs cite -1.0 to -0.1 dBFS as the standard ceiling band. Sources:
- https://www.who.int/news-room/questions-and-answers/item/deafness-and-hearing-loss-safe-listening
- https://tech.ebu.ch/publications/r128
- https://tech.ebu.ch/docs/r/r128s2.pdf
- https://www.izotope.com/en/learn/true-peak-limiter

### Finding: Volume above 100% without DSP is not possible

`IAudioEndpointVolume::SetMasterVolumeLevelScalar` clamps to [0.0, 1.0]. Microsoft removed in-box "Loudness Equalization" enhancement in KB4497934 (2019). Any "boost" must be DSP — your own gain stage in WASAPI loopback/render path, or hand off to APO/FxSound. Source: https://www.pcworld.com/article/2647501/windows-100-volume-is-a-phony-limit-heres-how-i-boost-audio-6x-higher.html

### Wevito decisions (mapped to C-PHASE 16/17)

1. NAudio for v1; CsWin32 only for missing pieces.
2. Volume scope = endpoint master + per-session sliders (EarTrumpet pattern). Skip `IPolicyConfig` for v1.
3. **No custom APO.** Hand off to Equalizer APO (config.txt generation) as primary, FxSound as secondary external option.
4. Document the reboot-on-install reality of Equalizer APO in the setup guide.
5. Internal gain hard-clamped to 0 dB by default; up to +6 dB only behind a true-peak limiter at -1 dBTP and a WHO 80 dB(A) safety warning.
6. UI must explicitly say "Windows volume cannot exceed 100% without DSP" so users don't expect the impossible.

## 3. Translation Providers (informs C-PHASE 14, 15)

### Finding: DeepL API

Free tier = 500,000 chars/month, no cost. Pro = $5.49/month + $25 per 1M chars. ~33 languages. Glossary supported per-language-pair via API. Pro tier does **not** train on input; Free tier "may be used to improve" the *web/app* product but not the *API* (users conflate these). Official `DeepL.net` NuGet (1.19.0). Sources:
- https://github.com/DeepLcom/deepl-dotnet
- https://www.deepl.com/en/pro-data-security
- https://support.deepl.com/hc/en-us/articles/360021200939-DeepL-API-plans

### Finding: Google Cloud Translation v3

$20 per 1M chars; first 500k/month free. Glossary free to create/use. AutoML custom models = $60-80 per 1M chars + $45/hr training (capped $300/job). Not permanently stored; transient up to a few days. Source: https://cloud.google.com/translate/pricing

### Finding: Azure AI Translator

S1 PAYG = $10 per 1M chars. F0 free = 2M chars/month. Custom models hosted hourly while deployed (cost trap). "No Trace by design" — not persisted. Source: https://azure.microsoft.com/en-us/pricing/details/translator/

### Finding: LibreTranslate

Powered by Argos Translate (OpenNMT/CTranslate2). Quality below DeepL/Google but acceptable for non-critical UX strings. ~30+ languages. Self-hostable on Windows with Python ≥3.8. Models several GB. Sources:
- https://github.com/LibreTranslate/LibreTranslate
- https://docs.libretranslate.com/guides/installation/

### Finding: Argos Translate

Active. Powers LibreTranslate. Python-native — from .NET 8 desktop, ship a localhost LibreTranslate sidecar rather than embed Argos. Source: https://github.com/argosopentech/argos-translate

### Finding: NLLB / M2M100 / SeamlessM4T

NLLB-200 distilled-600M is the only realistic local size for desktop (~1.2 GB, runs on CPU via CTranslate2 quantized int8). No first-party .NET binding; practical paths = ONNX Runtime export, CTranslate2 P/Invoke, or localhost sidecar. SeamlessM4T overkill for text-only. Sources:
- https://github.com/OpenNMT/CTranslate2
- https://huggingface.co/facebook/seamless-m4t-v2-large

### Finding: Privacy posture summary

| Provider | Retention | Trains on input? |
|---|---|---|
| DeepL Free (web/app) | May store + train | YES |
| DeepL API Free + Pro | Deleted after performance; 90-day grace if storage opted in | NO for Pro; Free tier text "may be used to improve" |
| Google Cloud Translation API | Transient up to a few days | NO |
| Azure AI Translator | Not persisted | NO |
| LibreTranslate self-hosted | None by default | NO |
| LibreTranslate.com hosted | IP + key 2 days; no text | NO |
| Argos / NLLB local | Never leaves machine | N/A |

Sources:
- https://www.deepl.com/en/pro-data-security
- https://docs.cloud.google.com/translate/data-usage
- https://learn.microsoft.com/en-us/legal/cognitive-services/translator/data-privacy-security
- https://portal.libretranslate.com/privacy.html

### Wevito decisions (mapped to C-PHASE 14/15)

1. **Default first cloud provider = DeepL API Free.** Already implemented (Phase 35 in code-side history); C-PHASE 14 adds glossary + QA.
2. **Default offline fallback = LibreTranslate localhost sidecar** (simpler integration than NLLB-via-ONNX). C-PHASE 15.
3. Default state = `provider: None`. No translation until user picks.
4. First-translation consent dialog: provider name, where text goes, retention, link to provider's privacy doc.
5. Persist API key in Windows Credential Manager (DPAPI), never plaintext config.
6. Show active provider name in result UI ("Translated by DeepL").
7. Glossary canonical form in Wevito's own schema; translate to per-provider format at sync time.

## 4. Agent Tool Permissions (informs C-PHASE 9, 26, 27)

### Finding: Anthropic Claude Agent SDK permission model

Order: deny rules → permission_mode → bypassPermissions → allow rules. `allowed_tools` = pre-approval allowlist; unlisted tools fall through to `permission_mode` and `can_use_tool`. Modes: `default`, `acceptEdits`, `plan` (no execution), `dontAsk`, `bypassPermissions`. The `can_use_tool` callback is the canonical per-call dynamic gate. Source: https://platform.claude.com/docs/en/agent-sdk/permissions

### Finding: MCP gives nothing for free

The protocol recommends but cannot enforce that clients prompt before invoking a tool. 2026 trend = a policy/control plane between client and server (Microsoft "Securing MCP", OpenClaw memory-pro). The host owns consent, scoping, policy. Sources:
- https://modelcontextprotocol.io/docs/tutorials/security/security_best_practices
- https://developer.microsoft.com/blog/securing-mcp-a-control-plane-for-agent-tool-execution

### Finding: OpenClaw deny-wins + per-agent allow override

`tools.allow`/`tools.deny` global and per-agent, deny-wins, case-insensitive, `*` wildcards. `tools.profile` sets a base allowlist (e.g. `messaging`). Per-agent `agents.list[].tools` overrides. RotatingFileHandler for logs (50 MB × 5). Sources:
- https://open-claw.bot/docs/tools/
- https://docs.openclaw.ai/concepts/multi-agent

### Finding: Microsoft Copilot Studio safety model

Least-privilege per agent, scoped permissions, explicit decision boundaries, auditable processes, tenant publish controls, mandatory approval for autonomous agents. Input authenticity (sender validation, trigger keywords) is first-class. Sources:
- https://learn.microsoft.com/en-us/microsoft-copilot-studio/guidance/autonomous-agents
- https://learn.microsoft.com/en-us/microsoft-copilot-studio/security-and-governance

### Finding: 2026 prompt-injection mitigations

Dominant patterns: dual-LLM (privileged tool-holder never reads untrusted; quarantined LLM emits structured/symbolic results), CaMeL-style sandboxed-DSL with taint-flow, spotlighting / `<untrusted>` tags wrapping web/email content, classifier-based scanning. The "lethal trifecta" rule: untrusted input + private data + external comms in one agent = no. Break one leg per agent. Sources:
- https://arxiv.org/html/2506.08837v2
- https://www.anthropic.com/news/prompt-injection-defenses
- https://simonw.substack.com/p/the-lethal-trifecta-for-ai-agents

### Finding: Capability tokens > role-based for agents

Agent-as-first-class-identity + capability tokens (FGA/ABAC). Each agent gets its own identity, just-in-time short-lived scoped credentials, attribute-based decisions. RBAC is the floor; capability/FGA is the production answer. Sources:
- https://workos.com/blog/best-authorization-platforms-ai-agent-permissions-2026
- https://www.strata.io/blog/agentic-identity/8-strategies-for-ai-agent-security/

### Finding: Approval-gate taxonomy

Four-gate framework (advisory / validating / blocking / escalating). Concrete forms: dry-run preview, policy-validation gate, human approval gate. Microsoft Agent Framework ships `require_script_approval=True`. n8n calls it the "Approval Gate Pattern." Wevito's report-only / preview / execute maps cleanly onto advisory / validating / blocking. Sources:
- https://www.digitalapplied.com/blog/agentic-workflow-approval-gate-framework-governance
- https://devblogs.microsoft.com/agent-framework/whats-new-in-agent-skills-code-skills-script-execution-and-approval-for-python/

### Finding: sqlite-vec is the 2026 stable embedded vector store

sqlite-vss is dormant; use **sqlite-vec**. Pure-C, no deps, runs everywhere SQLite does. Chroma wins for fastest dev experience. LanceDB wins for larger-than-memory and Rust-native predictable perf (what OpenClaw memory-pro uses). For Wevito's per-pet preference store: sqlite-vec + FTS5 in one DB file is lowest-risk stable pick. Sources:
- https://github.com/asg017/sqlite-vec
- https://www.lancedb.com/blog/openclaw-memory-from-zero-to-lancedb-pro
- https://zilliz.com/comparison/chroma-vs-lancedb

### Wevito decisions (mapped to C-PHASE 9/26/27)

1. Each helper pet = first-class identity with its own ID, virtual key, audit stream. No shared credentials.
2. Central tool registry; tools versioned and signed. Pets see only tools in their per-pet allowlist.
3. Allow/deny resolution: global deny → pet-level deny → permission-mode → pet-level allow → global allow. Deny wins.
4. Three permission modes per pet: `plan` (read/think only), `preview` (propose + diff, await user), `execute` (run pre-approved tools).
5. Four-gate model: advisory (auto-run), validating (preview diff), blocking (explicit approve), escalating (typed confirmation, rate-limited).
6. Capability tokens, not roles. Short-lived (minutes), narrowly scoped, re-issued per task.
7. Lethal-trifecta separation: Scout reads untrusted external content (docs/web/email), Builder doesn't read untrusted, Inspector reads sprite-only data. Dual-LLM pattern when one pet must consume web/email — quarantined sub-call returns structured fields tagged `<untrusted>`.
8. One sqlite file per pet using sqlite-vec + FTS5 for reviewed examples and preferences. Memory writes are themselves a gated tool (validating gate).
9. Append-only JSONL audit log per pet: tool, args hash, decision, gate fired, user response, latency. Rotate 50 MB. Surface "what did my pets do today" view.
10. Ship a one-click "pause all pets" before launch — research's biggest 2026 documented failure mode is governance-monitoring without a kill switch.

## 5. Sprite Pipeline / Asset Provenance (informs C-PHASE 20-23)

### Finding: 2026 manifest pattern

Game-side converged on Unity Addressables-style (catalog JSON + hash file, content-addressable bundle filenames). Broader software supply chain uses Sigstore/Cosign + in-toto / SLSA v1.1 attestations. GitHub Actions ships native artifact attestations. CRC-32 in Unity catalogs is integrity-only; Cosign+SLSA is the trust story. **BLAKE3** beats SHA-256 for big asset trees (faster, parallel). Sources:
- https://docs.unity3d.com/Manual/AssetBundles-Integrity.html
- https://docs.sigstore.dev/cosign/signing/other_types/
- https://github.com/BLAKE3-team/BLAKE3

### Finding: Contact sheet generation

Aseprite CLI (`--batch`, `--sheet`, `--data`, `--split-tags`) dominant for `.aseprite` source. ImageMagick `montage` universal fallback. Free Texture Packer, rTexPacker (raylib), I Love Sprites for atlas packing. TexturePacker still king commercially. Sources:
- https://www.aseprite.org/docs/cli/
- https://www.starheretic.com/blog/2025/02/imagemagick-sprite-sheets
- https://free-tex-packer.com/

### Finding: Atomic apply

No "sprite-tools" library specifically. Python idiom = `python-atomicwrites` (write to temp + `os.replace`). For binary asset mutation: snapshot to timestamped dir, write new files to staging, `os.replace`, keep N rolling backups. **`os.replace` is only atomic on the same filesystem** — Windows + network drive silently degrades. Sources:
- https://github.com/untitaker/python-atomicwrites
- https://docs.bswen.com/blog/2026-04-04-atomic-file-writing-python/

### Finding: Visual diff hierarchy

pixelmatch (slow, JS) → odiff (Zig+SIMD, ~6× faster) → Honeydiff (Rust+Rayon, 9-16× faster than odiff, with SSIM + spatial clustering). 2026 upgrade = SSIM ≥ 0.92 / LPIPS ≤ 0.20 as CI gates. dHash perceptual hashes for dedup. Sources:
- https://vizzly.dev/blog/honeydiff-vs-odiff-pixelmatch-benchmarks/
- https://github.com/dmtrKovalenko/odiff
- https://github.com/kornelski/dssim

### Finding: Apply / rollback

Game industry has not converged on a unified apply/rollback pattern. Perforce dominant for AAA. Git-LFS indie default. Anchorpoint (Git-LFS GUI for artists) gained traction in 2026. DVC exists but rarely used in games. Don't reinvent VCS — apply log + backup snapshot is enough rollback substrate. Source: https://www.strayspark.studio/blog/version-control-ue5-git-lfs-perforce

### Finding: Palette swap for RYBIV

PixelPaletteTool (with-the-love studios, GUI + CLI), Pixel Swap (web, CIELAB matching), PixelArtRecolor (open source). LUT-based palette-swap = engine-side cheap pattern. CIELAB can over-shift hues for saturated rainbow palettes (RYBIV is exactly this case) — keep Weighted-RGB fallback. Sources:
- https://withthelove.itch.io/pixelpalettetool
- https://pixelswap.art/
- https://github.com/VirtualZer0/PixelArtRecolor

### Finding: One-screen workbench UI references

No de-facto open-source "asset review workbench". Closest: Pixelorama (single-window FOSS pixel multitool, Godot-based), Tilf (lightweight cross-platform pixel editor), Avalonia DevTools Assets pane, ShareX 20 rebuilt image editor on Avalonia in 2026. Sources:
- https://github.com/Orama-Interactive/Pixelorama
- https://docs.avaloniaui.net/tools/developer-tools/assets-tool

### Finding: Desktop-pet / overlay patterns

WindowPet (Tauri + React, 45+ pets) leading FOSS reference. Open-LLM-VTuber ships desktop pet mode for Live2D. Shimeji-ee (Java, Windows-only) old guard. Tauri v2 = 2026 framework of choice. **Tauri v2 still has no native click-through API** — workaround is a Rust loop polling cursor position at ~60fps and toggling `setIgnoreCursorEvents`, burns CPU. macOS auto-passes-through fully transparent pixels; Windows/Linux do not. WPF on Windows: `WS_EX_LAYERED | WS_EX_TRANSPARENT` is ~5 lines of P/Invoke. Sources:
- https://github.com/SeakMengs/WindowPet
- https://blog.manasight.gg/why-i-chose-tauri-v2-for-a-desktop-overlay/
- https://github.com/tauri-apps/tauri/issues/13070

### Wevito decisions (mapped to C-PHASE 20-23)

1. Use BLAKE3 for content hashes in Sprite Workflow V2 manifest. Faster than SHA-256 for the asset tree.
2. Manifest fields: `{path, blake3, source_file, source_hash, generator_version}`.
3. Aseprite CLI when sources are `.aseprite`; SkiaSharp/System.Drawing for arbitrary PNG sets. JSON sidecar (`hash` format) alongside the sheet.
4. Apply pattern: hash → staging → backup → `File.Move(overwrite: true)` on same volume. Reject apply if sprites_runtime is on a different volume from `.staging`.
5. Visual diff: pixel diff + Hamming-distance dHash for dedup. Gate "approve" button on SSIM ≥ 0.95 vs golden.
6. Don't reinvent VCS. Apply log + backup snapshot = rollback.
7. Palette swap for the existing 6 RYBIV folders is **out of scope** — folders already exist. C-PHASE 20-23 only handle apply/rollback for one-row work like `drop_ball`. Future palette-swap work would use PixelPaletteTool or PixelArtRecolor.
8. One-screen workbench inspired by Pixelorama: left = queue, center = source/runtime/candidate/proof + animation preview, right = provenance/findings.
9. Wevito stays on WPF + WS_EX_LAYERED + WS_EX_TRANSPARENT for the overlay. Tauri rewrite is out of scope.

## Mapping Summary

| Phase | Primary research topic | Key decision |
|---|---|---|
| C-PHASE 11/12/13 | Windows screen capture | WGC + WDA_EXCLUDEFROMCAPTURE; MF SinkWriter for clips |
| C-PHASE 14/15 | Translation providers | DeepL default cloud, LibreTranslate sidecar fallback, default = none |
| C-PHASE 16/17 | Windows audio | NAudio for v1, no custom APO, Equalizer APO + FxSound handoff, EBU R 128 ceiling |
| C-PHASE 9/26/27 | Agent permissions | Deny-wins + per-pet allowlist, capability tokens, lethal-trifecta separation, sqlite-vec memory |
| C-PHASE 20-23 | Sprite pipeline | BLAKE3 manifest, atomic apply on same volume, SSIM ≥ 0.95 gate, Pixelorama-like one-screen workbench |

## Sources Index (Quick Reference)

Capture:
- [Windows.Graphics.Capture API](https://learn.microsoft.com/en-us/uwp/api/windows.graphics.capture?view=winrt-26100)
- [WPF screen-capture sample](https://github.com/microsoft/Windows.UI.Composition-Win32-Samples/tree/master/dotnet/WPF/ScreenCapture)
- [SetWindowDisplayAffinity](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setwindowdisplayaffinity)
- [Recall privacy controls](https://support.microsoft.com/en-us/windows/privacy-and-control-over-your-recall-experience-d404f672-7647-41e5-886c-a3c59680af15)

Audio:
- [NAudio](https://github.com/naudio/NAudio)
- [Equalizer APO](https://sourceforge.net/projects/equalizerapo/)
- [FxSound app](https://github.com/fxsound2/fxsound-app)
- [EarTrumpet](https://github.com/File-New-Project/EarTrumpet)
- [WHO safe listening](https://www.who.int/news-room/questions-and-answers/item/deafness-and-hearing-loss-safe-listening)
- [EBU R 128](https://tech.ebu.ch/publications/r128)

Translation:
- [DeepL .NET](https://github.com/DeepLcom/deepl-dotnet)
- [DeepL data security](https://www.deepl.com/en/pro-data-security)
- [Google Cloud Translation pricing](https://cloud.google.com/translate/pricing)
- [Azure Translator pricing](https://azure.microsoft.com/en-us/pricing/details/translator/)
- [LibreTranslate](https://github.com/LibreTranslate/LibreTranslate)

Agents:
- [Claude Agent SDK permissions](https://platform.claude.com/docs/en/agent-sdk/permissions)
- [MCP security](https://modelcontextprotocol.io/docs/tutorials/security/security_best_practices)
- [Securing MCP control plane](https://developer.microsoft.com/blog/securing-mcp-a-control-plane-for-agent-tool-execution)
- [OpenClaw tools](https://open-claw.bot/docs/tools/)
- [Lethal trifecta](https://simonw.substack.com/p/the-lethal-trifecta-for-ai-agents)
- [sqlite-vec](https://github.com/asg017/sqlite-vec)

Sprite pipeline:
- [BLAKE3](https://github.com/BLAKE3-team/BLAKE3)
- [Aseprite CLI](https://www.aseprite.org/docs/cli/)
- [python-atomicwrites](https://github.com/untitaker/python-atomicwrites)
- [Honeydiff benchmarks](https://vizzly.dev/blog/honeydiff-vs-odiff-pixelmatch-benchmarks/)
- [Pixelorama](https://github.com/Orama-Interactive/Pixelorama)
- [WindowPet](https://github.com/SeakMengs/WindowPet)

# End

For implementation details, see `CLAUDE_FULL_WEVITO_IMPLEMENTATION_PLAN_2026-05-05.md`. For per-phase prompts, see `CLAUDE_CODEX_MEDIUM_PHASE_PROMPTS_2026-05-05.md`.
