# Translation And Audio Tool Research Plan - 2026-05-05

## Goal

Add two planned Wevito tool families:

```text
translateText
   translate user-provided text into a chosen target language
   preserve glossary/style where possible
   write a reviewable result artifact

audioAssist
   help the user understand and improve PC audio volume
   start with safe volume/status controls
   later support approved boost/equalizer integrations
```

## Translation Research

### DeepL

Sources:

- https://developers.deepl.com/api-reference/translate
- https://support.deepl.com/hc/en-us/articles/4405021321746-Managing-glossaries-with-the-DeepL-API

Useful patterns:

- High-quality text translation through a REST API.
- Source language can be omitted for auto-detection.
- Target language is required.
- Glossaries can enforce terminology, but glossary language pair must match the request.
- Request body size limit requires chunking for longer text.
- API calls should go through a backend, not a browser client.

Wevito takeaway:

- Best first premium provider.
- Use backend-only API key handling.
- Support glossary later, but make the first version plain text translation plus review notes.

### Google Cloud Translation

Sources:

- https://cloud.google.com/translate/docs/advanced/glossary
- https://docs.cloud.google.com/translate/docs/reference/rest

Useful patterns:

- Cloud Translation API supports text translation and large/batch document workflows.
- Glossaries provide consistent domain terminology for product names, ambiguous words, borrowed words, etc.

Wevito takeaway:

- Good provider option for robust cloud translation and glossary workflows.
- More setup-heavy than a simple first provider.
- Best as a second provider after the adapter shape is stable.

### Azure AI Translator

Sources:

- https://learn.microsoft.com/azure/ai-services/translator/
- https://learn.microsoft.com/azure/ai-services/translator/custom-translator/concepts/customization

Useful patterns:

- Text and document translation through REST APIs.
- Custom Translator supports translation memory/custom neural systems for terminology and style.

Wevito takeaway:

- Strong enterprise-style provider.
- Useful later if the user wants Microsoft ecosystem integration.
- Should use same provider-router shape as DeepL/Google.

### LibreTranslate

Sources:

- https://docs.libretranslate.com/api/
- https://github.com/LibreTranslate/LibreTranslate

Useful patterns:

- Free/open-source machine translation API.
- Self-hosted/offline-capable.
- Simple endpoints for language detection and text/file translation.

Wevito takeaway:

- Best local/self-hosted fallback.
- Quality may be lower than commercial providers.
- Good for privacy-first workflows and no-cloud mode.

## Translation Tool Design

```text
User request
   |
   v
"Translate this to Spanish"
   |
   v
TaskIntent: translateText
   |
   v
Provider router
   |
   +-- DeepL
   +-- Google Cloud Translation
   +-- Azure AI Translator
   +-- LibreTranslate / self-hosted
   |
   v
Translation artifact
   |
   +-- translated-text.txt
   +-- translation-report.json
   +-- run-summary.md
```

### Translation Phases

#### Phase 38: Translation Contracts And Provider Router

- Add `translateText` task family.
- Add provider status records.
- Add translation request/result contracts.
- Add target/source language fields.
- Add glossary/style preference fields, unused at first.
- Add provider router with no network call yet.

#### Phase 39: Translation Preview Adapter

- Parse commands like:
  - `translate this to Spanish`
  - `translate "..." from English to French`
  - `make this Japanese`
- Produce a preview packet showing provider, target language, estimated character count, and whether credentials are configured.
- No external API call yet.

#### Phase 40: First Translation Execution Adapter

- Start with one provider, preferably DeepL if credentials are present.
- Approval required before first network call.
- Backend-only API key access.
- Write timestamped artifacts.
- Never auto-send private text without visible user action.

#### Phase 41: Glossary And QA Review

- Add glossary terms.
- Highlight preserved/changed terms.
- Add post-translation QA:
  - missing glossary term,
  - placeholders changed,
  - markdown/code formatting changed,
  - target text empty,
  - provider fallback used.

## Audio / Volume Booster Research

### Windows Core Audio APIs

Sources:

- https://learn.microsoft.com/windows/win32/api/_coreaudio/
- https://learn.microsoft.com/windows/win32/api/endpointvolume/nn-endpointvolume-iaudioendpointvolume

Useful patterns:

- Windows exposes endpoint and session volume APIs.
- `IAudioEndpointVolume` controls endpoint volume/mute.
- Per-session/application volume can be managed through Windows audio sessions.

Wevito takeaway:

- Best first implementation is safe volume status/control, not boost.
- We can show current output device, master volume, mute, and eventually per-app session volume.
- This does not boost beyond 100%.

