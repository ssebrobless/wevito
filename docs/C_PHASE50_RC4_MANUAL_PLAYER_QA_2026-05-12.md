# C-PHASE 50 RC4 Manual Player QA

Date: 2026-05-12

Branch: `claude-implementation/c-phase-50-rc4-manual-player-qa`

## Decision

```text
RC4 player QA
|
+-- public GitHub artifact download: PASS
+-- SHA256 verification: PASS
+-- clean extraction: PASS
+-- packaged full automation: PASS
+-- packaged drink scenario: PASS
+-- packaged fetch scenario: PASS
+-- packaged save-position recovery: PASS
+-- goose habitat viewport proof: PASS
+-- obvious screenshot visual blocker: NOT FOUND
`-- final stable promotion: STILL REQUIRES USER APPROVAL
```

Recommended decision label:

```text
accept_rc4_for_stable
```

Reason: no release-blocking issue was found in the clean public RC4 artifact during this pass. Stable promotion is still a separate explicit gate because it creates the final public stable release.

## Artifact Under Review

Release:

```text
tag: v0.1.0-desktop-rc4
url: https://github.com/ssebrobless/wevito/releases/tag/v0.1.0-desktop-rc4
asset: WevitoDesktopPet-v0.1.0-desktop-rc4-win64.zip
sha256: c12a61d0ddd55bfa611bc14480dd298584e2e9d3849ea8e01064beb71c4efacc
```

Clean QA root:

```text
vnext/artifacts/c-phase-50-rc4-manual-player-qa/
```

Download/extract summary:

```text
vnext/artifacts/c-phase-50-rc4-manual-player-qa/rc4-download-extract-summary.json
```

Bundle check:

| Check | Result |
| --- | --- |
| Zip SHA256 matches RC4 notes | PASS |
| `WevitoDesktopPet.exe` present | PASS |
| `WevitoDesktopBridge.exe` present | PASS |
| Top-level packaged asset files | 11392 |
| Godot `.pck` file required separately | NO |

## Packaged Player-Facing Automation

All runs used isolated timestamped `APPDATA` folders under the QA artifact root. Normal local save data was not touched.

Summary:

```text
vnext/artifacts/c-phase-50-rc4-manual-player-qa/rc4-player-qa-automation-summary.json
```

| Run | Scenario | Exit code | Report passed | Checks | Result | Report |
| --- | --- | ---: | --- | ---: | --- | --- |
| full | `fresh` | 0 | true | 25 | PASS | `vnext/artifacts/c-phase-50-rc4-manual-player-qa/rc4-full-report.json` |
| drink | `force_low_hydration_drink` | 0 | true | 26 | PASS | `vnext/artifacts/c-phase-50-rc4-manual-player-qa/rc4-drink-report.json` |
| fetch | `force_fetch_sequence` | 0 | true | 26 | PASS | `vnext/artifacts/c-phase-50-rc4-manual-player-qa/rc4-fetch-report.json` |
| position recovery | `force_save_position_recovery` | 0 | true | 3 | PASS | `vnext/artifacts/c-phase-50-rc4-manual-player-qa/rc4-position-recovery-report.json` |
| goose viewport | `c_phase_6_5_habitat_mirror_goose` | 0 | true | 7 | PASS | `vnext/artifacts/c-phase-50-rc4-manual-player-qa/rc4-goose-viewport-report.json` |

No `SCRIPT ERROR` or non-ObjectDB `ERROR:` log lines were found in these copied run logs.

## Player QA Checklist

| Area | Evidence | Status |
| --- | --- | --- |
| Fresh public artifact | Downloaded from GitHub release, hash matched exactly | PASS |
| Clean launch dependency | Packaged exe ran from clean extraction with no repo/editor dependency in automation mode | PASS |
| Overlay/control basics | Full run hit-tests settings, basket, actions, focused/passive overlay, pin/release, focus restore | PASS |
| Pet care actions | Full run proves core action tabs, action effects, medicine item flow, save/reset/reload | PASS |
| Drink behavior | Forced low-hydration scenario passed | PASS |
| Fetch behavior | Forced fetch sequence passed | PASS |
| Save-position recovery | Forced stale-position scenario passed | PASS |
| Goose habitat composition | App-generated screenshot captured and inspected | PASS |
| PET TASKS clarity | RC4 release notes and prior docs preserve report-first/gated language | PASS BY DOC/RELEASE REVIEW |
| Translation/audio/screenshot/model-call honesty | RC4 release notes preserve preview/gated wording and model calls remain disabled | PASS BY DOC/RELEASE REVIEW |

## Screenshot Proof

```text
vnext/artifacts/c-phase-50-rc4-manual-player-qa/rc4-goose-viewport.png
```

Visual inspection notes:

- Goose is in-frame.
- Contact shadow is visible under the feet.
- Habitat frame is not visibly clipped.
- No obvious fake PNG box, missing body hole, or severe silhouette artifact is visible in this proof screenshot.

## Residual Human Acceptance

This phase is the strongest QA pass I can perform from the code-side thread without pretending to be the final human player. The remaining stable-release judgment is a short owner acceptance pass:

- Does the overlay feel pleasant while using the PC normally?
- Are the controls discoverable enough in real use, not just hit-testable?
- Does the pet motion feel acceptable over time, not just in forced scenarios?
- Is the PET TASKS wording understandable to the user?

No blocker was found that justifies publishing RC5 before stable.

## Next Objective

If the user approves, proceed to:

```text
C-PHASE 52 - Stable Release Promotion
decision label: accept_rc4_for_stable
```

If the user spots a manual visual/control blocker first, use:

```text
fix_blockers_publish_rc5
```

## Gates Preserved

```text
still closed
|
+-- stable promotion until explicit approval
+-- live model calls
+-- broad sprite mutation
+-- visual generation/import
+-- prop-anchor edits
+-- all-color propagation
+-- asset-prep regeneration
+-- screen recording
+-- external audio booster control
`-- automatic training/data promotion
```
