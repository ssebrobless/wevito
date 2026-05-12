# Post-RC4 Next Todo List

Date: 2026-05-12

Branch: `claude-implementation/post-rc4-next-todo`

## Current Answer

```text
main plan status
|
+-- original Claude implementation roadmap
|   `-- complete through release-candidate packaging/validation
|
+-- post-roadmap release hardening phases
|   `-- complete through C-PHASE 49 / validated RC4
|
`-- not complete yet
    +-- manual player QA decision
    +-- stable release promotion or RC5 fix cycle
    +-- optional future feature gates after stable
```

We are complete with the main implementation plan in the practical sense: Wevito has a validated RC4 prerelease built from current main and clean-tested from GitHub.

We are **not** done with the final release workflow yet. The next decision is still:

```text
promote_rc4_to_stable
publish_rc5_for_fixes
hold_release_for_manual_qa
```

Recommended default:

```text
hold_release_for_manual_qa
```

Reason: the machine proof is green, but Wevito is a visual desktop pet overlay. Stable should wait for at least one short human pass over launch feel, overlay behavior, controls, animation readability, and PET TASKS clarity.

## Next Phase Shape

```text
next work
|
+-- C-PHASE 50: RC4 manual player QA
|   +-- user-visible launch/overlay check
|   +-- pet controls and visual motion check
|   +-- PET TASKS clarity check
|   `-- produce go/no-go decision
|
+-- C-PHASE 51: if issues found
|   +-- fix only release-blocking issues
|   +-- publish RC5
|   `-- clean-validate RC5
|
+-- C-PHASE 52: if no issues found
|   +-- promote RC4 to stable
|   +-- update release/help docs
|   `-- mark prior RCs superseded
|
`-- C-PHASE 53+: post-stable backlog
    +-- AI helper consent flag
    +-- optional visual/tool polish
    +-- future sprite workflow / model features
    `-- keep gated features gated
```

## C-PHASE 50 - RC4 Manual Player QA

Goal: decide whether RC4 feels stable enough to promote.

Inputs:

- `docs/C_PHASE49_RC4_PACKAGE_AND_CLEAN_VALIDATION_2026-05-12.md`
- GitHub release: `https://github.com/ssebrobless/wevito/releases/tag/v0.1.0-desktop-rc4`
- Asset: `WevitoDesktopPet-v0.1.0-desktop-rc4-win64.zip`
- SHA256: `c12a61d0ddd55bfa611bc14480dd298584e2e9d3849ea8e01064beb71c4efacc`

Tasks:

- Download RC4 from GitHub, not from local `builds/release`.
- Extract to a clean folder outside the repo.
- Launch `WevitoDesktopPet.exe`.
- Confirm the desktop pet appears without a terminal, Godot editor, or repo dependency.
- Confirm the pet is not stranded off-screen.
- Confirm click-through/pinned/unpinned behavior is understandable.
- Confirm feed, drink, play/fetch, groom, bath, medicine/doctor, rest, basket, and settings are discoverable enough.
- Confirm the pet faces the direction of movement and does not look obviously stuck in a wrong animation.
- Confirm goose habitat/placement/contact shadow looks acceptable.
- Confirm PET TASKS reads as report-first and does not imply unsafe hidden automation.
- Confirm translation/audio/screenshot/model-call wording is honest and gated.

Artifacts to produce:

```text
docs/C_PHASE50_RC4_MANUAL_PLAYER_QA_2026-05-12.md
vnext/artifacts/c-phase-50-rc4-manual-player-qa/
```

Decision labels:

```text
accept_rc4_for_stable
fix_blockers_publish_rc5
hold_for_more_manual_qa
```

## C-PHASE 51 - If Manual QA Finds Blockers

Only run this if C-PHASE 50 returns:

```text
fix_blockers_publish_rc5
```

Scope:

- Fix only release-blocking issues found during RC4 manual QA.
- Do not expand into new tools, broad sprite cleanup, live model calls, or visual generation.
- Package as `v0.1.0-desktop-rc5`.
- Clean-download and validate RC5 like RC4.

Validation:

- `dotnet build .\vnext\Wevito.VNext.sln`
- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build`
- Clean GitHub download hash check.
- Packaged automation:
  - full/fresh
  - `force_low_hydration_drink`
  - `force_fetch_sequence`
  - `force_save_position_recovery`
  - `c_phase_6_5_habitat_mirror_goose`

Output:

```text
docs/C_PHASE51_RC5_FIX_AND_CLEAN_VALIDATION_2026-05-12.md
```

## C-PHASE 52 - Stable Release Promotion

Only run this if C-PHASE 50 returns:

```text
accept_rc4_for_stable
```

Or if C-PHASE 51 validates RC5 and the user approves stable.

Tasks:

- Promote the accepted RC to stable release.
- Keep the accepted RC asset/hash documented.
- Update release notes to remove prerelease language where appropriate.
- Mark older RCs as superseded in docs or release notes.
- Update the user help guide if manual QA discovered wording issues.
- Create final stable release report.

Suggested stable tag:

```text
v0.1.0-desktop
```

Output:

```text
docs/C_PHASE52_STABLE_RELEASE_PROMOTION_2026-05-12.md
```

Stop condition:

- Do not promote stable without explicit user approval.

## C-PHASE 53+ - Post-Stable Backlog

These are intentionally after stable release, unless the user explicitly reprioritizes.

### AI Helper Model Calls

Current state:

- Dormant Core model seam exists.
- Live model calls are not approved.

Next safe phase:

```text
C-PHASE 53: model capability flag + first-call consent UI
```

Scope:

- Add disabled-by-default `pet_model_adapter` setting in Shell.
- Surface `ModelConsentNoticeBuilder` text.
- Show helper/tool allowlist in plain language.
- Keep first call behind explicit approval.
- Use fake adapter in tests.
- Do not call Anthropic/OpenAI/live provider yet.

### Visual / Sprite Work

Current state:

- Broad cleanup complete.
- Color queue empty.
- Optional animation expansion gated.

Next safe phase:

```text
C-PHASE 54: visual-side review follow-up after stable
```

Scope:

- Read visual-side feedback from C-PHASE 47 prompt.
- Only plan targeted fixes from actual user/visual findings.
- No broad mutation, no generation/import, no all-color propagation without proof plan.

### Sprite Workflow V2 / Creative Lab

Current state:

- Workflow and lab concepts exist.
- Release should not wait on them.

Next safe phase:

```text
C-PHASE 55: post-stable tool workbench planning refresh
```

Scope:

- Revisit Sprite Workflow V2 and Creative Learning Lab with RC/stable baseline protected.
- Keep report-only / preview / approval-gated execution model.
- Do not make pet overlay feel like a dev dashboard.

## Gates That Remain Closed

```text
closed unless explicitly opened
|
+-- stable promotion
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

## Immediate Recommendation

Start:

```text
C-PHASE 50 - RC4 Manual Player QA
```

Use RC4 from GitHub:

```text
https://github.com/ssebrobless/wevito/releases/tag/v0.1.0-desktop-rc4
```

After that, choose:

```text
accept_rc4_for_stable
fix_blockers_publish_rc5
hold_for_more_manual_qa
```