### Windows Audio Processing Objects

Source:

- https://learn.microsoft.com/windows-hardware/drivers/audio/audio-processing-object-architecture

Useful patterns:

- APOs are Windows audio DSP effects inserted into the software device pipe.
- System effects can include equalizers, reverb, automatic gain control, and other DSP effects.
- APOs are driver-level/user-mode COM components and require careful installation/configuration.

Wevito takeaway:

- True system-wide volume boosting/equalization belongs in APO territory.
- We should not build a custom APO casually inside Wevito.
- Use APO-aware external integrations or handoff first.

### Equalizer APO

Source:

- https://sourceforge.net/projects/equalizerapo/

Useful patterns:

- System-wide parametric/graphic equalizer for Windows.
- Implemented as an Audio Processing Object for the Windows system effect infrastructure.
- Supports Windows 7-11.
- Applications that bypass system effects may not be affected.

Wevito takeaway:

- Best open-source model for real system-wide gain/EQ.
- Potential integration path:
  - detect installation,
  - explain device compatibility,
  - generate safe preset/config snippets,
  - open config folder/editor,
  - never silently overwrite config.

### FxSound

Sources:

- https://fxsound.org/
- https://github.com/fxsound2/fxsound-app

Useful patterns:

- Free/open-source Windows audio enhancer.
- Designed to enhance sound quality, increase volume, boost bass, and provide EQ/effects/presets.

Wevito takeaway:

- Best user-friendly external tool inspiration or optional handoff.
- We should not reimplement the whole DSP engine initially.
- We can eventually provide a "use FxSound/Equalizer APO" setup guide or launcher/status check.

### EarTrumpet

Source:

- https://github.com/File-New-Project/EarTrumpet

Useful patterns:

- Windows per-application volume mixer.
- WPF/Windows audio mixer patterns.

Wevito takeaway:

- Good reference for a friendly volume mixer UX.
- Useful for safe per-app/session volume control before any boost.

## Audio Tool Design

```text
User request
   |
   v
"Make my PC louder"
   |
   v
audioAssist task card
   |
   +-- Safe first actions
   |     +-- show current output device
   |     +-- show master volume/mute
   |     +-- raise Windows volume up to 100% with approval
   |     +-- show per-app volume later
   |
   +-- Boost actions
         +-- explain that >100% needs DSP/APO/external enhancer
         +-- generate safe Equalizer APO/FxSound setup guide
         +-- optional handoff/launcher
```

## Audio Safety Rules

- Do not silently change system audio.
- Do not exceed Windows volume without explicit user approval and warning.
- Show hearing/speaker safety note before any boost/gain preset.
- Prefer "normalize/clarify" over "blindly amplify."
- Use limiter/headroom guidance when suggesting gain.
- Never install drivers/APOs automatically.
- Do not edit Equalizer APO/FxSound configs silently.

## Audio Phases

### Phase 42: Audio Assist Contracts And Policy

- Add `audioAssist` task family.
- Add audio action kinds:
  - inspect volume,
  - set volume,
  - mute/unmute,
  - boost guide,
  - external enhancer handoff.
- Policy:
  - inspect volume: low risk.
  - set volume up to 100%: approval required.
  - boost/external config: approval required.
  - driver/APO install: blocked.

### Phase 43: Audio Status Preview

- Read-only report:
  - output device,
  - master volume,
  - mute state,
  - available audio sessions if feasible.
- Write markdown/JSON artifacts.
- No volume changes.

### Phase 44: Safe Volume Control

- Approval-gated master-volume control through Windows Core Audio.
- Maximum 100%.
- No boost.
- Clear UI feedback.

### Phase 45: External Boost Handoff

- Detect whether FxSound or Equalizer APO appears installed.
- Generate a setup/troubleshooting guide.
- Optionally open official download/docs page.
- Do not install automatically.

### Phase 46: Boost Preset Planning

- Generate conservative Equalizer APO/FxSound preset suggestions.
- Include limiter/headroom warnings.
- Store as a text plan only.
- User applies manually unless a later controlled mutation gate is approved.

## Recommended Immediate Placement In Master Plan

Translation should come after the local/report tools and before high-risk browser automation:

```text
Phase 38-41: translateText
```

Audio should come after capture/tool hub is stable because it affects the user's PC environment:

```text
Phase 42-46: audioAssist
```

## Best First Implementation

Start neither with live translation nor live volume boosting.

Best first code step after Tool Hub/capture foundations:

```text
Translation:
   translateText contracts + provider status preview

Audio:
   audioAssist contracts + read-only audio status preview
```

