# C-PHASE 52 Stable Release Promotion

Date: 2026-05-12

Branch: `claude-implementation/c-phase-52-stable-release-promotion`

## Decision

```text
stable release promotion
|
+-- user approval label: promote_rc4_to_stable
+-- source candidate: v0.1.0-desktop-rc4
+-- source artifact hash verified: PASS
+-- stable GitHub release created: PASS
+-- stable artifact digest verified: PASS
+-- stable release is prerelease: false
`-- post-stable backlog: remains gated
```

Wevito Desktop v0.1.0 is now the stable desktop release.

Stable release:

```text
tag: v0.1.0-desktop
name: Wevito Desktop v0.1.0
url: https://github.com/ssebrobless/wevito/releases/tag/v0.1.0-desktop
target commit: 97285ad9887423fc42cc3b562087180ff0d8f90e
prerelease: false
```

Why target commit `97285ad9887423fc42cc3b562087180ff0d8f90e`:

- This is the commit targeted by the validated RC4 release.
- The stable asset is byte-identical to the accepted RC4 asset.
- Post-RC4 docs and QA reports live on `main`, but the binary provenance should point at the code commit that produced the promoted package.

## Promoted Artifact

Source RC:

```text
tag: v0.1.0-desktop-rc4
asset: WevitoDesktopPet-v0.1.0-desktop-rc4-win64.zip
sha256: c12a61d0ddd55bfa611bc14480dd298584e2e9d3849ea8e01064beb71c4efacc
```

Stable asset:

```text
asset: WevitoDesktopPet-v0.1.0-desktop-win64.zip
size: 141677058 bytes
sha256: c12a61d0ddd55bfa611bc14480dd298584e2e9d3849ea8e01064beb71c4efacc
digest from GitHub: sha256:c12a61d0ddd55bfa611bc14480dd298584e2e9d3849ea8e01064beb71c4efacc
```

Promotion artifact root:

```text
vnext/artifacts/c-phase-52-stable-release-promotion/
```

Machine-readable summary:

```text
vnext/artifacts/c-phase-52-stable-release-promotion/stable-release-summary.json
```

Release notes used:

```text
vnext/artifacts/c-phase-52-stable-release-promotion/stable-release-notes.md
```

## Evidence Chain

```text
stable v0.1.0
|
+-- promoted from RC4 bytes
|   `-- SHA256 c12a61d0ddd55bfa611bc14480dd298584e2e9d3849ea8e01064beb71c4efacc
|
+-- RC4 package validation
|   `-- docs/C_PHASE49_RC4_PACKAGE_AND_CLEAN_VALIDATION_2026-05-12.md
|
+-- RC4 player-facing QA
|   `-- docs/C_PHASE50_RC4_MANUAL_PLAYER_QA_2026-05-12.md
|
`-- stable release record
    `-- this document
```

RC4 validation covered:

- clean GitHub download and SHA256 verification,
- packaged full/fresh automation,
- forced drink scenario,
- forced fetch scenario,
- forced save-position recovery,
- goose habitat viewport proof,
- vNext build and tests.

C-PHASE 50 found no blocker requiring RC5 before stable promotion.

## Release Notes Summary

The stable release notes state that this is the validated RC4 package promoted to stable and list these included capabilities:

- desktop pet overlay with three-pet play loop,
- feed, drink, play/fetch, groom, bath, medicine/doctor, rest, basket, settings, save/reload, and position recovery flows,
- habitat placement/contact-shadow proof for the goose scenario,
- PET TASKS copy and tool flows remain report-first, preview-first, and approval-gated.

## RC Supersession

The following release candidates are now superseded by stable `v0.1.0-desktop`:

```text
v0.1.0-desktop-rc1
v0.1.0-desktop-rc2
v0.1.0-desktop-rc3
v0.1.0-desktop-rc4
```

RC4 remains preserved as provenance for the promoted binary.

## Gates Still Closed After Stable

```text
closed unless explicitly opened
|
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

## Next Work

The post-RC4 release list is complete through stable promotion.

Recommended next phase:

```text
C-PHASE 53 - Current Project Progress And Capability Gap Report
```

Goal:

- inspect the current code, docs, assets, release state, and tool surfaces,
- estimate completion by project area,
- identify what is missing or intentionally gated,
- create a concrete next-improvement plan.
