# C-PHASE 33 Desktop RC Draft Release

Date: 2026-05-12

Branch: `claude-implementation/c-phase-33-release-draft-proof`

## Goal

Create and verify a GitHub draft prerelease for the desktop RC bundle produced during C-PHASE 32.

## Release State

```text
Git tag
└── v0.1.0-desktop-rc1
    └── 947b97e64 Merge pull request #35
        └── Draft prerelease: Wevito Desktop RC1
            └── Asset: WevitoDesktopPet-vcphase32-desktop-rc-proof-win64.zip
```

## GitHub Release

- Tag: `v0.1.0-desktop-rc1`
- Name: `Wevito Desktop RC1`
- State: draft
- Prerelease: true
- Draft URL: `https://github.com/ssebrobless/wevito/releases/tag/untagged-3a44ffffe6a2456c2069`

GitHub gives draft releases an `untagged-*` URL until publication. The release metadata still reports `tagName=v0.1.0-desktop-rc1`.

## Uploaded Asset

- Name: `WevitoDesktopPet-vcphase32-desktop-rc-proof-win64.zip`
- Size: `141617616` bytes
- GitHub digest: `sha256:66943278158a13597313d0448e038515f928f182a8ed01e6404dcd9d5ae52979`
- Asset state: `uploaded`
- Download count at verification: `0`

## Verification

Command:

```powershell
gh release view v0.1.0-desktop-rc1 --json tagName,name,isDraft,isPrerelease,url,assets
```

Result:

- `tagName` was `v0.1.0-desktop-rc1`.
- `isDraft` was `true`.
- `isPrerelease` was `true`.
- The desktop RC zip was attached and uploaded.

## Next Decision

The draft can stay private for review, or it can be published after a manual download/install smoke test from the GitHub asset.

Recommended next validation before publishing:

1. Download the draft asset from GitHub.
2. Extract to a clean folder outside the repo.
3. Launch `WevitoDesktopPet.exe` from the extracted folder.
4. Confirm the pet appears and runtime art loads from `assets/`.
5. Publish the draft prerelease only after that clean-folder smoke passes.
